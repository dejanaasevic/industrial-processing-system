using System.Diagnostics;

public class ProcessingSystem
{
    // Dictionary of all jobs ever submitted
    private readonly Dictionary<Guid, Job> _allJobs = new();

    // Queue of pending jobs, ordered by priority
    private readonly PriorityQueue<Job, int> _queue = new();

    // HashSet of seen jobs for idempotency check
    private readonly HashSet<Guid> _seenId = new();

    // Dictionary of pending jobs and their completion sources for result tracking
    private readonly Dictionary<Guid, TaskCompletionSource<int>> _pendingJobs = new();

    // Signals worker threads that a new job is available
    private readonly SemaphoreSlim _jobAvailable = new(0);

    private readonly object _lock = new();
    private readonly int _maxQueueSize;
    private readonly JobProcessor _jobProcessor = new();

    public event EventHandler<JobEventArgs>? JobCompleted;
    public event EventHandler<JobEventArgs>? JobFailed;
    public event EventHandler<JobEventArgs>? JobAborted;

    public ProcessingSystem(SystemConfig config)
    {
        _maxQueueSize = config.MaxQueueSize;
        
        for (int i = 0; i < config.WorkerCount; i++)
        {
            Task.Run(() => WorkerLoop());
        }

        foreach (Job job in config.Jobs)
        {
            try
            {
                Submit(job);
                Console.WriteLine($"[INIT] Loaded job from config: {job.Id} | Type: {job.Type} | Priority: {job.Priority}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[INIT] Failed to submit job {job.Id}: {ex.Message}");
            }
        }
    }

    /// Submits a job to the queue
    public JobHandle Submit(Job job)
    {
        lock (_lock)
        {
            if (_seenId.Contains(job.Id))
            {
                throw new InvalidOperationException($"Job {job.Id} already submitted.");
            }

            if (_queue.Count >= _maxQueueSize)
            {
                throw new Exception($"Queue full (max {_maxQueueSize}).");
            }

            _seenId.Add(job.Id);
            _allJobs[job.Id] = job;

            var tcs = new TaskCompletionSource<int>();
            _pendingJobs[job.Id] = tcs;

            _queue.Enqueue(job, job.Priority);
            _jobAvailable.Release(); 

            return new JobHandle(job.Id, tcs.Task);
        }
    }

    /// Worker loop that continuously processes jobs from the queue
    private async Task WorkerLoop()
    {
        while (true)
        {
            await _jobAvailable.WaitAsync();

            Job job;
            TaskCompletionSource<int> tcs;

            lock (_lock)
            {
                if (!_queue.TryDequeue(out job, out _)) continue;
                if (!_pendingJobs.TryGetValue(job.Id, out tcs)) continue;
            }

            await ExecuteJob(job, tcs);
        }
    }

    /// Executes a job with up to 3 attempts (1 original + 2 retries)
    private async Task ExecuteJob(Job job, TaskCompletionSource<int> tcs)
    {
        const int maxAttempts = 3;
        int attempts = 0;

        while (attempts < maxAttempts)
        {
            var stopwatch = Stopwatch.StartNew();
            try
            {
                var result = await _jobProcessor.ExecuteJob(job).WaitAsync(TimeSpan.FromSeconds(2));
                stopwatch.Stop();
                tcs.TrySetResult(result);

                JobCompleted?.Invoke(this, new JobEventArgs(job.Id, result, JobStatus.Complete, job.Type, stopwatch.Elapsed));
                lock (_lock)
                {
                    _pendingJobs.Remove(job.Id);
                }
                return;
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                attempts++;
                if (attempts == maxAttempts)
                {
                    tcs.TrySetException(ex);
                    JobAborted?.Invoke(this, new JobEventArgs(job.Id, -1, JobStatus.Abort, job.Type, stopwatch.Elapsed));
                    lock (_lock)
                    {
                        _pendingJobs.Remove(job.Id);
                    }
                    return;
                }
                JobFailed?.Invoke(this, new JobEventArgs(job.Id, -1, JobStatus.Fail, job.Type, stopwatch.Elapsed));
                
                await Task.Delay(50 * attempts);
            }
        }
    }

    /// Returns the job object for a given ID 
    public Job GetJob(Guid id)
    {
        lock (_lock)
        {
            _allJobs.TryGetValue(id, out Job job);
            return job;
        }
    }

    /// Returns top N jobs currently in queue, ordered by priority
    public IEnumerable<Job> GetTopJobs(int n)
    {
        lock (_lock)
        {
            return _queue.UnorderedItems.OrderBy(x => x.Priority).Take(n).Select(x => x.Element).ToList();
        }
    }
}
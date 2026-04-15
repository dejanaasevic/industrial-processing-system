public class ProcessingSystem
{
    private readonly Dictionary<Guid, Job> _allJobs = new();                                
    private readonly PriorityQueue<Job, int> _queue = new();                                
    private readonly HashSet<Guid> _seenId = new();
    private readonly Dictionary<Guid, TaskCompletionSource<int>> _pendigJobs = new();

    private readonly SemaphoreSlim _jobAvailable = new(0);                                  
    private readonly object _lock = new();

    private readonly int _maxQueueSize;

    private readonly JobProcessor _jobProcessor = new();

    public event EventHandler<JobEventArgs>? JobCompleted;
    public event EventHandler<JobEventArgs>? JobFailed;

    public ProcessingSystem(SystemConfig config)
    {
        _maxQueueSize = config.MaxQueueSize;

        for(int i = 0; i < config.WorkerCount; i++)
        {
            Task.Run(() => WorkerLoop());
        }

        foreach(Job job in config.Jobs)
        {
            Submit(job);
        }
    }

    public JobHandle Submit(Job job)
    {
        lock (_lock)
        {

            if (_queue.Count >= _maxQueueSize || _seenId.Contains(job.Id))
            {
                return null;
            }

            _seenId.Add(job.Id);
            _allJobs[job.Id] = job;

            var tcs = new TaskCompletionSource<int>();
            _pendigJobs[job.Id] = tcs; 

            _queue.Enqueue(job, job.Priority);
            _jobAvailable.Release();

            return new JobHandle(job.Id, tcs.Task);
        }
    }

    private async Task WorkerLoop(){
        while (true)
        {
            await _jobAvailable.WaitAsync();

            Job job;
            TaskCompletionSource<int> tcs;

            lock (_lock)
            {
                if (!_queue.TryDequeue(out job, out _)) continue;
                tcs = _pendigJobs[job.Id];
            }
            await ExecuteJob(job, tcs);

        }
    }

    private async Task ExecuteJob(Job job, TaskCompletionSource<int> tcs)
    {
        int attempts = 0;
        while (true)
        {
            try
            {
                var result = await _jobProcessor.ExecuteJob(job).WaitAsync(TimeSpan.FromSeconds(2));
                tcs.SetResult(result);
                JobCompleted?.Invoke(this, new JobEventArgs(job.Id, result));
                return;
            }

            catch(Exception ex)
            {
                attempts++;
                if(attempts >= 3){
                    tcs.SetException(ex);
                    JobFailed?.Invoke(this, new JobEventArgs(job.Id, -1));
                    return;
                }
            }
        }
    }

    public Job GetJob(Guid id)
    {
        lock (_lock)
        {
            Job job;
            _allJobs.TryGetValue(id, out job);
            return job;
        }
    }

    public IEnumerable<Job> GetTopJobs(int n, int _)
    {
        lock (_lock)
        {
            return _queue.UnorderedItems.OrderBy(x => x.Priority).Take(n).Select(x => x.Element).ToList();
        }
    }

}
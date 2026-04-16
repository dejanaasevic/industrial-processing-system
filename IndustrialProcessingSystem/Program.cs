using System;
class Program
{
    static async Task Main()
    {
        Console.WriteLine("[SYSTEM] Loading configuration...");
        SystemConfig config = ConfigLoader.Load("SystemConfig.xml");
        Console.WriteLine($"[SYSTEM] Config loaded: {config.WorkerCount} workers, max queue size {config.MaxQueueSize}");

        ProcessingSystem processingSystem = new ProcessingSystem(config);
        EventLogger logger = new EventLogger("log.txt");
        ReportGenerator reportGenerator = new ReportGenerator("reports");

        processingSystem.JobCompleted += (sender, e) =>
        {
            Console.WriteLine($"[COMPLETED] Job {e.Id} | Type: {e.Type} | Result: {e.Result} | Duration: {e.Duration.TotalMilliseconds:F0}ms");
            _ = logger.LogEvent(e.Id, e.Status, e.Result);
            reportGenerator.RecordJob(e.Type, true, e.Duration);
        };

        processingSystem.JobFailed += (sender, e) =>
        {
            Console.WriteLine($"[FAILED]    Job {e.Id} | Type: {e.Type} | Duration: {e.Duration.TotalMilliseconds:F0}ms (retrying...)");
            _ = logger.LogEvent(e.Id, e.Status, e.Result);
            reportGenerator.RecordJob(e.Type, false, e.Duration);
        };

        processingSystem.JobAborted += (sender, e) =>
        {
            Console.WriteLine($"[ABORTED]   Job {e.Id} | Type: {e.Type} | Gave up after 3 attempts");
            _ = logger.LogEvent(e.Id, e.Status, e.Result);
            reportGenerator.RecordJob(e.Type, false, e.Duration);
        };

        Console.WriteLine($"[SYSTEM] Starting {config.WorkerCount} producer threads...");
        List<Task> producerTasks = new List<Task>();

        for (int i = 0; i < config.WorkerCount; i++)
        {
            int producerId = i + 1; 
            producerTasks.Add(Task.Run(async () =>
            {
                Random random = Random.Shared;
                while (true)
                {
                    try
                    {
                        Job job = GenerateRandomJob(random);
                        JobHandle handle = processingSystem.Submit(job);
                        Console.WriteLine($"[SUBMITTED] Producer #{producerId} submitted Job {job.Id} | Type: {job.Type} | Priority: {job.Priority}");
                    }
                    catch (InvalidOperationException ex)
                    {
                        Console.WriteLine($"[WARN] Producer #{producerId}: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[WARN] Producer #{producerId} submit failed: {ex.Message}");
                    }

                    await Task.Delay(random.Next(5000, 10000));
                }
            }));
        }

        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(15));
                PrintQueueStatus(processingSystem);
            }
        });

        Console.WriteLine("[SYSTEM] System running. Press Ctrl+C to stop.");
        await Task.WhenAll(producerTasks);
    }

    /// Demonstrates GetTopJobs and GetJob — prints top 3 pending jobs from queue.
    private static void PrintQueueStatus(ProcessingSystem processingSystem)
    {
        Console.WriteLine("\n========== QUEUE STATUS ==========");
        var topJobs = processingSystem.GetTopJobs(3).ToList();

        if (!topJobs.Any())
        {
            Console.WriteLine("[STATUS] Queue is currently empty.");
        }
        else
        {
            Console.WriteLine($"[STATUS] Top {topJobs.Count} jobs in queue (by priority):");
            foreach (var job in topJobs)
            {
                Job fetched = processingSystem.GetJob(job.Id);
                if (fetched != null)
                {
                    Console.WriteLine($"  -> Id: {fetched.Id} | Type: {fetched.Type} | Priority: {fetched.Priority} | Payload: {fetched.Payload}");
                }
            }
        }
        Console.WriteLine("===================================\n");
    }

    // Generates a random job for both Prime and IO type with random parameters
    private static Job GenerateRandomJob(Random random)
    {
        JobType type = (JobType)random.Next(0, 2);
        string payload;

        if (type == JobType.Prime)
        {
            int numbers = random.Next(1, 10);
            int threads = random.Next(1, 10);
            payload = $"numbers:{numbers}_000,threads:{threads}";
        }
        else
        {
            int delay = random.Next(1, 4);
            payload = $"delay:{delay}_00"; 
        }

        int priority = random.Next(1, 6);
        return new Job(type, payload, priority);
    }
}
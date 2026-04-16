using System;
class Program
{
    private static readonly object _consoleLock = new();
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
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"[COMPLETED] Job {e.Id} | Type: {e.Type} | Result: {e.Result} | Duration: {e.Duration.TotalMilliseconds:F0}ms");
                Console.ResetColor();
            }
            reportGenerator.RecordJob(e.Type, true, e.Duration);
        };

        processingSystem.JobFailed += (sender, e) =>
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.Magenta;
                Console.WriteLine($"[FAILED] Job {e.Id}");
                Console.ResetColor();
            }
            _ = logger.LogEvent(e.Id, e.Status, e.Result);
            reportGenerator.RecordJob(e.Type, false, e.Duration);
        };

        processingSystem.JobAborted += (sender, e) =>
        {
            lock (_consoleLock)
            {
                Console.ForegroundColor = ConsoleColor.DarkMagenta;
                Console.WriteLine($"[ABORTED] Job {e.Id} | Type: {e.Type} | Gave up after 3 attempts");
                Console.ResetColor();
            }
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
                    for (int j = 0; j < random.Next(3, 8); j++)
                    {
                        try
                        {
                            Job job = GenerateRandomJob(random);
                            JobHandle handle = processingSystem.Submit(job);
                            lock (_consoleLock)
                            {
                                Console.ForegroundColor = ConsoleColor.White;
                                Console.WriteLine($"[SUBMITTED] Producer #{producerId} submitted Job {job.Id} | Type: {job.Type} | Priority: {job.Priority}");
                                Console.ResetColor();
                            }
                        }
                        catch (InvalidOperationException ex)
                        {
                            lock (_consoleLock)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"[WARN] Producer #{producerId}: {ex.Message}");
                                Console.ResetColor();
                            }
                        }
                        catch (Exception ex)
                        {
                            lock (_consoleLock)
                            {
                                Console.ForegroundColor = ConsoleColor.DarkYellow;
                                Console.WriteLine($"[WARN] Producer #{producerId} submit failed: {ex.Message}");
                                Console.ResetColor();
                            }
                        }

                    }
                    await Task.Delay(random.Next(500, 1500));
                }
            }));
        }

        _ = Task.Run(async () =>
        {
            while (true)
            {
                await Task.Delay(TimeSpan.FromSeconds(3));
                PrintQueueStatus(processingSystem);
            }
        });

        Console.WriteLine("[SYSTEM] System running. Press Ctrl+C to stop.");
        await Task.WhenAll(producerTasks);
    }

    /// Demonstrates GetTopJobs and GetJob — prints top 3 pending jobs from queue.
    private static void PrintQueueStatus(ProcessingSystem processingSystem)
    {
        lock (_consoleLock)
        {
            Console.WriteLine("\n========== QUEUE STATUS ==========");

            var topJobs = processingSystem.GetTopJobs(3).ToList();

            if (!topJobs.Any())
            {
                Console.WriteLine("[STATUS] Queue is currently empty.");
            }
            else
            {
                Console.WriteLine($"[STATUS] Top {topJobs.Count} jobs in queue:");
                foreach (var job in topJobs)
                {
                    var fetched = processingSystem.GetJob(job.Id);
                    Console.WriteLine($"  -> Id: {fetched.Id} | Type: {fetched.Type} | Priority: {fetched.Priority}");
                }
            }
            Console.WriteLine("===================================\n");
        }
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
            payload = $"delay:{delay}_000";
        }

        int priority = random.Next(1, 6);
        return new Job(type, payload, priority);
    }
}
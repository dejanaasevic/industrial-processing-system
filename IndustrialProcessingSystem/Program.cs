using System;
class Program
{
    static async Task Main()
    {
        SystemConfig config = ConfigLoader.Load("SystemConfig.xml");
        ProcessingSystem processingSystem = new ProcessingSystem(config);
        EventLogger logger = new EventLogger("log.txt");
        ReportGenerator reportGenerator = new ReportGenerator("reports");

        processingSystem.JobCompleted += async (sender, e) =>
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobCompleted += (sender, e) =>
            reportGenerator.RecordJob(e.Type, true, e.Duration);

        processingSystem.JobFailed += async (sender, e) =>
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobFailed += (sender, e) =>
            reportGenerator.RecordJob(e.Type, false, e.Duration);

        processingSystem.JobAborted += async (sender, e) =>
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobAborted += (sender, e) =>
            reportGenerator.RecordJob(e.Type, false, e.Duration);

        int workerCount = config.WorkerCount;
        List<Task> producerTasks = new List<Task>();
        for(int i = 0; i < workerCount; i++)
        {
            producerTasks.Add(Task.Run( async () =>
            {
                Random random = Random.Shared;
                while (true)
                {
                    try
                    {
                        Job job = GenerateRandomJob(random);
                        processingSystem.Submit(job);
                    }
                    catch(Exception ex){
                        Console.WriteLine($"Submit failed: {ex.Message}");
                    }
                    await Task.Delay(random.Next(5000, 10000));
                }

            }));
        }
        await Task.WhenAll(producerTasks);
    }

    private static Job GenerateRandomJob(Random random)
    {
        JobType type = (JobType)random.Next(0, 2);
        string payload = String.Empty;
        if(type == JobType.Prime)
        {
            int numbers = random.Next(1, 10);
            int threads = random.Next(1, 10);
            payload = $"numbers:{numbers}_000,threads:{threads}";
        }
        else
        {
            int delay = random.Next(1, 20);
            payload = $"delay:{delay}_000";
        }

        int priority = random.Next(1, 6);
        return new Job(type, payload, priority);
    }
}
using System;
class Program
{
    static async Task Main()
    {
        SystemConfig config = ConfigLoader.Load("SystemConfig.xml");
        Console.WriteLine($"Workers: {config.WorkerCount}");
        Console.WriteLine($"MaxQueue: {config.MaxQueueSize}");

        JobProcessor processor = new JobProcessor();
        
        foreach (var job in config.Jobs)
        {
            Console.WriteLine($"{job.Type} | {job.Payload} | {job.Priority}");
            var result = await processor.ExecuteJob(job);
            Console.WriteLine(result);
        }
    }
}
using System;
class Program
{
    static void Main()
    {
        SystemConfig config = ConfigLoader.Load("SystemConfig.xml");
        Console.WriteLine($"Workers: {config.WorkerCount}");
        Console.WriteLine($"MaxQueue: {config.MaxQueueSize}");

        foreach (var job in config.Jobs)
        {
            Console.WriteLine($"{job.Type} | {job.Payload} | {job.Priority}");
        }
    }
}
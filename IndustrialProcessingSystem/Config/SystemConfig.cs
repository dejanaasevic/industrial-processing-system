public class SystemConfig
{
    public int WorkerCount { get; set; }
    public int MaxQueueSize { get; set; }
    public List<Job> Jobs { get; set; } = new List<Job>();

    public SystemConfig(int workerCount, int maxQueueSize, List<Job> jobs)
    {
        WorkerCount = workerCount;
        MaxQueueSize = maxQueueSize;
        Jobs = jobs;
    }
}
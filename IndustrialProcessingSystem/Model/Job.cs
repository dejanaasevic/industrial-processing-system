public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public JobType Type { get; set; }
    public string Payload { get; set; }
    public int Priority { get; set; }

    public Job(JobType type, string payload, int priority)
    {
        Type = type;
        Payload = payload;
        Priority = priority;
    }
}
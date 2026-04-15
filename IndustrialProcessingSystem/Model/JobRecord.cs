public class JobRecord
{
    public JobType Type { get; set; }
    public bool Success { get; set; }
    public TimeSpan Duration { get; set; }

    public JobRecord(JobType type, bool success, TimeSpan duration)
    {
        Type = type;
        Success = success;
        Duration = duration;
    }
}


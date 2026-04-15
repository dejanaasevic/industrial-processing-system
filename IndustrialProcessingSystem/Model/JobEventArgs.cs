
public class JobEventArgs : EventArgs
{
    public Guid Id { get; set; }
    public int Result { get; set; }

    public JobStatus Status { get; set; }
    public JobType Type { get; }
    public TimeSpan Duration { get; }

    public JobEventArgs(Guid id, int result, JobStatus status, JobType type, TimeSpan duration)
    {
        Id = id;
        Result = result;
        Status = status;
        Type = type;
        Duration = duration;
    }
}
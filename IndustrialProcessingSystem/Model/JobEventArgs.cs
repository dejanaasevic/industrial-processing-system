
public class JobEventArgs : EventArgs
{
    public Guid Id { get; set; }
    public int Result { get; set; }

    public JobStatus Status { get; set; }

    public JobEventArgs(Guid id, JobStatus status, int result)
    {
        Id = id;
        Status = Status;
        Result = result;
    }
}

public class JobEventArgs
{
    public Guid id { get; set; }
    public int result { get; set; }

    public JobEventArgs(Guid id, int result)
    {
        this.id = id;
        this.result = result;
    }
}
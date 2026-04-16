public class EventLogger
{
    private readonly string filePath;
    private readonly SemaphoreSlim _write = new(1, 1);

    public EventLogger(string filePath)
    {
        this.filePath = filePath;
    }

    public async Task LogEvent(Guid id, JobStatus status, int result)
    {
        string line;
        if (status == JobStatus.Fail || status == JobStatus.Abort)
        {
            line = $"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] [{status}] {id}, null\n";
        }
        else
        {
            line = $"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] [{status}] {id}, {result}\n";
        }

        await _write.WaitAsync();

        try
        {
            await File.AppendAllTextAsync(filePath, line);
        }
        finally
        {
            _write.Release();
        }
    }
}
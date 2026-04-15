public class EventLogger
{
    public readonly string filePath;
    public readonly SemaphoreSlim _write = new(1, 1);

    public EventLogger(string filePath)
    {
        this.filePath = filePath;
    }

    public async Task LogEvent(Guid id, JobStatus status, int result)
    {
        string line;
        if (status == JobStatus.Failed || status == JobStatus.Aborted)
        {
            line = $"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] [{status}] {id}, null\n";
        }
        line = $"[{DateTime.Now:dd-MM-yyyy HH:mm:ss}] [{status}] {id}, {result}\n";

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
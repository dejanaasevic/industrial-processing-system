using System;
class Program
{
    static async Task Main()
    {
        SystemConfig config = ConfigLoader.Load("SystemConfig.xml");
        ProcessingSystem processingSystem = new ProcessingSystem(config);
        EventLogger logger = new EventLogger("log.txt");
        ReportGenerator reportGenerator = new ReportGenerator("reports");

        processingSystem.JobCompleted += async (sender, e) =>
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobCompleted += (sender, e) =>
            reportGenerator.RecordJob(e.Type, true, e.Duration);

        processingSystem.JobFailed += async (sender, e) =>
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobFailed += (sender, e) =>
            reportGenerator.RecordJob(e.Type, false, e.Duration);

        processingSystem.JobAborted += async (sender, e) =>
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobAborted += (sender, e) =>
            reportGenerator.RecordJob(e.Type, false, e.Duration);
    }
}
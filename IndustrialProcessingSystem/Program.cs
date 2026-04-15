using System;
class Program
{
    static async Task Main()
    {
        SystemConfig config = ConfigLoader.Load("SystemConfig.xml");
        ProcessingSystem processingSystem = new ProcessingSystem(config);
        EventLogger logger = new EventLogger("log.txt");

        processingSystem.JobCompleted += async (sender, e) => 
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobFailed += async (sender, e) =>
            await logger.LogEvent(e.Id, e.Status, e.Result);

        processingSystem.JobAborted += async (sender, e) =>
             await logger.LogEvent(e.Id, e.Status, e.Result);
    }
}
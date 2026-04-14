using System.Xml.Linq;

public class ConfigLoader
{
    public static SystemConfig Load(string filePath)
    {
        var xml = XElement.Load(filePath);

        int workerCount = (int)xml.Element("WorkerCount");
        int maxQueueSize = (int)xml.Element("MaxQueueSize");

        var jobsXml = xml.Element("Jobs").Elements("Job");

        List<Job> jobs = new List<Job>();

        foreach (var jobElement in jobsXml)
        {
            JobType type = Enum.Parse<JobType>(jobElement.Attribute("Type").Value);
            string payload = jobElement.Attribute("Payload").Value;
            int priority = int.Parse(jobElement.Attribute("Priority").Value);

            jobs.Add(new Job(type, payload, priority));
        }

        return new SystemConfig(workerCount, maxQueueSize, jobs);
    }
}
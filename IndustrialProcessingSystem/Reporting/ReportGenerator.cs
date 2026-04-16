using System.Xml.Linq;

public class ReportGenerator
{
    private readonly string _reportDirectory;
    private int _fileIndex = 0;
    private readonly List<JobRecord> _jobRecords = new();
    private readonly object _lock = new();
    public  ReportGenerator(String reportDirectory)
    {
        _reportDirectory = reportDirectory;
        Directory.CreateDirectory(_reportDirectory);
        _ = Task.Run(() => GenerateReportLoop());
    }

    public void RecordJob(JobType type, bool success, TimeSpan duration)
    {
        lock (_lock)
        {
            _jobRecords.Add(new JobRecord(type, success, duration));
        }
    }

    private async Task GenerateReportLoop()
    {
        while (true)
        {
            await Task.Delay(TimeSpan.FromMinutes(1));
            GenerateReport();
        }
    }

    private void GenerateReport()
    {
        List<JobRecord> records;
        lock (_lock)
        {
            records = _jobRecords.ToList();
            _jobRecords.Clear();
        }

        var completed = records.Where(r => r.Success == true);
        var unsuccessful = records.Where(r => r.Success == false);

        var countByType = completed.GroupBy(r => r.Type).Select(g => (Type: g.Key, Count: g.Count()));
        var averageDurationByType = completed.GroupBy(r => r.Type).Select(g => (Type: g.Key, AvgMs: g.Average(r => r.Duration.TotalMilliseconds)));
        var failedByType = unsuccessful.GroupBy(r => r.Type).OrderBy(g => g.Key).Select(g => (Type: g.Key, Count: g.Count()));

        var xml = GenerateXml(countByType, averageDurationByType, failedByType);

        string fileName = Path.Combine(_reportDirectory, $"report_{(_fileIndex % 10) + 1}.xml");
        xml.Save(fileName);
        
        _fileIndex++;
    }

    private XElement GenerateXml(IEnumerable<(JobType Type, int Count)> countByType, IEnumerable<(JobType Type, double AvgMs)> averageDurationByType, IEnumerable<(JobType Type, int Count)> failedByType)
    {
        return new XElement("Report",
            new XAttribute("GeneratedAt", DateTime.Now),

            new XElement("CompletedByType",
                countByType.Select(x =>
                    new XElement("Entry",
                        new XAttribute("Type", x.Type),
                        new XAttribute("Count", x.Count)))),

            new XElement("AvgDurationByType",
                averageDurationByType.Select(x =>
                    new XElement("Entry",
                        new XAttribute("Type", x.Type),
                        new XAttribute("AvgMs", x.AvgMs)))),

            new XElement("FailedByType",
                failedByType.Select(x =>
                    new XElement("Entry",
                        new XAttribute("Type", x.Type),
                        new XAttribute("Count", x.Count))))
        );
    }
}

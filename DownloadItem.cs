namespace XiPHiAS.MediaFetch;

public class DownloadItem
{
    public required string Url { get; init; }

    public required string FileName { get; set; }

    public required string DestinationPath { get; set; }

    public bool OverwriteExisting { get; set; }

    public long BytesDownloaded { get; set; }

    public long? TotalBytes { get; set; }

    public double BytesPerSecond { get; set; }

    public TimeSpan? EstimatedTimeRemaining { get; set; }

    public bool ExistingFileIsComplete { get; set; }

    public string Status { get; set; } = "Waiting";

    public int Progress
    {
        get
        {
            if (TotalBytes is null || TotalBytes <= 0)
                return 0;

            return (int)Math.Min(
                100,
                BytesDownloaded * 100 / TotalBytes.Value
            );
        }
    }
}

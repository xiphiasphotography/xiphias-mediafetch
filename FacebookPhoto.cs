namespace XiPHiAS.MediaFetch;

internal sealed class FacebookPhoto
{
    public required string PhotoUrl { get; init; }
    public required string ResolvedUrl { get; init; }
    public required string OriginalFileName { get; init; }
    public int AlbumIndex { get; init; }
    public int Width { get; init; }
    public int Height { get; init; }
}

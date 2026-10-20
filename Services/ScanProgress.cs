namespace FileCompareTool.Services;

public class ScanProgress
{
    public int TotalPaths { get; set; }
    public int ProcessedPaths { get; set; }
    public int FilesFound { get; set; }
    public string CurrentPath { get; set; } = string.Empty;
    public bool IsCancelled { get; set; }
}

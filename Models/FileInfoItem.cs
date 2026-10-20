namespace FileCompareTool.Models;

public class FileInfoItem
{
    public string FullPath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public string Extension { get; set; } = string.Empty;
    public string DirectoryName { get; set; } = string.Empty;

    public string ExtensionLabel => string.IsNullOrWhiteSpace(Extension)
        ? "FILE"
        : Extension.TrimStart('.').ToUpperInvariant();
}

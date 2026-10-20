using CommunityToolkit.Mvvm.ComponentModel;
using FileCompareTool.Services;

namespace FileCompareTool.Models;

public partial class AddedPath : ObservableObject
{
    [ObservableProperty]
    private string fullPath = string.Empty;

    [ObservableProperty]
    private bool isDirectory;

    [ObservableProperty]
    private long fileCount;

    [ObservableProperty]
    private bool isCounting;

    public string DisplayText => $"{(IsDirectory ? "📁" : "📄")} {FullPath}";

    public string IconGlyph => IsDirectory ? "\uE8B7" : "\uE7C3";

    public string SubText => !IsDirectory
        ? string.Empty
        : IsCounting
            ? LocalizationService.T("Path_Counting")
            : FileCount < 0
                ? LocalizationService.T("Path_CountUnavailable")
                : LocalizationService.Format("Path_FilesCountFormat", FileCount);

    public void RefreshLocalizedText()
    {
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(SubText));
    }

    partial void OnFullPathChanged(string value)
    {
        OnPropertyChanged(nameof(DisplayText));
    }

    partial void OnIsDirectoryChanged(bool value)
    {
        OnPropertyChanged(nameof(DisplayText));
        OnPropertyChanged(nameof(SubText));
        OnPropertyChanged(nameof(IconGlyph));
    }

    partial void OnFileCountChanged(long value)
    {
        OnPropertyChanged(nameof(SubText));
    }

    partial void OnIsCountingChanged(bool value)
    {
        OnPropertyChanged(nameof(SubText));
    }
}

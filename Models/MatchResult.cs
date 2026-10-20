using System.Collections.Generic;
using CommunityToolkit.Mvvm.ComponentModel;

namespace FileCompareTool.Models;

public partial class MatchResult : ObservableObject
{
    [ObservableProperty]
    private string fileName = string.Empty;

    [ObservableProperty]
    private List<FileInfoItem> aFiles = new();

    [ObservableProperty]
    private List<FileInfoItem> bFiles = new();

    [ObservableProperty]
    private bool isSelected = true;

    public int ACount => AFiles.Count;
    public int BCount => BFiles.Count;

    public string ExtensionLabel
    {
        get
        {
            var extension = System.IO.Path.GetExtension(FileName);
            return string.IsNullOrEmpty(extension)
                ? "FILE"
                : extension.TrimStart('.').ToUpperInvariant();
        }
    }

    // 预计算显示文本，避免绑定问题
    public string FileNameDisplay => FileName;
    public string ACountDisplay => ACount.ToString();
    public string BCountDisplay => BCount.ToString();

    partial void OnFileNameChanged(string value)
    {
        OnPropertyChanged(nameof(FileNameDisplay));
        OnPropertyChanged(nameof(ExtensionLabel));
    }

    partial void OnAFilesChanged(List<FileInfoItem> value)
    {
        OnPropertyChanged(nameof(ACount));
        OnPropertyChanged(nameof(ACountDisplay));
    }

    partial void OnBFilesChanged(List<FileInfoItem> value)
    {
        OnPropertyChanged(nameof(BCount));
        OnPropertyChanged(nameof(BCountDisplay));
    }
}

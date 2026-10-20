using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FileCompareTool.Models;

namespace FileCompareTool.Tests;

internal static class ModelTests
{
    public static Task AddedPathSubTextReflectsState()
    {
        var file = new AddedPath { FullPath = @"C:\A\a.txt", IsDirectory = false, FileCount = 1 };
        Assert.Equal(string.Empty, file.SubText, "文件条目不显示文件数");

        var folder = new AddedPath { FullPath = @"C:\A", IsDirectory = true, IsCounting = true };
        Assert.Equal("正在统计…", folder.SubText, "统计中应显示提示");

        folder.IsCounting = false;
        folder.FileCount = 12;
        Assert.True(folder.SubText.Contains("12"), "统计完成后应显示文件数");

        folder.FileCount = -1;
        Assert.True(folder.SubText.Contains("无法"), "统计失败时应给出提示");

        return Task.CompletedTask;
    }

    public static Task MatchResultRaisesCountNotifications()
    {
        var match = new MatchResult
        {
            BFiles = new List<FileInfoItem> { new(), new() }
        };

        var changed = new List<string?>();
        match.PropertyChanged += (_, e) => changed.Add(e.PropertyName);

        match.BFiles = new List<FileInfoItem> { new() };

        Assert.True(changed.Contains(nameof(MatchResult.BCount)), "BFiles 变化时应通知 BCount");
        Assert.Equal(1, match.BCount, "BCount 应立即反映新列表");

        changed.Clear();
        match.AFiles = new List<FileInfoItem> { new(), new(), new() };
        Assert.True(changed.Contains(nameof(MatchResult.ACount)), "AFiles 变化时应通知 ACount");
        Assert.Equal(3, match.ACount, "ACount 应立即反映新列表");

        match.FileName = "archive.tar.gz";
        Assert.Equal("GZ", match.ExtensionLabel, "扩展名标签应取最后一个扩展名并转为大写");
        Assert.True(changed.Contains(nameof(MatchResult.ExtensionLabel)), "文件名变化时应通知扩展名标签");

        var path = new AddedPath { IsDirectory = true };
        Assert.Equal("\uE8B7", path.IconGlyph, "文件夹应使用文件夹图标");
        path.IsDirectory = false;
        Assert.Equal("\uE7C3", path.IconGlyph, "文件应使用页面图标");

        return Task.CompletedTask;
    }
}
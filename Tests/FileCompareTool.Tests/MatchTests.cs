using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using FileCompareTool.Models;
using FileCompareTool.Services;

namespace FileCompareTool.Tests;

internal static class MatchTests
{
    public static Task MatchIsCaseInsensitiveAndKeepsDuplicateCounts()
    {
        var aFiles = new List<FileInfoItem>
        {
            File(@"C:\A\Photo.JPG"),
            File(@"C:\A\one\dup.jpg"),
            File(@"C:\A\two\dup.jpg")
        };

        var bFiles = new List<FileInfoItem>
        {
            File(@"C:\B\photo.jpg"),
            File(@"C:\B\one\dup.jpg"),
            File(@"C:\B\two\dup.jpg"),
            File(@"C:\B\three\dup.jpg"),
            File(@"C:\B\nomatch.jpg")
        };

        var matches = FileMatchService.FindMatches(aFiles, bFiles);

        Assert.Equal(2, matches.Count, "应匹配 2 个文件名");
        Assert.Equal("dup.jpg", matches[0].FileName, "结果应按文件名排序");
        Assert.Equal(2, matches[0].ACount, "A 中应有 2 个 dup.jpg");
        Assert.Equal(3, matches[0].BCount, "B 中应有 3 个 dup.jpg");
        Assert.Equal("Photo.JPG", matches[1].FileName, "文件名匹配应不区分大小写");
        Assert.True(matches.All(m => m.IsSelected), "新匹配项默认应被选中");

        return Task.CompletedTask;
    }

    public static Task MatchReturnsEmptyWhenNoNamesOverlap()
    {
        var aFiles = new[] { File(@"C:\A\only-a.txt") };
        var bFiles = new[] { File(@"C:\B\only-b.txt") };

        var matches = FileMatchService.FindMatches(aFiles, bFiles);
        Assert.Equal(0, matches.Count, "没有同名文件时不应产生匹配");

        return Task.CompletedTask;
    }

    private static FileInfoItem File(string path)
    {
        return new FileInfoItem
        {
            FullPath = path,
            FileName = Path.GetFileName(path),
            Extension = Path.GetExtension(path),
            DirectoryName = Path.GetDirectoryName(path) ?? string.Empty
        };
    }
}

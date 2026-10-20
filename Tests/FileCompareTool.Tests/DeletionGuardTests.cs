using System;
using System.IO;
using System.Threading.Tasks;
using FileCompareTool.Services;

namespace FileCompareTool.Tests;

internal static class DeletionGuardTests
{
    public static Task GuardOnlyAllowsScannedBFiles()
    {
        using var temp = new TempDirectory();
        var aRoot = temp.CreateDirectory("A");
        var bRoot = temp.CreateDirectory("B");
        var aFile = temp.CreateFile("A/reference.txt");
        var bFile = temp.CreateFile("B/duplicate.txt");
        var otherBFile = temp.CreateFile("B/not-scanned.txt");

        var guard = DeletionGuardService.Create(
            new[] { aRoot },
            new[] { bRoot },
            new[] { bFile });

        Assert.True(guard(bFile), "扫描记录中的 B 文件应允许删除");
        Assert.False(guard(aFile), "A 文件绝不允许删除");
        Assert.False(guard(otherBFile), "未在扫描快照中的 B 文件不允许删除");
        Assert.False(guard(Path.Combine(bRoot, "missing.txt")), "不存在的文件应拒绝");
        Assert.False(guard(@"C:\Windows\notepad.exe"), "B 路径之外的文件应拒绝");

        return Task.CompletedTask;
    }

    public static Task GuardRejectsPathTraversalAndAOverlap()
    {
        using var temp = new TempDirectory();
        var aRoot = temp.CreateDirectory("A");
        var bRoot = temp.CreateDirectory("B");
        var nestedInA = temp.CreateDirectory("A/nested");
        var bFile = temp.CreateFile("B/duplicate.txt");
        var aFile = temp.CreateFile("A/reference.txt");

        // 即使 B 根目录与 A 有嵌套，也不允许删除 A 中的文件
        var guard = DeletionGuardService.Create(
            new[] { aRoot },
            new[] { bRoot, nestedInA },
            new[] { bFile, aFile });

        var traversalPath = Path.Combine(bRoot, "..", "A", "reference.txt");
        Assert.False(guard(traversalPath), ".. 路径穿越到 A 后应拒绝");
        Assert.False(guard(aFile), "A 目录内的文件应拒绝");
        Assert.True(guard(bFile), "合法 B 文件仍应允许删除");

        return Task.CompletedTask;
    }

    public static Task GuardWithEmptySnapshotRejectsEverything()
    {
        using var temp = new TempDirectory();
        var bFile = temp.CreateFile("B/duplicate.txt");
        var bRoot = temp.CreateDirectory("B");

        var guard = DeletionGuardService.Create(Array.Empty<string>(), new[] { bRoot }, Array.Empty<string>());
        Assert.False(guard(bFile), "没有扫描快照时必须拒绝所有删除");

        return Task.CompletedTask;
    }
}

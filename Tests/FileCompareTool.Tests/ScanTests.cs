using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FileCompareTool.Services;

namespace FileCompareTool.Tests;

internal static class ScanTests
{
    public static async Task ScanFindsFilesRecursivelyAndDedupes()
    {
        using var temp = new TempDirectory();
        var directFile = temp.CreateFile("a.txt");
        temp.CreateFile("b.jpg");
        temp.CreateFile("sub/deep/c.txt");

        var progress = new InlineProgress<ScanProgress>();
        var files = await FileScanService.ScanPathsAsync(
            new[] { temp.Root, temp.Root, directFile }, progress);

        Assert.Equal(3, files.Count, "应只返回 3 个不重复的文件");
        Assert.Equal(
            3,
            files.Select(f => f.FullPath).Distinct(StringComparer.OrdinalIgnoreCase).Count(),
            "文件路径应去重");
        Assert.True(files.All(f => File.Exists(f.FullPath)), "所有结果都应该真实存在");
        Assert.True(progress.Values.Count >= 2, "应至少报告一次扫描进度");
    }

    public static async Task ScanIgnoresMissingAndEmptyPaths()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("only.txt");

        var files = await FileScanService.ScanPathsAsync(
            new[] { Path.Combine(temp.Root, "missing"), "", temp.Root });

        Assert.Equal(1, files.Count, "不存在的路径应被跳过");
    }

    public static async Task ScanHonorsCancellation()
    {
        using var temp = new TempDirectory();
        temp.CreateFile("a.txt");

        using var cts = new CancellationTokenSource();
        cts.Cancel();

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => FileScanService.ScanPathsAsync(new[] { temp.Root }, null, cts.Token),
            "预先取消时应抛出 OperationCanceledException");
    }

    public static async Task ScanDoesNotFollowJunctionLoop()
    {
        using var temp = new TempDirectory();
        var data = temp.CreateDirectory("data");
        temp.CreateFile("data/only.txt");

        var junction = Path.Combine(data, "loop");
        var startInfo = new ProcessStartInfo
        {
            FileName = "cmd.exe",
            Arguments = $"/c mklink /J \"{junction}\" \"{temp.Root}\"",
            UseShellExecute = false,
            CreateNoWindow = true,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using var process = Process.Start(startInfo);
        if (process == null)
        {
            Console.WriteLine("  SKIP：无法启动 cmd.exe，跳过目录联接测试");
            return;
        }

        await process.WaitForExitAsync();
        if (process.ExitCode != 0 || !Directory.Exists(junction))
        {
            Console.WriteLine("  SKIP：当前环境无法创建目录联接，跳过该测试");
            return;
        }

        try
        {
            var scanTask = FileScanService.ScanPathsAsync(new[] { temp.Root });
            var completed = await Task.WhenAny(scanTask, Task.Delay(TimeSpan.FromSeconds(5)));
            Assert.True(ReferenceEquals(completed, scanTask), "扫描未在 5 秒内结束，可能存在联接循环");

            var files = await scanTask;
            Assert.Equal(1, files.Count, "目录联接不应被重复扫描");
        }
        finally
        {
            try
            {
                Directory.Delete(junction);
            }
            catch
            {
                // 忽略清理失败
            }
        }
    }
}

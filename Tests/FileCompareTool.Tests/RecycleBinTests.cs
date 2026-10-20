using System;
using System.IO;
using System.Threading.Tasks;
using FileCompareTool.Services;

namespace FileCompareTool.Tests;

internal static class RecycleBinTests
{
    public static async Task RejectsWhenValidatorSaysNo()
    {
        using var temp = new TempDirectory();
        var file = temp.CreateFile("keep.txt");

        var result = await RecycleBinService.DeleteFilesAsync(new[] { file }, _ => false);

        Assert.Equal(1, result.FailCount, "验证失败时应有 1 个失败");
        Assert.Equal(0, result.SuccessCount, "验证失败时不应有成功");
        Assert.True(File.Exists(file), "验证失败的文件必须原样保留");
        Assert.True(result.FailedReasons[0].Contains("验证"), "失败原因应为路径验证失败");
    }

    public static async Task ReportsMissingFiles()
    {
        using var temp = new TempDirectory();
        var missing = Path.Combine(temp.Root, "missing.txt");

        var result = await RecycleBinService.DeleteFilesAsync(new[] { missing }, _ => true);

        Assert.Equal(1, result.FailCount, "文件不存在时应失败");
        Assert.True(result.FailedReasons[0].Contains("不存在"), "失败原因应为文件不存在");
    }

    public static async Task MovesFileToRecycleBinWhenEnabled()
    {
        if (Environment.GetEnvironmentVariable("FCT_RUN_RECYCLE_TEST") != "1")
        {
            Console.WriteLine("  SKIP：设置环境变量 FCT_RUN_RECYCLE_TEST=1 可执行真实回收站删除测试");
            return;
        }

        using var temp = new TempDirectory();
        var file = temp.CreateFile("recycle-me.txt", "recycle test " + Guid.NewGuid().ToString("N"));

        var result = await RecycleBinService.DeleteFilesAsync(new[] { file }, _ => true);

        Assert.Equal(1, result.SuccessCount, "应成功移动到回收站");
        Assert.Equal(0, result.FailCount, "不应有失败");
        Assert.False(File.Exists(file), "文件应已离开原位置");
    }
}
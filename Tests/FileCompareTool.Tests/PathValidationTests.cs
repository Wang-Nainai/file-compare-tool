using System.IO;
using System.Threading.Tasks;
using FileCompareTool.Services;

namespace FileCompareTool.Tests;

internal static class PathValidationTests
{
    public static Task SeparateDirectoriesAreAllowed()
    {
        using var temp = new TempDirectory();
        var a = temp.CreateDirectory("A");
        var b = temp.CreateDirectory("B");

        var (conflict, _) = PathValidationService.CheckPathConflict(new[] { a }, new[] { b });
        Assert.False(conflict, "互不相关的目录不应冲突");

        return Task.CompletedTask;
    }

    public static Task SameAndNestedDirectoriesConflict()
    {
        using var temp = new TempDirectory();
        var a = temp.CreateDirectory("A");
        var nested = temp.CreateDirectory("A/nested");
        var b = temp.CreateDirectory("B");

        Assert.True(PathValidationService.CheckPathConflict(new[] { a }, new[] { a }).hasConflict,
            "相同目录应冲突");
        Assert.True(PathValidationService.CheckPathConflict(new[] { a }, new[] { nested }).hasConflict,
            "B 位于 A 中时应冲突");
        Assert.True(PathValidationService.CheckPathConflict(new[] { nested }, new[] { a }).hasConflict,
            "A 位于 B 中时应冲突");
        Assert.False(PathValidationService.CheckPathConflict(new[] { a }, new[] { b }).hasConflict,
            "不同目录不应冲突");
        Assert.True(PathValidationService.CheckPathConflict(new[] { a + "\\" }, new[] { a.ToUpperInvariant() }).hasConflict,
            "大小写和末尾分隔符不应绕过冲突检查");

        return Task.CompletedTask;
    }

    public static Task DifferentFilesInSameDirectoryAreAllowed()
    {
        using var temp = new TempDirectory();
        var fileA = temp.CreateFile("shared/a.txt");
        var fileB = temp.CreateFile("shared/b.txt");

        var (conflict, _) = PathValidationService.CheckPathConflict(new[] { fileA }, new[] { fileB });
        Assert.False(conflict, "同一目录下的两个不同文件应允许比较");

        return Task.CompletedTask;
    }

    public static Task FileInsideOtherSideDirectoryConflicts()
    {
        using var temp = new TempDirectory();
        var a = temp.CreateDirectory("A");
        var fileInA = temp.CreateFile("A/inside.txt");

        var (conflict, _) = PathValidationService.CheckPathConflict(new[] { fileInA }, new[] { a });
        Assert.True(conflict, "文件位于另一侧目录中时应冲突");

        var (conflict2, _) = PathValidationService.CheckPathConflict(
            new[] { fileInA }, new[] { temp.CreateDirectory("B") });
        Assert.False(conflict2, "文件不位于另一侧目录中时不应冲突");

        return Task.CompletedTask;
    }

    public static Task MissingPathsAreRejected()
    {
        using var temp = new TempDirectory();
        var b = temp.CreateDirectory("B");
        var missing = Path.Combine(temp.Root, "missing");

        var (conflict, message) = PathValidationService.CheckPathConflict(new[] { missing }, new[] { b });
        Assert.True(conflict, "不存在的路径应被拒绝");
        Assert.True(message.Contains("不存在"), "错误信息应说明路径不存在");

        return Task.CompletedTask;
    }
}

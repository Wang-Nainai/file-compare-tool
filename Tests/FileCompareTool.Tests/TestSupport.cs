using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace FileCompareTool.Tests;

internal static class Assert
{
    public static void True(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException("断言失败：" + message);
    }

    public static void False(bool condition, string message)
    {
        True(!condition, message);
    }

    public static void Equal<T>(T expected, T actual, string message)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException($"断言失败：{message}（期望：{expected}，实际：{actual}）");
    }

    public static void NotNull(object? value, string message)
    {
        if (value == null)
            throw new InvalidOperationException("断言失败：" + message);
    }

    public static async Task ThrowsAsync<TException>(Func<Task> action, string message)
        where TException : Exception
    {
        try
        {
            await action();
        }
        catch (TException)
        {
            return;
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                $"断言失败：{message}（期望 {typeof(TException).Name}，实际 {ex.GetType().Name}）", ex);
        }

        throw new InvalidOperationException($"断言失败：{message}（没有抛出 {typeof(TException).Name}）");
    }
}

internal sealed class TempDirectory : IDisposable
{
    public string Root { get; }

    public TempDirectory()
    {
        Root = Path.Combine(Path.GetTempPath(), "FileCompareToolTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
    }

    public string CreateFile(string relativePath, string content = "test")
    {
        var path = Path.Combine(Root, relativePath);
        var directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
            Directory.CreateDirectory(directory);
        File.WriteAllText(path, content);
        return path;
    }

    public string CreateDirectory(string relativePath)
    {
        var path = Path.Combine(Root, relativePath);
        Directory.CreateDirectory(path);
        return path;
    }

    public void Dispose()
    {
        try
        {
            Directory.Delete(Root, true);
        }
        catch
        {
            // 测试清理失败不影响测试结果
        }
    }
}

internal sealed class InlineProgress<T> : IProgress<T>
{
    private readonly object _lock = new();
    private readonly List<T> _values = new();

    public IReadOnlyList<T> Values
    {
        get
        {
            lock (_lock)
                return _values.ToList();
        }
    }

    public void Report(T value)
    {
        lock (_lock)
            _values.Add(value);
    }
}

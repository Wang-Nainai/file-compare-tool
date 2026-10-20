using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using FileCompareTool.Models;

namespace FileCompareTool.Services;

public static class FileScanService
{
    private const int ProgressInterval = 200;

    public static Task<List<FileInfoItem>> ScanPathsAsync(
        IEnumerable<string> paths,
        IProgress<ScanProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        return Task.Run(() => ScanCore(paths, progress, cancellationToken), cancellationToken);
    }

    private static List<FileInfoItem> ScanCore(
        IEnumerable<string> paths,
        IProgress<ScanProgress>? progress,
        CancellationToken ct)
    {
        var pathList = PreparePaths(paths);
        var results = new List<FileInfoItem>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        for (var i = 0; i < pathList.Count; i++)
        {
            ct.ThrowIfCancellationRequested();

            var path = pathList[i];
            if (File.Exists(path))
            {
                AddFile(path, results, seen);
            }
            else if (Directory.Exists(path))
            {
                ScanDirectory(path, results, seen, progress, ct);
            }

            progress?.Report(new ScanProgress
            {
                TotalPaths = pathList.Count,
                ProcessedPaths = i + 1,
                FilesFound = results.Count,
                CurrentPath = path
            });
        }

        return results;
    }

    private static void ScanDirectory(
        string root,
        List<FileInfoItem> results,
        HashSet<string> seen,
        IProgress<ScanProgress>? progress,
        CancellationToken ct)
    {
        var stack = new Stack<string>();
        stack.Push(root);
        var sinceLastReport = 0;

        while (stack.Count > 0)
        {
            ct.ThrowIfCancellationRequested();
            var dir = stack.Pop();

            foreach (var file in EnumerateSafely(() => Directory.EnumerateFiles(dir)))
            {
                ct.ThrowIfCancellationRequested();

                if (!AddFile(file, results, seen))
                    continue;

                sinceLastReport++;
                if (sinceLastReport < ProgressInterval)
                    continue;

                sinceLastReport = 0;
                progress?.Report(new ScanProgress
                {
                    TotalPaths = 0,
                    ProcessedPaths = 0,
                    FilesFound = results.Count,
                    CurrentPath = file
                });
            }

            foreach (var subDir in EnumerateSafely(() => Directory.EnumerateDirectories(dir)))
            {
                ct.ThrowIfCancellationRequested();

                try
                {
                    // 跳过目录联接/符号链接，避免循环扫描以及删除时穿透到别处
                    var attributes = File.GetAttributes(subDir);
                    if ((attributes & FileAttributes.ReparsePoint) != 0)
                        continue;
                }
                catch
                {
                    continue;
                }

                stack.Push(subDir);
            }
        }
    }

    private static bool AddFile(string path, List<FileInfoItem> results, HashSet<string> seen)
    {
        try
        {
            var info = new FileInfo(path);
            if (!seen.Add(info.FullName))
                return false;

            results.Add(new FileInfoItem
            {
                FullPath = info.FullName,
                FileName = info.Name,
                Extension = info.Extension,
                DirectoryName = info.DirectoryName ?? string.Empty
            });
            return true;
        }
        catch
        {
            // 文件在扫描过程中被删除、无权限等情况直接跳过
            return false;
        }
    }

    private static List<string> PreparePaths(IEnumerable<string> paths)
    {
        var result = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var path in paths)
        {
            try
            {
                var normalized = PathValidationService.NormalizePath(path);
                if (seen.Add(normalized))
                    result.Add(normalized);
            }
            catch
            {
                // 忽略无效路径
            }
        }

        return result;
    }

    /// <summary>
    /// 逐个枚举并在每次 MoveNext 时捕获异常，避免某个无权限子目录中断整个扫描。
    /// </summary>
    private static IEnumerable<string> EnumerateSafely(Func<IEnumerable<string>> factory)
    {
        IEnumerator<string>? enumerator = null;
        try
        {
            enumerator = factory().GetEnumerator();
        }
        catch
        {
            yield break;
        }

        if (enumerator == null)
            yield break;

        using (enumerator)
        {
            while (true)
            {
                bool hasNext;
                try
                {
                    hasNext = enumerator.MoveNext();
                }
                catch
                {
                    yield break;
                }

                if (!hasNext)
                    yield break;

                yield return enumerator.Current;
            }
        }
    }
}

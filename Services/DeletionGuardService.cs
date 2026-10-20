using System;
using System.Collections.Generic;
using System.Linq;

namespace FileCompareTool.Services;

/// <summary>
/// 删除前最后一道防线：只允许删除本次扫描得到的 B 文件，并且该文件不能位于任何 A 路径中。
/// 即使匹配结果或界面状态被意外修改，也不会碰到 A 区域的文件。
/// </summary>
public static class DeletionGuardService
{
    public static Func<string, bool> Create(
        IEnumerable<string> aRoots,
        IEnumerable<string> bRoots,
        IEnumerable<string> allowedFiles)
    {
        var aEntries = BuildEntries(aRoots);
        var bEntries = BuildEntries(bRoots);

        var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var file in allowedFiles)
        {
            try
            {
                allowed.Add(PathValidationService.NormalizePath(file));
            }
            catch
            {
                // 忽略无效的快照路径
            }
        }

        if (bEntries.Count == 0 || allowed.Count == 0)
            return _ => false;

        return path =>
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string fullPath;
            try
            {
                fullPath = PathValidationService.NormalizePath(path);
            }
            catch
            {
                return false;
            }

            // 必须正好是扫描时记录的 B 文件
            if (!allowed.Contains(fullPath))
                return false;

            // 不允许位于任何 A 路径中
            if (aEntries.Any(entry => MatchesRoot(fullPath, entry)))
                return false;

            // 必须位于本次扫描的 B 路径中
            return bEntries.Any(entry => MatchesRoot(fullPath, entry));
        };
    }

    private static List<PathEntry> BuildEntries(IEnumerable<string> paths)
    {
        var entries = new List<PathEntry>();
        foreach (var path in paths)
        {
            if (PathValidationService.TryCreatePathEntry(path, out var entry, out _))
                entries.Add(entry);
        }
        return entries;
    }

    private static bool MatchesRoot(string normalizedPath, PathEntry root)
    {
        if (!root.IsDirectory)
            return string.Equals(normalizedPath, root.Normalized, StringComparison.OrdinalIgnoreCase);

        return PathValidationService.IsWithinOrEqual(normalizedPath, root.Normalized);
    }
}

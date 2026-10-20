using System;
using System.Collections.Generic;
using System.IO;

namespace FileCompareTool.Services;

public readonly record struct PathEntry(string Original, string Normalized, bool IsDirectory);

public static class PathValidationService
{
    /// <summary>
    /// 检查 A/B 路径是否存在冲突。相同、互为父子目录、或某个文件位于另一侧的目录中，
    /// 都可能导致同一文件被两侧同时扫描，进而在删除 B 文件时误删 A 文件。
    /// 两个不同文件位于同一目录下是允许的。
    /// </summary>
    public static (bool hasConflict, string message) CheckPathConflict(
        IEnumerable<string> aPaths, IEnumerable<string> bPaths)
    {
        var aEntries = new List<PathEntry>();
        var bEntries = new List<PathEntry>();

        foreach (var path in aPaths)
        {
            if (!TryCreatePathEntry(path, out var entry, out var error))
                return (true, error);
            aEntries.Add(entry);
        }

        foreach (var path in bPaths)
        {
            if (!TryCreatePathEntry(path, out var entry, out var error))
                return (true, error);
            bEntries.Add(entry);
        }

        foreach (var a in aEntries)
        {
            foreach (var b in bEntries)
            {
                if (!Overlaps(a, b)) continue;

                return (true, LocalizationService.Format(
                    "PathConflict_Format", a.Original, b.Original));
            }
        }

        return (false, string.Empty);
    }

    /// <summary>
    /// 转为绝对路径并去掉末尾分隔符（盘符根目录除外）。
    /// </summary>
    public static string NormalizePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(LocalizationService.T("Error_PathEmpty"), nameof(path));

        var fullPath = Path.GetFullPath(path.Trim());
        var root = Path.GetPathRoot(fullPath) ?? string.Empty;

        if (fullPath.Length > root.Length)
            fullPath = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

        return fullPath.Length == 0 ? root : fullPath;
    }

    public static bool TryCreatePathEntry(string path, out PathEntry entry, out string error)
    {
        entry = default;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(path))
        {
            error = LocalizationService.T("Error_EmptyPath");
            return false;
        }

        try
        {
            var normalized = NormalizePath(path);
            var isDirectory = Directory.Exists(normalized);
            var isFile = File.Exists(normalized);

            if (!isDirectory && !isFile)
            {
                error = LocalizationService.Format("Error_PathInvalidFormat", path);
                return false;
            }

            entry = new PathEntry(path, normalized, isDirectory);
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException
                                       or PathTooLongException or System.Security.SecurityException)
        {
            error = LocalizationService.Format("Error_PathInvalidDetailFormat", path, ex.Message);
            return false;
        }
    }

    /// <summary>
    /// 判断 <paramref name="path"/> 是否位于 <paramref name="root"/> 中（包含 root 本身）。
    /// 调用方传入的路径可以带相对路径或 ..，内部会先规范化。
    /// </summary>
    public static bool IsWithinOrEqual(string path, string root)
    {
        var normalizedPath = NormalizePath(path);
        var normalizedRoot = NormalizePath(root);

        if (string.Equals(normalizedPath, normalizedRoot, StringComparison.OrdinalIgnoreCase))
            return true;

        return IsStrictlyWithin(normalizedPath, normalizedRoot);
    }

    private static bool Overlaps(PathEntry a, PathEntry b)
    {
        if (string.Equals(a.Normalized, b.Normalized, StringComparison.OrdinalIgnoreCase))
            return true;

        if (a.IsDirectory && IsStrictlyWithin(b.Normalized, a.Normalized))
            return true;

        if (b.IsDirectory && IsStrictlyWithin(a.Normalized, b.Normalized))
            return true;

        return false;
    }

    private static bool IsStrictlyWithin(string child, string parent)
    {
        if (string.Equals(child, parent, StringComparison.OrdinalIgnoreCase))
            return false;

        var prefix = parent.EndsWith(Path.DirectorySeparatorChar)
            ? parent
            : parent + Path.DirectorySeparatorChar;

        return child.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);
    }
}

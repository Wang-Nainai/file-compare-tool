using System;
using System.Collections.Generic;
using System.Linq;
using FileCompareTool.Models;

namespace FileCompareTool.Services;

public static class FileMatchService
{
    /// <summary>
    /// 按文件名（不区分大小写）匹配 A/B。只返回两侧都存在的文件名。
    /// </summary>
    public static List<MatchResult> FindMatches(
        IEnumerable<FileInfoItem> aFiles,
        IEnumerable<FileInfoItem> bFiles)
    {
        var aGroups = aFiles
            .GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var bGroups = bFiles
            .GroupBy(f => f.FileName, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);

        var results = new List<MatchResult>();

        foreach (var group in aGroups.OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
        {
            if (!bGroups.TryGetValue(group.Key, out var bMatches))
                continue;

            results.Add(new MatchResult
            {
                FileName = group.Key,
                AFiles = group.Value,
                BFiles = bMatches,
                IsSelected = true
            });
        }

        return results;
    }
}

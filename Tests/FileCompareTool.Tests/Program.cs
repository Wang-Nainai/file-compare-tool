using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FileCompareTool.Services;

namespace FileCompareTool.Tests;

internal static class Program
{
    private static async Task<int> Main()
    {
        Console.OutputEncoding = Encoding.UTF8;
        LocalizationService.SetLanguage(LocalizationService.ChineseSimplified, persist: false);

        var tests = new List<(string Name, Func<Task> Run)>
        {
            ("扫描：递归、去重与进度", ScanTests.ScanFindsFilesRecursivelyAndDedupes),
            ("扫描：忽略不存在/空路径", ScanTests.ScanIgnoresMissingAndEmptyPaths),
            ("扫描：支持取消", ScanTests.ScanHonorsCancellation),
            ("扫描：不跟随目录联接循环", ScanTests.ScanDoesNotFollowJunctionLoop),
            ("匹配：不区分大小写且保留重复数量", MatchTests.MatchIsCaseInsensitiveAndKeepsDuplicateCounts),
            ("匹配：无同名文件时返回空", MatchTests.MatchReturnsEmptyWhenNoNamesOverlap),
            ("路径校验：独立目录允许", PathValidationTests.SeparateDirectoriesAreAllowed),
            ("路径校验：相同/嵌套目录冲突", PathValidationTests.SameAndNestedDirectoriesConflict),
            ("路径校验：同目录不同文件允许", PathValidationTests.DifferentFilesInSameDirectoryAreAllowed),
            ("路径校验：文件位于另一侧目录冲突", PathValidationTests.FileInsideOtherSideDirectoryConflicts),
            ("路径校验：拒绝不存在的路径", PathValidationTests.MissingPathsAreRejected),
            ("删除守卫：只允许扫描到的 B 文件", DeletionGuardTests.GuardOnlyAllowsScannedBFiles),
            ("删除守卫：拒绝路径穿越与 A 文件", DeletionGuardTests.GuardRejectsPathTraversalAndAOverlap),
            ("删除守卫：空快照拒绝所有删除", DeletionGuardTests.GuardWithEmptySnapshotRejectsEverything),
            ("回收站：验证失败保留文件", RecycleBinTests.RejectsWhenValidatorSaysNo),
            ("回收站：报告文件不存在", RecycleBinTests.ReportsMissingFiles),
            ("回收站：真实移动到回收站", RecycleBinTests.MovesFileToRecycleBinWhenEnabled),
            ("本地化：中英文资源键一致", LocalizationTests.CatalogsHaveMatchingKeys),
            ("本地化：切换语言并格式化文本", LocalizationTests.SwitchingLanguageChangesFormattedText),
            ("模型：AddedPath 文本与状态", ModelTests.AddedPathSubTextReflectsState),
            ("模型：MatchResult 数量通知", ModelTests.MatchResultRaisesCountNotifications),
            ("视图模型：扫描/选择/删除确认/结果失效", ViewModelTests.EndToEndScanSelectionAndSafeDelete),
            ("视图模型：无匹配时的空状态", ViewModelTests.NoMatchesShowsFriendlyEmptyState),
            ("视图模型：切换语言刷新界面文本", ViewModelTests.LanguageSwitchRefreshesVisibleState),
            ("视图模型：拒绝冲突路径", ViewModelTests.ScanRejectsConflictingPaths),
            ("视图模型：取消扫描后状态一致", ViewModelTests.ScanCancelKeepsViewModelConsistent)
        };

        var failed = 0;
        foreach (var (name, run) in tests)
        {
            try
            {
                await run();
                Console.WriteLine($"[PASS] {name}");
            }
            catch (Exception ex)
            {
                failed++;
                Console.WriteLine($"[FAIL] {name}");
                Console.WriteLine(Indent(ex.ToString()));
            }
        }

        Console.WriteLine();
        Console.WriteLine(failed == 0
            ? $"全部通过：{tests.Count} 个测试。"
            : $"测试完成：{tests.Count - failed} 通过，{failed} 失败。");

        return failed == 0 ? 0 : 1;
    }

    private static string Indent(string text)
    {
        var lines = text.Split('\n');
        for (var i = 0; i < lines.Length; i++)
            lines[i] = "    " + lines[i].TrimEnd('\r');
        return string.Join(Environment.NewLine, lines);
    }
}
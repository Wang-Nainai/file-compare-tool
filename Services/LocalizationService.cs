using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Windows;

namespace FileCompareTool.Services;

/// <summary>
/// 统一的界面语言资源。XAML 通过 DynamicResource 使用这里的键，
/// 代码中的状态栏、弹窗和错误提示通过 T / Format 获取当前语言文本。
/// </summary>
public static class LocalizationService
{
    public const string ChineseSimplified = "zh-CN";
    public const string English = "en-US";

    private static readonly string[] SupportedLanguageCodes = { ChineseSimplified, English };

    private static readonly Dictionary<string, string> ChineseCatalog = new(StringComparer.Ordinal)
    {
        ["App_Title"] = "文件名比对清理工具",
        ["App_Subtitle"] = "按文件名比较 A / B，安全清理 B 中的重复文件",
        ["App_PrivacyBadge"] = "本地处理 · 数据不会上传",
        ["Tooltip_LanguageSwitch"] = "切换界面语言",

        ["Main_A_Section"] = "A · 参考文件",
        ["Main_B_Section"] = "B · 待处理文件",
        ["Unit_FilesWithSpace"] = " 个文件",
        ["Unit_MatchesWithSpace"] = " 项",
        ["Button_AddFiles"] = "添加文件",
        ["Button_AddFolder"] = "添加文件夹",
        ["Button_Cancel"] = "取消",
        ["Button_StartScan"] = "开始扫描",
        ["Button_DeleteSelected"] = "删除选中项",
        ["Button_SelectAll"] = "全选",
        ["Button_DeselectAll"] = "全不选",
        ["Button_InvertSelection"] = "反选",
        ["Tooltip_AddFilesA"] = "选择一个或多个文件加入 A",
        ["Tooltip_AddFolderA"] = "选择一个文件夹加入 A",
        ["Tooltip_AddFilesB"] = "选择一个或多个文件加入 B",
        ["Tooltip_AddFolderB"] = "选择一个文件夹加入 B",
        ["Tooltip_RemovePath"] = "移除该路径",
        ["Drop_HintTitle"] = "拖入文件或文件夹",
        ["Drop_HintSubtitle"] = "或使用右上角的添加按钮",
        ["Metric_AFiles"] = "A 文件",
        ["Metric_BFiles"] = "B 文件",
        ["Metric_Matches"] = "匹配",
        ["Metric_Selected"] = "已选",
        ["Results_Title"] = "匹配结果",
        ["View_List"] = "列表",
        ["View_Details"] = "详细",
        ["View_Tiles"] = "平铺",
        ["View_Icons"] = "图标",
        ["Automation_SelectMatch"] = "选中此匹配项",
        ["DetailView_A_Label"] = "A · 参考",
        ["DetailView_B_Label"] = "B · 待处理",
        ["Empty_Step1"] = " 添加路径",
        ["Empty_Step2"] = " 开始扫描",
        ["Empty_Step3"] = " 勾选并清理",

        ["Confirm_WindowTitle"] = "确认删除",
        ["Confirm_Heading"] = "移动到回收站？",
        ["Confirm_Subheading"] = "此操作可以从回收站恢复",
        ["Confirm_MessagePrefix"] = "确定将选中的 ",
        ["Confirm_MessageSuffix"] = " 个 B 文件移动到 Windows 回收站吗？A 区域中的参考文件不会被修改。",
        ["Confirm_Info"] = "如果文件所在磁盘没有回收站，Windows 会提示是否永久删除。",
        ["Confirm_Action"] = "确认删除",

        ["Detail_WindowTitle"] = "文件详情",
        ["Detail_Subtitle"] = "文件详情 · 双击打开，右键查看完整路径",
        ["Detail_A_CountLabel"] = "A 参考 ",
        ["Detail_B_CountLabel"] = "B 待处理 ",
        ["Detail_FooterOpen"] = "双击文件用默认程序打开",
        ["Detail_FooterInfo"] = "右键查看文件完整信息",
        ["Detail_FileInfoTitle"] = "文件信息",
        ["Detail_FileNameLabel"] = "文件名",
        ["Detail_FullPathLabel"] = "完整路径",
        ["Detail_DirectoryLabel"] = "所在目录",
        ["Detail_ExtensionLabel"] = "扩展名",
        ["Detail_InfoFormat"] = "文件名：{0}\n完整路径：{1}\n所在目录：{2}\n扩展名：{3}",
        ["Detail_WindowTitleFormat"] = "文件详情 - {0}",
        ["Context_ViewDetails"] = "查看详情",
        ["Context_OpenDefault"] = "用默认程序打开",

        ["Dialog_HintTitle"] = "提示",
        ["Dialog_ErrorTitle"] = "错误",
        ["Error_FileMissingFormat"] = "文件不存在：{0}",
        ["Error_CannotOpenFormat"] = "无法打开：{0}",
        ["Error_CannotOpenFileFormat"] = "无法打开文件：{0}",
        ["Error_UnexpectedFormat"] = "程序出错：{0}\n\n错误已记录到：\n{1}",
        ["Dialog_AllFilesFilter"] = "所有文件|*.*",
        ["Dialog_SelectFilesA"] = "选择要加入 A 的文件",
        ["Dialog_SelectFolderA"] = "选择要加入 A 的文件夹",
        ["Dialog_SelectFilesB"] = "选择要加入 B 的文件",
        ["Dialog_SelectFolderB"] = "选择要加入 B 的文件夹",

        ["Status_Ready"] = "就绪",
        ["Status_FileAdded"] = "已添加文件。",
        ["Status_FolderAddedCounting"] = "已添加文件夹，正在统计文件数…",
        ["Status_PathsAddedCounting"] = "已添加路径，正在统计文件数…",
        ["Status_PathRemoved"] = "已移除路径。",
        ["Status_PathConflict"] = "路径设置有误。",
        ["Status_ScanningA"] = "正在扫描 A…",
        ["Status_ScanningB"] = "正在扫描 B…",
        ["Status_ScanProgressFormat"] = "正在扫描 {0}… 已找到 {1:N0} 个文件",
        ["Status_ScanCancelling"] = "正在取消扫描…",
        ["Status_ScanCancelled"] = "扫描已取消。",
        ["Status_PathChanged"] = "路径已更改，请重新扫描。",
        ["Status_ScanCompleteFormat"] = "完成：A {0:N0} 个文件，B {1:N0} 个文件，匹配 {2:N0} 个文件名。",
        ["Status_ScanError"] = "扫描出错。",
        ["Status_DeletingStartFormat"] = "正在将 0/{0} 个文件移动到回收站…",
        ["Status_DeletingProgressFormat"] = "正在将 {0}/{1} 个文件移动到回收站：{2}",
        ["Status_DeleteCompleteFormat"] = "删除完成：成功 {0} 个，失败 {1} 个。",
        ["Status_NoMatches"] = "已无匹配项。",
        ["Status_DeleteError"] = "删除出错。",

        ["Empty_NoResultTitle"] = "还没有匹配结果",
        ["Empty_NoResultSubtitle"] = "先在上方添加 A / B 路径，然后点击“开始扫描”",
        ["Empty_PathsChangedTitle"] = "还没有匹配结果",
        ["Empty_PathsChangedSubtitle"] = "路径已更改，点击“开始扫描”重新比较。",
        ["Empty_ScanningTitle"] = "正在扫描…",
        ["Empty_ScanningSubtitle"] = "正在读取 A / B 中的文件，请稍候。",
        ["Empty_NoMatchesTitle"] = "没有找到同名文件",
        ["Empty_NoMatchesSubtitle"] = "A 和 B 中没有同名文件，可以调整路径后重新扫描。",
        ["Empty_ScanCancelledTitle"] = "扫描已取消",
        ["Empty_ScanCancelledSubtitle"] = "可以重新点击“开始扫描”。",
        ["Empty_PathConflictTitle"] = "路径设置有误",
        ["Empty_PathConflictSubtitle"] = "请选择互不重叠的 A / B 路径后再扫描。",
        ["Empty_ScanErrorTitle"] = "扫描出错",
        ["Empty_ScanErrorSubtitle"] = "请查看提示或错误日志后重试。",
        ["Empty_DeletedTitle"] = "已清理完成",
        ["Empty_DeletedSubtitle"] = "选中的 B 文件已移动到回收站，A 参考文件未受影响。",
        ["Empty_SelectResultSubtitle"] = "选择结果后可在这里查看详情。",

        ["Message_AddBothPaths"] = "请先添加 A 和 B 区域的文件或文件夹。",
        ["Message_NoSelection"] = "没有选中的匹配项。",
        ["Message_ResultsExpired"] = "匹配结果已失效，请重新扫描后再删除。",
        ["Message_FailureDetailsHeader"] = "失败详情：",
        ["Message_FailureItemFormat"] = "{0}：{1}",
        ["Message_MoreFailuresFormat"] = "…另有 {0} 个失败。",
        ["Message_ScanErrorFormat"] = "扫描出错：{0}\n\n详细日志：{1}",
        ["Message_DeleteErrorFormat"] = "删除出错：{0}\n\n详细日志：{1}",

        ["Path_Counting"] = "正在统计…",
        ["Path_CountUnavailable"] = "无法统计文件数",
        ["Path_FilesCountFormat"] = "{0:N0} 个文件",

        ["PathConflict_Format"] = "A 和 B 的路径存在冲突：\nA: {0}\nB: {1}\n\n两者相同，或其中一个位于另一个的目录中。这样会让同一文件同时出现在 A/B 中，删除时可能误删参考文件。请选择互不重叠的文件或文件夹。",
        ["Error_PathEmpty"] = "路径不能为空。",
        ["Error_EmptyPath"] = "存在空路径，请检查 A / B 区域的路径。",
        ["Error_PathInvalidFormat"] = "路径不存在或无效：{0}",
        ["Error_PathInvalidDetailFormat"] = "路径无效：{0}\n{1}",

        ["Delete_ReasonPathValidation"] = "路径验证失败",
        ["Delete_ReasonFileMissing"] = "文件不存在",
        ["Delete_ReasonPathMismatch"] = "文件路径不匹配",
        ["Delete_ErrorRecycleCodeFormat"] = "移动到回收站失败，错误代码：{0}",
        ["Delete_ErrorCancelled"] = "操作已取消。",
        ["Delete_ErrorNotMoved"] = "文件未能移动到回收站。"
    };

    private static readonly Dictionary<string, string> EnglishCatalog = new(StringComparer.Ordinal)
    {
        ["App_Title"] = "Filename Match & Cleanup",
        ["App_Subtitle"] = "Match A/B by filename and safely clean duplicates from B",
        ["App_PrivacyBadge"] = "Local only · No data uploaded",
        ["Tooltip_LanguageSwitch"] = "Switch interface language",

        ["Main_A_Section"] = "A · Reference files",
        ["Main_B_Section"] = "B · Files to clean",
        ["Unit_FilesWithSpace"] = " files",
        ["Unit_MatchesWithSpace"] = " matches",
        ["Button_AddFiles"] = "Add files",
        ["Button_AddFolder"] = "Add folder",
        ["Button_Cancel"] = "Cancel",
        ["Button_StartScan"] = "Start scan",
        ["Button_DeleteSelected"] = "Delete selected",
        ["Button_SelectAll"] = "Select all",
        ["Button_DeselectAll"] = "Select none",
        ["Button_InvertSelection"] = "Invert",
        ["Tooltip_AddFilesA"] = "Add one or more files to A",
        ["Tooltip_AddFolderA"] = "Add a folder to A",
        ["Tooltip_AddFilesB"] = "Add one or more files to B",
        ["Tooltip_AddFolderB"] = "Add a folder to B",
        ["Tooltip_RemovePath"] = "Remove this path",
        ["Drop_HintTitle"] = "Drop files or folders here",
        ["Drop_HintSubtitle"] = "or use the add buttons above",
        ["Metric_AFiles"] = "A files",
        ["Metric_BFiles"] = "B files",
        ["Metric_Matches"] = "Matches",
        ["Metric_Selected"] = "Selected",
        ["Results_Title"] = "Matches",
        ["View_List"] = "List",
        ["View_Details"] = "Details",
        ["View_Tiles"] = "Tiles",
        ["View_Icons"] = "Icons",
        ["Automation_SelectMatch"] = "Select this match",
        ["DetailView_A_Label"] = "A · Reference",
        ["DetailView_B_Label"] = "B · Pending",
        ["Empty_Step1"] = " Add paths",
        ["Empty_Step2"] = " Start scan",
        ["Empty_Step3"] = " Select & clean",

        ["Confirm_WindowTitle"] = "Confirm deletion",
        ["Confirm_Heading"] = "Move to Recycle Bin?",
        ["Confirm_Subheading"] = "You can restore the files later",
        ["Confirm_MessagePrefix"] = "Move ",
        ["Confirm_MessageSuffix"] = " selected B file(s) to the Recycle Bin? Reference files in A will not be modified.",
        ["Confirm_Info"] = "If the drive has no Recycle Bin, Windows will ask whether to delete the files permanently.",
        ["Confirm_Action"] = "Delete",

        ["Detail_WindowTitle"] = "File details",
        ["Detail_Subtitle"] = "File details · Double-click to open, right-click for full path",
        ["Detail_A_CountLabel"] = "A reference ",
        ["Detail_B_CountLabel"] = "B pending ",
        ["Detail_FooterOpen"] = "Double-click a file to open it with the default app",
        ["Detail_FooterInfo"] = "Right-click to view full file information",
        ["Detail_FileInfoTitle"] = "File information",
        ["Detail_FileNameLabel"] = "File name",
        ["Detail_FullPathLabel"] = "Full path",
        ["Detail_DirectoryLabel"] = "Directory",
        ["Detail_ExtensionLabel"] = "Extension",
        ["Detail_InfoFormat"] = "File name: {0}\nFull path: {1}\nDirectory: {2}\nExtension: {3}",
        ["Detail_WindowTitleFormat"] = "File details - {0}",
        ["Context_ViewDetails"] = "View details",
        ["Context_OpenDefault"] = "Open with default app",

        ["Dialog_HintTitle"] = "Notice",
        ["Dialog_ErrorTitle"] = "Error",
        ["Error_FileMissingFormat"] = "File not found: {0}",
        ["Error_CannotOpenFormat"] = "Cannot open: {0}",
        ["Error_CannotOpenFileFormat"] = "Cannot open file: {0}",
        ["Error_UnexpectedFormat"] = "Application error: {0}\n\nDetails were logged to:\n{1}",
        ["Dialog_AllFilesFilter"] = "All files|*.*",
        ["Dialog_SelectFilesA"] = "Select files to add to A",
        ["Dialog_SelectFolderA"] = "Select a folder to add to A",
        ["Dialog_SelectFilesB"] = "Select files to add to B",
        ["Dialog_SelectFolderB"] = "Select a folder to add to B",

        ["Status_Ready"] = "Ready",
        ["Status_FileAdded"] = "Files added.",
        ["Status_FolderAddedCounting"] = "Folder added. Counting files…",
        ["Status_PathsAddedCounting"] = "Paths added. Counting files…",
        ["Status_PathRemoved"] = "Path removed.",
        ["Status_PathConflict"] = "Invalid path configuration.",
        ["Status_ScanningA"] = "Scanning A…",
        ["Status_ScanningB"] = "Scanning B…",
        ["Status_ScanProgressFormat"] = "Scanning {0}… {1:N0} files found",
        ["Status_ScanCancelling"] = "Cancelling scan…",
        ["Status_ScanCancelled"] = "Scan cancelled.",
        ["Status_PathChanged"] = "Paths changed. Please scan again.",
        ["Status_ScanCompleteFormat"] = "Completed: A {0:N0} files, B {1:N0} files, {2:N0} matching filenames.",
        ["Status_ScanError"] = "Scan failed.",
        ["Status_DeletingStartFormat"] = "Moving 0/{0} files to the Recycle Bin…",
        ["Status_DeletingProgressFormat"] = "Moving {0}/{1} files to the Recycle Bin: {2}",
        ["Status_DeleteCompleteFormat"] = "Deletion complete: {0} succeeded, {1} failed.",
        ["Status_NoMatches"] = "No matches remaining.",
        ["Status_DeleteError"] = "Deletion failed.",

        ["Empty_NoResultTitle"] = "No results yet",
        ["Empty_NoResultSubtitle"] = "Add A/B paths above, then click “Start scan”.",
        ["Empty_PathsChangedTitle"] = "No results yet",
        ["Empty_PathsChangedSubtitle"] = "Paths changed. Click “Start scan” to compare again.",
        ["Empty_ScanningTitle"] = "Scanning…",
        ["Empty_ScanningSubtitle"] = "Reading files from A and B. Please wait.",
        ["Empty_NoMatchesTitle"] = "No matching filenames",
        ["Empty_NoMatchesSubtitle"] = "A and B have no filenames in common. Adjust the paths and scan again.",
        ["Empty_ScanCancelledTitle"] = "Scan cancelled",
        ["Empty_ScanCancelledSubtitle"] = "Click “Start scan” to try again.",
        ["Empty_PathConflictTitle"] = "Invalid paths",
        ["Empty_PathConflictSubtitle"] = "Choose A/B paths that do not overlap, then scan again.",
        ["Empty_ScanErrorTitle"] = "Scan failed",
        ["Empty_ScanErrorSubtitle"] = "Check the message or error log and try again.",
        ["Empty_DeletedTitle"] = "Cleanup complete",
        ["Empty_DeletedSubtitle"] = "The selected B files were moved to the Recycle Bin. Reference files in A were not modified.",
        ["Empty_SelectResultSubtitle"] = "Select a result to view details here.",

        ["Message_AddBothPaths"] = "Add files or folders to both A and B first.",
        ["Message_NoSelection"] = "No matching items are selected.",
        ["Message_ResultsExpired"] = "These results are no longer valid. Scan again before deleting.",
        ["Message_FailureDetailsHeader"] = "Failure details:",
        ["Message_FailureItemFormat"] = "{0}: {1}",
        ["Message_MoreFailuresFormat"] = "…and {0} more failed.",
        ["Message_ScanErrorFormat"] = "Scan failed: {0}\n\nLog file: {1}",
        ["Message_DeleteErrorFormat"] = "Deletion failed: {0}\n\nLog file: {1}",

        ["Path_Counting"] = "Counting…",
        ["Path_CountUnavailable"] = "Unable to count files",
        ["Path_FilesCountFormat"] = "{0:N0} files",

        ["PathConflict_Format"] = "A and B paths overlap:\nA: {0}\nB: {1}\n\nThey are the same, or one is inside the other. This would make the same file appear in both A and B and could delete reference files. Choose paths that do not overlap.",
        ["Error_PathEmpty"] = "Path cannot be empty.",
        ["Error_EmptyPath"] = "An empty path was found. Check the paths in A/B.",
        ["Error_PathInvalidFormat"] = "Path does not exist or is invalid: {0}",
        ["Error_PathInvalidDetailFormat"] = "Invalid path: {0}\n{1}",

        ["Delete_ReasonPathValidation"] = "Path validation failed",
        ["Delete_ReasonFileMissing"] = "File not found",
        ["Delete_ReasonPathMismatch"] = "File path does not match",
        ["Delete_ErrorRecycleCodeFormat"] = "Failed to move to the Recycle Bin. Error code: {0}",
        ["Delete_ErrorCancelled"] = "Operation cancelled.",
        ["Delete_ErrorNotMoved"] = "The file was not moved to the Recycle Bin."
    };

    private static readonly Dictionary<string, Dictionary<string, string>> Catalogs =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [ChineseSimplified] = ChineseCatalog,
            [English] = EnglishCatalog
        };

    private static ResourceDictionary? _languageResources;
    private static string _currentLanguage = ChineseSimplified;

    public static string CurrentLanguage => _currentLanguage;

    public static IReadOnlyList<string> SupportedLanguages => SupportedLanguageCodes;

    public static event EventHandler? LanguageChanged;

    public static void Initialize()
    {
        var saved = LoadSavedLanguage();
        _currentLanguage = saved ?? ChineseSimplified;
        ApplyToApplication();
    }

    public static void SetLanguage(string language, bool persist = true)
    {
        if (!Catalogs.ContainsKey(language))
            language = ChineseSimplified;

        if (string.Equals(_currentLanguage, language, StringComparison.OrdinalIgnoreCase))
        {
            if (Application.Current != null && _languageResources == null)
                ApplyToApplication();
            return;
        }

        _currentLanguage = language;
        ApplyToApplication();

        if (persist)
            SaveLanguage(language);

        LanguageChanged?.Invoke(null, EventArgs.Empty);
    }

    public static string T(string key)
    {
        var catalog = Catalogs.TryGetValue(_currentLanguage, out var current)
            ? current
            : ChineseCatalog;

        if (catalog.TryGetValue(key, out var value))
            return value;

        return ChineseCatalog.TryGetValue(key, out var fallback) ? fallback : key;
    }

    public static string Format(string key, params object?[] args)
    {
        try
        {
            return string.Format(CultureInfo.CurrentCulture, T(key), args);
        }
        catch (FormatException)
        {
            return T(key);
        }
    }

    public static IReadOnlyList<string> GetCatalogKeys(string language)
    {
        if (!Catalogs.TryGetValue(language, out var catalog))
            return Array.Empty<string>();

        return catalog.Keys.OrderBy(k => k, StringComparer.Ordinal).ToList();
    }

    private static void ApplyToApplication()
    {
        var app = Application.Current;
        if (app == null)
            return;

        var catalog = Catalogs.TryGetValue(_currentLanguage, out var current)
            ? current
            : ChineseCatalog;

        var resources = new ResourceDictionary();
        foreach (var (key, value) in catalog)
            resources[key] = value;

        var oldResources = _languageResources;
        app.Resources.MergedDictionaries.Add(resources);
        _languageResources = resources;

        if (oldResources != null)
            app.Resources.MergedDictionaries.Remove(oldResources);
    }

    private static string LanguageFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "FileCompareTool",
        "language.txt");

    private static string? LoadSavedLanguage()
    {
        try
        {
            if (!File.Exists(LanguageFilePath))
                return null;

            var value = File.ReadAllText(LanguageFilePath).Trim();
            return Catalogs.ContainsKey(value) ? value : null;
        }
        catch
        {
            return null;
        }
    }

    private static void SaveLanguage(string language)
    {
        try
        {
            var path = LanguageFilePath;
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, language);
        }
        catch
        {
            // 语言偏好保存失败不应影响使用
        }
    }
}

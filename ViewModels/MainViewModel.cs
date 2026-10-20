using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using FileCompareTool.Models;
using FileCompareTool.Services;

namespace FileCompareTool.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private CancellationTokenSource? _scanCts;
    private int _pathVersion;
    private bool _statsFromScan;
    private bool _acceptProgress;
    private bool _suppressSelectionCount;
    private bool _syncingLanguage;

    private string _statusKey = "Status_Ready";
    private object?[] _statusArgs = Array.Empty<object?>();
    private string _emptyTitleKey = "Empty_NoResultTitle";
    private string _emptySubtitleKey = "Empty_NoResultSubtitle";

    private List<string>? _aRootsSnapshot;
    private List<string>? _bRootsSnapshot;
    private HashSet<string>? _allowedBFiles;

    public MainViewModel()
    {
        LocalizationService.LanguageChanged += OnLanguageChanged;
    }

    public ObservableCollection<AddedPath> APaths { get; } = new();
    public ObservableCollection<AddedPath> BPaths { get; } = new();
    public ObservableCollection<MatchResult> MatchResults { get; } = new();

    [ObservableProperty]
    private bool isScanning;

    [ObservableProperty]
    private bool isDeleting;

    [ObservableProperty]
    private string statusMessage = LocalizationService.T("Status_Ready");

    [ObservableProperty]
    private string emptyStateTitle = LocalizationService.T("Empty_NoResultTitle");

    [ObservableProperty]
    private string emptyStateSubtitle = LocalizationService.T("Empty_NoResultSubtitle");

    [ObservableProperty]
    private bool hasMatchResults;

    [ObservableProperty]
    private int totalAFiles;

    [ObservableProperty]
    private int totalBFiles;

    [ObservableProperty]
    private int totalMatches;

    [ObservableProperty]
    private int selectedCount;

    [ObservableProperty]
    private double scanProgressValue;

    [ObservableProperty]
    private bool isViewModeList = true;

    [ObservableProperty]
    private bool isViewModeDetails;

    [ObservableProperty]
    private bool isViewModeTiles;

    [ObservableProperty]
    private bool isViewModeIcons;

    [ObservableProperty]
    private bool isLanguageChinese = LocalizationService.CurrentLanguage == LocalizationService.ChineseSimplified;

    [ObservableProperty]
    private bool isLanguageEnglish = LocalizationService.CurrentLanguage == LocalizationService.English;

    /// <summary>由视图层提供删除确认对话框；返回 false 时不执行删除。</summary>
    public Func<int, bool>? ConfirmDelete { get; set; }

    public event EventHandler<string>? ShowMessageBox;

    public bool CanDelete => HasMatchResults && !IsDeleting && !IsScanning;

    public bool CanStart => !IsScanning && !IsDeleting;

    public bool ShowEmptyState => !HasMatchResults;

    partial void OnHasMatchResultsChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    partial void OnIsDeletingChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanStart));
    }

    partial void OnIsScanningChanged(bool value)
    {
        OnPropertyChanged(nameof(CanDelete));
        OnPropertyChanged(nameof(CanStart));
        OnPropertyChanged(nameof(ShowEmptyState));
    }

    partial void OnIsLanguageChineseChanged(bool value)
    {
        if (value && !_syncingLanguage)
            SwitchLanguage(LocalizationService.ChineseSimplified);
    }

    partial void OnIsLanguageEnglishChanged(bool value)
    {
        if (value && !_syncingLanguage)
            SwitchLanguage(LocalizationService.English);
    }

    private void SwitchLanguage(string language)
    {
        if (string.Equals(LocalizationService.CurrentLanguage, language,
                StringComparison.OrdinalIgnoreCase))
            return;

        LocalizationService.SetLanguage(language);
    }

    private void OnLanguageChanged(object? sender, EventArgs e)
    {
        _syncingLanguage = true;
        try
        {
            IsLanguageChinese = LocalizationService.CurrentLanguage == LocalizationService.ChineseSimplified;
            IsLanguageEnglish = LocalizationService.CurrentLanguage == LocalizationService.English;
        }
        finally
        {
            _syncingLanguage = false;
        }

        RefreshLocalizedTexts();
    }

    private void RefreshLocalizedTexts()
    {
        StatusMessage = LocalizationService.Format(_statusKey, _statusArgs);
        EmptyStateTitle = LocalizationService.T(_emptyTitleKey);
        EmptyStateSubtitle = LocalizationService.T(_emptySubtitleKey);

        foreach (var path in APaths.Concat(BPaths))
            path.RefreshLocalizedText();
    }

    private void SetStatus(string key, params object?[] args)
    {
        _statusKey = key;
        _statusArgs = args;
        StatusMessage = LocalizationService.Format(key, args);
    }

    #region 添加/移除路径

    [RelayCommand]
    private void AddFilesToA()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = LocalizationService.T("Dialog_AllFilesFilter"),
            Title = LocalizationService.T("Dialog_SelectFilesA")
        };

        if (dialog.ShowDialog() != true)
            return;

        if (AddFiles(dialog.FileNames, APaths))
            OnPathsChanged("Status_FileAdded");
    }

    [RelayCommand]
    private async Task AddFoldersToAAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = LocalizationService.T("Dialog_SelectFolderA")
        };

        if (dialog.ShowDialog() == true)
            await AddFolderAsync(dialog.FolderName, APaths);
    }

    [RelayCommand]
    private void AddFilesToB()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog
        {
            Multiselect = true,
            Filter = LocalizationService.T("Dialog_AllFilesFilter"),
            Title = LocalizationService.T("Dialog_SelectFilesB")
        };

        if (dialog.ShowDialog() != true)
            return;

        if (AddFiles(dialog.FileNames, BPaths))
            OnPathsChanged("Status_FileAdded");
    }

    [RelayCommand]
    private async Task AddFoldersToBAsync()
    {
        var dialog = new Microsoft.Win32.OpenFolderDialog
        {
            Title = LocalizationService.T("Dialog_SelectFolderB")
        };

        if (dialog.ShowDialog() == true)
            await AddFolderAsync(dialog.FolderName, BPaths);
    }

    private static bool AddFiles(IEnumerable<string> files, ObservableCollection<AddedPath> target)
    {
        var existing = new HashSet<string>(
            target.Select(p => p.FullPath), StringComparer.OrdinalIgnoreCase);
        var added = false;

        foreach (var file in files)
        {
            string fullPath;
            try
            {
                fullPath = PathValidationService.NormalizePath(file);
            }
            catch
            {
                continue;
            }

            if (!File.Exists(fullPath) || !existing.Add(fullPath))
                continue;

            target.Add(new AddedPath
            {
                FullPath = fullPath,
                IsDirectory = false,
                FileCount = 1
            });
            added = true;
        }

        return added;
    }

    private async Task AddFolderAsync(string folder, ObservableCollection<AddedPath> target)
    {
        string fullPath;
        try
        {
            fullPath = PathValidationService.NormalizePath(folder);
        }
        catch
        {
            return;
        }

        if (!Directory.Exists(fullPath) ||
            target.Any(p => string.Equals(p.FullPath, fullPath, StringComparison.OrdinalIgnoreCase)))
        {
            return;
        }

        var item = new AddedPath
        {
            FullPath = fullPath,
            IsDirectory = true,
            FileCount = 0,
            IsCounting = true
        };
        target.Add(item);
        OnPathsChanged("Status_FolderAddedCounting");

        item.FileCount = await Task.Run(() => CountFiles(fullPath));
        item.IsCounting = false;
        UpdateStats();

        if (_statusKey == "Status_FolderAddedCounting")
            SetStatus("Status_Ready");
    }

    [RelayCommand]
    private void RemovePathA(AddedPath? path)
    {
        if (path != null && APaths.Remove(path))
            OnPathsChanged("Status_PathRemoved");
    }

    [RelayCommand]
    private void RemovePathB(AddedPath? path)
    {
        if (path != null && BPaths.Remove(path))
            OnPathsChanged("Status_PathRemoved");
    }

    public void HandleDropA(string[] paths)
    {
        _ = AddDroppedPathsAsync(paths, APaths);
    }

    public void HandleDropB(string[] paths)
    {
        _ = AddDroppedPathsAsync(paths, BPaths);
    }

    private async Task AddDroppedPathsAsync(string[] paths, ObservableCollection<AddedPath> target)
    {
        try
        {
            var existing = new HashSet<string>(
                target.Select(p => p.FullPath), StringComparer.OrdinalIgnoreCase);
            var folders = new List<AddedPath>();
            var added = false;

            foreach (var rawPath in paths)
            {
                string fullPath;
                try
                {
                    fullPath = PathValidationService.NormalizePath(rawPath);
                }
                catch
                {
                    continue;
                }

                if (!existing.Add(fullPath))
                    continue;

                if (File.Exists(fullPath))
                {
                    target.Add(new AddedPath
                    {
                        FullPath = fullPath,
                        IsDirectory = false,
                        FileCount = 1
                    });
                    added = true;
                }
                else if (Directory.Exists(fullPath))
                {
                    var item = new AddedPath
                    {
                        FullPath = fullPath,
                        IsDirectory = true,
                        FileCount = 0,
                        IsCounting = true
                    };
                    target.Add(item);
                    folders.Add(item);
                    added = true;
                }
            }

            if (!added)
                return;

            OnPathsChanged("Status_PathsAddedCounting");

            await Task.WhenAll(folders.Select(async item =>
            {
                item.FileCount = await Task.Run(() => CountFiles(item.FullPath));
                item.IsCounting = false;
            }));

            UpdateStats();

            if (_statusKey == "Status_PathsAddedCounting")
                SetStatus("Status_Ready");
        }
        catch (Exception ex)
        {
            LogService.Error("添加拖放路径失败", ex);
        }
    }

    private static int CountFiles(string path)
    {
        try
        {
            var options = new EnumerationOptions
            {
                RecurseSubdirectories = true,
                IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint,
                ReturnSpecialDirectories = false
            };

            return Directory.EnumerateFiles(path, "*", options).Count();
        }
        catch
        {
            return -1;
        }
    }

    private void OnPathsChanged(string statusKey)
    {
        _pathVersion++;

        if (IsScanning)
            _scanCts?.Cancel();

        ClearResults();
        _statsFromScan = false;
        UpdateStats();
        SetStatus(statusKey);

        if (APaths.Count == 0 || BPaths.Count == 0)
            SetEmptyState("Empty_NoResultTitle", "Empty_NoResultSubtitle");
        else
            SetEmptyState("Empty_PathsChangedTitle", "Empty_PathsChangedSubtitle");
    }

    private void SetEmptyState(string titleKey, string subtitleKey)
    {
        _emptyTitleKey = titleKey;
        _emptySubtitleKey = subtitleKey;
        EmptyStateTitle = LocalizationService.T(titleKey);
        EmptyStateSubtitle = LocalizationService.T(subtitleKey);
    }

    #endregion

    #region 扫描

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task StartScanAsync()
    {
        if (IsScanning || IsDeleting)
            return;

        if (APaths.Count == 0 || BPaths.Count == 0)
        {
            ShowMessageBox?.Invoke(this, LocalizationService.T("Message_AddBothPaths"));
            return;
        }

        var aPaths = APaths.Select(p => p.FullPath).ToList();
        var bPaths = BPaths.Select(p => p.FullPath).ToList();

        var (hasConflict, conflictMessage) = PathValidationService.CheckPathConflict(aPaths, bPaths);
        if (hasConflict)
        {
            SetStatus("Status_PathConflict");
            SetEmptyState("Empty_PathConflictTitle", "Empty_PathConflictSubtitle");
            ShowMessageBox?.Invoke(this, conflictMessage);
            return;
        }

        var version = _pathVersion;
        ClearResults();
        _statsFromScan = false;
        UpdateStats();

        IsScanning = true;
        _acceptProgress = true;
        ScanProgressValue = 0;
        SetStatus("Status_ScanningA");
        SetEmptyState("Empty_ScanningTitle", "Empty_ScanningSubtitle");

        _scanCts = new CancellationTokenSource();
        var token = _scanCts.Token;
        var totalRoots = aPaths.Count + bPaths.Count;

        try
        {
            var aProgress = new Progress<ScanProgress>(p =>
                UpdateScanProgress(p, version, "A", 0, totalRoots));

            var aFiles = await FileScanService.ScanPathsAsync(aPaths, aProgress, token);
            token.ThrowIfCancellationRequested();
            ScanProgressValue = Math.Min(100, aPaths.Count * 100.0 / Math.Max(1, totalRoots));

            SetStatus("Status_ScanningB");
            var bProgress = new Progress<ScanProgress>(p =>
                UpdateScanProgress(p, version, "B", aPaths.Count, totalRoots));

            var bFiles = await FileScanService.ScanPathsAsync(bPaths, bProgress, token);
            token.ThrowIfCancellationRequested();

            _acceptProgress = false;

            if (version != _pathVersion)
            {
                SetStatus("Status_PathChanged");
                return;
            }

            TotalAFiles = aFiles.Count;
            TotalBFiles = bFiles.Count;
            _statsFromScan = true;

            var matches = FileMatchService.FindMatches(aFiles, bFiles);
            foreach (var match in matches)
            {
                match.PropertyChanged += OnMatchPropertyChanged;
                MatchResults.Add(match);
            }

            TotalMatches = matches.Count;
            SelectedCount = matches.Count;
            HasMatchResults = matches.Count > 0;

            _aRootsSnapshot = aPaths;
            _bRootsSnapshot = bPaths;
            _allowedBFiles = new HashSet<string>(
                bFiles.Select(f => f.FullPath), StringComparer.OrdinalIgnoreCase);

            ScanProgressValue = 100;
            SetStatus("Status_ScanCompleteFormat", aFiles.Count, bFiles.Count, matches.Count);

            if (matches.Count == 0)
            {
                SetEmptyState("Empty_NoMatchesTitle", "Empty_NoMatchesSubtitle");
            }
            else
            {
                SetEmptyState("Empty_NoResultTitle", "Empty_SelectResultSubtitle");
            }
        }
        catch (OperationCanceledException)
        {
            _acceptProgress = false;
            if (version != _pathVersion)
            {
                SetStatus("Status_PathChanged");
                SetEmptyState("Empty_PathsChangedTitle", "Empty_PathsChangedSubtitle");
            }
            else
            {
                SetStatus("Status_ScanCancelled");
                SetEmptyState("Empty_ScanCancelledTitle", "Empty_ScanCancelledSubtitle");
            }

            _statsFromScan = false;
            UpdateStats();
        }
        catch (Exception ex)
        {
            _acceptProgress = false;
            LogService.Error("扫描失败", ex);
            SetStatus("Status_ScanError");
            SetEmptyState("Empty_ScanErrorTitle", "Empty_ScanErrorSubtitle");
            _statsFromScan = false;
            UpdateStats();
            ShowMessageBox?.Invoke(this,
                LocalizationService.Format("Message_ScanErrorFormat", ex.Message, LogService.LogFilePath));
        }
        finally
        {
            _acceptProgress = false;
            IsScanning = false;
            _scanCts?.Dispose();
            _scanCts = null;
        }
    }

    private void UpdateScanProgress(
        ScanProgress progress, int version, string side, int processedBefore, int totalRoots)
    {
        if (!_acceptProgress || version != _pathVersion)
            return;

        SetStatus("Status_ScanProgressFormat", side, progress.FilesFound);

        if (progress.TotalPaths > 0 && totalRoots > 0)
        {
            ScanProgressValue = Math.Min(
                100,
                (processedBefore + progress.ProcessedPaths) * 100.0 / totalRoots);
        }
    }

    [RelayCommand]
    private void CancelScan()
    {
        if (!IsScanning)
            return;

        SetStatus("Status_ScanCancelling");
        _scanCts?.Cancel();
    }

    #endregion

    #region 选择与删除

    [RelayCommand]
    private void SelectAll()
    {
        SetSelection(_ => true);
    }

    [RelayCommand]
    private void DeselectAll()
    {
        SetSelection(_ => false);
    }

    [RelayCommand]
    private void InvertSelection()
    {
        SetSelection(match => !match.IsSelected);
    }

    private void SetSelection(Func<MatchResult, bool> selector)
    {
        _suppressSelectionCount = true;
        try
        {
            foreach (var match in MatchResults)
                match.IsSelected = selector(match);
        }
        finally
        {
            _suppressSelectionCount = false;
        }

        UpdateSelectedCount();
    }

    private void OnMatchPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (_suppressSelectionCount)
            return;

        if (e.PropertyName == nameof(MatchResult.IsSelected))
            UpdateSelectedCount();
    }

    private void UpdateSelectedCount()
    {
        SelectedCount = MatchResults.Count(r => r.IsSelected);
    }

    [RelayCommand(AllowConcurrentExecutions = false)]
    private async Task DeleteSelectedAsync()
    {
        if (IsDeleting || IsScanning)
            return;

        var selected = MatchResults.Where(r => r.IsSelected).ToList();
        if (selected.Count == 0)
        {
            ShowMessageBox?.Invoke(this, LocalizationService.T("Message_NoSelection"));
            return;
        }

        var toDelete = selected
            .SelectMany(r => r.BFiles)
            .Select(f => f.FullPath)
            .Where(p => !string.IsNullOrWhiteSpace(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (toDelete.Count == 0)
            return;

        if (_aRootsSnapshot == null || _bRootsSnapshot == null || _allowedBFiles == null)
        {
            ShowMessageBox?.Invoke(this, LocalizationService.T("Message_ResultsExpired"));
            return;
        }

        if (ConfirmDelete == null || !ConfirmDelete(toDelete.Count))
            return;

        var guard = DeletionGuardService.Create(_aRootsSnapshot, _bRootsSnapshot, _allowedBFiles);

        IsDeleting = true;
        SetStatus("Status_DeletingStartFormat", toDelete.Count);

        try
        {
            var progress = new Progress<(int current, int total, string currentFile)>(p =>
            {
                if (IsDeleting)
                {
                    SetStatus("Status_DeletingProgressFormat",
                        p.current, p.total, Path.GetFileName(p.currentFile));
                }
            });

            var result = await RecycleBinService.DeleteFilesAsync(toDelete, guard, progress);

            foreach (var item in MatchResults.ToList())
            {
                var remaining = item.BFiles.Where(f => File.Exists(f.FullPath)).ToList();
                if (remaining.Count != item.BFiles.Count)
                    item.BFiles = remaining;

                if (remaining.Count > 0)
                    continue;

                item.PropertyChanged -= OnMatchPropertyChanged;
                MatchResults.Remove(item);
            }

            TotalBFiles = Math.Max(0, TotalBFiles - result.SuccessCount);
            TotalMatches = MatchResults.Count;
            UpdateSelectedCount();
            HasMatchResults = MatchResults.Count > 0;

            if (HasMatchResults)
                SetStatus("Status_DeleteCompleteFormat", result.SuccessCount, result.FailCount);
            else
                SetStatus("Status_NoMatches");

            if (!HasMatchResults)
                SetEmptyState("Empty_DeletedTitle", "Empty_DeletedSubtitle");

            _ = RefreshBPathCountsAsync(_pathVersion);

            var summary = LocalizationService.Format(
                "Status_DeleteCompleteFormat", result.SuccessCount, result.FailCount);
            if (result.FailCount > 0)
            {
                var details = result.FailedFiles
                    .Zip(result.FailedReasons, (file, reason) =>
                        LocalizationService.Format("Message_FailureItemFormat", file, reason))
                    .Take(5);
                summary += "\n\n" + LocalizationService.T("Message_FailureDetailsHeader") +
                           "\n" + string.Join("\n", details);
                if (result.FailCount > 5)
                    summary += "\n" + LocalizationService.Format(
                        "Message_MoreFailuresFormat", result.FailCount - 5);
            }

            ShowMessageBox?.Invoke(this, summary);
        }
        catch (Exception ex)
        {
            LogService.Error("删除失败", ex);
            SetStatus("Status_DeleteError");
            ShowMessageBox?.Invoke(this,
                LocalizationService.Format("Message_DeleteErrorFormat", ex.Message, LogService.LogFilePath));
        }
        finally
        {
            IsDeleting = false;
        }
    }

    #endregion

    #region 统计与视图

    private async Task RefreshBPathCountsAsync(int expectedVersion)
    {
        try
        {
            var folders = BPaths.Where(p => p.IsDirectory).ToList();
            var counts = await Task.WhenAll(folders.Select(async item =>
            {
                var count = await Task.Run(() => CountFiles(item.FullPath));
                return Directory.Exists(item.FullPath) ? count : 0;
            }));

            if (_pathVersion != expectedVersion)
                return;

            for (var i = 0; i < folders.Count; i++)
            {
                folders[i].FileCount = counts[i];
                folders[i].IsCounting = false;
            }

            foreach (var fileItem in BPaths
                         .Where(p => !p.IsDirectory && !File.Exists(p.FullPath))
                         .ToList())
            {
                BPaths.Remove(fileItem);
            }
        }
        catch (Exception ex)
        {
            LogService.Error("刷新 B 路径文件数失败", ex);
        }
    }

    private void UpdateStats()
    {
        if (_statsFromScan)
            return;

        TotalAFiles = SumFileCounts(APaths);
        TotalBFiles = SumFileCounts(BPaths);
    }

    private static int SumFileCounts(IEnumerable<AddedPath> paths)
    {
        long total = 0;
        foreach (var path in paths)
        {
            var count = path.IsDirectory ? Math.Max(0, path.FileCount) : 1;
            total = Math.Min(int.MaxValue, total + count);
        }
        return (int)total;
    }

    private void ClearResults()
    {
        foreach (var match in MatchResults)
            match.PropertyChanged -= OnMatchPropertyChanged;

        MatchResults.Clear();
        HasMatchResults = false;
        TotalMatches = 0;
        SelectedCount = 0;

        _aRootsSnapshot = null;
        _bRootsSnapshot = null;
        _allowedBFiles = null;
    }

    [RelayCommand]
    private void SetViewMode(string? mode)
    {
        IsViewModeList = mode == "List";
        IsViewModeDetails = mode == "Details";
        IsViewModeTiles = mode == "Tiles";
        IsViewModeIcons = mode == "Icons";
    }

    #endregion
}

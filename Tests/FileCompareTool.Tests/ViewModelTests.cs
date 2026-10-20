using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.Input;
using FileCompareTool.Models;
using FileCompareTool.ViewModels;

namespace FileCompareTool.Tests;

internal static class ViewModelTests
{
    public static async Task EndToEndScanSelectionAndSafeDelete()
    {
        using var temp = new TempDirectory();
        var aRoot = temp.CreateDirectory("A");
        var bRoot = temp.CreateDirectory("B");

        temp.CreateFile("A/image_001.jpg");
        temp.CreateFile("A/sub1/duplicate.jpg");
        temp.CreateFile("A/sub2/duplicate.jpg");
        temp.CreateFile("A/nomatch.png");

        temp.CreateFile("B/image_001.jpg");
        temp.CreateFile("B/sub1/duplicate.jpg");
        temp.CreateFile("B/sub2/duplicate.jpg");
        temp.CreateFile("B/sub3/duplicate.jpg");
        temp.CreateFile("B/sub1/duplicate_extra.jpg");
        temp.CreateFile("B/nomatch.jpg");
        temp.CreateFile("B/test.jpg");

        var messages = new List<string>();
        var vm = new MainViewModel();
        vm.ShowMessageBox += (_, message) => messages.Add(message);
        vm.APaths.Add(new AddedPath { FullPath = aRoot, IsDirectory = true, FileCount = 4 });
        vm.BPaths.Add(new AddedPath { FullPath = bRoot, IsDirectory = true, FileCount = 7 });

        await ExecuteAsync(vm.StartScanCommand);

        Assert.False(vm.IsScanning, "扫描结束后 IsScanning 应为 false");
        Assert.True(vm.HasMatchResults, "应显示匹配结果");
        Assert.Equal(2, vm.TotalMatches, "应有 2 个匹配的文件名");
        Assert.Equal(4, vm.TotalAFiles, "A 区应有 4 个文件");
        Assert.Equal(7, vm.TotalBFiles, "B 区应有 7 个文件");
        Assert.Equal(2, vm.SelectedCount, "默认应全选");
        Assert.True(vm.StatusMessage.Contains("完成"), "状态栏应显示完成信息");
        Assert.Equal(0, messages.Count, "正常扫描不应弹出错误消息");

        var duplicate = vm.MatchResults.Single(m => m.FileName == "duplicate.jpg");
        Assert.Equal(2, duplicate.ACount, "duplicate.jpg 在 A 中应有 2 个");
        Assert.Equal(3, duplicate.BCount, "duplicate.jpg 在 B 中应有 3 个");

        // 勾选框变化应实时更新已选数量
        duplicate.IsSelected = false;
        Assert.Equal(1, vm.SelectedCount, "取消勾选后已选数量应更新");
        vm.SelectAllCommand.Execute(null);
        Assert.Equal(2, vm.SelectedCount, "全选后已选数量应为 2");
        vm.InvertSelectionCommand.Execute(null);
        Assert.Equal(0, vm.SelectedCount, "反选后已选数量应为 0");
        vm.SelectAllCommand.Execute(null);

        // 视图模式切换
        vm.SetViewModeCommand.Execute("Tiles");
        Assert.True(vm.IsViewModeTiles, "应切换到平铺视图");
        vm.SetViewModeCommand.Execute("List");
        Assert.True(vm.IsViewModeList, "应切换回列表视图");

        // 用户取消确认时不应删除任何文件
        var confirmCalled = false;
        vm.ConfirmDelete = _ => { confirmCalled = true; return false; };
        await ExecuteAsync(vm.DeleteSelectedCommand);

        Assert.True(confirmCalled, "应弹出删除确认");
        Assert.True(File.Exists(Path.Combine(bRoot, "image_001.jpg")), "取消删除后文件必须存在");
        Assert.Equal(2, vm.TotalMatches, "取消删除后匹配项不应变化");
        Assert.False(vm.CanDelete == false, "仍有匹配时应允许删除");

        // 修改路径后旧结果应失效，避免误删
        vm.RemovePathACommand.Execute(vm.APaths[0]);
        Assert.Equal(0, vm.MatchResults.Count, "路径变化后应清空旧结果");
        Assert.False(vm.HasMatchResults, "路径变化后不应再显示结果");
    }

    public static async Task NoMatchesShowsFriendlyEmptyState()
    {
        using var temp = new TempDirectory();
        var aRoot = temp.CreateDirectory("A");
        var bRoot = temp.CreateDirectory("B");
        temp.CreateFile("A/alpha.txt");
        temp.CreateFile("B/beta.txt");

        var vm = new MainViewModel();
        vm.APaths.Add(new AddedPath { FullPath = aRoot, IsDirectory = true });
        vm.BPaths.Add(new AddedPath { FullPath = bRoot, IsDirectory = true });

        await ExecuteAsync(vm.StartScanCommand);

        Assert.False(vm.HasMatchResults, "没有同名文件时不应显示结果");
        Assert.True(vm.ShowEmptyState, "没有结果时应显示空状态");
        Assert.Equal("没有找到同名文件", vm.EmptyStateTitle, "空状态标题应明确说明没有匹配");

        vm.RemovePathACommand.Execute(vm.APaths[0]);
        Assert.True(vm.EmptyStateSubtitle.Contains("添加"), "清空一侧路径后应提示重新添加路径");
    }

    public static Task LanguageSwitchRefreshesVisibleState()
    {
        var original = FileCompareTool.Services.LocalizationService.CurrentLanguage;
        try
        {
            FileCompareTool.Services.LocalizationService.SetLanguage(
                FileCompareTool.Services.LocalizationService.ChineseSimplified, persist: false);

            var vm = new MainViewModel();
            var folder = new AddedPath
            {
                FullPath = @"C:\Demo",
                IsDirectory = true,
                FileCount = 3
            };
            vm.APaths.Add(folder);

            vm.IsLanguageEnglish = true;

            Assert.Equal("Ready", vm.StatusMessage, "切换英语后状态栏应立即更新");
            Assert.Equal("No results yet", vm.EmptyStateTitle, "切换英语后空状态应立即更新");
            Assert.Equal("3 files", folder.SubText, "切换英语后路径文件数应立即更新");

            vm.IsLanguageChinese = true;

            Assert.Equal("就绪", vm.StatusMessage, "切回中文后状态栏应立即更新");
            Assert.Equal("还没有匹配结果", vm.EmptyStateTitle, "切回中文后空状态应立即更新");
            Assert.Equal("3 个文件", folder.SubText, "切回中文后路径文件数应立即更新");
        }
        finally
        {
            FileCompareTool.Services.LocalizationService.SetLanguage(original, persist: false);
        }

        return Task.CompletedTask;
    }

    public static async Task ScanRejectsConflictingPaths()
    {
        using var temp = new TempDirectory();
        var root = temp.CreateDirectory("same");
        temp.CreateFile("same/a.txt");

        var messages = new List<string>();
        var vm = new MainViewModel();
        vm.ShowMessageBox += (_, message) => messages.Add(message);
        vm.APaths.Add(new AddedPath { FullPath = root, IsDirectory = true });
        vm.BPaths.Add(new AddedPath { FullPath = root, IsDirectory = true });

        await ExecuteAsync(vm.StartScanCommand);

        Assert.False(vm.IsScanning, "冲突检查后不应停留在扫描状态");
        Assert.Equal(0, vm.MatchResults.Count, "路径冲突时不应产生结果");
        Assert.Equal(1, messages.Count, "路径冲突时应提示用户");
        Assert.True(messages[0].Contains("冲突"), "提示信息应说明路径冲突");
    }

    public static async Task ScanCancelKeepsViewModelConsistent()
    {
        using var temp = new TempDirectory();
        var aRoot = temp.CreateDirectory("A");
        var bRoot = temp.CreateDirectory("B");
        for (var i = 0; i < 300; i++)
        {
            temp.CreateFile($"A/file_{i:D4}.txt");
            temp.CreateFile($"B/file_{i:D4}.txt");
        }

        var vm = new MainViewModel();
        vm.APaths.Add(new AddedPath { FullPath = aRoot, IsDirectory = true });
        vm.BPaths.Add(new AddedPath { FullPath = bRoot, IsDirectory = true });

        var task = ExecuteAsync(vm.StartScanCommand);
        vm.CancelScanCommand.Execute(null);
        await task;

        Assert.False(vm.IsScanning, "取消后 IsScanning 应为 false");
        Assert.False(vm.IsDeleting, "取消扫描不应影响删除状态");
    }

    private static async Task ExecuteAsync(IAsyncRelayCommand command)
    {
        await command.ExecuteAsync(null);
    }
}
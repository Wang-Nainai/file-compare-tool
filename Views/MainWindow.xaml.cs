using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FileCompareTool.Models;
using FileCompareTool.Services;
using FileCompareTool.ViewModels;

namespace FileCompareTool.Views;

public partial class MainWindow : Window
{
    private Brush? _originalBorderBrush;
    private Brush? _originalBackground;

    public MainWindow()
    {
        InitializeComponent();
        Loaded += MainWindow_Loaded;
    }

    private void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm)
            return;

        vm.ShowMessageBox -= OnShowMessageBox;
        vm.ShowMessageBox += OnShowMessageBox;
        vm.ConfirmDelete = ShowDeleteConfirmation;
    }

    private void OnShowMessageBox(object? sender, string message)
    {
        MessageBox.Show(this, message, LocalizationService.T("App_Title"),
            MessageBoxButton.OK, MessageBoxImage.Information);
    }

    private bool ShowDeleteConfirmation(int fileCount)
    {
        var dialog = new ConfirmDeleteWindow(fileCount) { Owner = this };
        return dialog.ShowDialog() == true;
    }

    #region Drag & Drop

    private void OnDragEnterA(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var border = (System.Windows.Controls.Border)sender;
            _originalBorderBrush = border.BorderBrush;
            _originalBackground = border.Background;
            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F7FF"));
            e.Handled = true;
        }
    }

    private void OnDragLeaveA(object sender, DragEventArgs e)
    {
        var border = (System.Windows.Controls.Border)sender;
        border.BorderBrush = _originalBorderBrush;
        border.Background = _originalBackground;
    }

    private void OnDragEnterB(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var border = (System.Windows.Controls.Border)sender;
            _originalBorderBrush = border.BorderBrush;
            _originalBackground = border.Background;
            border.BorderBrush = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
            border.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F0F7FF"));
            e.Handled = true;
        }
    }

    private void OnDragLeaveB(object sender, DragEventArgs e)
    {
        var border = (System.Windows.Controls.Border)sender;
        border.BorderBrush = _originalBorderBrush;
        border.Background = _originalBackground;
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop) ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnDropA(object sender, DragEventArgs e)
    {
        ResetBorder(sender);
        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] paths &&
            DataContext is MainViewModel vm)
            vm.HandleDropA(paths);
        e.Handled = true;
    }

    private void OnDropB(object sender, DragEventArgs e)
    {
        ResetBorder(sender);
        if (e.Data.GetDataPresent(DataFormats.FileDrop) &&
            e.Data.GetData(DataFormats.FileDrop) is string[] paths &&
            DataContext is MainViewModel vm)
            vm.HandleDropB(paths);
        e.Handled = true;
    }

    private void ResetBorder(object sender)
    {
        var border = (System.Windows.Controls.Border)sender;
        if (_originalBorderBrush != null) border.BorderBrush = _originalBorderBrush;
        if (_originalBackground != null) border.Background = _originalBackground;
    }

    #endregion

    #region Match Result Interaction

    private void OnMatchDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var match = GetMatch(sender);
        if (match == null) return;

        if (match.BFiles.Count > 0)
            OpenFile(match.BFiles[0].FullPath);
        else if (match.AFiles.Count > 0)
            OpenFile(match.AFiles[0].FullPath);
    }

    private void OnMatchRightClick(object sender, MouseButtonEventArgs e)
    {
        var match = GetMatch(sender);
        if (match == null) return;

        var menu = new ContextMenu();
        var detailItem = new MenuItem { Header = LocalizationService.T("Context_ViewDetails"), Tag = match };
        detailItem.Click += (_, _) => ShowDetail(match);
        var openItem = new MenuItem { Header = LocalizationService.T("Context_OpenDefault"), Tag = match };
        openItem.Click += (_, _) => OpenMatchFile(match);
        menu.Items.Add(detailItem);
        menu.Items.Add(new Separator());
        menu.Items.Add(openItem);
        menu.IsOpen = true;
        e.Handled = true;
    }

    private void ShowDetail(MatchResult match)
    {
        var win = new MatchDetailWindow
        {
            FileName = match.FileName,
            Title = LocalizationService.Format("Detail_WindowTitleFormat", match.FileName),
            ACount = match.ACount,
            BCount = match.BCount,
            AFiles = match.AFiles,
            BFiles = match.BFiles,
            Owner = this
        };
        win.ShowDialog();
    }

    private void OpenMatchFile(MatchResult match)
    {
        if (match.BFiles.Count > 0)
            OpenFile(match.BFiles[0].FullPath);
        else if (match.AFiles.Count > 0)
            OpenFile(match.AFiles[0].FullPath);
    }

    private MatchResult? GetMatch(object sender)
    {
        DependencyObject? item = null;
        Point pos = new();

        if (sender is ListBox listBox)
        {
            pos = Mouse.GetPosition(listBox);
            item = listBox.InputHitTest(pos) as DependencyObject;
        }

        while (item != null)
        {
            if (item is FrameworkElement element && element.DataContext is MatchResult match)
                return match;
            item = VisualTreeHelper.GetParent(item);
        }
        return null;
    }

    #endregion

    private static void OpenFile(string path)
    {
        try
        {
            if (File.Exists(path))
                Process.Start(new ProcessStartInfo { FileName = path, UseShellExecute = true });
            else
                MessageBox.Show(
                    LocalizationService.Format("Error_FileMissingFormat", path),
                    LocalizationService.T("Dialog_HintTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                LocalizationService.Format("Error_CannotOpenFormat", ex.Message),
                LocalizationService.T("Dialog_ErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

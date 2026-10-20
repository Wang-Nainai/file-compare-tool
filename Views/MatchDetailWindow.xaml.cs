using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using FileCompareTool.Models;
using FileCompareTool.Services;

namespace FileCompareTool.Views;

public partial class MatchDetailWindow : Window
{
    public string FileName { get; set; } = string.Empty;
    public int ACount { get; set; }
    public int BCount { get; set; }
    public System.Collections.Generic.List<FileInfoItem> AFiles { get; set; } = new();
    public System.Collections.Generic.List<FileInfoItem> BFiles { get; set; } = new();

    public MatchDetailWindow()
    {
        InitializeComponent();
    }

    // 双击 → 打开文件
    private void OnFileDoubleClick(object sender, MouseButtonEventArgs e)
    {
        var file = GetFile(sender, e);
        if (file != null)
            OpenFile(file.FullPath);
    }

    // 右键 → 显示文件信息
    private void OnFileRightClick(object sender, MouseButtonEventArgs e)
    {
        var file = GetFile(sender, e);
        if (file == null)
            return;

        if (sender is ListBox listBox)
            listBox.SelectedItem = file;

        var info = LocalizationService.Format(
            "Detail_InfoFormat",
            file.FileName,
            file.FullPath,
            file.DirectoryName,
            file.Extension);
        MessageBox.Show(this, info, LocalizationService.T("Detail_FileInfoTitle"),
            MessageBoxButton.OK, MessageBoxImage.Information);
        e.Handled = true;
    }

    private static FileInfoItem? GetFile(object sender, MouseButtonEventArgs e)
    {
        if (sender is not ListBox listBox)
            return null;

        var item = listBox.InputHitTest(e.GetPosition(listBox)) as DependencyObject;
        while (item != null)
        {
            if (item is FrameworkElement element && element.DataContext is FileInfoItem file)
                return file;
            item = VisualTreeHelper.GetParent(item);
        }

        return null;
    }

    private static void OpenFile(string filePath)
    {
        try
        {
            if (System.IO.File.Exists(filePath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = filePath,
                    UseShellExecute = true
                });
            }
            else
            {
                MessageBox.Show(
                    LocalizationService.Format("Error_FileMissingFormat", filePath),
                    LocalizationService.T("Dialog_HintTitle"),
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show(
                LocalizationService.Format("Error_CannotOpenFileFormat", ex.Message),
                LocalizationService.T("Dialog_ErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}

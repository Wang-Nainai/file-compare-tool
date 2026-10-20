using System.Windows;

namespace FileCompareTool.Views;

public partial class ConfirmDeleteWindow : Window
{
    public int FileCount { get; }

    public ConfirmDeleteWindow()
        : this(0)
    {
    }

    public ConfirmDeleteWindow(int fileCount)
    {
        FileCount = fileCount;
        InitializeComponent();
    }

    private void OnConfirmClick(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }
}

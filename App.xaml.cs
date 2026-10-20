using System;
using System.Threading.Tasks;
using System.Windows;
using FileCompareTool.Services;

namespace FileCompareTool;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        LocalizationService.Initialize();
        base.OnStartup(e);

        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
        {
            if (args.ExceptionObject is Exception ex)
                LogService.Error("UnhandledException", ex);
        };

        DispatcherUnhandledException += (_, args) =>
        {
            LogService.Error("DispatcherUnhandledException", args.Exception);
            args.Handled = true;
            MessageBox.Show(
                LocalizationService.Format(
                    "Error_UnexpectedFormat",
                    args.Exception.Message,
                    LogService.LogFilePath),
                LocalizationService.T("Dialog_ErrorTitle"),
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        };

        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogService.Error("UnobservedTaskException", args.Exception);
            args.SetObserved();
        };
    }
}

using System.Configuration;
using System.Data;
using System.Windows;

namespace LiveTranslator;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        this.DispatcherUnhandledException += (s, args) =>
        {
            System.Windows.MessageBox.Show($"Unhandled Dispatcher Exception: {args.Exception.Message}\n\n{args.Exception.StackTrace}", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            args.Handled = true;
        };

        AppDomain.CurrentDomain.UnhandledException += (s, args) =>
        {
            var ex = args.ExceptionObject as Exception;
            System.Windows.MessageBox.Show($"Fatal Exception: {ex?.Message}\n\n{ex?.StackTrace}", "Fatal Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
        };
    }
}


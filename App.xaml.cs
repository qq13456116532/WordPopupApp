using System.Configuration;
using System.Data;
using System.Windows;

namespace WordPopupApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    // App.xaml.cs
public partial class App : Application
{
    public App()
    {
        DispatcherUnhandledException += (_, e) =>
        {
            MessageBox.Show($"UI线程未处理异常：{e.Exception}");
            e.Handled = true;           // 防止默认崩溃
        };

        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception;
            MessageBox.Show($"非UI线程未处理异常：{ex?.Message}");
        };
    }
}


}

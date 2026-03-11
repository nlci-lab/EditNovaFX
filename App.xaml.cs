using System.Windows;
using VideoEditor.Views;

namespace VideoEditor
{
    public partial class App : Application
    {
        public App()
        {
            this.DispatcherUnhandledException += App_DispatcherUnhandledException;
        }

        private void App_Startup(object sender, StartupEventArgs e)
        {
            // Show the animated splash screen
            var splash = new AppSplashScreen();
            splash.Show();

            // Begin the load animation; when it finishes, open the main window
            splash.BeginLoadSequence(onFinished: () =>
            {
                var mainWindow = new MainWindow();
                mainWindow.Show();

                // Make this the main window so the app exits when it closes
                Application.Current.MainWindow = mainWindow;
            });
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            MessageBox.Show($"An unhandled exception occurred: {e.Exception.Message}\n\nStack Trace:\n{e.Exception.StackTrace}", "Application Error", MessageBoxButton.OK, MessageBoxImage.Error);
            e.Handled = true;
        }
    }
}

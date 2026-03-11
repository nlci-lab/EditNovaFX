using System.Diagnostics;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using VideoEditor.ViewModels;

namespace VideoEditor.Views
{
    public partial class AudioToSubtitleWindow : Window
    {
        public AudioToSubtitleWindow()
        {
            InitializeComponent();
            DataContext = new AudioToSubtitleViewModel();
        }

        private void ApiKeyBox_PasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is AudioToSubtitleViewModel vm && sender is PasswordBox pb)
            {
                vm.ApiKey = pb.Password;
            }
        }

        /// <summary>
        /// "⬇ Download" button inside the model table — opens the download URL in the user's browser.
        /// </summary>
        private void DownloadModel_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string url && !string.IsNullOrWhiteSpace(url))
            {
                OpenUrl(url);
            }
        }

        /// <summary>
        /// Hyperlink for the whisper-cli.exe GitHub releases page.
        /// </summary>
        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            OpenUrl(e.Uri.AbsoluteUri);
            e.Handled = true;
        }

        private static void OpenUrl(string url)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = url,
                    UseShellExecute = true   // lets Windows choose the default browser
                });
            }
            catch
            {
                MessageBox.Show($"Could not open browser.\nPlease visit:\n{url}",
                                "Open URL", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
    }
}

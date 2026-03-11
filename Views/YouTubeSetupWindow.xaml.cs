using System;
using System.Windows;
using VideoEditor.Services;
using System.Diagnostics;

namespace VideoEditor.Views
{
    public partial class YouTubeSetupWindow : Window
    {
        private readonly YouTubePublishingService _service;

        public YouTubeSetupWindow()
        {
            InitializeComponent();
            _service = new YouTubePublishingService();
            
            if (_service.HasCredentials)
            {
                ClientIdBox.Text = _service.ClientId;
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            string id = ClientIdBox.Text.Trim();
            string secret = ClientSecretBox.Password.Trim();

            if (string.IsNullOrEmpty(id) || string.IsNullOrEmpty(secret))
            {
                MessageBox.Show("Please enter both Client ID and Client Secret.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                _service.SaveCredentials(id, secret);
                MessageBox.Show("YouTube Configuration Saved! You can now login to your channel.", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                this.DialogResult = true;
                this.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving configuration: {ex.Message}", "System Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Help_Click(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://console.cloud.google.com/") { UseShellExecute = true });
        }
    }
}

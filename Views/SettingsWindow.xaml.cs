using System.Windows;
using VideoEditor.ViewModels;

namespace VideoEditor.Views
{
    public partial class SettingsWindow : Window
    {
        public SettingsWindow()
        {
            InitializeComponent();
            DataContext = new SettingsViewModel();
        }
    }
}

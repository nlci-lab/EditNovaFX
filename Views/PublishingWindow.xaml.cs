using System.Windows;
using VideoEditor.Models;
using VideoEditor.ViewModels;

namespace VideoEditor.Views
{
    public partial class PublishingWindow : Window
    {
        public PublishingWindow(Project project)
        {
            InitializeComponent();
            DataContext = new PublishingViewModel(project);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

using System.Windows;
using VideoEditor.Models;
using VideoEditor.ViewModels;

namespace VideoEditor.Views
{
    public partial class ExportWindow : Window
    {
        public ExportSettings? ResultSettings { get; private set; }

        public ExportWindow(Project project)
        {
            InitializeComponent();
            DataContext = new ExportViewModel(project);
        }

        private void Export_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is ExportViewModel vm)
            {
                ResultSettings = vm.GetSettings();
                DialogResult = true;
                Close();
            }
        }
    }
}

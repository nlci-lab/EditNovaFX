using System.Windows;
using System.Windows.Controls;
using VideoEditor.Models;
using VideoEditor.ViewModels;

namespace VideoEditor.Views
{
    public partial class HelpWindow : Window
    {
        public HelpWindow()
        {
            InitializeComponent();
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (DataContext is HelpViewModel vm && sender is TreeViewItem item && item.Header is HelpTopic topic)
            {
                vm.SelectedTopic = topic;
                e.Handled = true; // prevent event from bubbling to parent tree items
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox textBox && !string.IsNullOrWhiteSpace(textBox.Text) && textBox.Text.Length > 1)
            {
                SelectTopic(textBox.Text);
            }
        }

        public void SelectTopic(string topicTitle)
        {
            if (DataContext is HelpViewModel vm)
            {
                vm.SelectTopicByTitle(topicTitle);
            }
        }
    }
}

using System.Windows;

namespace WordPopupApp.Views
{
    public partial class TemplateEditWindow : Window
    {
        public string TemplateText { get; private set; }

        public TemplateEditWindow(string initialTemplate)
        {
            InitializeComponent();
            TemplateBox.Text = initialTemplate;
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            TemplateText = TemplateBox.Text;
            this.DialogResult = true;
        }
    }
}
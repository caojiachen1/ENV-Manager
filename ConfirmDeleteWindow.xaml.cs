using System.Windows;

namespace EnvVarViewer
{
    public partial class ConfirmDeleteWindow : Wpf.Ui.Controls.FluentWindow
    {
        public string VariableName { get; set; }

        public ConfirmDeleteWindow(string variableName)
        {
            InitializeComponent();
            DataContext = this;
            VariableName = variableName;
        }

        private void ConfirmButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
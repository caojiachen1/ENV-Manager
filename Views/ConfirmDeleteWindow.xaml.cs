using System.Windows;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    /// <summary>
    /// Interaction logic for ConfirmDeleteWindow.xaml
    /// </summary>
    public partial class ConfirmDeleteWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly ConfirmDeleteViewModel _viewModel;

        public ConfirmDeleteWindow(string variableName)
        {
            InitializeComponent();
            _viewModel = new ConfirmDeleteViewModel(variableName);
            DataContext = _viewModel;
            _viewModel.CloseWindow = () => this.Close();
        }

        public new bool? ShowDialog()
        {
            base.ShowDialog();
            return _viewModel.DialogResult;
        }
    }
}
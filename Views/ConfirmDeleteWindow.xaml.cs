using System.Windows;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    public partial class ConfirmDeleteWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly ConfirmDeleteViewModel _viewModel;

        public ConfirmDeleteWindow(string variableName)
        {
            InitializeComponent();
            _viewModel = new ConfirmDeleteViewModel(variableName);
            DataContext = _viewModel;
            _viewModel.CloseWindow = Close;

            Closing += (s, e) => DialogResult = _viewModel.DialogResult;
        }
    }
}
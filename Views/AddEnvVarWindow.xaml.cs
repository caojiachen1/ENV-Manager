using System;
using System.Collections.Generic;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    /// <summary>
    /// Interaction logic for AddEnvVarWindow.xaml
    /// </summary>
    public partial class AddEnvVarWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly AddEnvVarViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance for adding environment variables
        /// </summary>
        public AddEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars)
        {
            InitializeComponent();
            _viewModel = new AddEnvVarViewModel(userEnvVars, systemEnvVars);
            DataContext = _viewModel;
            _viewModel.CloseWindow += (s, e) => Close();
        }

        /// <summary>
        /// Event triggered when an environment variable is added
        /// </summary>
        public event EventHandler EnvVarAdded
        {
            add { _viewModel.EnvVarAdded += value; }
            remove { _viewModel.EnvVarAdded -= value; }
        }
    }
}
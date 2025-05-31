using System;
using System.Collections.Generic;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    /// <summary>
    /// Interaction logic for ModifyEnvVarWindow.xaml
    /// </summary>
    public partial class ModifyEnvVarWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly ModifyEnvVarViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance for modifying existing environment variables
        /// </summary>
        public ModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, string name, string value, bool isUserNode)
        {
            InitializeComponent();
            string scope = isUserNode ? "user" : "system";
            _viewModel = new ModifyEnvVarViewModel(userEnvVars, systemEnvVars, name, value, scope);
            DataContext = _viewModel;
            _viewModel.CloseWindow += (s, e) => Close();
        }

        /// <summary>
        /// Event triggered when an environment variable is modified
        /// </summary>
        public event EventHandler EnvVarModified
        {
            add { _viewModel.EnvVarModified += value; }
            remove { _viewModel.EnvVarModified -= value; }
        }
    }
}
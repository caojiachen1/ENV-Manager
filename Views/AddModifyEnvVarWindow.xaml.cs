using System;
using System.Collections.Generic;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    public partial class AddModifyEnvVarWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly AddModifyEnvVarWindowViewModel _viewModel;

        /// <summary>
        /// Initializes a new instance for adding environment variables
        /// </summary>
        public AddModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, Dictionary<string, string> modifiedEnvVars, HashSet<string> deletedEnvVars)
        {
            InitializeComponent();
            _viewModel = new AddModifyEnvVarWindowViewModel(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars);
            DataContext = _viewModel;
            _viewModel.CloseWindow += (s, e) => Close();
        }

        /// <summary>
        /// Initializes a new instance for modifying existing environment variables
        /// </summary>
        public AddModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, Dictionary<string, string> modifiedEnvVars, HashSet<string> deletedEnvVars, string name, string value)
        {
            InitializeComponent();
            _viewModel = new AddModifyEnvVarWindowViewModel(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars, name, value);
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
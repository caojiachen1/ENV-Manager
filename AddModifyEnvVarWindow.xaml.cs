using System;
using System.Collections.Generic;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    public partial class AddModifyEnvVarWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly AddModifyEnvVarWindowViewModel _viewModel;

        /// <summary>
        /// 初始化新实例
        /// </summary>
        public AddModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, Dictionary<string, string> modifiedEnvVars, HashSet<string> deletedEnvVars)
        {
            InitializeComponent();
            _viewModel = new AddModifyEnvVarWindowViewModel(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars);
            DataContext = _viewModel;
            _viewModel.CloseWindow += (s, e) => Close();
        }

        /// <summary>
        /// 初始化用于修改现有环境变量的新实例
        /// </summary>
        public AddModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, Dictionary<string, string> modifiedEnvVars, HashSet<string> deletedEnvVars, string name, string value)
        {
            InitializeComponent();
            _viewModel = new AddModifyEnvVarWindowViewModel(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars, name, value);
            DataContext = _viewModel;
            _viewModel.CloseWindow += (s, e) => Close();
        }

        /// <summary>
        /// 环境变量添加事件
        /// </summary>
        public event EventHandler EnvVarAdded
        {
            add { _viewModel.EnvVarAdded += value; }
            remove { _viewModel.EnvVarAdded -= value; }
        }

        /// <summary>
        /// 环境变量修改事件
        /// </summary>
        public event EventHandler EnvVarModified
        {
            add { _viewModel.EnvVarModified += value; }
            remove { _viewModel.EnvVarModified -= value; }
        }
    }
}
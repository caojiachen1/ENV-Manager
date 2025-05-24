using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    public partial class ModifyPathWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly ModifyPathWindowViewModel _viewModel;

        public ModifyPathWindow(string pathValue, bool isUserPath)
        {
            InitializeComponent();
            _viewModel = new ModifyPathWindowViewModel(pathValue, isUserPath);
            DataContext = _viewModel;
            _viewModel.PathModified += (s, e) => this.Close();
        }

        /// <summary>
        /// Handles the double-click event on the path list to copy the selected path to clipboard
        /// </summary>
        private void OnPathListBoxDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (_viewModel.SelectedPath != null)
            {
                _viewModel.CopyPathCommand.Execute(null);
                e.Handled = true;
            }
        }
    }
}
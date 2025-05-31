using System;
using System.Windows;
using System.Windows.Input;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    /// <summary>
    /// Interaction logic for ModifyPathWindow.xaml
    /// </summary>
    public partial class ModifyPathWindow : Wpf.Ui.Controls.FluentWindow
    {
        public ModifyPathWindow(string pathValue, bool isUserPath)
        {
            InitializeComponent();
            DataContext = new ModifyPathWindowViewModel(pathValue, isUserPath);
        }

        private void OnPathListBoxDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PathListBox.SelectedItem is string selectedPath && !string.IsNullOrEmpty(selectedPath))
            {
                try
                {
                    System.Windows.Clipboard.SetText(selectedPath);
                    if (DataContext is ModifyPathWindowViewModel viewModel)
                    {
                        var truncated = selectedPath.Length > 30 ? selectedPath.Substring(0, 30) + "..." : selectedPath;
                        viewModel.SetStatusMessage($"Copied to clipboard: {truncated}");
                    }
                }
                catch (Exception ex)
                {
                    if (DataContext is ModifyPathWindowViewModel viewModel)
                    {
                        viewModel.SetStatusMessage($"Failed to copy: {ex.Message}", true);
                    }
                }
            }
        }
    }
}
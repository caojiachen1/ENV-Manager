using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EnvVarViewer
{
    public partial class ModifyPathWindow : Wpf.Ui.Controls.FluentWindow
    {
        private List<string> pathEntries;
        private bool isUserPath;

        public event EventHandler PathModified;

        public ModifyPathWindow(string pathValue, bool isUserPath)
        {
            InitializeComponent();
            this.isUserPath = isUserPath;
            pathEntries = new List<string>(pathValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            PathListBox.ItemsSource = pathEntries;
        }

        public string GetPathValue()
        {
            return string.Join(";", pathEntries);
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            string newEntry = NewPathTextBox.Text.Trim();
            if (!string.IsNullOrEmpty(newEntry) && !pathEntries.Contains(newEntry))
            {
                pathEntries.Add(newEntry);
                PathListBox.Items.Refresh();
                NewPathTextBox.Clear();
            }
        }

        private void RemoveButton_Click(object sender, RoutedEventArgs e)
        {
            if (PathListBox.SelectedItem != null)
            {
                pathEntries.Remove(PathListBox.SelectedItem.ToString());
                PathListBox.Items.Refresh();
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            string newPathValue = GetPathValue();
            SetEnvironmentVariable(newPathValue);
            PathModified?.Invoke(this, EventArgs.Empty);
            this.Close();
        }

        private void SetEnvironmentVariable(string value)
        {
            try
            {
                string variableName = "PATH";
                EnvironmentVariableTarget target = isUserPath ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Machine;
                Environment.SetEnvironmentVariable(variableName, value, target);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set environment variable: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void PathListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (PathListBox.SelectedItem != null)
            {
                string selectedPath = PathListBox.SelectedItem.ToString();
                Clipboard.SetText(selectedPath);
                var chunked = selectedPath.Length >= 25 ? $"{selectedPath.Substring(0, 25)}..." : selectedPath;
                StatusLabel.Content = $"Copied {chunked} to clipboard";
            }
        }
    }
}
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// ViewModel for ModifyPathWindow, handles PATH environment variable modification logic
    /// </summary>
    public class ModifyPathWindowViewModel : ViewModelBase
    {
        private ObservableCollection<string> _pathEntries;
        private string _newPathEntry;
        private string _selectedPath;
        private string _statusMessage;
        private readonly bool _isUserPath;

        public ObservableCollection<string> PathEntries
        {
            get => _pathEntries;
            set => SetProperty(ref _pathEntries, value);
        }

        public string NewPathEntry
        {
            get => _newPathEntry;
            set => SetProperty(ref _newPathEntry, value);
        }

        public string SelectedPath
        {
            get => _selectedPath;
            set => SetProperty(ref _selectedPath, value);
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set => SetProperty(ref _statusMessage, value);
        }

        public ICommand AddPathCommand { get; }
        public ICommand RemovePathCommand { get; }
        public ICommand SavePathCommand { get; }
        public ICommand CopyPathCommand { get; }

        public event EventHandler PathModified;

        public ModifyPathWindowViewModel(string pathValue, bool isUserPath)
        {
            _isUserPath = isUserPath;
            _pathEntries = new ObservableCollection<string>(pathValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            
            AddPathCommand = new RelayCommand(AddPath, CanAddPath);
            RemovePathCommand = new RelayCommand(RemovePath, () => SelectedPath != null);
            SavePathCommand = new RelayCommand(SavePath);
            CopyPathCommand = new RelayCommand(CopySelectedPath, () => SelectedPath != null);
        }

        private bool CanAddPath()
        {
            return !string.IsNullOrEmpty(NewPathEntry?.Trim()) && 
                   !PathEntries.Contains(NewPathEntry.Trim());
        }

        private void AddPath()
        {
            string newEntry = NewPathEntry.Trim();
            PathEntries.Add(newEntry);
            NewPathEntry = string.Empty;
        }

        private void RemovePath()
        {
            if (SelectedPath != null)
            {
                PathEntries.Remove(SelectedPath);
                SelectedPath = null;
            }
        }

        private void SavePath()
        {
            string newPathValue = string.Join(";", PathEntries);
            SetEnvironmentVariable(newPathValue);
            PathModified?.Invoke(this, EventArgs.Empty);
        }

        private void CopySelectedPath()
        {
            if (SelectedPath != null)
            {
                Clipboard.SetText(SelectedPath);
                var chunked = SelectedPath.Length >= 25 ? $"{SelectedPath.Substring(0, 25)}..." : SelectedPath;
                StatusMessage = $"Copied {chunked} to clipboard";
            }
        }

        /// <summary>
        /// Updates the PATH environment variable
        /// </summary>
        /// <param name="value">New PATH value to set</param>
        private void SetEnvironmentVariable(string value)
        {
            try
            {
                string variableName = "PATH";
                EnvironmentVariableTarget target = _isUserPath ? EnvironmentVariableTarget.User : EnvironmentVariableTarget.Machine;
                Environment.SetEnvironmentVariable(variableName, value, target);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to set environment variable: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
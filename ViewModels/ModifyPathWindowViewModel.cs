using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Input;
using System.Linq;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
// using System.Windows.Forms;
// using Microsoft.WindowsAPICodePack.Dialogs;
using System.Windows.Forms;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// ViewModel for ModifyPathWindow, handles PATH environment variable modification logic
    /// </summary>
    public class ModifyPathWindowViewModel : ViewModelBase
    {
        private ObservableCollection<string> _pathEntries;
        private ObservableCollection<string> _originalPathEntries;
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
            // set => SetProperty(ref _newPathEntry, value);
            set
            {
                if (SetProperty(ref _newPathEntry, value))
                {
                    AddPathCommand.NotifyCanExecuteChanged(); // Update command availability based on new value
                }
            }
        }

        public string? SelectedPath
        {
            get => _selectedPath;
            // set => SetProperty(ref _selectedPath, value);
            set
            {
                if (SetProperty(ref _selectedPath, value))
                {
                    RemovePathCommand.NotifyCanExecuteChanged(); // Update command availability based on new value
                    CopyPathCommand.NotifyCanExecuteChanged(); // Update copy command availability
                }
            }
        }

        public string StatusMessage
        {
            get => _statusMessage;
            set
            {
                if (SetProperty(ref _statusMessage, value))
                {
                    OnPropertyChanged(nameof(StatusMessage));
                }
            }
        }

        public RelayCommand AddPathCommand { get; }
        public RelayCommand RemovePathCommand { get; }
        public ICommand SavePathCommand { get; }
        public RelayCommand CopyPathCommand { get; private set; }
        public RelayCommand BrowsePathCommand { get; }

        /// <summary>
        /// Check if the save button can be pressed
        /// </summary>
        /// <returns>True if the path has been modified, false otherwise</returns>
        private bool CanSavePath()
        {
            return !_pathEntries.SequenceEqual(_originalPathEntries);
        }

        public event EventHandler PathModified;

        public ModifyPathWindowViewModel(string pathValue, bool isUserPath)
        {
            _isUserPath = isUserPath;
            _pathEntries = new ObservableCollection<string>(pathValue.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries));
            _originalPathEntries = new ObservableCollection<string>(_pathEntries);
            
            AddPathCommand = new RelayCommand(AddPath, CanAddPath);
            RemovePathCommand = new RelayCommand(RemovePath, CanDeletePath);
            SavePathCommand = new RelayCommand(SavePath, CanSavePath);
            CopyPathCommand = new RelayCommand(CopySelectedPath, () => SelectedPath != null);
            BrowsePathCommand = new RelayCommand(BrowsePath);
        }

        // Check if the delete button can be pressed
        private bool CanDeletePath()
        {
            return SelectedPath != null;
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
            ((RelayCommand)SavePathCommand).NotifyCanExecuteChanged(); // Update save button availability
        }

        private void RemovePath()
        {
            if (SelectedPath != null)
            {
                PathEntries.Remove(SelectedPath);
                SelectedPath = null;
                ((RelayCommand)SavePathCommand).NotifyCanExecuteChanged(); // Update save button availability
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
                System.Windows.Clipboard.SetText(SelectedPath);
                var chunked = SelectedPath.Length >= 25 ? $"{SelectedPath.Substring(0, 25)}..." : SelectedPath;
                StatusMessage = $"Copied {chunked} to clipboard";
            }
        }

        /// <summary>
        /// Browses for a folder using System.Windows.Forms.FolderBrowserDialog and sets the selected path to NewPathEntry.
        /// </summary>
        private void BrowsePath()
        {
            var dlg = new FolderBrowserDialog();
            // Assume currentDirectory is defined somewhere, if not, need to adjust.
            // dlg.SelectedPath = currentDirectory;
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                NewPathEntry = dlg.SelectedPath;
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
                System.Windows.MessageBox.Show($"Failed to set environment variable: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
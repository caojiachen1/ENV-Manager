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
            set => SetProperty(ref _statusMessage, value); // Remove duplicate OnPropertyChanged call
        }

        public RelayCommand AddPathCommand { get; private set; }
        public RelayCommand RemovePathCommand { get; private set; }
        public ICommand SavePathCommand { get; private set; }
        public RelayCommand CopyPathCommand { get; private set; }
        public RelayCommand BrowsePathCommand { get; private set; }

        private readonly string _pathSeparator = ";";

        /// <summary>
        /// Check if the save button can be pressed
        /// </summary>
        /// <returns>True if the path has been modified, false otherwise</returns>
        private bool CanSavePath()
        {
            return PathEntries.Count != _originalPathEntries.Count ||
                   !PathEntries.SequenceEqual(_originalPathEntries, StringComparer.OrdinalIgnoreCase);
        }

        public event EventHandler PathModified;

        public ModifyPathWindowViewModel(string pathValue, bool isUserPath)
        {
            _isUserPath = isUserPath;
            
            // Use StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries for .NET 5+
            var paths = pathValue?.Split(new[] { _pathSeparator }, StringSplitOptions.RemoveEmptyEntries) ?? Array.Empty<string>();
            _pathEntries = new ObservableCollection<string>(paths.Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)));
            _originalPathEntries = new ObservableCollection<string>(_pathEntries);
            
            InitializeCommands();
            
            // Subscribe to collection changes for better save state management
            _pathEntries.CollectionChanged += (s, e) => 
            {
                ((RelayCommand)SavePathCommand).NotifyCanExecuteChanged();
            };
        }

        private void InitializeCommands()
        {
            AddPathCommand = new RelayCommand(AddPath, CanAddPath);
            RemovePathCommand = new RelayCommand(RemovePath, CanDeletePath);
            SavePathCommand = new RelayCommand(SavePath, CanSavePath);
            CopyPathCommand = new RelayCommand(CopySelectedPath, () => !string.IsNullOrEmpty(SelectedPath));
            BrowsePathCommand = new RelayCommand(BrowsePath);
        }

        // Check if the delete button can be pressed
        private bool CanDeletePath()
        {
            return SelectedPath != null;
        }

        private bool CanAddPath()
        {
            if (string.IsNullOrWhiteSpace(NewPathEntry)) return false;
            
            var trimmed = NewPathEntry.Trim();
            return !string.IsNullOrEmpty(trimmed) && 
                   !PathEntries.Any(p => string.Equals(p, trimmed, StringComparison.OrdinalIgnoreCase));
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
            try
            {
                // Remove empty entries and duplicates more efficiently
                var cleanedPaths = PathEntries
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim())
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .ToList();

                string newPathValue = string.Join(_pathSeparator, cleanedPaths);
                SetEnvironmentVariable(newPathValue);
                
                // Update original collection for future comparisons
                _originalPathEntries.Clear();
                foreach (var path in cleanedPaths)
                {
                    _originalPathEntries.Add(path);
                }
                
                // Update current collection to reflect cleaned state
                PathEntries.Clear();
                foreach (var path in cleanedPaths)
                {
                    PathEntries.Add(path);
                }
                
                PathModified?.Invoke(this, EventArgs.Empty);
                StatusMessage = "PATH updated successfully";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to update PATH: {ex.Message}";
            }
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
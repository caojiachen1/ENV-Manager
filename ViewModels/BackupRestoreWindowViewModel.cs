using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;

namespace EnvVarViewer.ViewModels
{
    public class BackupRestoreWindowViewModel : ViewModelBase
    {
        private string _selectedBackupFile;
        private Dictionary<string, string> _previewUserVars;
        private Dictionary<string, string> _previewSystemVars;
        private bool _backupUserVars = true;
        private bool _backupSystemVars = true;
        private bool _restoreUserVars = true;
        private bool _restoreSystemVars = true;
        private string _backupStatus;
        private string _restoreStatus;
        public event PropertyChangedEventHandler PropertyChanged;

        private ICommand _backupCommand;
        private ICommand _restoreCommand;
        private bool _overwriteExisting = true;

        public ICommand BackupCommand => _backupCommand ??= new RelayCommand(ExecuteBackup);
        public ICommand RestoreCommand => _restoreCommand ??= new RelayCommand(ExecuteRestore);

        public bool OverwriteExisting
        {
            get => _overwriteExisting;
            set => SetField(ref _overwriteExisting, value);
        }

        public string SelectedBackupFile
        {
            get => _selectedBackupFile;
            set => SetField(ref _selectedBackupFile, value);
        }

        public bool BackupUserVars
        {
            get => _backupUserVars;
            set => SetField(ref _backupUserVars, value);
        }

        public bool BackupSystemVars
        {
            get => _backupSystemVars;
            set => SetField(ref _backupSystemVars, value);
        }

        public bool RestoreUserVars
        {
            get => _restoreUserVars;
            set => SetField(ref _restoreUserVars, value);
        }

        public bool RestoreSystemVars
        {
            get => _restoreSystemVars;
            set => SetField(ref _restoreSystemVars, value);
        }

        public string BackupStatus
        {
            get => _backupStatus;
            set => SetField(ref _backupStatus, value);
        }

        public string RestoreStatus
        {
            get => _restoreStatus;
            set => SetField(ref _restoreStatus, value);
        }
        public MainWindowViewModel MainWindowViewModel { get; set;}

        // public BackupRestoreWindowViewModel(Dictionary<string, string> userVars, Dictionary<string, string> systemVars)
        public BackupRestoreWindowViewModel(ViewModels.MainWindowViewModel viewModel)
        {
            this.MainWindowViewModel = viewModel;
            // UserEnvVars = viewModel.UserEnvVars;
            // SystemEnvVars = viewModel.SystemEnvVars;
            _previewUserVars = new Dictionary<string, string>();
            _previewSystemVars = new Dictionary<string, string>();
        }

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
            // return new Models.EnvironmentVariableModel.IsAdministrator();
        }

        private void ExecuteBackup()
        {
            try
            {
                var saveFileDialog = new Microsoft.Win32.SaveFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".txt",
                    Title = "Export Environment Variables"
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    using (StreamWriter file = new StreamWriter(saveFileDialog.FileName))
                    {
                        file.WriteLine($"Environment Variables Export - {DateTime.Now}\n");

                        if (BackupUserVars)
                        {
                            file.WriteLine("[User Environment Variables]");
                            foreach (var kvp in MainWindowViewModel.UserEnvVars)
                            {
                                file.WriteLine($"{kvp.Key}={kvp.Value}");
                            }
                            file.WriteLine();
                        }

                        if (BackupSystemVars)
                        {
                            file.WriteLine("[System Environment Variables]");
                            foreach (var kvp in MainWindowViewModel.SystemEnvVars)
                            {
                                file.WriteLine($"{kvp.Key}={kvp.Value}");
                            }
                        }

                        BackupStatus = "Backup completed successfully";
                    }
                }
            }
            catch (Exception ex)
            {
                BackupStatus = $"Backup failed: {ex.Message}";
            }
        }

        private void ExecuteRestore()
        {
            try
            {
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = ".txt",
                    Title = "Import Environment Variables"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedBackupFile = openFileDialog.FileName;
                    RestoreStatus = "Restore completed successfully";
                }
            }
            catch (Exception ex)
            {
                RestoreStatus = $"Restore failed: {ex.Message}";
            }
        }
    }
}
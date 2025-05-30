using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using CommunityToolkit.Mvvm.Input;
using System.Windows;

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
            var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "Environment Variable Backup (*.envbackup)|*.envbackup|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = ".envbackup",
                Title = "Save Environment Variables Backup",
                FileName = $"EnvBackup_{timestamp}.envbackup"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using (StreamWriter file = new StreamWriter(saveFileDialog.FileName))
                    {
                        bool isTxtFormat = Path.GetExtension(saveFileDialog.FileName).ToLower() == ".txt";

                        // Write backup file header
                        if (isTxtFormat)
                        {
                            file.WriteLine($"Environment Variables Export - {DateTime.Now}\n");
                        }
                        else
                        {
                            file.WriteLine("# Environment Variables Backup File");
                            file.WriteLine($"# Creation Time: {DateTime.Now}");
                            file.WriteLine("# Format: [Type]:[Variable Name]=[Variable Value]");
                            file.WriteLine();
                        }

                        // Backup user environment variables
                        if (BackupUserVars)
                        {
                            if (isTxtFormat)
                            {
                                file.WriteLine("[User Variables]");
                                foreach (var kv in MainWindowViewModel.UserEnvVars)
                                {
                                    file.WriteLine($"{kv.Key}={kv.Value}");
                                }
                                file.WriteLine();
                            }
                            else
                            {
                                file.WriteLine("[USER_VARIABLES]");
                                foreach (var kv in MainWindowViewModel.UserEnvVars)
                                {
                                    file.WriteLine($"USER:{kv.Key}={kv.Value}");
                                }
                                file.WriteLine();
                            }
                        }

                        // Backup system environment variables
                        if (BackupSystemVars)
                        {
                            if (isTxtFormat)
                            {
                                file.WriteLine("[System Variables]");
                                foreach (var kv in MainWindowViewModel.SystemEnvVars)
                                {
                                    file.WriteLine($"{kv.Key}={kv.Value}");
                                }
                            }
                            else
                            {
                                file.WriteLine("[SYSTEM_VARIABLES]");
                                foreach (var kv in MainWindowViewModel.SystemEnvVars)
                                {
                                    file.WriteLine($"SYSTEM:{kv.Key}={kv.Value}");
                                }
                            }
                        }
                    }

                    BackupStatus = "Backup Successful!";
                    OnPropertyChanged(nameof(BackupStatus));
                }
                catch (Exception ex)
                {
                    System.Windows.MessageBox.Show($"An error occurred during the backup process: {ex.Message}", "Backup error", MessageBoxButton.OK, MessageBoxImage.Error);
                    BackupStatus = "Backup failed";
                    OnPropertyChanged(nameof(BackupStatus));
                }
            }
        }

        // private void ExecuteBackup()
        // {
        //     try
        //     {
        //         var saveFileDialog = new Microsoft.Win32.SaveFileDialog
        //         {
        //             Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
        //             DefaultExt = ".txt",
        //             Title = "Export Environment Variables"
        //         };

        //         if (saveFileDialog.ShowDialog() == true)
        //         {
        //             using (StreamWriter file = new StreamWriter(saveFileDialog.FileName))
        //             {
        //                 file.WriteLine($"Environment Variables Export - {DateTime.Now}\n");

        //                 if (BackupUserVars)
        //                 {
        //                     file.WriteLine("[User Environment Variables]");
        //                     foreach (var kvp in MainWindowViewModel.UserEnvVars)
        //                     {
        //                         file.WriteLine($"{kvp.Key}={kvp.Value}");
        //                     }
        //                     file.WriteLine();
        //                 }

        //                 if (BackupSystemVars)
        //                 {
        //                     file.WriteLine("[System Environment Variables]");
        //                     foreach (var kvp in MainWindowViewModel.SystemEnvVars)
        //                     {
        //                         file.WriteLine($"{kvp.Key}={kvp.Value}");
        //                     }
        //                 }

        //                 BackupStatus = "Backup completed successfully";
        //             }
        //         }
        //     }
        //     catch (Exception ex)
        //     {
        //         BackupStatus = $"Backup failed: {ex.Message}";
        //     }
        // }

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
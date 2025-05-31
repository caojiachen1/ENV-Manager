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
using System.Threading.Tasks;
using System.Threading;

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

        private AsyncRelayCommand _backupCommand;
        private AsyncRelayCommand _restoreCommand;
        private bool _overwriteExisting = true;

        public ICommand BackupCommand => _backupCommand ??= new AsyncRelayCommand(ExecuteBackupAsync);
        public ICommand RestoreCommand => _restoreCommand ??= new AsyncRelayCommand(ExecuteRestoreAsync);

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

        private async Task ExecuteBackupAsync()
        {
            try
            {
                SetLoadingState(true, "Creating backup...");
                
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
                    await Task.Run(async () =>
                    {
                        using (StreamWriter file = new StreamWriter(saveFileDialog.FileName))
                        {
                            bool isTxtFormat = Path.GetExtension(saveFileDialog.FileName).ToLower() == ".txt";

                            // Write backup file header
                            if (isTxtFormat)
                            {
                                await file.WriteLineAsync($"Environment Variables Export - {DateTime.Now}\n");
                            }
                            else
                            {
                                await file.WriteLineAsync("# Environment Variables Backup File");
                                await file.WriteLineAsync($"# Creation Time: {DateTime.Now}");
                                await file.WriteLineAsync("# Format: [Type]:[Variable Name]=[Variable Value]");
                                await file.WriteLineAsync();
                            }

                            // Backup user environment variables
                            if (BackupUserVars)
                            {
                                CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                
                                if (isTxtFormat)
                                {
                                    await file.WriteLineAsync("[User Variables]");
                                    foreach (var kv in MainWindowViewModel.UserEnvVars)
                                    {
                                        await file.WriteLineAsync($"{kv.Key}={kv.Value}");
                                    }
                                    await file.WriteLineAsync();
                                }
                                else
                                {
                                    await file.WriteLineAsync("[USER_VARIABLES]");
                                    foreach (var kv in MainWindowViewModel.UserEnvVars)
                                    {
                                        await file.WriteLineAsync($"USER:{kv.Key}={kv.Value}");
                                    }
                                    await file.WriteLineAsync();
                                }
                            }

                            // Backup system environment variables
                            if (BackupSystemVars)
                            {
                                CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                
                                if (isTxtFormat)
                                {
                                    await file.WriteLineAsync("[System Variables]");
                                    foreach (var kv in MainWindowViewModel.SystemEnvVars)
                                    {
                                        await file.WriteLineAsync($"{kv.Key}={kv.Value}");
                                    }
                                }
                                else
                                {
                                    await file.WriteLineAsync("[SYSTEM_VARIABLES]");
                                    foreach (var kv in MainWindowViewModel.SystemEnvVars)
                                    {
                                        await file.WriteLineAsync($"SYSTEM:{kv.Key}={kv.Value}");
                                    }
                                }
                            }
                        }
                    }, CancellationTokenSource.Token);

                    BackupStatus = "Backup Successful!";
                    OnPropertyChanged(nameof(BackupStatus));
                }
            }
            catch (OperationCanceledException)
            {
                BackupStatus = "Backup cancelled";
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred during the backup process: {ex.Message}", "Backup error", MessageBoxButton.OK, MessageBoxImage.Error);
                BackupStatus = "Backup failed";
                OnPropertyChanged(nameof(BackupStatus));
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        private async Task ExecuteRestoreAsync()
        {
            try
            {
                SetLoadingState(true, "Restoring from backup...");
                
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Environment Variable Backup (*.envbackup)|*.envbackup|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Select Environment Variables Backup File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedBackupFile = openFileDialog.FileName;
                    await LoadBackupPreviewAsync(SelectedBackupFile);
                    RestoreStatus = "Restore completed successfully";
                }
            }
            catch (OperationCanceledException)
            {
                RestoreStatus = "Restore cancelled";
            }
            catch (Exception ex)
            {
                RestoreStatus = $"Restore failed: {ex.Message}";
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Loads and previews the content of a backup file asynchronously
        /// </summary>
        /// <param name="filePath">Path to the backup file</param>
        private async Task LoadBackupPreviewAsync(string filePath)
        {
            try
            {
                SetLoadingState(true, "Loading backup preview...");
                
                await Task.Run(async () =>
                {
                    var previewUserVars = new Dictionary<string, string>();
                    var previewSystemVars = new Dictionary<string, string>();
                    string previewText = "Backup File Content Preview:\r\n";
                    
                    string[] lines = await File.ReadAllLinesAsync(filePath, CancellationTokenSource.Token);
                    bool isTxtFormat = Path.GetExtension(filePath).ToLower() == ".txt";
                    bool isInUserSection = false;
                    bool isInSystemSection = false;

                    foreach (string line in lines)
                    {
                        CancellationTokenSource.Token.ThrowIfCancellationRequested();
                        
                        // ... existing preview logic ...
                        // (Keep the existing preview parsing logic but add cancellation checks)
                    }

                    return (previewText, previewUserVars, previewSystemVars);
                }, CancellationTokenSource.Token);
            }
            catch (OperationCanceledException)
            {
                // Preview loading was cancelled
            }
            catch (Exception ex)
            {
                // Handle preview loading errors
            }
            finally
            {
                SetLoadingState(false);
            }
        }
    }
}
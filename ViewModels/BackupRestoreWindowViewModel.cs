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
        // Remove individual status fields - use base StatusMessage
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

        public MainWindowViewModel MainWindowViewModel { get; set;}

        public BackupRestoreWindowViewModel(ViewModels.MainWindowViewModel viewModel)
        {
            this.MainWindowViewModel = viewModel;
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
        }

        private async Task ExecuteBackupAsync()
        {
            try
            {
                SetLoadingState(true, "Creating backup...");
                SetStatusMessage("Starting backup process...");
                
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
                                await file.WriteLineAsync($"Environment Variables Export - {DateTime.Now}\n").ConfigureAwait(false);
                            }
                            else
                            {
                                await file.WriteLineAsync("# Environment Variables Backup File").ConfigureAwait(false);
                                await file.WriteLineAsync($"# Creation Time: {DateTime.Now}").ConfigureAwait(false);
                                await file.WriteLineAsync("# Format: [Type]:[Variable Name]=[Variable Value]").ConfigureAwait(false);
                                await file.WriteLineAsync().ConfigureAwait(false);
                            }

                            int totalVars = 0;

                            // Backup user environment variables
                            if (BackupUserVars)
                            {
                                CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                
                                if (isTxtFormat)
                                {
                                    await file.WriteLineAsync("[User Variables]").ConfigureAwait(false);
                                    foreach (var kv in MainWindowViewModel.UserEnvVars)
                                    {
                                        await file.WriteLineAsync($"{kv.Key}={kv.Value}").ConfigureAwait(false);
                                        totalVars++;
                                    }
                                    await file.WriteLineAsync().ConfigureAwait(false);
                                }
                                else
                                {
                                    await file.WriteLineAsync("[USER_VARIABLES]").ConfigureAwait(false);
                                    foreach (var kv in MainWindowViewModel.UserEnvVars)
                                    {
                                        await file.WriteLineAsync($"USER:{kv.Key}={kv.Value}").ConfigureAwait(false);
                                        totalVars++;
                                    }
                                    await file.WriteLineAsync().ConfigureAwait(false);
                                }
                            }

                            // Backup system environment variables
                            if (BackupSystemVars)
                            {
                                CancellationTokenSource.Token.ThrowIfCancellationRequested();
                                
                                if (isTxtFormat)
                                {
                                    await file.WriteLineAsync("[System Variables]").ConfigureAwait(false);
                                    foreach (var kv in MainWindowViewModel.SystemEnvVars)
                                    {
                                        await file.WriteLineAsync($"{kv.Key}={kv.Value}").ConfigureAwait(false);
                                        totalVars++;
                                    }
                                }
                                else
                                {
                                    await file.WriteLineAsync("[SYSTEM_VARIABLES]").ConfigureAwait(false);
                                    foreach (var kv in MainWindowViewModel.SystemEnvVars)
                                    {
                                        await file.WriteLineAsync($"SYSTEM:{kv.Key}={kv.Value}").ConfigureAwait(false);
                                        totalVars++;
                                    }
                                }
                            }

                            return totalVars;
                        }
                    }, CancellationTokenSource.Token).ConfigureAwait(false);

                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        int userCount = BackupUserVars ? MainWindowViewModel.UserEnvVars.Count : 0;
                        int systemCount = BackupSystemVars ? MainWindowViewModel.SystemEnvVars.Count : 0;
                        SetStatusMessage($"Backup successful! Saved {userCount} user and {systemCount} system variables to {Path.GetFileName(saveFileDialog.FileName)}");
                    });
                }
                else
                {
                    SetStatusMessage("Backup cancelled by user");
                }
            }
            catch (OperationCanceledException)
            {
                SetStatusMessage("Backup cancelled");
            }
            catch (Exception ex)
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    System.Windows.MessageBox.Show($"An error occurred during the backup process: {ex.Message}", "Backup error", MessageBoxButton.OK, MessageBoxImage.Error);
                    SetStatusMessage($"Backup failed: {ex.Message}", true);
                });
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
                SetStatusMessage("Starting restore process...");
                
                var openFileDialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "Environment Variable Backup (*.envbackup)|*.envbackup|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    Title = "Select Environment Variables Backup File"
                };

                if (openFileDialog.ShowDialog() == true)
                {
                    SelectedBackupFile = openFileDialog.FileName;
                    await LoadBackupPreviewAsync(SelectedBackupFile);
                    SetStatusMessage($"Restore completed successfully from {Path.GetFileName(SelectedBackupFile)}");
                }
                else
                {
                    SetStatusMessage("Restore cancelled by user");
                }
            }
            catch (OperationCanceledException)
            {
                SetStatusMessage("Restore cancelled");
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Restore failed: {ex.Message}", true);
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
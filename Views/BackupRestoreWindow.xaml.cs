using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Security.Principal;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    public partial class BackupRestoreWindow : Wpf.Ui.Controls.FluentWindow
    {
        public MainWindowViewModel MainWindowViewModel { get; }
        public ViewModels.BackupRestoreWindowViewModel ViewModel { get; private set; }
        public string selectedBackupFile = string.Empty;
        private Dictionary<string, string> previewUserVars = new Dictionary<string, string>();
        private Dictionary<string, string> previewSystemVars = new Dictionary<string, string>();

        // public BackupRestoreWindow(Dictionary<string, string> userVars, Dictionary<string, string> systemVars)
        /// <summary>
        /// Initializes a new instance of the BackupRestoreWindow
        /// </summary>
        /// <param name="viewModel">The main window view model</param>
        public BackupRestoreWindow(ViewModels.MainWindowViewModel viewModel)
        {
            InitializeComponent();
            this.MainWindowViewModel = viewModel;
            ViewModel = new ViewModels.BackupRestoreWindowViewModel(viewModel);
            DataContext = ViewModel;
        }

        /// <summary>
        /// Checks if current user has administrator privileges
        /// </summary>
        /// <returns>True if user is administrator</returns>
        private bool IsAdministrator()
        {
            return new Models.EnvironmentVariableModel().IsAdministrator();
        }

        /// <summary>
        /// Handles backup button click event to save environment variables to a file
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BackupButton_Click(object sender, RoutedEventArgs e)
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
                        if (ViewModel.BackupUserVars)
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
                        if (ViewModel.BackupSystemVars)
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

                    ViewModel.BackupStatus = "Backup Successful!";
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"An error occurred during the backup process: {ex.Message}", "Backup error", MessageBoxButton.OK, MessageBoxImage.Error);
                    ViewModel.BackupStatus = "Backup failed";
                }
            }
        }

        /// <summary>
        /// Handles browse button click event to select a backup file
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void BrowseButton_Click(object sender, RoutedEventArgs e)
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Filter = "Environment Variable Backup (*.envbackup)|*.envbackup|Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                Title = "Select Environment Variables Backup File"
            };

            if (openFileDialog.ShowDialog() == true)
            {
                selectedBackupFile = openFileDialog.FileName;
                RestoreFilePathTextBox.Text = selectedBackupFile;
                RestoreButton.IsEnabled = true;
                LoadBackupPreview(selectedBackupFile);
            }
        }

        /// <summary>
        /// Loads and previews the content of a backup file
        /// </summary>
        /// <param name="filePath">Path to the backup file</param>
        private void LoadBackupPreview(string filePath)
        {
            try
            {
                previewUserVars.Clear();
                previewSystemVars.Clear();
                PreviewTextBlock.Text = "";

                string[] lines = File.ReadAllLines(filePath);
                string previewText = "Backup File Content Preview:\r\n";
                bool isTxtFormat = Path.GetExtension(filePath).ToLower() == ".txt";
                bool isInUserSection = false;
                bool isInSystemSection = false;

                foreach (string line in lines)
                {
                    if (string.IsNullOrWhiteSpace(line))
                    {
                        previewText += line + "\r\n";
                        continue;
                    }

                    if (isTxtFormat)
                    {
                        if (line.StartsWith("[User Variables]"))
                        {
                            isInUserSection = true;
                            isInSystemSection = false;
                            previewText += line + "\r\n";
                            continue;
                        }
                        else if (line.StartsWith("[System Variables]"))
                        {
                            isInUserSection = false;
                            isInSystemSection = true;
                            previewText += line + "\r\n";
                            continue;
                        }

                        int equalIndex = line.IndexOf('=');
                        if (equalIndex > 0)
                        {
                            string key = line.Substring(0, equalIndex);
                            string value = line.Substring(equalIndex + 1);

                            if (isInUserSection)
                            {
                                previewUserVars[key] = value;
                            }
                            else if (isInSystemSection)
                            {
                                previewSystemVars[key] = value;
                            }

                            previewText += line + "\r\n";
                        }
                    }
                    else // .envbackup format
                    {
                        if (line.StartsWith("#") || line.StartsWith("["))
                        {
                            previewText += line + "\r\n";
                            continue;
                        }

                        if (line.StartsWith("USER:") || line.StartsWith("SYSTEM:"))
                        {
                            string[] parts = line.Split(new[] { ':' }, 2);
                            if (parts.Length == 2)
                            {
                                string type = parts[0];
                                string content = parts[1];
                                int equalIndex = content.IndexOf('=');

                                if (equalIndex > 0)
                                {
                                    string key = content.Substring(0, equalIndex);
                                    string value = content.Substring(equalIndex + 1);

                                    if (type == "USER")
                                    {
                                        previewUserVars[key] = value;
                                    }
                                    else if (type == "SYSTEM")
                                    {
                                        previewSystemVars[key] = value;
                                    }

                                    previewText += line + "\r\n";
                                }
                            }
                        }
                    }
                }

                PreviewTextBlock.Text = previewText;
                RestoreStatusText.Text = $"{previewUserVars.Count} User Vars and {previewSystemVars.Count} Sys Vars found";
            }
            catch (Exception ex)
            {
                PreviewTextBlock.Text = $"Failed to load backup file: {ex.Message}";
                RestoreStatusText.Text = "Failed to load backup";
                RestoreButton.IsEnabled = false;
            }
        }

        /// <summary>
        /// Handles restore button click event to restore environment variables from backup
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="e">Event arguments</param>
        private void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (!IsAdministrator() && RestoreSystemVarsCheckBox.IsChecked == true)
            {
                MessageBox.Show("Restoring system environment variables requires administrator privileges. Please run the program as administrator.", "Insufficient Privileges", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (string.IsNullOrEmpty(selectedBackupFile) || !File.Exists(selectedBackupFile))
            {
                MessageBox.Show("Please select a valid backup file.", "File Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                int restoredCount = 0;

                // Restore user environment variables
                if (RestoreUserVarsCheckBox.IsChecked == true && previewUserVars.Count > 0) {
                    foreach (var kv in previewUserVars) {
                        if ((OverwriteExistingCheckBox.IsChecked == true || !MainWindowViewModel.UserEnvVars.ContainsKey(kv.Key)) &&
                            (!MainWindowViewModel.UserEnvVars.ContainsKey(kv.Key) || MainWindowViewModel.UserEnvVars[kv.Key] != kv.Value)) {
                            Environment.SetEnvironmentVariable(kv.Key, kv.Value, EnvironmentVariableTarget.User);
                            restoredCount++;
                        }
                    }
                }

                // Restore system environment variables
                if (RestoreSystemVarsCheckBox.IsChecked == true && previewSystemVars.Count > 0) {
                    foreach (var kv in previewSystemVars) {
                        if ((OverwriteExistingCheckBox.IsChecked == true || !MainWindowViewModel.SystemEnvVars.ContainsKey(kv.Key)) &&
                            (!MainWindowViewModel.SystemEnvVars.ContainsKey(kv.Key) || MainWindowViewModel.SystemEnvVars[kv.Key] != kv.Value)) {
                            Environment.SetEnvironmentVariable(kv.Key, kv.Value, EnvironmentVariableTarget.Machine);
                            restoredCount++;
                        }
                    }
                }

                RestoreStatusText.Text = $"Successfully restored {restoredCount} environment variables";
                MessageBox.Show($"Successfully restored {restoredCount} environment variables. Please refresh the main window to see the changes.", "Restore Successful", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the restore process: {ex.Message}", "Restore Error", MessageBoxButton.OK, MessageBoxImage.Error);
                RestoreStatusText.Text = "Restore failed";
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
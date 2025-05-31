using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using System.Security.AccessControl;
using System.Collections.Generic;
using System.ComponentModel;
using EnvVarViewer.ViewModels;

namespace EnvVarViewer
{
    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        public ViewModels.MainWindowViewModel ViewModel { get; private set; }

        public MainWindow()
        {
            // Initialize window components and apply dark theme with Mica backdrop
            InitializeComponent();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.Mica, true);
            
            // Initialize ViewModel and set as DataContext
            ViewModel = new ViewModels.MainWindowViewModel();
            DataContext = ViewModel;
            
            // Use weak event pattern to prevent memory leaks
            ViewModel.EnvVarListUpdated += OnEnvVarListUpdated;
            
            // Check if running as administrator, if not elevate privileges
            if (!ViewModel.IsAdministrator())
            {
                ViewModel.Elevate();
                return; // The program will restart with admin privileges
            }
            
            SearchBox.Focus(); // Set focus to the search box after initialization
            EnvVarTreeView.MouseDoubleClick += EnvVarTreeView_MouseDoubleClick;
        }

        private void OnEnvVarListUpdated(object sender, EventArgs e)
        {
            // Batch UI updates to improve performance with lower priority
            Dispatcher.BeginInvoke(() =>
            {
                EnvVarListBox.ItemsSource = null;
                EnvVarListBox.ItemsSource = ViewModel.EnvVarList;
            }, System.Windows.Threading.DispatcherPriority.Background);
        }

        /// <summary>
        /// Handles selection changes in the environment variables list
        /// </summary>
        private void EnvVarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                // Get the selected environment variable name
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (ViewModel.UserEnvVars.ContainsKey(selectedVar) || ViewModel.SystemEnvVars.ContainsKey(selectedVar))
                {
                    var userItem = EnvVarTreeView.Items[0] as TreeViewItem;
                    var systemItem = EnvVarTreeView.Items[1] as TreeViewItem;

                    userItem.Items.Clear();
                    systemItem.Items.Clear();

                    if (ViewModel.UserEnvVars.ContainsKey(selectedVar))
                    {
                        userItem.Items.Add(new KeyValuePair<string, string>(selectedVar, ViewModel.UserEnvVars[selectedVar]));
                    }

                    if (ViewModel.SystemEnvVars.ContainsKey(selectedVar))
                    {
                        systemItem.Items.Add(new KeyValuePair<string, string>(selectedVar, ViewModel.SystemEnvVars[selectedVar]));
                    }

                    StatusLabel.Text = "";
                }
                else
                {
                    StatusLabel.Text = "Environment variable not found";
                }
            }
        }

        private void SortOrderButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.CurrentSortOrder = ViewModel.CurrentSortOrder.Equals(EnvVarViewer.Models.SortOrder.Ascending) ? 
                EnvVarViewer.Models.SortOrder.Descending : EnvVarViewer.Models.SortOrder.Ascending;
            SortOrderButton.Content = ViewModel.CurrentSortOrder.Equals(EnvVarViewer.Models.SortOrder.Ascending) ? "↑" : "↓";
        }

        /// <summary>
        /// Refreshes the environment variables list and maintains selection
        /// </summary>
        private async void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Store currently selected variable before refresh
                string previouslySelectedVarKey = EnvVarListBox.SelectedItem as string;

                await ViewModel.LoadEnvVarsAsync().ConfigureAwait(false); // Optimize with ConfigureAwait

                // Update UI on dispatcher thread
                Dispatcher.Invoke(() =>
                {
                    SearchBox.Text = ""; // Clear search box
                    StatusLabel.Text = "";
                    
                    // Clear the TreeView display first. If no item is re-selected, it remains empty.
                    var userItem = EnvVarTreeView.Items[0] as TreeViewItem;
                    var systemItem = EnvVarTreeView.Items[1] as TreeViewItem;
                    userItem.Items.Clear();
                    systemItem.Items.Clear();

                    if (!string.IsNullOrEmpty(previouslySelectedVarKey))
                    {
                        // EnvVarListBox.ItemsSource is IEnumerable<string> from UpdateListBox()
                        if (EnvVarListBox.ItemsSource is System.Collections.Generic.IEnumerable<string> items && items.Contains(previouslySelectedVarKey))
                        {
                            EnvVarListBox.SelectedItem = previouslySelectedVarKey;
                            // Setting SelectedItem will trigger EnvVarListBox_SelectionChanged,
                            // which will update the UserEnvList and SystemEnvList in the TreeView with new values.
                        }
                        // If the previously selected item no longer exists (e.g., variable deleted),
                        // or if ItemsSource is not what we expect, the TreeView remains empty as cleared above.
                    }
                });
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() => StatusLabel.Text = $"Error refreshing: {ex.Message}");
            }
        }

        private async Task<bool> EnsureAdminPrivilegesAsync()
        {
            if (!await ViewModel.EnvVarModel.IsAdministratorAsync())
            {
                await ViewModel.EnvVarModel.ElevateAsync();
                return false;
            }
            return true;
        }

        private async void BackupRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await EnsureAdminPrivilegesAsync()) return;
            
            var backupRestoreWindow = new BackupRestoreWindow(ViewModel);
            backupRestoreWindow.ShowDialog();
            
            await RefreshAsync();
        }

        private async void AddButton_Click(object sender, RoutedEventArgs e)
        {
            if (!await EnsureAdminPrivilegesAsync()) return;
            
            var addWindow = new AddEnvVarWindow(ViewModel.UserEnvVars, ViewModel.SystemEnvVars);
            addWindow.EnvVarAdded += async (s, ev) => await ViewModel.UpdateListBoxAsync();
            addWindow.ShowDialog();
        }

        private async Task RefreshAsync()
        {
            await ViewModel.LoadEnvVarsAsync();
        }

        private void EnvVarListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (ViewModel.SystemEnvVars.ContainsKey(selectedVar) || ViewModel.UserEnvVars.ContainsKey(selectedVar))
                {
                    string value;
                    string source;

                    if (ViewModel.SystemEnvVars.ContainsKey(selectedVar))
                    {
                        value = ViewModel.SystemEnvVars[selectedVar];
                        source = "System";
                    }
                    else
                    {
                        value = ViewModel.UserEnvVars[selectedVar];
                        source = "User";
                    }

                    System.Windows.Clipboard.SetText(value);
                    StatusLabel.Text = $"Copied {selectedVar} ({source}) to clipboard";
                }
                else
                {
                    StatusLabel.Text = $"Environment variable {selectedVar} not found";
                }
            }
        }

        private void EnvVarTreeView_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var selectedItem = EnvVarTreeView.SelectedItem;
            if (selectedItem is KeyValuePair<string, string> keyValuePair)
            {
                string source = GetSource(keyValuePair.Key, keyValuePair.Value);
                System.Windows.Clipboard.SetText(keyValuePair.Value);
                StatusLabel.Text = $"Copied {keyValuePair.Key} ({source}) to clipboard";
            }
        }

        private string GetSource(string key, string value)
        {
            bool isUser = ViewModel.UserEnvVars.ContainsKey(key) && ViewModel.UserEnvVars[key] == value;
            bool isSystem = ViewModel.SystemEnvVars.ContainsKey(key) && ViewModel.SystemEnvVars[key] == value;

            if (isUser)
            {
                return "User";
            }
            else if (isSystem)
            {
                return "System";
            }
            return "Unknown";
        }

        private async void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (!await EnsureAdminPrivilegesAsync()) return;

                if (EnvVarListBox.SelectedItem != null)
                {
                    string selectedVar = EnvVarListBox.SelectedItem.ToString();
                    var selectedNode = GetSelectedTreeViewNode();

                    if (selectedNode != null)
                    {
                        bool isUserNode = selectedNode.Header.ToString() == "User";
                        string value = isUserNode ?
                                        (ViewModel.UserEnvVars.ContainsKey(selectedVar) ? ViewModel.UserEnvVars[selectedVar] : null) :
                                        (ViewModel.SystemEnvVars.ContainsKey(selectedVar) ? ViewModel.SystemEnvVars[selectedVar] : null);

                        await OpenModifyWindowAsync(selectedVar, value, isUserNode);
                    }
                    else
                    {
                        string value;
                        bool isUserVar = ViewModel.UserEnvVars.ContainsKey(selectedVar);
                        
                        if (isUserVar)
                        {
                            value = ViewModel.UserEnvVars[selectedVar];
                        }
                        else
                        {
                            value = ViewModel.SystemEnvVars.ContainsKey(selectedVar) ? ViewModel.SystemEnvVars[selectedVar] : null;
                        }

                        if (value == null)
                        {
                            StatusLabel.Text = $"Cannot find the value of environment variable '{selectedVar}'.";
                            return;
                        }

                        await OpenModifyWindowAsync(selectedVar, value, isUserVar);
                    }
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private async Task OpenModifyWindowAsync(string selectedVar, string value, bool isUserVar)
        {
            if (selectedVar.ToLower() == "path")
            {
                var modifyPathWindow = new ModifyPathWindow(value, isUserVar);
                var modifyPathViewModel = modifyPathWindow.DataContext as ViewModels.ModifyPathWindowViewModel;
                if (modifyPathViewModel != null)
                {
                    modifyPathViewModel.PathModified += async (ss, se) =>
                    {
                        if (isUserVar)
                        {
                            ViewModel.UserEnvVars[selectedVar] = string.Join(";", modifyPathViewModel.PathEntries);
                        }
                        else
                        {
                            ViewModel.SystemEnvVars[selectedVar] = string.Join(";", modifyPathViewModel.PathEntries);
                        }
                        await ViewModel.UpdateListBoxAsync().ConfigureAwait(false);
                    };
                }
                modifyPathWindow.ShowDialog();
            }
            else
            {
                var modifyWindow = new ModifyEnvVarWindow(ViewModel.UserEnvVars, ViewModel.SystemEnvVars, selectedVar, value, isUserVar);
                modifyWindow.EnvVarModified += async (s, ev) =>
                {
                    await ViewModel.UpdateListBoxAsync().ConfigureAwait(false);
                };
                modifyWindow.ShowDialog();
            }
        }

        private TreeViewItem GetSelectedTreeViewNode()
        {
            var selectedItem = EnvVarTreeView.SelectedItem;

            if (selectedItem is TreeViewItem treeViewItem)
            {
                if (treeViewItem.Parent is TreeViewItem parentItem)
                {
                    return parentItem;
                }
                return treeViewItem;
            }
            else if (selectedItem is KeyValuePair<string, string> keyValuePair)
            {
                var parentItem = GetParentTreeViewItem(keyValuePair);
                return parentItem;
            }

            return null;
        }

        private TreeViewItem GetParentTreeViewItem(KeyValuePair<string, string> keyValuePair)
        {
            return FindParentTreeViewItem(keyValuePair);
        }

        private TreeViewItem FindParentTreeViewItem(KeyValuePair<string, string> keyValuePair)
        {
            foreach (var item in EnvVarTreeView.Items)
            {
                if (item is TreeViewItem treeViewItem)
                {
                    foreach (var child in treeViewItem.Items)
                    {
                        if (child is KeyValuePair<string, string> childKeyValuePair && childKeyValuePair.Equals(keyValuePair))
                        {
                            return treeViewItem;
                        }
                    }
                }
            }
            return null;
        }

        private async void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Check administrator privileges asynchronously
                if (!await ViewModel.EnvVarModel.IsAdministratorAsync())
                {
                    await ViewModel.EnvVarModel.ElevateAsync();
                    return; // Return to avoid further execution if privilege elevation is required
                }
                
                if (EnvVarListBox.SelectedItem != null)
                {
                    string selectedVar = EnvVarListBox.SelectedItem.ToString();
                    if (ViewModel.UserEnvVars.ContainsKey(selectedVar) || ViewModel.SystemEnvVars.ContainsKey(selectedVar))
                    {
                        var result = new ConfirmDeleteWindow(selectedVar).ShowDialog();
                        if (result == true)
                        {
                            try
                            {
                                bool success = await ViewModel.DeleteEnvVarAsync(selectedVar, EnvironmentVariableTarget.User);
                                if (success)
                                {
                                    success = await ViewModel.DeleteEnvVarAsync(selectedVar, EnvironmentVariableTarget.Machine);
                                }
                                
                                if (success)
                                {
                                    StatusLabel.Text = $"Deleted {selectedVar}";
                                }
                                else
                                {
                                    StatusLabel.Text = $"Failed to delete {selectedVar}";
                                }
                            }
                            catch (System.Security.SecurityException)
                            {
                                System.Windows.MessageBox.Show("Permission denied. You do not have sufficient privileges to delete environment variables at this scope.");
                            }
                            catch (Exception ex)
                            {
                                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
                            }
                        }
                    }
                    else
                    {
                        StatusLabel.Text = $"Environment variable {selectedVar} not found";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusLabel.Text = $"Error: {ex.Message}";
            }
        }

        private void PinToTop_Click(object sender, RoutedEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                var items = EnvVarListBox.ItemsSource as IEnumerable<string>;
                if (items != null && items.Contains(selectedVar))
                {
                    var newOrder = items.OrderByDescending(item => item == selectedVar).ToList();
                    EnvVarListBox.ItemsSource = newOrder;
                    EnvVarListBox.SelectedItem = selectedVar;
                }
            }
        }

    }
}
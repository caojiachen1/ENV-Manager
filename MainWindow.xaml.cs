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
using System.ComponentModel; // Added for INotifyPropertyChanged if needed for future binding
//using Wpf.Ui.Controls;

namespace EnvVarViewer
{
    public enum SortOrder
    {
        Ascending,
        Descending
    }

    public partial class MainWindow : Wpf.Ui.Controls.FluentWindow
    {
        private Dictionary<string, string> userEnvVars;
        private Dictionary<string, string> systemEnvVars;
        private Dictionary<string, string> modifiedEnvVars;
        private HashSet<string> deletedEnvVars;
        private List<Window> _windows = new List<Window>();
        private SortOrder _currentSortOrder = SortOrder.Ascending;

        public MainWindow()
        {
            InitializeComponent();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, Wpf.Ui.Controls.WindowBackdropType.Mica, true);
            
            // 检查是否具有管理员权限，如果没有则提权
            if (!IsAdministrator())
            {
                Elevate();
                return; // 程序将重新以管理员权限启动
            }
            
            modifiedEnvVars = new Dictionary<string, string>();
            deletedEnvVars = new HashSet<string>();
            LoadEnvVars();
            SearchBox.Focus(); // Set focus to the search box after initialization
            
            EnvVarTreeView.MouseDoubleClick += EnvVarTreeView_MouseDoubleClick;
            // SortOrderComboBox.SelectionChanged += SortOrderComboBox_SelectionChanged; // Removed as ComboBox is replaced by Button
        }

        // Removed SortOrderComboBox_SelectionChanged as ComboBox is replaced by Button
        // private void SortOrderComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        // {
        //     if (SortOrderComboBox.SelectedItem is ComboBoxItem selectedItem)
        //     {
        //         if (Enum.TryParse<SortOrder>(selectedItem.Tag?.ToString(), out var newSortOrder))
        //         {
        //             ChangeSortOrder(newSortOrder);
        //         }
        //     }
        // }

        private void SortOrderButton_Click(object sender, RoutedEventArgs e)
        {
            if (_currentSortOrder == SortOrder.Ascending)
            {
                ChangeSortOrder(SortOrder.Descending);
                SortOrderButton.Content = "↓";
            }
            else
            {
                ChangeSortOrder(SortOrder.Ascending);
                SortOrderButton.Content = "↑";
            }
        }

        private bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        private void Elevate()
        {
            //return; // For Debug
            _windows.Clear();
            foreach (Window window in Application.Current.Windows)
            {
                _windows.Add(window);
            }

            if (!IsAdministrator())
            {
                var processInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName)
                {
                    Verb = "runas",
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(processInfo);
                    Application.Current.Shutdown();

                    //foreach (Window window in _windows)
                    //{
                    //    window.Show();
                    //}
                }
                catch (System.ComponentModel.Win32Exception ex)
                {
                    // 用户拒绝了权限提升请求
                    if (ex.NativeErrorCode == 1223) // 1223 是用户取消操作的错误码
                    {
                        // 这里可以添加一些提示信息，告诉用户需要管理员权限才能继续
                        //MessageBox.Show("This operation requires administrator privileges. Please run the application as an administrator.", "Permission Denied", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    else
                    {
                        // 其他异常处理
                        throw;
                    }
                }
            }
        }

        private void LoadEnvVars()
        {
            userEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            systemEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            UpdateListBox();
        }

        private void UpdateListBox()
        {
            var query = userEnvVars.Keys
                .Union(systemEnvVars.Keys)
                .Union(modifiedEnvVars.Keys)
                .Except(deletedEnvVars);

            if (_currentSortOrder == SortOrder.Ascending)
            {
                query = query.OrderBy(k => k);
            }
            else
            {
                query = query.OrderByDescending(k => k);
            }

            var combinedEnvVars = query.ToDictionary(k => k, k =>
                {
                    string userValue = userEnvVars.ContainsKey(k) ? userEnvVars[k] : null;
                    string systemValue = systemEnvVars.ContainsKey(k) ? systemEnvVars[k] : null;
                    string modifiedValue = modifiedEnvVars.ContainsKey(k) ? modifiedEnvVars[k] : null;

                    if (modifiedValue != null)
                        return "Modified:\n" + FormatValue(modifiedValue);
                    else if (userValue != null && systemValue != null)
                        return $"User:\n{FormatValue(userValue)}\n\nSystem:\n{FormatValue(systemValue)}";
                    else if (userValue != null)
                        return "User:\n" + FormatValue(userValue);
                    else if (systemValue != null)
                        return "System:\n" + FormatValue(systemValue);
                    else
                        return "Unknown";
                });

            EnvVarListBox.ItemsSource = combinedEnvVars.Keys;

            // Preserve selection if possible after re-sorting/filtering
            string currentSearchTerm = SearchBox.Text;
            if (!string.IsNullOrEmpty(currentSearchTerm))
            {
                SearchBox_TextChanged(SearchBox, null); // Re-apply search filter which also considers sorting
            }
        }

        private string FormatValue(string value)
        {
            if (value == null)
                return null;

            return string.Join(";\n", value.Split(';'));
        }

        private void EnvVarListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (userEnvVars.ContainsKey(selectedVar) || systemEnvVars.ContainsKey(selectedVar) || modifiedEnvVars.ContainsKey(selectedVar))
                {
                    var userItem = EnvVarTreeView.Items[0] as TreeViewItem;
                    var systemItem = EnvVarTreeView.Items[1] as TreeViewItem;

                    userItem.Items.Clear();
                    systemItem.Items.Clear();

                    if (userEnvVars.ContainsKey(selectedVar))
                    {
                        userItem.Items.Add(new KeyValuePair<string, string>(selectedVar, userEnvVars[selectedVar]));
                    }

                    if (systemEnvVars.ContainsKey(selectedVar))
                    {
                        systemItem.Items.Add(new KeyValuePair<string, string>(selectedVar, systemEnvVars[selectedVar]));
                    }

                    if (modifiedEnvVars.ContainsKey(selectedVar))
                    {
                        if (userEnvVars.ContainsKey(selectedVar))
                        {
                            userItem.Items.Add(new KeyValuePair<string, string>(selectedVar, modifiedEnvVars[selectedVar]));
                        }
                        else if (systemEnvVars.ContainsKey(selectedVar))
                        {
                            systemItem.Items.Add(new KeyValuePair<string, string>(selectedVar, modifiedEnvVars[selectedVar]));
                        }
                    }

                    StatusLabel.Text = "";
                }
                else
                {
                    StatusLabel.Text = "Environment variable not found";
                }
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string searchTerm = SearchBox.Text.ToLower();
            var query = userEnvVars.Keys
                .Union(systemEnvVars.Keys)
                .Union(modifiedEnvVars.Keys)
                .Except(deletedEnvVars)
                .Where(k => k.ToLower().Contains(searchTerm));

            if (_currentSortOrder == SortOrder.Ascending)
            {
                query = query.OrderBy(k => k);
            }
            else
            {
                query = query.OrderByDescending(k => k);
            }
            EnvVarListBox.ItemsSource = query;
        }

        // Placeholder for SortOrder changed event handler (will be connected to UI element)
        public void ChangeSortOrder(SortOrder newOrder)
        {
            if (_currentSortOrder != newOrder)
            {
                _currentSortOrder = newOrder;
                UpdateListBox(); // Refresh the list with the new sort order
                // If search text exists, re-apply search to maintain filtering with new sort order
                if (!string.IsNullOrWhiteSpace(SearchBox.Text))
                {
                    SearchBox_TextChanged(SearchBox, null); 
                }
            }
        }

        //private void RefreshButton_Click(object sender, RoutedEventArgs e)
        //{
        //    LoadEnvVars();
        //    SearchBox.Text = ""; // 清空搜索栏
        //    StatusLabel.Text = "";
        //    SystemEnvList.Items.Clear();
        //    UserEnvList.Items.Clear();
        //}

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            string previouslySelectedVarKey = EnvVarListBox.SelectedItem as string;

            LoadEnvVars(); // This calls UpdateListBox(), which re-populates EnvVarListBox.ItemsSource

            SearchBox.Text = ""; // 清空搜索栏
            StatusLabel.Text = "";
            
            // Clear the TreeView display first. If no item is re-selected, it remains empty.
            SystemEnvList.Items.Clear(); 
            UserEnvList.Items.Clear();

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
        }

        private void BackupRestoreButton_Click(object sender, RoutedEventArgs e)
        {
            Elevate(); // 确保有管理员权限
            var backupRestoreWindow = new BackupRestoreWindow(userEnvVars, systemEnvVars);
            backupRestoreWindow.ShowDialog();
            
            // 窗口关闭后刷新环境变量列表，以显示可能的更改
            RefreshButton_Click(sender, e);
        }

        private void EnvVarListBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (systemEnvVars.ContainsKey(selectedVar) || userEnvVars.ContainsKey(selectedVar) || modifiedEnvVars.ContainsKey(selectedVar))
                {
                    string value;
                    string source;

                    if (systemEnvVars.ContainsKey(selectedVar))
                    {
                        value = systemEnvVars[selectedVar];
                        source = "System";
                    }
                    else if (userEnvVars.ContainsKey(selectedVar))
                    {
                        value = userEnvVars[selectedVar];
                        source = "User";
                    }
                    else
                    {
                        value = modifiedEnvVars[selectedVar];
                        source = "Modified";
                    }

                    Clipboard.SetText(value);
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
                Clipboard.SetText(keyValuePair.Value);
                StatusLabel.Text = $"Copied {keyValuePair.Key} ({source}) to clipboard";
            }
        }

        private string GetSource(string key, string value)
        {
            bool isUser = userEnvVars.ContainsKey(key) && userEnvVars[key] == value;
            bool isSystem = systemEnvVars.ContainsKey(key) && systemEnvVars[key] == value;
            bool isModified = modifiedEnvVars.ContainsKey(key) && modifiedEnvVars[key] == value;

            if (isUser)
            {
                return "User";
            }
            else if (isSystem)
            {
                return "System";
            }
            else if (isModified)
            {
                return "Modified";
            }
            return "Unknown";
        }

        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            Elevate();
            var addWindow = new AddModifyEnvVarWindow(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars);
            addWindow.EnvVarAdded += (s, ev) =>
            {
                UpdateListBox();
            };
            addWindow.ShowDialog();
        }

        private void ModifyButton_Click(object sender, RoutedEventArgs e)
        {
            Elevate();

            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                var selectedNode = GetSelectedTreeViewNode();

                if (selectedNode != null)
                {
                    bool isUserNode = selectedNode.Header.ToString() == "User";
                    string value = isUserNode ?
                                    (userEnvVars.ContainsKey(selectedVar) ? userEnvVars[selectedVar] : null) :
                                    (systemEnvVars.ContainsKey(selectedVar) ? systemEnvVars[selectedVar] : null);

                    if (selectedVar.ToLower() == "path")
                    {
                        var modifyPathWindow = new ModifyPathWindow(value, isUserNode);
                        modifyPathWindow.PathModified += (ss, se) =>
                        {
                            if (isUserNode)
                            {
                                userEnvVars[selectedVar] = modifyPathWindow.GetPathValue();
                            }
                            else
                            {
                                systemEnvVars[selectedVar] = modifyPathWindow.GetPathValue();
                            }
                            UpdateListBox();
                        };
                        modifyPathWindow.ShowDialog();
                    }
                    else
                    {
                        var modifyWindow = new AddModifyEnvVarWindow(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars, selectedVar, value);
                        modifyWindow.EnvVarModified += (s, ev) =>
                        {
                            UpdateListBox();
                        };
                        modifyWindow.ShowDialog();
                    }
                }
                else
                {
                    string value = (systemEnvVars.ContainsKey(selectedVar) ? systemEnvVars[selectedVar] : null);

                    if (selectedVar.ToLower() == "path")
                    {
                        var modifyPathWindow = new ModifyPathWindow(value, false);
                        modifyPathWindow.PathModified += (ss, se) =>
                        {
                            systemEnvVars[selectedVar] = modifyPathWindow.GetPathValue();
                            UpdateListBox();
                        };
                        modifyPathWindow.ShowDialog();
                    }
                    else
                    {
                        var modifyWindow = new AddModifyEnvVarWindow(userEnvVars, systemEnvVars, modifiedEnvVars, deletedEnvVars, selectedVar, value);
                        modifyWindow.EnvVarModified += (s, ev) =>
                        {
                            UpdateListBox();
                        };
                        modifyWindow.ShowDialog();
                    }
                }
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

        private void DeleteButton_Click(object sender, RoutedEventArgs e)
        {
            Elevate();
            if (EnvVarListBox.SelectedItem != null)
            {
                string selectedVar = EnvVarListBox.SelectedItem.ToString();
                if (userEnvVars.ContainsKey(selectedVar) || systemEnvVars.ContainsKey(selectedVar) || modifiedEnvVars.ContainsKey(selectedVar))
                {
                    var result = new ConfirmDeleteWindow(selectedVar).ShowDialog();
                    //var result = MessageBox.Show($"Are you sure you want to delete the environment variable '{selectedVar}'?", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                    if (result == true)
                    {
                        try
                        {
                            Environment.SetEnvironmentVariable(selectedVar, null, EnvironmentVariableTarget.User);
                            Environment.SetEnvironmentVariable(selectedVar, null, EnvironmentVariableTarget.Machine);
                            if (modifiedEnvVars.ContainsKey(selectedVar))
                            {
                                modifiedEnvVars.Remove(selectedVar);
                            }
                            deletedEnvVars.Add(selectedVar);
                            UpdateListBox();
                            StatusLabel.Text = $"Deleted {selectedVar}";
                        }
                        catch (System.Security.SecurityException ex)
                        {
                            MessageBox.Show("Permission denied. You do not have sufficient privileges to delete environment variables at this scope.");
                        }
                        catch (Exception ex)
                        {
                            MessageBox.Show($"An error occurred: {ex.Message}");
                        }
                    }
                }
                else
                {
                    StatusLabel.Text = $"Environment variable {selectedVar} not found";
                }
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
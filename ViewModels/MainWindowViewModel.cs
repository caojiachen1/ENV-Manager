using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Appearance;

namespace EnvVarViewer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private Dictionary<string, string> _userEnvVars;
        private Dictionary<string, string> _systemEnvVars;
        private Dictionary<string, string> _modifiedEnvVars;
        private HashSet<string> _deletedEnvVars;

        public Dictionary<string, string> UserEnvVars => _userEnvVars;
        public Dictionary<string, string> SystemEnvVars => _systemEnvVars;
        public Dictionary<string, string> ModifiedEnvVars => _modifiedEnvVars;
        public HashSet<string> DeletedEnvVars => _deletedEnvVars;
        
        private SortOrder _currentSortOrder;
        private string _searchText;
        private string _statusText;
        private string _selectedEnvVar;
        private IEnumerable<string> _envVarList;

        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class
        /// </summary>
        public MainWindowViewModel()
        {
            _modifiedEnvVars = new Dictionary<string, string>();
            _deletedEnvVars = new HashSet<string>();
            _currentSortOrder = SortOrder.Ascending;
            LoadEnvVars();
        }

        public IEnumerable<string> EnvVarList
        {
            get => _envVarList;
            private set
            {
                _envVarList = value;
                OnPropertyChanged(nameof(EnvVarList));
            }
        }

        public string SearchText
        {
            get => _searchText;
            set
            {
                _searchText = value;
                OnPropertyChanged(nameof(SearchText));
                UpdateListBox();
            }
        }

        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public string SelectedEnvVar
        {
            get => _selectedEnvVar;
            set
            {
                _selectedEnvVar = value;
                OnPropertyChanged(nameof(SelectedEnvVar));
            }
        }

        public SortOrder CurrentSortOrder
        {
            get => _currentSortOrder;
            set
            {
                _currentSortOrder = value;
                OnPropertyChanged(nameof(CurrentSortOrder));
                UpdateListBox();
            }
        }

        /// <summary>
        /// Loads environment variables from both user and system scope
        /// </summary>
        public void LoadEnvVars()
        {
            _userEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            _systemEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            UpdateListBox();
        }

        /// <summary>
        /// Updates the environment variables list based on current filters and sorting
        /// </summary>
        public void UpdateListBox()
        {
            // 重新加载环境变量
            _userEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.User)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            _systemEnvVars = Environment.GetEnvironmentVariables(EnvironmentVariableTarget.Machine)
                .Cast<System.Collections.DictionaryEntry>()
                .ToDictionary(kv => kv.Key.ToString(), kv => kv.Value.ToString());

            // 更新列表
            var query = _userEnvVars.Keys
                .Union(_systemEnvVars.Keys)
                .Union(_modifiedEnvVars.Keys)
                .Except(_deletedEnvVars);

            if (!string.IsNullOrEmpty(SearchText))
            {
                query = query.Where(k => k.ToLower().Contains(SearchText.ToLower()));
            }

            query = CurrentSortOrder == SortOrder.Ascending
                ? query.OrderBy(k => k)
                : query.OrderByDescending(k => k);

            EnvVarList = query.ToList();

            // 触发属性变更通知
            OnPropertyChanged(nameof(UserEnvVars));
            OnPropertyChanged(nameof(SystemEnvVars));
            OnPropertyChanged(nameof(ModifiedEnvVars));
            OnPropertyChanged(nameof(DeletedEnvVars));
            
            // 通知主窗口强制刷新列表
            EnvVarListUpdated?.Invoke(this, EventArgs.Empty);
        }
        
        /// <summary>
        /// 环境变量列表更新事件
        /// </summary>
        public event EventHandler EnvVarListUpdated;

        /// <summary>
        /// Checks if current user has administrator privileges
        /// </summary>
        /// <returns>True if user is administrator</returns>
        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        /// <summary>
        /// Attempts to elevate application privileges by restarting as administrator
        /// </summary>
        public void Elevate()
        {
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
                }
                catch (Win32Exception ex)
                {
                    if (ex.NativeErrorCode == 1223)
                    {
                        StatusText = "Administrator privileges required to continue";
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
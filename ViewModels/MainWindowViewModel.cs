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
    public class MainWindowViewModel : INotifyPropertyChanged
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

        public void UpdateListBox()
        {
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
        }

        public bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

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
                        StatusText = "需要管理员权限才能继续操作";
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
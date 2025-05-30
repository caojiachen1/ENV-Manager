using System;
using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// ViewModel for Modifying Environment Variable Window
    /// </summary>
    public class ModifyEnvVarViewModel : ViewModelBase
    {
        private Dictionary<string, string> _userEnvVars;
        private Dictionary<string, string> _systemEnvVars;
        private string _originalName;
        private string _name;
        private string _value;
        private int _selectedScopeIndex;

        public event EventHandler EnvVarModified;

        /// <summary>
        /// Environment variable name
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (SetProperty(ref _name, value)) {
                    if (SaveCommand != null) {
                        SaveCommand.NotifyCanExecuteChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Environment variable value
        /// </summary>
        public string Value
        {
            get => _value;
            set
            {
                if (string.IsNullOrEmpty(value)) return;
                if (SetProperty(ref _value, value)) {
                    if (SaveCommand != null) {
                        SaveCommand.NotifyCanExecuteChanged();
                    }
                }
            }
        }

        /// <summary>
        /// Selected scope index (0=User, 1=System)
        /// </summary>
        public int SelectedScopeIndex
        {
            get => _selectedScopeIndex;
            set => SetProperty(ref _selectedScopeIndex, value);
        }

        /// <summary>
        /// Save command
        /// </summary>
        public RelayCommand SaveCommand { get; }

        /// <summary>
        /// Initializes a new instance for modifying existing environment variables
        /// </summary>
        public ModifyEnvVarViewModel(
            Dictionary<string, string> userEnvVars,
            Dictionary<string, string> systemEnvVars,
            string name,
            string value)
        {
            _userEnvVars = userEnvVars;
            _systemEnvVars = systemEnvVars;
            _originalName = name;
            Name = name;
            Value = value;

            if (userEnvVars.ContainsKey(name))
            {
                SelectedScopeIndex = 0; // User
            }
            else if (systemEnvVars.ContainsKey(name))
            {
                SelectedScopeIndex = 1; // System
            }

            SaveCommand = new RelayCommand(ExecuteSave, CanExecuteSave);
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Value);
        }

        private void ExecuteSave()
        {
            string scope = SelectedScopeIndex switch
            {
                0 => "User",
                1 => "Machine",
                _ => "Process"
            };

            EnvironmentVariableTarget target = scope switch
            {
                "Process" => EnvironmentVariableTarget.Process,
                "User" => EnvironmentVariableTarget.User,
                "Machine" => EnvironmentVariableTarget.Machine,
                _ => EnvironmentVariableTarget.Process
            };

            try
            {
                Environment.SetEnvironmentVariable(_originalName, null, target);
                Environment.SetEnvironmentVariable(Name, Value, target);
                
                if (target == EnvironmentVariableTarget.User)
                {
                    _userEnvVars[Name] = Value;
                    if (_originalName != Name && _userEnvVars.ContainsKey(_originalName))
                    {
                        _userEnvVars.Remove(_originalName);
                    }
                }
                else if (target == EnvironmentVariableTarget.Machine)
                {
                    _systemEnvVars[Name] = Value;
                    if (_originalName != Name && _systemEnvVars.ContainsKey(_originalName))
                    {
                        _systemEnvVars.Remove(_originalName);
                    }
                }
                
                EnvVarModified?.Invoke(this, EventArgs.Empty);
                CloseWindow?.Invoke(this, EventArgs.Empty);
            }
            catch (System.Security.SecurityException)
            {
                System.Windows.MessageBox.Show("Permission denied. You do not have sufficient privileges to modify environment variables at this scope.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Window close event
        /// </summary>
        public event EventHandler CloseWindow;
    }
}
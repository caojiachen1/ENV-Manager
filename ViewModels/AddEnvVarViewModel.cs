using System;
using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.Input;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// ViewModel for Adding Environment Variable Window
    /// </summary>
    public class AddEnvVarViewModel : ViewModelBase
    {
        private Dictionary<string, string> _userEnvVars;
        private Dictionary<string, string> _systemEnvVars;
        private string _name;
        private string _value;
        private int _selectedScopeIndex;

        public event EventHandler EnvVarAdded;

        /// <summary>
        /// Environment variable name
        /// </summary>
        public string Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
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
                if (SetProperty(ref _value, value))
                {
                    SaveCommand.NotifyCanExecuteChanged();
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
        /// Initializes a new instance for adding environment variables
        /// </summary>
        public AddEnvVarViewModel(
            Dictionary<string, string> userEnvVars,
            Dictionary<string, string> systemEnvVars)
        {
            _userEnvVars = userEnvVars;
            _systemEnvVars = systemEnvVars;

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

            if (_userEnvVars.ContainsKey(Name) || _systemEnvVars.ContainsKey(Name))
            {
                MessageBox.Show($"Environment variable '{Name}' already exists.");
                return;
            }

            EnvironmentVariableTarget target = scope switch
            {
                "Process" => EnvironmentVariableTarget.Process,
                "User" => EnvironmentVariableTarget.User,
                "Machine" => EnvironmentVariableTarget.Machine,
                _ => EnvironmentVariableTarget.Process
            };

            try
            {
                Environment.SetEnvironmentVariable(Name, Value, target);
                if (target == EnvironmentVariableTarget.User)
                {
                    _userEnvVars[Name] = Value;
                }
                else if (target == EnvironmentVariableTarget.Machine)
                {
                    _systemEnvVars[Name] = Value;
                }
                EnvVarAdded?.Invoke(this, EventArgs.Empty);

                CloseWindow?.Invoke(this, EventArgs.Empty);
            }
            catch (System.Security.SecurityException)
            {
                MessageBox.Show("Permission denied. You do not have sufficient privileges to modify environment variables at this scope.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred: {ex.Message}");
            }
        }

        /// <summary>
        /// Window close event
        /// </summary>
        public event EventHandler CloseWindow;
    }
}
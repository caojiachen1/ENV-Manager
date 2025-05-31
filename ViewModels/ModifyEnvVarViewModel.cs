using System;
using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

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
            set
            {
                if (SetProperty(ref _selectedScopeIndex, value)) {
                    // Update the value based on the selected scope
                    string varName = Name;
                    if (value == 0 && _userEnvVars.ContainsKey(varName)) {
                        Value = _userEnvVars[varName];
                    } else if (value == 1 && _systemEnvVars.ContainsKey(varName)) {
                        Value = _systemEnvVars[varName];
                    } else {
                        Value = "";
                    }
                }
            }
        }

        /// <summary>
        /// Save command
        /// </summary>
        public AsyncRelayCommand SaveCommand { get; }

        /// <summary>
        /// Initializes a new instance for modifying existing environment variables
        /// </summary>
        public ModifyEnvVarViewModel(
            Dictionary<string, string> userEnvVars,
            Dictionary<string, string> systemEnvVars,
            string name,
            string value,
            string scope)
        {
            _userEnvVars = userEnvVars;
            _systemEnvVars = systemEnvVars;
            _originalName = name;
            Name = name;
            Value = value;
            SelectedScopeIndex = scope switch
            {
                "user" => 0,
                "system" => 1,
                _ => userEnvVars.ContainsKey(name) ? 0 : 1
            };

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanExecuteSave);
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrEmpty(Name) && !string.IsNullOrEmpty(Value);
        }

        private async Task ExecuteSaveAsync()
        {
            try
            {
                SetLoadingState(true, "Saving environment variable...");
                
                string scope = SelectedScopeIndex switch
                {
                    0 => "User",
                    1 => "System",
                    _ => "User"
                };

                EnvironmentVariableTarget target = scope switch
                {
                    "User" => EnvironmentVariableTarget.User,
                    "System" => EnvironmentVariableTarget.Machine,
                    _ => EnvironmentVariableTarget.User
                };

                var model = new Models.EnvironmentVariableModel();
                
                // Delete old variable if name changed
                if (_originalName != Name)
                {
                    await model.DeleteEnvVarAsync(_originalName, target, CancellationTokenSource.Token);
                }
                
                // Set new variable
                await model.SetEnvVarAsync(Name, Value, target, CancellationTokenSource.Token);
                
                // Update local dictionaries
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
            catch (OperationCanceledException)
            {
                // Operation was cancelled
            }
            catch (System.Security.SecurityException)
            {
                System.Windows.MessageBox.Show("Permission denied. You do not have sufficient privileges to modify environment variables at this scope.");
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"An error occurred: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Window close event
        /// </summary>
        public event EventHandler CloseWindow;
    }
}
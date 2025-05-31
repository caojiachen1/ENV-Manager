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
        private readonly Dictionary<string, string> _userEnvVars;
        private readonly Dictionary<string, string> _systemEnvVars;
        private readonly string _originalName;
        private string? _name;
        private string? _value;
        private int _selectedScopeIndex;

        public event EventHandler? EnvVarModified;
        public event EventHandler? CloseWindow;

        /// <summary>
        /// Environment variable name
        /// </summary>
        public string? Name
        {
            get => _name;
            set
            {
                if (SetProperty(ref _name, value))
                {
                    SaveCommand?.NotifyCanExecuteChanged();
                    ErrorMessage = null; // Clear errors when name changes
                }
            }
        }

        /// <summary>
        /// Environment variable value
        /// </summary>
        public string? Value
        {
            get => _value;
            set
            {
                if (SetProperty(ref _value, value))
                {
                    SaveCommand?.NotifyCanExecuteChanged();
                    ErrorMessage = null; // Clear errors when value changes
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
                if (SetProperty(ref _selectedScopeIndex, value))
                {
                    UpdateValueFromScope();
                }
            }
        }

        /// <summary>
        /// Save command
        /// </summary>
        public AsyncRelayCommand SaveCommand { get; }

        public ModifyEnvVarViewModel(
            Dictionary<string, string> userEnvVars,
            Dictionary<string, string> systemEnvVars,
            string name,
            string value,
            string scope)
        {
            _userEnvVars = userEnvVars ?? throw new ArgumentNullException(nameof(userEnvVars));
            _systemEnvVars = systemEnvVars ?? throw new ArgumentNullException(nameof(systemEnvVars));
            _originalName = name ?? throw new ArgumentNullException(nameof(name));
            
            Name = name;
            Value = value;
            SelectedScopeIndex = scope?.ToLower() switch
            {
                "user" => 0,
                "system" => 1,
                _ => userEnvVars.ContainsKey(name) ? 0 : 1
            };

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanExecuteSave);
        }

        private void UpdateValueFromScope()
        {
            if (string.IsNullOrEmpty(Name)) return;

            string? scopeValue = SelectedScopeIndex switch
            {
                0 when _userEnvVars.TryGetValue(Name, out var userVal) => userVal,
                1 when _systemEnvVars.TryGetValue(Name, out var systemVal) => systemVal,
                _ => string.Empty
            };
            
            if (scopeValue != null)
                Value = scopeValue;
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Value);
        }

        private async Task ExecuteSaveAsync()
        {
            if (!ValidateInput(Name, "Variable name") || !ValidateInput(Value, "Variable value"))
                return;

            await ExecuteAsync(async cancellationToken =>
            {
                var target = SelectedScopeIndex switch
                {
                    0 => EnvironmentVariableTarget.User,
                    1 => EnvironmentVariableTarget.Machine,
                    _ => EnvironmentVariableTarget.User
                };

                var model = new Models.EnvironmentVariableModel();
                
                // Delete old variable if name changed
                if (_originalName != Name)
                {
                    await model.DeleteEnvVarAsync(_originalName, target, cancellationToken).ConfigureAwait(false);
                }
                
                // Set new variable
                await model.SetEnvVarAsync(Name!, Value!, target, cancellationToken).ConfigureAwait(false);
                
                // Update local dictionaries
                var targetDict = target == EnvironmentVariableTarget.User ? _userEnvVars : _systemEnvVars;
                targetDict[Name!] = Value!;
                
                if (_originalName != Name && targetDict.ContainsKey(_originalName))
                {
                    targetDict.Remove(_originalName);
                }
                
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    EnvVarModified?.Invoke(this, EventArgs.Empty);
                    CloseWindow?.Invoke(this, EventArgs.Empty);
                });
            }, "Saving environment variable...", "Failed to save environment variable");
        }
    }
}
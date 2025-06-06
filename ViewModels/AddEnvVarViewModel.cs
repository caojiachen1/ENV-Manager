using System;
using System.Collections.Generic;
using System.Windows;
using CommunityToolkit.Mvvm.Input;
using System.Threading.Tasks;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// ViewModel for Adding Environment Variable Window
    /// </summary>
    public class AddEnvVarViewModel : ViewModelBase
    {
        private readonly Dictionary<string, string> _userEnvVars;
        private readonly Dictionary<string, string> _systemEnvVars;
        private string? _name;
        private string? _value;
        private int _selectedScopeIndex;

        public event EventHandler? EnvVarAdded;
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
                    SaveCommand.NotifyCanExecuteChanged();
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
                    SaveCommand.NotifyCanExecuteChanged();
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
            set => SetProperty(ref _selectedScopeIndex, value);
        }

        /// <summary>
        /// Save command
        /// </summary>
        public AsyncRelayCommand SaveCommand { get; }

        public AddEnvVarViewModel(
            Dictionary<string, string> userEnvVars,
            Dictionary<string, string> systemEnvVars)
        {
            _userEnvVars = userEnvVars ?? throw new ArgumentNullException(nameof(userEnvVars));
            _systemEnvVars = systemEnvVars ?? throw new ArgumentNullException(nameof(systemEnvVars));

            SaveCommand = new AsyncRelayCommand(ExecuteSaveAsync, CanExecuteSave);
        }

        private bool CanExecuteSave()
        {
            return !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(Value);
        }

        private async Task ExecuteSaveAsync()
        {
            if (!ValidateInput(Name, "Variable name") || !ValidateInput(Value, "Variable value"))
                return;

            // Check for invalid characters in environment variable names
            if (Name!.Contains('=') || Name.Contains('\0'))
            {
                SetErrorState("Environment variable name contains invalid characters");
                return;
            }

            var target = SelectedScopeIndex switch
            {
                0 => EnvironmentVariableTarget.User,
                1 => EnvironmentVariableTarget.Machine,
                _ => EnvironmentVariableTarget.User
            };

            // Check if variable already exists in current scope
            var targetDict = target == EnvironmentVariableTarget.User ? _userEnvVars : _systemEnvVars;
            if (targetDict.ContainsKey(Name!))
            {
                var scopeName = target == EnvironmentVariableTarget.User ? "User" : "System";
                SetErrorState($"Environment variable '{Name}' already exists in {scopeName} scope");
                return;
            }

            // Close window immediately after validation
            CloseWindow?.Invoke(this, EventArgs.Empty);

            // Execute the operation asynchronously in background
            _ = Task.Run(async () =>
            {
                try
                {
                    // Show status message on UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetStatusMessage($"Adding environment variable '{Name}'...", false);
                    });

                    var model = new Models.EnvironmentVariableModel();
                    await model.SetEnvVarAsync(Name!, Value!, target, CancellationToken).ConfigureAwait(false);
                    
                    // Update local dictionary
                    targetDict[Name!] = Value!;
                    
                    // Update UI on dispatcher thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetStatusMessage($"Successfully added environment variable '{Name}'");
                        EnvVarAdded?.Invoke(this, EventArgs.Empty);
                    });
                }
                catch (OperationCanceledException)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetStatusMessage("Operation was cancelled", true);
                    });
                }
                catch (UnauthorizedAccessException ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetStatusMessage($"Access denied: {ex.Message}", true);
                    });
                }
                catch (Exception ex)
                {
                    System.Windows.Application.Current.Dispatcher.Invoke(() =>
                    {
                        SetStatusMessage($"Failed to add environment variable: {ex.Message}", true);
                    });
                    System.Diagnostics.Debug.WriteLine($"Error in {GetType().Name}: {ex}");
                }
            });
        }
    }
}
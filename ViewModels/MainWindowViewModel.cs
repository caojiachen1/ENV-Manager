using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Security.Principal;
using System.Windows;
using System.Windows.Input;
using Wpf.Ui.Appearance;
using EnvVarViewer.Models;
using System.Threading.Tasks;
using System.Threading;

namespace EnvVarViewer.ViewModels
{
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly EnvironmentVariableModel _envVarModel;
        private Dictionary<string, string>? _userEnvVars;
        private Dictionary<string, string>? _systemEnvVars;
        
        private EnvVarViewer.Models.SortOrder _currentSortOrder = EnvVarViewer.Models.SortOrder.Ascending;
        private string? _searchText;
        private string _statusText = "Ready";
        private string? _selectedEnvVar;
        private IEnumerable<string>? _envVarList;
        private readonly SemaphoreSlim _loadingSemaphore = new(1, 1);

        public Dictionary<string, string> UserEnvVars => _userEnvVars ?? new Dictionary<string, string>();
        public Dictionary<string, string> SystemEnvVars => _systemEnvVars ?? new Dictionary<string, string>();
        
        /// <summary>
        /// Event raised when the environment variables list is updated
        /// </summary>
        public event EventHandler? EnvVarListUpdated;

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class
        /// </summary>
        public MainWindowViewModel()
        {
            _envVarModel = new EnvironmentVariableModel();
            _ = LoadEnvVarsAsync(); // Fire and forget for initial load
        }

        public IEnumerable<string>? EnvVarList
        {
            get => _envVarList;
            private set => SetProperty(ref _envVarList, value);
        }

        public string? SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = UpdateListBoxAsync();
                }
            }
        }

        public string StatusText
        {
            get => _statusText;
            set => SetProperty(ref _statusText, value);
        }

        public string? SelectedEnvVar
        {
            get => _selectedEnvVar;
            set => SetProperty(ref _selectedEnvVar, value);
        }

        public EnvVarViewer.Models.SortOrder CurrentSortOrder
        {
            get => _currentSortOrder;
            set
            {
                if (SetProperty(ref _currentSortOrder, value))
                {
                    _ = UpdateListBoxAsync();
                }
            }
        }

        /// <summary>
        /// Gets the environment variable model for external access
        /// </summary>
        public EnvironmentVariableModel EnvVarModel => _envVarModel;

        /// <summary>
        /// Loads environment variables from both user and system scope asynchronously
        /// </summary>
        public async Task LoadEnvVarsAsync()
        {
            if (!await _loadingSemaphore.WaitAsync(100))
                return;

            try
            {
                await ExecuteAsync(async cancellationToken =>
                {
                    var tasks = new[]
                    {
                        _envVarModel.LoadEnvVarsAsync(EnvironmentVariableTarget.User, cancellationToken),
                        _envVarModel.LoadEnvVarsAsync(EnvironmentVariableTarget.Machine, cancellationToken)
                    };
                    
                    var results = await Task.WhenAll(tasks).ConfigureAwait(false);
                    
                    _userEnvVars = results[0];
                    _systemEnvVars = results[1];
                    
                    await UpdateListBoxAsync().ConfigureAwait(false);
                    
                    // Update status on UI thread
                    System.Windows.Application.Current.Dispatcher.Invoke(() => 
                        StatusText = $"Loaded {_userEnvVars.Count} user and {_systemEnvVars.Count} system variables");
                }, "Loading environment variables...", "Failed to load environment variables");
            }
            finally
            {
                _loadingSemaphore.Release();
            }
        }

        /// <summary>
        /// Updates the environment variables list based on current filters and sorting asynchronously
        /// </summary>
        public async Task UpdateListBoxAsync()
        {
            if (_userEnvVars == null || _systemEnvVars == null)
                return;

            await ExecuteAsync(async cancellationToken =>
            {
                var result = await Task.Run(() =>
                {
                    var allKeys = new HashSet<string>(_userEnvVars.Keys);
                    allKeys.UnionWith(_systemEnvVars.Keys);

                    IEnumerable<string> query = allKeys;

                    if (!string.IsNullOrWhiteSpace(SearchText))
                    {
                        var searchLower = SearchText.ToLowerInvariant();
                        query = query.Where(k => k.ToLowerInvariant().Contains(searchLower));
                    }

                    query = CurrentSortOrder == EnvVarViewer.Models.SortOrder.Ascending
                        ? query.OrderBy(k => k, StringComparer.OrdinalIgnoreCase)
                        : query.OrderByDescending(k => k, StringComparer.OrdinalIgnoreCase);

                    return query.ToList();
                }, cancellationToken).ConfigureAwait(false);

                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    EnvVarList = result;
                    EnvVarListUpdated?.Invoke(this, EventArgs.Empty);
                });
            }, errorPrefix: "Failed to update environment variables list");
        }

        /// <summary>
        /// Sets an environment variable asynchronously
        /// </summary>
        public async Task<bool> SetEnvVarAsync(string name, string value, EnvironmentVariableTarget target)
        {
            if (!ValidateInput(name, "Variable name") || !ValidateInput(value, "Variable value"))
                return false;

            try
            {
                await ExecuteAsync(async cancellationToken =>
                {
                    await _envVarModel.SetEnvVarAsync(name, value, target, cancellationToken).ConfigureAwait(false);
                    await LoadEnvVarsAsync().ConfigureAwait(false);
                }, $"Setting environment variable '{name}'...", "Failed to set environment variable");

                StatusText = $"Environment variable '{name}' set successfully";
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Deletes an environment variable asynchronously
        /// </summary>
        public async Task<bool> DeleteEnvVarAsync(string name, EnvironmentVariableTarget target)
        {
            if (!ValidateInput(name, "Variable name"))
                return false;

            try
            {
                await ExecuteAsync(async cancellationToken =>
                {
                    await _envVarModel.DeleteEnvVarAsync(name, target, cancellationToken).ConfigureAwait(false);
                    await LoadEnvVarsAsync().ConfigureAwait(false);
                }, $"Deleting environment variable '{name}'...", "Failed to delete environment variable");

                StatusText = $"Environment variable '{name}' deleted successfully";
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Cancels all ongoing async operations
        /// </summary>
        public void CancelOperations()
        {
            try
            {
                CancellationTokenSource?.Cancel();
                StatusText = "Operations cancelled";
            }
            catch (Exception ex)
            {
                StatusText = $"Error cancelling operations: {ex.Message}";
            }
        }

        /// <summary>
        /// Refreshes the environment variables list asynchronously
        /// </summary>
        public async Task RefreshAsync()
        {
            await LoadEnvVarsAsync();
        }

        // Synchronous versions for backward compatibility
        public void LoadEnvVars() => _ = LoadEnvVarsAsync();
        public void UpdateListBox() => _ = UpdateListBoxAsync();

        /// <summary>
        /// Checks if current user has administrator privileges
        /// </summary>
        /// <returns>True if user is administrator</returns>
        public bool IsAdministrator()
        {
            return _envVarModel.IsAdministrator(); // Use Model
        }

        /// <summary>
        /// Sets the loading state and updates status text
        /// </summary>
        /// <param name="isLoading">Whether the application is loading</param>
        /// <param name="statusMessage">Optional status message to display</param>
        private void SetLoadingState(bool isLoading, string statusMessage = null)
        {
            IsLoading = isLoading;
            if (!string.IsNullOrEmpty(statusMessage))
            {
                StatusText = statusMessage;
            }
            else if (!isLoading)
            {
                StatusText = "Ready";
            }
        }

        /// <summary>
        /// Attempts to elevate application privileges by restarting as administrator asynchronously
        /// </summary>
        public async Task ElevateAsync()
        {
            await ExecuteAsync(async cancellationToken =>
            {
                bool isAdmin = await _envVarModel.IsAdministratorAsync(cancellationToken).ConfigureAwait(false);
                if (!isAdmin)
                {
                    bool success = await _envVarModel.ElevateAsync(cancellationToken).ConfigureAwait(false);
                    if (success)
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
                    }
                    else
                    {
                        System.Windows.Application.Current.Dispatcher.Invoke(() => 
                            StatusText = "Administrator privileges required to continue");
                    }
                }
            }, "Elevating privileges...", "Failed to elevate privileges");
        }

        /// <summary>
        /// Attempts to elevate application privileges by restarting as administrator
        /// </summary>
        public void Elevate()
        {
            if (!_envVarModel.IsAdministrator())
            {
                var processInfo = new ProcessStartInfo(Process.GetCurrentProcess().MainModule.FileName)
                {
                    Verb = "runas",
                    UseShellExecute = true
                };

                try
                {
                    Process.Start(processInfo);
                    System.Windows.Application.Current.Shutdown();
                }
                catch (System.ComponentModel.Win32Exception ex)
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

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _loadingSemaphore?.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
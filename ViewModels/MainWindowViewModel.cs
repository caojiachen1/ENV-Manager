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
    public enum SortOrder
    {
        Ascending,
        Descending
    }

    public class MainWindowViewModel : ViewModelBase
    {
        private readonly EnvironmentVariableModel _envVarModel;
        private Dictionary<string, string> _userEnvVars;
        private Dictionary<string, string> _systemEnvVars;
        private CancellationTokenSource _cancellationTokenSource;
        private bool _isLoading;

        public Dictionary<string, string> UserEnvVars => _userEnvVars;
        public Dictionary<string, string> SystemEnvVars => _systemEnvVars;
        
        private SortOrder _currentSortOrder;
        private string _searchText;
        private string _statusText;
        private string _selectedEnvVar;
        private IEnumerable<string> _envVarList;

        public event PropertyChangedEventHandler PropertyChanged;
        
        /// <summary>
        /// Event raised when the environment variables list is updated
        /// </summary>
        public event EventHandler EnvVarListUpdated;

        /// <summary>
        /// Gets or sets the cancellation token source for async operations
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource
        {
            get => _cancellationTokenSource;
            set => _cancellationTokenSource = value;
        }

        /// <summary>
        /// Gets or sets whether the view model is currently loading data
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            private set
            {
                _isLoading = value;
                OnPropertyChanged(nameof(IsLoading));
            }
        }

        /// <summary>
        /// Initializes a new instance of the MainWindowViewModel class
        /// </summary>
        public MainWindowViewModel()
        {
            _envVarModel = new EnvironmentVariableModel();
            _currentSortOrder = SortOrder.Ascending;
            _ = LoadEnvVarsAsync(); // Fire and forget for initial load
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
        /// Gets the environment variable model for external access
        /// </summary>
        public EnvironmentVariableModel EnvVarModel => _envVarModel;

        /// <summary>
        /// Loads environment variables from both user and system scope asynchronously
        /// </summary>
        public async Task LoadEnvVarsAsync()
        {
            try
            {
                SetLoadingState(true, "Loading environment variables...");
                CancellationTokenSource = new CancellationTokenSource();

                var userTask = _envVarModel.LoadEnvVarsAsync(EnvironmentVariableTarget.User, CancellationTokenSource.Token);
                var systemTask = _envVarModel.LoadEnvVarsAsync(EnvironmentVariableTarget.Machine, CancellationTokenSource.Token);

                var results = await Task.WhenAll(userTask, systemTask);
                
                _userEnvVars = results[0];
                _systemEnvVars = results[1];
                
                await UpdateListBoxAsync();
            }
            catch (OperationCanceledException)
            {
                StatusText = "Loading cancelled";
            }
            catch (Exception ex)
            {
                StatusText = $"Error loading environment variables: {ex.Message}";
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Updates the environment variables list based on current filters and sorting asynchronously
        /// </summary>
        public async Task UpdateListBoxAsync()
        {
            try
            {
                SetLoadingState(true, "Updating environment variables list...");
                
                var result = await Task.Run(() =>
                {
                    CancellationTokenSource.Token.ThrowIfCancellationRequested();
                    
                    // Update the filtered and sorted list of environment variables
                    var query = _userEnvVars?.Keys.Union(_systemEnvVars?.Keys ?? Enumerable.Empty<string>()) ?? Enumerable.Empty<string>();

                    if (!string.IsNullOrEmpty(SearchText))
                    {
                        query = query.Where(k => k.ToLower().Contains(SearchText.ToLower()));
                    }

                    query = CurrentSortOrder.Equals(SortOrder.Ascending)
                        ? query.OrderBy(k => k)
                        : query.OrderByDescending(k => k);

                    return query.ToList();
                }, CancellationTokenSource.Token);

                // Update UI on main thread
                EnvVarList = result;
                
                // Notify property changes for UI updates
                OnPropertyChanged(nameof(UserEnvVars));
                OnPropertyChanged(nameof(SystemEnvVars));
                
                // Notify main window to refresh the environment variables list
                EnvVarListUpdated?.Invoke(this, EventArgs.Empty);
            }
            catch (OperationCanceledException)
            {
                StatusText = "Update cancelled";
            }
            catch (Exception ex)
            {
                StatusText = $"Error updating list: {ex.Message}";
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Sets an environment variable asynchronously
        /// </summary>
        /// <param name="name">The name of the environment variable</param>
        /// <param name="value">The value of the environment variable</param>
        /// <param name="target">The target scope (User or Machine)</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> SetEnvVarAsync(string name, string value, EnvironmentVariableTarget target)
        {
            try
            {
                SetLoadingState(true, $"Setting environment variable '{name}'...");
                
                if (CancellationTokenSource == null)
                    CancellationTokenSource = new CancellationTokenSource();

                await _envVarModel.SetEnvVarAsync(name, value, target, CancellationTokenSource.Token);
                
                // Reload environment variables to reflect changes
                await LoadEnvVarsAsync();
                
                StatusText = $"Environment variable '{name}' set successfully";
                return true;
            }
            catch (OperationCanceledException)
            {
                StatusText = "Operation cancelled";
                return false;
            }
            catch (Exception ex)
            {
                StatusText = $"Error setting environment variable: {ex.Message}";
                return false;
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Deletes an environment variable asynchronously
        /// </summary>
        /// <param name="name">The name of the environment variable</param>
        /// <param name="target">The target scope (User or Machine)</param>
        /// <returns>True if successful, false otherwise</returns>
        public async Task<bool> DeleteEnvVarAsync(string name, EnvironmentVariableTarget target)
        {
            try
            {
                SetLoadingState(true, $"Deleting environment variable '{name}'...");
                
                if (CancellationTokenSource == null)
                    CancellationTokenSource = new CancellationTokenSource();

                await _envVarModel.DeleteEnvVarAsync(name, target, CancellationTokenSource.Token);
                
                // Reload environment variables to reflect changes
                await LoadEnvVarsAsync();
                
                StatusText = $"Environment variable '{name}' deleted successfully";
                return true;
            }
            catch (OperationCanceledException)
            {
                StatusText = "Operation cancelled";
                return false;
            }
            catch (Exception ex)
            {
                StatusText = $"Error deleting environment variable: {ex.Message}";
                return false;
            }
            finally
            {
                SetLoadingState(false);
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
            try
            {
                bool isAdmin = await _envVarModel.IsAdministratorAsync();
                if (!isAdmin)
                {
                    bool success = await _envVarModel.ElevateAsync();
                    if (success)
                    {
                        System.Windows.Application.Current.Shutdown();
                    }
                    else
                    {
                        StatusText = "Administrator privileges required to continue";
                    }
                }
            }
            catch (Exception ex)
            {
                StatusText = $"Error during elevation: {ex.Message}";
            }
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

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
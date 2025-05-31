using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// Abstract base class providing common functionality for all ViewModels
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged, IDisposable
    {
        public event PropertyChangedEventHandler? PropertyChanged;

        private bool _isLoading;
        private string? _loadingMessage;
        private string? _errorMessage;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _disposed;

        /// <summary>
        /// Indicates if a long-running operation is in progress
        /// </summary>
        public bool IsLoading
        {
            get => _isLoading;
            protected set => SetProperty(ref _isLoading, value);
        }

        /// <summary>
        /// Message displayed during loading operations
        /// </summary>
        public string? LoadingMessage
        {
            get => _loadingMessage;
            protected set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// Error message for display to user
        /// </summary>
        public string? ErrorMessage
        {
            get => _errorMessage;
            protected set => SetProperty(ref _errorMessage, value);
        }

        /// <summary>
        /// Cancellation token source for async operations
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource
        {
            get => _cancellationTokenSource ??= new CancellationTokenSource();
            set
            {
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = value;
            }
        }

        /// <summary>
        /// Gets the cancellation token for async operations
        /// </summary>
        protected CancellationToken CancellationToken => CancellationTokenSource.Token;

        /// <summary>
        /// Sets the loading state with an optional message
        /// </summary>
        /// <param name="isLoading">Loading state</param>
        /// <param name="message">Loading message</param>
        protected void SetLoadingState(bool isLoading, string? message = null)
        {
            IsLoading = isLoading;
            LoadingMessage = message ?? string.Empty;
            if (!isLoading)
            {
                ErrorMessage = null; // Clear error when not loading
            }
        }

        /// <summary>
        /// Sets an error message and stops loading
        /// </summary>
        /// <param name="errorMessage">Error message to display</param>
        protected void SetErrorState(string errorMessage)
        {
            ErrorMessage = errorMessage;
            SetLoadingState(false);
        }

        /// <summary>
        /// Cancels any ongoing async operations
        /// </summary>
        protected void CancelOperations()
        {
            try
            {
                _cancellationTokenSource?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Token source already disposed
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Executes an async operation with proper error handling and loading state management
        /// </summary>
        /// <param name="operation">The async operation to execute</param>
        /// <param name="loadingMessage">Message to show during loading</param>
        /// <param name="errorPrefix">Prefix for error messages</param>
        protected async Task ExecuteAsync(Func<CancellationToken, Task> operation, string? loadingMessage = null, string errorPrefix = "Operation failed")
        {
            try
            {
                SetLoadingState(true, loadingMessage);
                await operation(CancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException)
            {
                SetErrorState("Operation was cancelled");
            }
            catch (Exception ex)
            {
                SetErrorState($"{errorPrefix}: {ex.Message}");
            }
            finally
            {
                SetLoadingState(false);
            }
        }

        /// <summary>
        /// Validates a string input
        /// </summary>
        /// <param name="value">Value to validate</param>
        /// <param name="fieldName">Field name for error messages</param>
        /// <param name="allowEmpty">Whether empty values are allowed</param>
        /// <returns>True if valid</returns>
        protected bool ValidateInput(string? value, string fieldName, bool allowEmpty = false)
        {
            if (!allowEmpty && string.IsNullOrWhiteSpace(value))
            {
                SetErrorState($"{fieldName} cannot be empty");
                return false;
            }
            return true;
        }

        /// <summary>
        /// Sets property value and raises PropertyChanged event
        /// </summary>
        /// <typeparam name="T">Property type</typeparam>
        /// <param name="field">Field reference</param>
        /// <param name="value">New value</param>
        /// <param name="propertyName">Property name (optional)</param>
        /// <returns>Returns true if value was changed</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (Equals(field, value)) return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Raises PropertyChanged event
        /// </summary>
        /// <param name="propertyName">Property name</param>
        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Dispose method to clean up resources
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _cancellationTokenSource = null;
                _disposed = true;
            }
        }
    }
}
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
        public event PropertyChangedEventHandler PropertyChanged;

        private bool _isLoading;
        private string _loadingMessage;
        private CancellationTokenSource _cancellationTokenSource;
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
        public string LoadingMessage
        {
            get => _loadingMessage;
            protected set => SetProperty(ref _loadingMessage, value);
        }

        /// <summary>
        /// Cancellation token source for async operations
        /// </summary>
        protected CancellationTokenSource CancellationTokenSource
        {
            get => _cancellationTokenSource ??= new CancellationTokenSource();
            set => _cancellationTokenSource = value;
        }

        /// <summary>
        /// Sets the loading state with an optional message
        /// </summary>
        /// <param name="isLoading">Loading state</param>
        /// <param name="message">Loading message</param>
        protected void SetLoadingState(bool isLoading, string message = null)
        {
            IsLoading = isLoading;
            LoadingMessage = message ?? string.Empty;
        }

        /// <summary>
        /// Cancels any ongoing async operations
        /// </summary>
        protected void CancelOperations()
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;
            SetLoadingState(false);
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
                CancelOperations();
                _disposed = true;
            }
        }
    }
}
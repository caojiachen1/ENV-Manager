using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace EnvVarViewer.ViewModels
{
    /// <summary>
    /// Abstract base class providing common functionality for all ViewModels
    /// </summary>
    public abstract class ViewModelBase : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

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
    }
}
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EnvVarViewer.Services;
// using Label = System.Windows.Controls.Label;

namespace EnvVarViewer.Views
{
    public partial class SettingsWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly ThemeService _themeService;
        private bool _isInitializing = true;

        public SettingsWindow()
        {
            InitializeComponent();
            _themeService = ThemeService.Instance;
            Loaded += SettingsWindow_Loaded;
        }

        private void SettingsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            LoadCurrentTheme();
            //ApplyThemeColors(_themeService.CurrentTheme);
            _isInitializing = false;
        }

        private void LoadCurrentTheme()
        {
            string currentTheme = _themeService.CurrentTheme;
            System.Diagnostics.Debug.WriteLine($"Loading theme: {currentTheme}");
            
            // Ensure controls exist
            if (DarkThemeRadio != null && LightThemeRadio != null)
            {
                // First clear all checked states
                DarkThemeRadio.IsChecked = false;
                LightThemeRadio.IsChecked = false;
                
                // Then set the correct checked state according to the current theme
                if (currentTheme == "Dark")
                {
                    DarkThemeRadio.IsChecked = true;
                    System.Diagnostics.Debug.WriteLine("Set Dark theme radio checked");
                }
                else
                {
                    LightThemeRadio.IsChecked = true;
                    System.Diagnostics.Debug.WriteLine("Set Light theme radio checked");
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("RadioButton controls are null");
            }
        }

        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (_isInitializing) return;

            if (sender is System.Windows.Controls.RadioButton radio && radio.Tag != null)
            {
                string theme = radio.Tag.ToString();
                _themeService.SetTheme(theme);
                ApplyThemeSpecificStyling(theme);
            }
        }

        private void ApplyThemeSpecificStyling(string theme)
        {
            // Find the main window and apply theme-specific styles
            var mainWindow = System.Windows.Application.Current.MainWindow;
            if (mainWindow != null)
            {
                // Find the search label
                var searchLabel = FindVisualChild<System.Windows.Controls.TextBlock>(mainWindow, "SearchLabel");

                if (searchLabel != null)
                {
                    if (theme == "Light")
                    {
                        searchLabel.Foreground = new SolidColorBrush(Colors.Black);
                    }
                    else
                    {
                        searchLabel.Foreground = new SolidColorBrush(Colors.White);
                    }
                }
            }
        }

        private T FindVisualChild<T>(DependencyObject parent, string name) where T : DependencyObject
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is T && (child as FrameworkElement)?.Name == name)
                {
                    return child as T;
                }
                var result = FindVisualChild<T>(child, name);
                if (result != null) return result;
            }
            return null;
        }

        private System.Windows.Controls.TextBlock FindTextBlockWithText(DependencyObject parent, string text)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (child is System.Windows.Controls.TextBlock textBlock && textBlock.Text?.ToLower().Contains(text.ToLower()) == true)
                {
                    return textBlock;
                }
                var result = FindTextBlockWithText(child, text);
                if (result != null) return result;
            }
            return null;
        }

        private void OKButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}

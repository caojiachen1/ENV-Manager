using System;
using System.Collections.Generic;
using EnvVarViewer.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using EnvVarViewer.Services;

namespace EnvVarViewer
{
    /// <summary>
    /// Interaction logic for AddEnvVarWindow.xaml
    /// </summary>
    public partial class AddEnvVarWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly AddEnvVarViewModel _viewModel;
        private readonly ThemeService _themeService;

        /// <summary>
        /// Initializes a new instance for adding environment variables
        /// </summary>
        public AddEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars)
        {
            InitializeComponent();
            _viewModel = new AddEnvVarViewModel(userEnvVars, systemEnvVars);
            DataContext = _viewModel;
            _viewModel.CloseWindow += (s, e) => Close();
            _themeService = ThemeService.Instance;
            Loaded += AddEnvVarWindow_Loaded;
            _themeService.ThemeChanged += OnThemeChanged;
        }

        /// <summary>
        /// Event triggered when an environment variable is added
        /// </summary>
        public event EventHandler EnvVarAdded
        {
            add { _viewModel.EnvVarAdded += value; }
            remove { _viewModel.EnvVarAdded -= value; }
        }

        private void AddEnvVarWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ApplyThemeToLabels(_themeService.CurrentTheme);
        }

        private void OnThemeChanged(string theme)
        {
            Dispatcher.Invoke(() => ApplyThemeToLabels(theme));
        }

        private void ApplyThemeToLabels(string theme)
        {
            var labelColor = theme == "Light" ? System.Windows.Media.Brushes.Black : System.Windows.Media.Brushes.White;
            // Assuming the labels have x:Name set in XAML: NameLabel, ValueLabel, ScopeLabel
            var nameLabel = this.FindName("NameLabel") as TextBlock;
            var valueLabel = this.FindName("ValueLabel") as TextBlock;
            var scopeLabel = this.FindName("ScopeLabel") as TextBlock;
            if (nameLabel != null) nameLabel.Foreground = labelColor;
            if (valueLabel != null) valueLabel.Foreground = labelColor;
            if (scopeLabel != null) scopeLabel.Foreground = labelColor;
        }
    }
}
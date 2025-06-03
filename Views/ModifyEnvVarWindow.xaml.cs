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
    /// Interaction logic for ModifyEnvVarWindow.xaml
    /// </summary>
    public partial class ModifyEnvVarWindow : Wpf.Ui.Controls.FluentWindow
    {
        private readonly ModifyEnvVarViewModel _viewModel;
        private readonly ThemeService _themeService;

        /// <summary>
        /// Initializes a new instance for modifying existing environment variables
        /// </summary>
        public ModifyEnvVarWindow(Dictionary<string, string> userEnvVars, Dictionary<string, string> systemEnvVars, string name, string value, bool isUserNode)
        {
            InitializeComponent();
            string scope = isUserNode ? "user" : "system";
            _viewModel = new ModifyEnvVarViewModel(userEnvVars, systemEnvVars, name, value, scope);
            DataContext = _viewModel;
            _viewModel.CloseWindow += (s, e) => Close();
            _themeService = ThemeService.Instance;
            Loaded += ModifyEnvVarWindow_Loaded;
            _themeService.ThemeChanged += OnThemeChanged;
        }

        private void ModifyEnvVarWindow_Loaded(object sender, RoutedEventArgs e)
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
            var nameLabel = this.FindName("NameLabel") as TextBlock;
            var valueLabel = this.FindName("ValueLabel") as TextBlock;
            var scopeLabel = this.FindName("ScopeLabel") as TextBlock;
            if (nameLabel != null) nameLabel.Foreground = labelColor;
            if (valueLabel != null) valueLabel.Foreground = labelColor;
            if (scopeLabel != null) scopeLabel.Foreground = labelColor;
        }

        /// <summary>
        /// Event triggered when an environment variable is modified
        /// </summary>
        public event EventHandler EnvVarModified
        {
            add { _viewModel.EnvVarModified += value; }
            remove { _viewModel.EnvVarModified -= value; }
        }
    }
}
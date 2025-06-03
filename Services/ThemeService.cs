using System;
using System.Configuration;
using System.Windows;
using Wpf.Ui.Appearance;

namespace EnvVarViewer.Services
{
    public class ThemeService
    {
        private static ThemeService _instance;
        public static ThemeService Instance => _instance ??= new ThemeService();

        public event Action<string> ThemeChanged; // Add this event

        public string CurrentTheme { get; private set; } = "Dark";

        private ThemeService()
        {
            LoadSavedTheme();
        }

        public void SetTheme(string theme)
        {
            CurrentTheme = theme;
            
            switch (theme)
            {
                case "Light":
                    ApplicationThemeManager.Apply(ApplicationTheme.Light);
                    break;
                case "Dark":
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark);
                    break;
            }

            SaveTheme(theme);
            ThemeChanged?.Invoke(theme); // Notify subscribers
        }

        private void LoadSavedTheme()
        {
            try
            {
                var savedTheme = Properties.Settings.Default.Theme;
                if (!string.IsNullOrEmpty(savedTheme))
                {
                    SetTheme(savedTheme);
                }
            }
            catch
            {
                SetTheme("Dark"); // Default to dark theme
            }
        }

        private void SaveTheme(string theme)
        {
            try
            {
                Properties.Settings.Default.Theme = theme;
                Properties.Settings.Default.Save();
            }
            catch
            {
                // Handle save error silently
            }
        }
    }
}

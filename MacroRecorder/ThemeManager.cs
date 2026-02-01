using System.Windows;
using System.Windows.Media;

namespace MacroRecorder
{
    public static class ThemeManager
    {
        public static bool IsSystemDarkTheme()
        {
            try
            {
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue)
                    {
                        return intValue == 0;
                    }
                }
            }
            catch
            {
            }
            return false;
        }

        public static void ApplySystemTheme()
        {
            if (Application.Current == null) return;
            
            bool isDark = IsSystemDarkTheme();
            
            var resources = Application.Current.Resources;
            
            resources["BackgroundBrush"] = isDark 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#1E1E1E"))
                : new SolidColorBrush(Colors.White);
            
            resources["SurfaceBrush"] = isDark 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#2D2D2D"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#F3F3F3"));
            
            resources["PrimaryBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#0078D4"));
            resources["DangerBrush"] = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#D13438"));
            
            resources["BorderBrush"] = isDark 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#3D3D3D"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#E0E0E0"));
            
            resources["TextPrimaryBrush"] = isDark 
                ? new SolidColorBrush(Colors.White)
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#202020"));
            
            resources["TextSecondaryBrush"] = isDark 
                ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#B0B0B0"))
                : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#606060"));
        }
    }
}

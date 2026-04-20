using System;
using System.Windows;

namespace MedHelp
{
    public partial class App : Application
    {
        public App()
        {
            InitializeComponent();
            this.ShutdownMode = ShutdownMode.OnLastWindowClose;

            try
            {
                var lightTheme = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MaterialDesignThemes.Wpf;component/Themes/MaterialDesignTheme.Light.xaml")
                };
                Resources.MergedDictionaries.Add(lightTheme);

                var colorTheme = new ResourceDictionary
                {
                    Source = new Uri("pack://application:,,,/MaterialDesignColors;component/Themes/MaterialDesignColor.DeepPurple.xaml")
                };
                Resources.MergedDictionaries.Add(colorTheme);

                System.Diagnostics.Debug.WriteLine("✅ Material Design успешно загружен.");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"❌ Ошибка загрузки темы: {ex.Message}");

                MessageBox.Show(
                    $"Не удалось загрузить тему оформления.\nПриложение запустится в стандартном режиме Windows.\n\nОшибка: {ex.Message}",
                    "Внимание",
                    MessageBoxButton.OK,
                    MessageBoxImage.Warning);
            }
        }
    }
}
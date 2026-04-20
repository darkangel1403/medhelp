using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using MedHelp.Data;
using MedHelp.Views;

namespace MedHelp.Views
{
    public partial class SplashScreen : Window
    {
        private readonly string[] _loadingTips = new[]
        {
            "💡 Совет: Вы можете вызвать врача на дом через личный кабинет.",
            "💡 Знаете ли вы? Расписание талонов обновляется автоматически.",
            "💡 Безопасность: Ваши данные защищены современным шифрованием.",
            "💡 Скорость: Поиск врача занимает менее 2 секунд.",
            "💡 Удобство: Электронный талон нельзя потерять."
        };

        public SplashScreen()
        {
            InitializeComponent();
            Loaded += SplashScreen_Loaded;
        }

        private async void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            var fadeStory = (Storyboard)FindResource("FadeInAnimation");
            var pulseStory = (Storyboard)FindResource("PulseAnimation");
            fadeStory.Begin(MainBorder);
            pulseStory.Begin(this);
            await SimulateLoadingAsync();
        }

        private async Task SimulateLoadingAsync()
        {
            Random rand = new Random();
            await UpdateProgress(0, "Загрузка конфигурации...");
            await Task.Delay(800);
            tipsText.Text = _loadingTips[rand.Next(_loadingTips.Length)];
            await Task.Delay(1000);
            await UpdateProgress(30, "Проверка подключения к базе данных...");
            await Task.Delay(1000);
            try
            {
                var db = new DatabaseHelper();
                bool isConnected = await db.TestConnectionAsync();
                if (!isConnected)
                    throw new Exception("Нет связи с сервером");
                await UpdateProgress(60, "Подключение успешно установлено.");
                await Task.Delay(800);
            }
            catch (Exception ex)
            {
                await UpdateProgress(100, "Ошибка: " + ex.Message, true);
                tipsText.Text = "Проверьте подключение к сети или обратитесь к администратору.";
                await Task.Delay(2000);
                Application.Current.Shutdown();
                return;
            }
            await UpdateProgress(80, "Загрузка справочников...");
            await Task.Delay(800);
            tipsText.Text = _loadingTips[rand.Next(_loadingTips.Length)];
            await UpdateProgress(95, "Формирование главного меню...");
            await Task.Delay(600);
            await UpdateProgress(100, "Готово!");
            await Task.Delay(500);
            var mainMenu = new MainMenuWindow();
            mainMenu.Show();
            mainMenu.Activate();
            var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fadeOut.Completed += (s, e) => Close();
            MainBorder.BeginAnimation(UIElement.OpacityProperty, fadeOut);
        }

        private async Task UpdateProgress(int value, string status, bool isError = false)
        {
            progressBar.Value = value;
            statusText.Text = status;
            statusText.Foreground = isError ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Gray;
            await Task.Delay(50);
        }
    }
}
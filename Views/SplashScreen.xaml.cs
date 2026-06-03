using System;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Animation;
using MedHelp.Data;

namespace MedHelp.Views
{
    public partial class SplashScreen : Window
    {
        private readonly string[] _tips = new[] { "💡 Совет: Вы можете вызвать врача на дом через личный кабинет.", " Знаете ли вы? Расписание талонов обновляется автоматически.", "💡 Безопасность: Ваши данные защищены шифрованием.", "💡 Скорость: Поиск врача занимает менее 2 секунд.", " Удобство: Электронный талон нельзя потерять." };
        public SplashScreen() { InitializeComponent(); Loaded += SplashScreen_Loaded; }
        private async void SplashScreen_Loaded(object sender, RoutedEventArgs e)
        {
            ((Storyboard)FindResource("FadeIn")).Begin(MainBorder);
            ((Storyboard)FindResource("Pulse")).Begin(this);
            await SimulateLoadingAsync();
        }
        private async Task SimulateLoadingAsync()
        {
            Random rand = new Random();
            await UpdateProgress(0, "Загрузка конфигурации...");
            await Task.Delay(600);
            tipsText.Text = _tips[rand.Next(_tips.Length)];
            await Task.Delay(800);
            await UpdateProgress(30, "Проверка подключения к базе данных...");
            await Task.Delay(800);
            try
            {
                bool ok = await new DatabaseHelper().TestConnectionAsync();
                if (!ok) throw new Exception("Нет связи с сервером");
                await UpdateProgress(60, "Подключение установлено.");
                await Task.Delay(600);
            }
            catch (Exception ex)
            {
                await UpdateProgress(100, "Ошибка: " + ex.Message, true);
                tipsText.Text = "Проверьте сеть или обратитесь к администратору.";
                await Task.Delay(1500);
                Application.Current.Shutdown();
                return;
            }
            await UpdateProgress(80, "Загрузка справочников...");
            await Task.Delay(600);
            tipsText.Text = _tips[rand.Next(_tips.Length)];
            await UpdateProgress(95, "Формирование меню...");
            await Task.Delay(500);
            await UpdateProgress(100, "Готово!");
            await Task.Delay(400);
            var main = new MainMenuWindow();
            main.Show();
            main.Activate();
            var fade = new DoubleAnimation(1, 0, TimeSpan.FromSeconds(0.3));
            fade.Completed += (s, e) => Close();
            MainBorder.BeginAnimation(UIElement.OpacityProperty, fade);
        }
        private async Task UpdateProgress(int value, string status, bool err = false)
        {
            progressBar.Value = value;
            statusText.Text = status;
            statusText.Foreground = err ? System.Windows.Media.Brushes.Red : System.Windows.Media.Brushes.Gray;
            await Task.Delay(40);
        }
    }
}

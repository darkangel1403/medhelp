using MedHelp.Data;
using MedHelp.Services;
using MedHelp.Views;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class RegistryOfficeWindow : Window
    {
        public RegistryOfficeWindow()
        {
            InitializeComponent();
            Loaded += RegistryOfficeWindow_Loaded;
        }

        private async void RegistryOfficeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SessionManager.CurrentUserFullName))
            {
                statusUser.Text = $"Пользователь: {SessionManager.CurrentUserFullName}";
                statusRole.Text = $"Роль: {SessionManager.CurrentUserRole}";
                welcomeTextBlock.Text = $"Добро пожаловать, {SessionManager.CurrentUserFullName}!";

                bool isAdmin = SessionManager.CurrentUserRole == "Admin";

                if (isAdmin)
                {
                    btnAdminPanel.Visibility = Visibility.Visible;
                    mnuDirectories.Visibility = Visibility.Visible;
                    mnuOperations.Visibility = Visibility.Visible;       
                    mnuRefreshSchedule.Visibility = Visibility.Visible;

                    try
                    {
                        var db = new DatabaseHelper();
                        await db.GenerateAndCleanAppointmentsAsync(DateTime.Now.Date, DateTime.Now.AddDays(30));
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Ошибка генерации талонов: {ex.Message}");
                    }
                }
                else
                {
                    btnAdminPanel.Visibility = Visibility.Collapsed;
                    mnuDirectories.Visibility = Visibility.Collapsed;
                    mnuOperations.Visibility = Visibility.Collapsed;      
                    mnuRefreshSchedule.Visibility = Visibility.Collapsed;
                }
            }
            else
            {
                MessageBox.Show("Сессия не найдена. Выполните вход.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool isAdmin = SessionManager.CurrentUserRole == "Admin";

            switch (e.Key)
            {
                case Key.F1:
                    new AboutProgramWindow().ShowDialog();
                    e.Handled = true;
                    break;
                case Key.F2:
                    new AboutDeveloperWindow().ShowDialog();
                    e.Handled = true;
                    break;
                case Key.F3:
                    new HelpWindow().ShowDialog();
                    e.Handled = true;
                    break;
                case Key.F5:
                    if (isAdmin)
                    {
                        MnuRefreshSchedule_Click(null, null);
                        e.Handled = true;
                    }
                    else
                    {
                        MessageBox.Show("Функция обновления расписания доступна только администратору.", "Доступ запрещен", MessageBoxButton.OK, MessageBoxImage.Warning);
                        e.Handled = true;
                    }
                    break;
            }
        }

        private void MnuExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите выйти из приложения?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Application.Current.Shutdown();
            }
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.CurrentUserId = 0;
            SessionManager.CurrentUserLogin = string.Empty;
            SessionManager.CurrentUserFullName = string.Empty;
            SessionManager.CurrentUserRole = string.Empty;
            SessionManager.CurrentUserAddress = string.Empty;
            SessionManager.CurrentUserPhone = string.Empty;

            var existingMenu = Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w is MainMenuWindow);
            if (existingMenu != null)
            {
                existingMenu.Show();
                existingMenu.Activate();
            }
            else
            {
                new MainMenuWindow().Show();
            }
            Close();
        }

        private void MnuSpecialists_Click(object sender, RoutedEventArgs e) => new SpecialistsWindow().ShowDialog();
        private void MnuCallHome_Click(object sender, RoutedEventArgs e) => new CallDoctorHomeWindow().ShowDialog();
        private void MnuAboutProgram_Click(object sender, RoutedEventArgs e) => new AboutProgramWindow().ShowDialog();
        private void MnuAboutDeveloper_Click(object sender, RoutedEventArgs e) => new AboutDeveloperWindow().ShowDialog();
        private void MnuHelp_Click(object sender, RoutedEventArgs e) => new HelpWindow().ShowDialog();
        private void BtnPersonalAccount_Click(object sender, RoutedEventArgs e) => new PersonalAccountWindow().ShowDialog();
        private void BtnSpecialists_Click(object sender, RoutedEventArgs e) => new SpecialistsWindow().ShowDialog();
        private void BtnCallHome_Click(object sender, RoutedEventArgs e) => new CallDoctorHomeWindow().ShowDialog();

        private void MiDoctors_Click(object sender, RoutedEventArgs e)
        {
            new AdminDoctorsWindow(mode: (bool?)null).ShowDialog();
        }

        private void MiPatients_Click(object sender, RoutedEventArgs e)
        {
            new AdminPatientsWindow(mode: (bool?)null).ShowDialog();
        }

        private void MiStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (SessionManager.CurrentUserRole == "Admin")
            {
                new AdminStatisticsWindow().ShowDialog();
            }
            else
            {
                MessageBox.Show("Доступно только администратору.", "Ошибка доступа", MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }

        private async void MnuRefreshSchedule_Click(object? sender, RoutedEventArgs? e)
        {
            var result = MessageBox.Show("Обновить расписание талонов на следующий месяц?\nЭто действие создаст талоны для всех врачей, включая недавно добавленных.", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                try
                {
                    var db = new DatabaseHelper();
                    await db.GenerateAndCleanAppointmentsAsync(DateTime.Now.Date, DateTime.Now.AddDays(30));
                    MessageBox.Show("Расписание успешно обновлено.\nТалоны сгенерированы для всех активных врачей.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnAddDoctor_Click(object sender, RoutedEventArgs e)
        {
            new AdminDoctorsWindow(mode: true).ShowDialog();
        }

        private void BtnEditDoctor_Click(object sender, RoutedEventArgs e)
        {
            new AdminDoctorsWindow(mode: true).ShowDialog();
        }

        private void BtnDeleteDoctor_Click(object sender, RoutedEventArgs e)
        {
            new AdminDoctorsWindow(mode: false).ShowDialog();
        }

        private void BtnAddPatient_Click(object sender, RoutedEventArgs e)
        {
            new AdminPatientsWindow(mode: true).ShowDialog();
        }

        private void BtnEditPatient_Click(object sender, RoutedEventArgs e)
        {
            new AdminPatientsWindow(mode: true).ShowDialog();
        }

        private void BtnDeletePatient_Click(object sender, RoutedEventArgs e)
        {
            new AdminPatientsWindow(mode: false).ShowDialog();
        }
    }
}
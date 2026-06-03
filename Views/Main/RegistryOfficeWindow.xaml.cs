using MedHelp.Data;
using MedHelp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class RegistryOfficeWindow : Window
    {
        public RegistryOfficeWindow() { InitializeComponent(); Loaded += RegistryOfficeWindow_Loaded; }

        private async void RegistryOfficeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SessionManager.CurrentUserFullName))
            {
                statusUser.Text = $"Пользователь: {SessionManager.CurrentUserFullName}";
                statusRole.Text = $"Роль: {SessionManager.CurrentUserRole}";
                welcomeTextBlock.Text = $"Добро пожаловать, {SessionManager.CurrentUserFullName}!";
                bool isAdmin = SessionManager.CurrentUserRole == "Admin";
                btnAdminPanel.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                mnuDirectories.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                mnuOperations.Visibility = isAdmin ? Visibility.Visible : Visibility.Collapsed;
                if (isAdmin)
                {
                    try { await new DatabaseHelper().GenerateAndCleanAppointmentsAsync(DateTime.Now.Date, DateTime.Now.AddDays(30)); }
                    catch (Exception ex) { System.Diagnostics.Debug.WriteLine(ex.Message); }
                }
            }
            else
            {
                MessageBox.Show("Сессия не найдена. Выполните вход.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                new LoginWindow().Show();
                Close();
            }
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            bool isAdmin = SessionManager.CurrentUserRole == "Admin";
            if (e.Key == Key.F1) { new AboutProgramWindow().ShowDialog(); e.Handled = true; }
            else if (e.Key == Key.F2) { new AboutDeveloperWindow().ShowDialog(); e.Handled = true; }
            else if (e.Key == Key.F3) { new HelpWindow().ShowDialog(); e.Handled = true; }
            else if (e.Key == Key.F5)
            {
                if (isAdmin) { MnuRefreshSchedule_Click(null, null); e.Handled = true; }
                else { MessageBox.Show("Доступно только администратору.", "Отказ", MessageBoxButton.OK, MessageBoxImage.Warning); e.Handled = true; }
            }
        }

        private void MnuExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Выйти из приложения?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }

        private void BtnLogout_Click(object sender, RoutedEventArgs e)
        {
            SessionManager.CurrentUserId = 0;
            SessionManager.CurrentUserLogin = SessionManager.CurrentUserFullName = SessionManager.CurrentUserRole = string.Empty;
            var existing = Application.Current.Windows.Cast<Window>().FirstOrDefault(w => w is MainMenuWindow);
            if (existing != null) { existing.Show(); existing.Activate(); } else new MainMenuWindow().Show();
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
        private void MiDoctors_Click(object sender, RoutedEventArgs e) => new AdminDoctorsWindow(mode: (bool?)null).ShowDialog();
        private void MiPatients_Click(object sender, RoutedEventArgs e) => new AdminPatientsWindow(mode: (bool?)null).ShowDialog();
        private void MiStatistics_Click(object sender, RoutedEventArgs e)
        {
            if (SessionManager.CurrentUserRole == "Admin") new AdminStatisticsWindow().ShowDialog();
            else MessageBox.Show("Доступно только администратору.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        private async void MnuRefreshSchedule_Click(object? sender, RoutedEventArgs? e)
        {
            if (MessageBox.Show("Обновить расписание на месяц?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                try
                {
                    await new DatabaseHelper().GenerateAndCleanAppointmentsAsync(DateTime.Now.Date, DateTime.Now.AddDays(30));
                    MessageBox.Show("Расписание обновлено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }
        private void BtnAddDoctor_Click(object sender, RoutedEventArgs e) => new AdminDoctorsWindow(mode: true).ShowDialog();
        private void BtnEditDoctor_Click(object sender, RoutedEventArgs e) => new AdminDoctorsWindow(mode: true).ShowDialog();
        private void BtnDeleteDoctor_Click(object sender, RoutedEventArgs e) => new AdminDoctorsWindow(mode: false).ShowDialog();
        private void BtnAddPatient_Click(object sender, RoutedEventArgs e) => new AdminPatientsWindow(mode: true).ShowDialog();
        private void BtnEditPatient_Click(object sender, RoutedEventArgs e) => new AdminPatientsWindow(mode: true).ShowDialog();
        private void BtnDeletePatient_Click(object sender, RoutedEventArgs e) => new AdminPatientsWindow(mode: false).ShowDialog();
    }
}
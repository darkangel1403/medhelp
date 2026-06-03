using MedHelp.Data;
using MedHelp.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MedHelp.Views
{
    public partial class AdminPatientsWindow : Window
    {
        private ObservableCollection<User>? usersList;
        private User? selectedUser;
        private readonly bool? mode;

        public AdminPatientsWindow(bool? mode)
        {
            InitializeComponent();
            this.mode = mode;
            Loaded += AdminPatientsWindow_Loaded;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            if (mode == true) { Title = "Управление пациентами"; btnAdd.Visibility = btnEdit.Visibility = Visibility.Visible; }
            else if (mode == false) { Title = "Удаление пациента"; btnAdd.Visibility = btnEdit.Visibility = Visibility.Collapsed; btnAction.Visibility = Visibility.Visible; }
            else { Title = "Справочник пациентов"; btnAction.Visibility = Visibility.Collapsed; btnAdd.Visibility = btnEdit.Visibility = Visibility.Visible; }
            btnEdit.IsEnabled = btnAction.IsEnabled = false;
        }

        private async void AdminPatientsWindow_Loaded(object sender, RoutedEventArgs e) { await LoadUsersAsync(); }

        private async Task LoadUsersAsync()
        {
            try { usersList = new ObservableCollection<User>(await new DatabaseHelper().GetAllUsersAsync()); dgUsers.ItemsSource = usersList; }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (usersList == null) return;
            string f = txtSearch.Text.Trim().ToLower();
            dgUsers.ItemsSource = string.IsNullOrEmpty(f) ? usersList : usersList.Where(u => (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(f)) || (!string.IsNullOrEmpty(u.Login) && u.Login.ToLower().Contains(f))).ToList();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) { txtSearch.Focus(); }

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = dgUsers.SelectedItem as User;
            bool has = selectedUser != null;
            if (mode != false) btnEdit.IsEnabled = has;
            if (mode == false) btnAction.IsEnabled = has;
        }

        private async void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null) { MessageBox.Show("Выберите пациента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (selectedUser.Role == "Admin") { MessageBox.Show("Нельзя удалить администратора.", "Запрещено", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show($"Удалить {selectedUser.FullName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    if (await new DatabaseHelper().DeleteUserAsync(selectedUser.UserId)) { usersList?.Remove(selectedUser); txtSearch.Clear(); MessageBox.Show("Удалено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); selectedUser = null; btnAction.IsEnabled = false; }
                    else MessageBox.Show("Не удалось удалить.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new UserEditWindow(null);
            if (dlg.ShowDialog() == true && dlg.User != null)
            {
                try { await new DatabaseHelper().RegisterUserAsync(dlg.User.FullName, dlg.User.Login, dlg.User.PhoneNumber ?? "", dlg.User.Address, "1234"); MessageBox.Show("Добавлен. Пароль: 1234", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); await LoadUsersAsync(); txtSearch.Clear(); }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null) { MessageBox.Show("Выберите пациента.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var dlg = new UserEditWindow(new User { UserId = selectedUser.UserId, Login = selectedUser.Login, FullName = selectedUser.FullName, Address = selectedUser.Address ?? string.Empty, PhoneNumber = selectedUser.PhoneNumber ?? string.Empty, Role = selectedUser.Role });
            if (dlg.ShowDialog() == true && dlg.User != null)
            {
                try { await new DatabaseHelper().UpdateUserAsync(dlg.User); MessageBox.Show("Обновлено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); await LoadUsersAsync(); txtSearch.Clear(); }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
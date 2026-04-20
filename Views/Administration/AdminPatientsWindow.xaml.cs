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

            if (mode == true)
            {
                Title = "Управление пациентами (Добавление/Редактирование)";
                btnAdd.Visibility = Visibility.Visible;
                btnEdit.Visibility = Visibility.Visible;
            }
            else if (mode == false)
            {
                Title = "Удаление пациента";
                btnAdd.Visibility = Visibility.Collapsed;
                btnEdit.Visibility = Visibility.Collapsed;
                btnAction.Visibility = Visibility.Visible;
                btnAction.Content = "🗑️ Удалить выбранного";
            }
            else
            {
                Title = "Справочник пациентов";
                btnAction.Visibility = Visibility.Collapsed;
                btnAdd.Visibility = Visibility.Visible;
                btnEdit.Visibility = Visibility.Visible; 
            }

            btnEdit.IsEnabled = false;
            btnAction.IsEnabled = false;
        }

        private async void AdminPatientsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadUsersAsync();
        }

        private async Task LoadUsersAsync()
        {
            try
            {
                var db = new DatabaseHelper();
                var list = await db.GetAllUsersAsync();
                usersList = new ObservableCollection<User>(list);
                dgUsers.ItemsSource = usersList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (usersList == null) return;

            string filter = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(filter))
            {
                dgUsers.ItemsSource = usersList;
            }
            else
            {
                var filtered = usersList.Where(u =>
                    (!string.IsNullOrEmpty(u.FullName) && u.FullName.ToLower().Contains(filter)) ||
                    (!string.IsNullOrEmpty(u.Login) && u.Login.ToLower().Contains(filter))
                ).ToList();
                dgUsers.ItemsSource = filtered;
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Focus();
        }

        private void DgUsers_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedUser = dgUsers.SelectedItem as User;

            bool hasSelection = selectedUser != null;

            if (mode != false)
            {
                btnEdit.IsEnabled = hasSelection;
            }

            if (mode == false)
            {
                btnAction.IsEnabled = hasSelection;
            }
        }

        private async void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пациента из списка.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (selectedUser.Role == "Admin")
            {
                MessageBox.Show("Нельзя удалить учетную запись администратора.", "Запрещено", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var confirm = MessageBox.Show($"Вы уверены, что хотите удалить пациента {selectedUser.FullName}?\nВНИМАНИЕ: Будут также удалены все его записи на прием и вызовы врача!", "Подтверждение удаления", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var db = new DatabaseHelper();
                    bool success = await db.DeleteUserAsync(selectedUser.UserId);
                    if (success)
                    {
                        usersList?.Remove(selectedUser);
                        txtSearch.Clear();
                        MessageBox.Show("Пациент и связанные данные успешно удалены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        selectedUser = null;
                        btnAction.IsEnabled = false;
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить пациента.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new UserEditWindow(null);
            if (dialog.ShowDialog() == true && dialog.User != null)
            {
                try
                {
                    string tempPassword = "1234";
                    var db = new DatabaseHelper();
                    await db.RegisterUserAsync(dialog.User.FullName, dialog.User.Login, dialog.User.PhoneNumber ?? "", dialog.User.Address, tempPassword);
                    MessageBox.Show($"Пациент успешно добавлен.\nВременный пароль: {tempPassword}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadUsersAsync();
                    txtSearch.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedUser == null)
            {
                MessageBox.Show("Выберите пациента для редактирования.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dialog = new UserEditWindow(new User
            {
                UserId = selectedUser.UserId,
                Login = selectedUser.Login,
                FullName = selectedUser.FullName,
                Address = selectedUser.Address ?? string.Empty,
                PhoneNumber = selectedUser.PhoneNumber ?? string.Empty,
                Role = selectedUser.Role
            });

            if (dialog.ShowDialog() == true && dialog.User != null)
            {
                try
                {
                    var db = new DatabaseHelper();
                    await db.UpdateUserAsync(dialog.User);
                    MessageBox.Show("Данные пациента успешно обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadUsersAsync();
                    txtSearch.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}

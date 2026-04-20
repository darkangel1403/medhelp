using System.Windows;
using MedHelp.Data;
using MedHelp.Services;

namespace MedHelp.Views
{
    public partial class EditUserDetailsWindow : Window
    {
        public string EditedFullName { get; private set; } = string.Empty;
        public string EditedLogin { get; private set; } = string.Empty;
        public string EditedAddress { get; private set; } = string.Empty;
        public string EditedPhone { get; private set; } = string.Empty;

        public EditUserDetailsWindow(string currentFullName, string currentLogin, string currentAddress, string currentPhone)
        {
            InitializeComponent();
            tbFullName.Text = currentFullName;
            tbLogin.Text = currentLogin;
            tbAddress.Text = currentAddress;
            tbPhone.Text = currentPhone;
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbFullName.Text) || string.IsNullOrWhiteSpace(tbLogin.Text))
            {
                MessageBox.Show("Заполните ФИО и Логин.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            EditedFullName = tbFullName.Text;
            EditedLogin = tbLogin.Text;
            EditedAddress = tbAddress.Text;
            EditedPhone = tbPhone.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var changePasswordWindow = new ChangePasswordWindow();
            if (changePasswordWindow.ShowDialog() == true)
            {
                string oldPwd = changePasswordWindow.OldPassword ?? string.Empty;
                string newPwd = changePasswordWindow.NewPassword ?? string.Empty;
                if (string.IsNullOrEmpty(oldPwd) || string.IsNullOrEmpty(newPwd))
                {
                    MessageBox.Show("Пароли не могут быть пустыми.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }
                try
                {
                    var db = new DatabaseHelper();
                    bool success = await db.ChangeUserPasswordAsync(SessionManager.CurrentUserId, oldPwd, newPwd);
                    if (success)
                    {
                        MessageBox.Show("Пароль успешно изменён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    else
                    {
                        MessageBox.Show("Неверный старый пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }
    }
}
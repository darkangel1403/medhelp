using System.Windows;

namespace MedHelp.Views
{
    public partial class ChangePasswordWindow : Window
    {
        public string? OldPassword { get; private set; }
        public string? NewPassword { get; private set; }

        public ChangePasswordWindow()
        {
            InitializeComponent();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string oldPwd = pwdOldPassword.Password;
            string newPwd = pwdNewPassword.Password;
            string confirmPwd = pwdConfirmPassword.Password;

            if (string.IsNullOrEmpty(oldPwd) || string.IsNullOrEmpty(newPwd) || string.IsNullOrEmpty(confirmPwd))
            {
                MessageBox.Show("Заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (newPwd.Length < 4)
            {
                MessageBox.Show("Пароль должен быть не менее 4 символов.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (newPwd != confirmPwd)
            {
                MessageBox.Show("Новые пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (oldPwd == newPwd)
            {
                MessageBox.Show("Новый пароль должен отличаться от старого.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            OldPassword = oldPwd;
            NewPassword = newPwd;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
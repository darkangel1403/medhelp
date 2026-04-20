using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;
using MedHelp.Data;
using MedHelp.Views;

namespace MedHelp.Views
{
    public partial class LoginWindow : Window
    {
        public LoginWindow()
        {
            InitializeComponent();
            txtLogin.PreviewTextInput += TxtLogin_PreviewTextInput;
        }

        private void TxtLogin_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9]+$");
        }

        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string login = txtLogin.Text.Trim();
            string password = pwdPassword.Password;
            if (string.IsNullOrWhiteSpace(login) || string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Введите логин и пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var db = new DatabaseHelper();
                bool isValid = await db.ValidateUserAsync(login, password);
                if (isValid)
                {
                    this.Close();
                    var registry = new RegistryOfficeWindow();
                    registry.Show();
                }
                else
                {
                    MessageBox.Show("Неверный логин или пароль.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    pwdPassword.Clear();
                }
            }
            catch
            {
                MessageBox.Show("Ошибка подключения к базе данных.", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}

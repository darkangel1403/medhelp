using MedHelp.Data;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class RegistrationWindow : Window
    {
        private readonly string[] _streets = new[]
        {
            "Голубева И.П.", "Алибегова И.Я.", "Белецкого Е.М.", "Янки Брыля",
            "пр. Газеты Звезда", "пр. Газеты Правда", "Дзержинского пр.",
            "Ельских", "Крапивы Кондрата", "Любимова И.Е. пр.", "Маршала Лосика",
            "Михалово", "Михаловская", "Острожских", "Рафиева Н.", "Русановича А.П.", "Тышкевич"
        };

        public RegistrationWindow()
        {
            InitializeComponent();
            cmbAddress.ItemsSource = _streets;
            cmbAddress.SelectedIndex = -1;
            txtLogin.PreviewTextInput += (s, e) => e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9]+$");
            txtFullName.PreviewTextInput += (s, e) => e.Handled = !Regex.IsMatch(e.Text, @"^[а-яА-ЯёЁ\s]+$");
            txtPhone.PreviewTextInput += (s, e) => e.Handled = !Regex.IsMatch(e.Text, @"^[\d\+]+$");
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) { DialogResult = false; Close(); }
        }

        private async void BtnRegister_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtFullName.Text) || string.IsNullOrWhiteSpace(txtLogin.Text) ||
                string.IsNullOrWhiteSpace(txtPhone.Text) || string.IsNullOrWhiteSpace(cmbAddress.Text) ||
                string.IsNullOrWhiteSpace(pwdPassword.Password))
            {
                MessageBox.Show("Заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (pwdPassword.Password != pwdConfirm.Password)
            {
                MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            try
            {
                var db = new DatabaseHelper();
                await db.RegisterUserAsync(txtFullName.Text, txtLogin.Text, txtPhone.Text, cmbAddress.Text, pwdPassword.Password);
                MessageBox.Show("Регистрация успешна!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
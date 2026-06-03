using MedHelp.Data;
using MedHelp.Services;
using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class EditUserDetailsWindow : Window
    {
        public string EditedFullName { get; private set; } = string.Empty;
        public string EditedLogin { get; private set; } = string.Empty;
        public string EditedAddress { get; private set; } = string.Empty;
        public string EditedPhone { get; private set; } = string.Empty;

        private readonly string[] _streets = new[]
        {
            "Голубева И.П.", "Алибегова И.Я.", "Белецкого Е.М.", "Янки Брыля",
            "пр. Газеты Звезда", "пр. Газеты Правда", "Дзержинского пр.",
            "Ельских", "Крапивы Кондрата", "Любимова И.Е. пр.", "Маршала Лосика",
            "Михалово", "Михаловская", "Острожских", "Рафиева Н.", "Русановича А.П.", "Тышкевич"
        };

        public EditUserDetailsWindow(string fullName, string login, string address, string phone)
        {
            InitializeComponent();
            cmbAddress.ItemsSource = _streets;
            cmbAddress.IsEditable = false;
            tbFullName.Text = fullName;
            tbLogin.Text = login;
            cmbAddress.Text = address;
            tbPhone.Text = phone;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { DialogResult = false; Close(); } }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbFullName.Text) || string.IsNullOrWhiteSpace(tbLogin.Text))
            {
                MessageBox.Show("Заполните ФИО и Логин.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            EditedFullName = tbFullName.Text;
            EditedLogin = tbLogin.Text;
            EditedAddress = cmbAddress.Text;
            EditedPhone = tbPhone.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }

        private async void BtnChangePassword_Click(object sender, RoutedEventArgs e)
        {
            var win = new ChangePasswordWindow();
            if (win.ShowDialog() == true && win.OldPassword != null && win.NewPassword != null)
            {
                try
                {
                    bool ok = await new DatabaseHelper().ChangeUserPasswordAsync(SessionManager.CurrentUserId, win.OldPassword, win.NewPassword);
                    MessageBox.Show(ok ? "Пароль изменён." : "Неверный старый пароль.", ok ? "Успех" : "Ошибка", MessageBoxButton.OK, ok ? MessageBoxImage.Information : MessageBoxImage.Error);
                }
                catch (System.Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }
    }
}
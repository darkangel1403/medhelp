using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class ChangePasswordWindow : Window
    {
        public string? OldPassword { get; private set; }
        public string? NewPassword { get; private set; }

        public ChangePasswordWindow() { InitializeComponent(); }
        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { DialogResult = false; Close(); } }
        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            string old = pwdOld.Password, nw = pwdNew.Password, conf = pwdConfirm.Password;
            if (string.IsNullOrEmpty(old) || string.IsNullOrEmpty(nw) || string.IsNullOrEmpty(conf)) { MessageBox.Show("Заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (nw.Length < 4) { MessageBox.Show("Минимум 4 символа.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (nw != conf) { MessageBox.Show("Пароли не совпадают.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (old == nw) { MessageBox.Show("Пароль должен отличаться.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            OldPassword = old; NewPassword = nw;
            DialogResult = true;
            Close();
        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
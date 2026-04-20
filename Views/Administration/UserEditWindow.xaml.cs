using MedHelp.Models;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class UserEditWindow : Window
    {
        public User? User { get; private set; }

        public UserEditWindow(User? user)
        {
            InitializeComponent();
            tbLogin.PreviewTextInput += TbLogin_PreviewTextInput;
            tbFullName.PreviewTextInput += TbFullName_PreviewTextInput;
            tbPhone.PreviewTextInput += TbPhone_PreviewTextInput;

            if (user != null)
            {
                User = new User
                {
                    UserId = user.UserId,
                    Login = user.Login,
                    FullName = user.FullName,
                    Address = user.Address,
                    PhoneNumber = user.PhoneNumber,
                    Role = user.Role
                };
                tbLogin.Text = user.Login;
                tbLogin.IsEnabled = false;
                tbFullName.Text = user.FullName;
                tbAddress.Text = user.Address;
                tbPhone.Text = user.PhoneNumber ?? "";
                Title = "Редактирование пользователя";
            }
            else
            {
                User = new User { Role = "User" };
                Title = "Добавление пользователя";
            }
        }

        private void TbLogin_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[a-zA-Z0-9]+$");
        }

        private void TbFullName_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[а-яА-ЯёЁ\s]+$");
        }

        private void TbPhone_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            e.Handled = !Regex.IsMatch(e.Text, @"^[\d\+]+$");
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbLogin.Text) || string.IsNullOrWhiteSpace(tbFullName.Text))
            {
                MessageBox.Show("Заполните Логин и ФИО.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (User == null) User = new User { Role = "User" };
            User.Login = tbLogin.Text;
            User.FullName = tbFullName.Text;
            User.Address = tbAddress.Text;
            User.PhoneNumber = tbPhone.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

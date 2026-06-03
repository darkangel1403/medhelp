using System.Windows;

namespace MedHelp.Views
{
    public partial class MainMenuWindow : Window
    {
        public MainMenuWindow() { InitializeComponent(); }

        private void BtnRegister_Click(object sender, RoutedEventArgs e) { new RegistrationWindow().ShowDialog(); }
        private void BtnLogin_Click(object sender, RoutedEventArgs e) { new LoginWindow().ShowDialog(); }
        private void BtnExit_Click(object sender, RoutedEventArgs e)
        {
            if (MessageBox.Show("Вы действительно хотите выйти?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                Application.Current.Shutdown();
        }
    }
}
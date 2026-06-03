using MedHelp.Data;
using MedHelp.Services;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class CallDoctorHomeWindow : Window
    {
        private readonly string[] _streets = new[]
        {
            "Голубева И.П.", "Алибегова И.Я.", "Белецкого Е.М.", "Янки Брыля",
            "пр. Газеты Звезда", "пр. Газеты Правда", "Дзержинского пр.",
            "Ельских", "Крапивы Кондрата", "Любимова И.Е. пр.", "Маршала Лосика",
            "Михалово", "Михаловская", "Острожских", "Рафиева Н.", "Русановича А.П.", "Тышкевич"
        };

        public CallDoctorHomeWindow()
        {
            InitializeComponent();
            cmbAddress.ItemsSource = _streets;
            cmbAddress.IsEditable = false;
            if (!string.IsNullOrEmpty(SessionManager.CurrentUserAddress)) cmbAddress.Text = SessionManager.CurrentUserAddress;
            else if (_streets.Length > 0) cmbAddress.SelectedIndex = 0;
            txtPhone.Text = SessionManager.CurrentUserPhone ?? "+375 ";
            txtPhone.PreviewTextInput += (s, e) => e.Handled = !Regex.IsMatch(e.Text, @"^[\d\+]+$");
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e) { if (e.Key == Key.Escape) { DialogResult = false; Close(); } }
        private async void BtnSubmit_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(cmbAddress.Text) || string.IsNullOrWhiteSpace(txtPhone.Text) || string.IsNullOrWhiteSpace(txtComplaints.Text))
            { MessageBox.Show("Заполните все поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            try
            {
                var db = new DatabaseHelper();
                var docs = await db.GetAllDoctorsAsync();
                if (!docs.Any()) throw new System.Exception("Нет доступных врачей.");
                await db.CreateHomeVisitAsync(SessionManager.CurrentUserId, docs.First().DoctorId, cmbAddress.Text, txtPhone.Text, txtComplaints.Text);
                MessageBox.Show("Заявка отправлена!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                DialogResult = true;
                Close();
            }
            catch (System.Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }
        private void BtnCancel_Click(object sender, RoutedEventArgs e) { DialogResult = false; Close(); }
    }
}
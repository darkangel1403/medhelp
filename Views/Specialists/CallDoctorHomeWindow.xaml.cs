using MedHelp.Data;
using MedHelp.Services;
using System;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;

namespace MedHelp.Views
{
    public partial class CallDoctorHomeWindow : Window
    {
        public CallDoctorHomeWindow()
        {
            InitializeComponent();
            Loaded += CallDoctorHomeWindow_Loaded;
        }

        private void CallDoctorHomeWindow_Loaded(object sender, RoutedEventArgs e)
        {
            if (!string.IsNullOrEmpty(SessionManager.CurrentUserAddress))
            {
                cmbAddress.Items.Add(SessionManager.CurrentUserAddress);
                cmbAddress.SelectedIndex = 0;
            }
            else
            {
                cmbAddress.Items.Add("Введите адрес...");
            }
            txtPhone.Text = !string.IsNullOrEmpty(SessionManager.CurrentUserPhone) ? SessionManager.CurrentUserPhone : "+375 ";
        }

        private async void BtnSubmitCall_Click(object sender, RoutedEventArgs e)
        {
            string address = cmbAddress.Text.Trim();
            string phone = txtPhone.Text.Trim();
            string complaints = txtComplaints.Text.Trim();

            if (string.IsNullOrWhiteSpace(address) || string.IsNullOrWhiteSpace(phone) || string.IsNullOrWhiteSpace(complaints))
            {
                MessageBox.Show("Заполните все обязательные поля.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (!Regex.IsMatch(phone, @"^[\d\+]+$"))
            {
                MessageBox.Show("Неверный формат телефона. Разрешены только цифры и знак +.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                var db = new DatabaseHelper();

                var doctors = await db.GetAllDoctorsAsync();

                if (doctors == null || !doctors.Any())
                {
                    MessageBox.Show("Ошибка: В базе данных нет ни одного врача. Невозможно создать вызов.", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                int selectedDoctorId = doctors.First().DoctorId;

                bool success = await db.CreateHomeVisitAsync(
                    SessionManager.CurrentUserId,
                    selectedDoctorId,
                    address,
                    phone,
                    complaints
                );

                if (success)
                {
                    MessageBox.Show($"Заявка на вызов врача успешно оформлена!\nВрач: {doctors.First().FullName}", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
                else
                {
                    MessageBox.Show("Не удалось отправить заявку. Попробуйте позже.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при отправке заявки: {ex.Message}\n\nВозможно, в базе данных нет врачей или нарушена связь таблиц.", "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelCall_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
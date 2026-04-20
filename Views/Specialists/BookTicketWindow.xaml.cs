using MedHelp.Data;
using MedHelp.Models;
using MedHelp.Services;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MedHelp.Views
{
    public partial class BookTicketWindow : Window
    {
        private readonly Doctor doctor = null!;
        private Appointment? selectedTicket;

        public BookTicketWindow(Doctor selectedDoctor)
        {
            if (selectedDoctor == null)
            {
                MessageBox.Show("Ошибка: врач не выбран.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                this.Close();
                return;
            }
            this.doctor = selectedDoctor;
            InitializeComponent();
            lblDoctorName.Text = doctor.FullName;
            dpDate.SelectedDateChanged += DpDate_SelectedDateChanged;
            dpDate.SelectedDate = DateTime.Today;
            this.Loaded += BookTicketWindow_Loaded;
        }

        private async void BookTicketWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFreeTicketsAsync(DateTime.Today);
        }

        private async void DpDate_SelectedDateChanged(object? sender, SelectionChangedEventArgs e)
        {
            if (dpDate.SelectedDate.HasValue)
            {
                await LoadFreeTicketsAsync(dpDate.SelectedDate.Value);
            }
        }

        private async Task LoadFreeTicketsAsync(DateTime date)
        {
            try
            {
                dgFreeTickets.ItemsSource = null;
                selectedTicket = null;
                btnConfirmBooking.IsEnabled = false;
                var db = new DatabaseHelper();
                var allTickets = await db.GetFreeTicketsAsync(doctor.DoctorId, date);
                DateTime currentTime = DateTime.Now.AddMinutes(10);
                var filteredTickets = allTickets.Where(ticket => ticket.VisitDate >= currentTime).ToList();
                dgFreeTickets.ItemsSource = filteredTickets;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки талонов: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                dgFreeTickets.ItemsSource = null;
                btnConfirmBooking.IsEnabled = false;
            }
        }

        private void DgFreeTickets_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedTicket = dgFreeTickets.SelectedItem as Appointment;
            btnConfirmBooking.IsEnabled = selectedTicket != null;
        }

        private async void BtnConfirmBooking_Click(object sender, RoutedEventArgs e)
        {
            if (selectedTicket == null)
            {
                MessageBox.Show("Выберите талон.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            try
            {
                var db = new DatabaseHelper();
                bool success = await db.BookAppointmentByIdAsync(selectedTicket.AppointmentId, SessionManager.CurrentUserId);
                if (success)
                {
                    MessageBox.Show("Талон успешно заказан!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    this.DialogResult = true;
                    this.Close();
                }
            }
            catch (InvalidOperationException ioEx)
            {
                MessageBox.Show(ioEx.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка при заказе талона: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnCancelBooking_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
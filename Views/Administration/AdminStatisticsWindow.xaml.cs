using LiveCharts;
using LiveCharts.Wpf;
using MedHelp.Data;
using MedHelp.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace MedHelp.Views
{
    public partial class AdminStatisticsWindow : Window
    {
        public SeriesCollection SpecialtySeries { get; set; } = null!;
        public SeriesCollection VisitsSeries { get; set; } = null!;
        public List<string> SpecialtyLabels { get; set; } = null!;
        public List<string> VisitLabels { get; set; } = null!;
        public Func<double, string> YFormatter { get; set; } = null!;

        public AdminStatisticsWindow()
        {
            InitializeComponent();
            DataContext = this;
            YFormatter = value => value.ToString("N0");
            Loaded += AdminStatisticsWindow_Loaded;
        }

        private async void AdminStatisticsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                var db = new DatabaseHelper();
                var allDoctors = await db.GetAllDoctorsAsync();

                var allAppointments = await GetAllAppointmentsAsync();
                var allVisits = await GetAllVisitsAsync();

                LoadSpecialtyChart(allDoctors, allAppointments);
                LoadVisitsChart(allVisits);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки статистики: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            var appointments = new List<Appointment>();
            const string connectionString = @"Server=(localdb)\MSSQLLocalDB; Database=MedHelpDB; Integrated Security=true; TrustServerCertificate=true;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = @"SELECT a.AppointmentId, a.DoctorId, a.Status, d.Specialty FROM dbo.Appointments a INNER JOIN dbo.Doctors d ON a.DoctorId = d.DoctorId WHERE a.Status IN (N'Забронирован', N'Подтверждён', N'На приёме')";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                appointments.Add(new Appointment
                {
                    DoctorId = Convert.ToInt32(reader["DoctorId"]),
                    Status = reader.GetString("Status"),
                    Specialty = reader.GetString("Specialty")
                });
            }
            return appointments;
        }

        private static async Task<List<HomeVisit>> GetAllVisitsAsync()
        {
            var visits = new List<HomeVisit>();
            const string connectionString = @"Server=(localdb)\MSSQLLocalDB; Database=MedHelpDB; Integrated Security=true; TrustServerCertificate=true;";

            await using var connection = new SqlConnection(connectionString);
            await connection.OpenAsync();
            const string sql = @"SELECT RequestDate FROM dbo.HomeVisits WHERE RequestDate >= DATEADD(DAY, -7, GETDATE())";

            await using var command = new SqlCommand(sql, connection);
            await using var reader = await command.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                visits.Add(new HomeVisit { RequestDate = (DateTime)reader["RequestDate"] });
            }
            return visits;
        }

        private void LoadSpecialtyChart(List<Doctor> doctors, List<Appointment> appointments)
        {
            var stats = appointments
                .Join(doctors, a => a.DoctorId, d => d.DoctorId, (a, d) => d.Specialty)
                .GroupBy(s => s)
                .Select(g => new { Specialty = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            SpecialtyLabels = stats.Select(x => x.Specialty).ToList();

            var values = new ChartValues<int>(stats.Select(x => x.Count));

            SpecialtySeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Активные записи",
                    Values = values,
                    Fill = System.Windows.Media.Brushes.SteelBlue
                }
            };
        }

        private void LoadVisitsChart(List<HomeVisit> visits)
        {
            var last7Days = Enumerable.Range(0, 7)
                .Select(i => DateTime.Today.AddDays(-6 + i))
                .ToList();

            var stats = last7Days.Select(date => new
            {
                Date = date,
                Count = visits.Count(v => v.RequestDate.Date == date)
            }).ToList();

            VisitLabels = stats.Select(x => x.Date.ToString("dd.MM")).ToList();

            var values = new ChartValues<int>(stats.Select(x => x.Count));

            VisitsSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Вызовы за 7 дней",
                    Values = values,
                    PointGeometrySize = 8,
                    StrokeThickness = 2,
                    Fill = System.Windows.Media.Brushes.Transparent
                }
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

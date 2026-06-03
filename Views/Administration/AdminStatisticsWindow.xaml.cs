using LiveCharts;
using LiveCharts.Wpf;
using MedHelp.Data;
using MedHelp.Models;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows;

namespace MedHelp.Views
{
    public partial class AdminStatisticsWindow : Window, INotifyPropertyChanged
    {
        private SeriesCollection _specialtySeries = new();
        private SeriesCollection _visitsSeries = new();
        private List<string> _specialtyLabels = new();
        private List<string> _visitLabels = new();
        private Func<double, string> _yFormatter = value => value.ToString("N0");

        public SeriesCollection SpecialtySeries
        {
            get => _specialtySeries;
            set { _specialtySeries = value; OnPropertyChanged(); }
        }

        public SeriesCollection VisitsSeries
        {
            get => _visitsSeries;
            set { _visitsSeries = value; OnPropertyChanged(); }
        }

        public List<string> SpecialtyLabels
        {
            get => _specialtyLabels;
            set { _specialtyLabels = value; OnPropertyChanged(); }
        }

        public List<string> VisitLabels
        {
            get => _visitLabels;
            set { _visitLabels = value; OnPropertyChanged(); }
        }

        public Func<double, string> YFormatter
        {
            get => _yFormatter;
            set { _yFormatter = value; OnPropertyChanged(); }
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        protected void OnPropertyChanged([CallerMemberName] string? name = null) =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        public AdminStatisticsWindow()
        {
            InitializeComponent();
            DataContext = this;
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
                var appts = await GetAllAppointmentsAsync();
                var visits = await GetAllVisitsAsync();

                LoadSpecialtyChart(appts);
                LoadVisitsChart(visits);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки данных: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private static async Task<List<Appointment>> GetAllAppointmentsAsync()
        {
            var list = new List<Appointment>();
            const string conn = @"Server=(localdb)\MSSQLLocalDB; Database=MedHelpDB; Integrated Security=true; TrustServerCertificate=true;";

            try
            {
                await using var c = new SqlConnection(conn);
                await c.OpenAsync();
                const string sql = @"SELECT a.DoctorId, a.Status, d.Specialty 
                                     FROM dbo.Appointments a 
                                     INNER JOIN dbo.Doctors d ON a.DoctorId = d.DoctorId 
                                     WHERE a.Status IN (N'Забронирован', N'Подтверждён', N'На приёме', N'Успешно')";

                await using var cmd = new SqlCommand(sql, c);
                await using var r = await cmd.ExecuteReaderAsync();

                while (await r.ReadAsync())
                {
                    list.Add(new Appointment
                    {
                        DoctorId = Convert.ToInt32(r["DoctorId"]),
                        Status = r.GetString("Status"),
                        Specialty = r.GetString("Specialty")
                    });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка SQL (записи): {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return list;
        }

        private static async Task<List<HomeVisit>> GetAllVisitsAsync()
        {
            var list = new List<HomeVisit>();
            const string conn = @"Server=(localdb)\MSSQLLocalDB; Database=MedHelpDB; Integrated Security=true; TrustServerCertificate=true;";

            try
            {
                await using var c = new SqlConnection(conn);
                await c.OpenAsync();
                const string sql = @"SELECT RequestDate FROM dbo.HomeVisits WHERE RequestDate >= DATEADD(DAY, -7, GETDATE())";

                await using var cmd = new SqlCommand(sql, c);
                await using var r = await cmd.ExecuteReaderAsync();

                while (await r.ReadAsync())
                {
                    list.Add(new HomeVisit { RequestDate = (DateTime)r["RequestDate"] });
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка SQL (вызовы): {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            return list;
        }

        private void LoadSpecialtyChart(List<Appointment> appts)
        {
            var stats = appts
                .Where(a => !string.IsNullOrEmpty(a.Specialty))
                .GroupBy(a => a.Specialty!)
                .Select(g => new { Specialty = g.Key, Count = g.Count() })
                .OrderByDescending(x => x.Count)
                .Take(10)
                .ToList();

            SpecialtyLabels = stats.Select(x => x.Specialty).ToList();

            SpecialtySeries = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Записи",
                    Values = new ChartValues<int>(stats.Select(x => x.Count)),
                    Fill = System.Windows.Media.Brushes.SteelBlue
                }
            };
        }

        private void LoadVisitsChart(List<HomeVisit> visits)
        {
            var days = Enumerable.Range(0, 7).Select(i => DateTime.Today.AddDays(-6 + i)).ToList();
            var stats = days.Select(d => new
            {
                Date = d,
                Count = visits.Count(v => v.RequestDate.Date == d)
            }).ToList();

            VisitLabels = stats.Select(x => x.Date.ToString("dd.MM")).ToList();

            VisitsSeries = new SeriesCollection
            {
                new LineSeries
                {
                    Title = "Вызовы",
                    Values = new ChartValues<int>(stats.Select(x => x.Count)),
                    PointGeometrySize = 8,
                    StrokeThickness = 2,
                    Fill = System.Windows.Media.Brushes.Transparent
                }
            };
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
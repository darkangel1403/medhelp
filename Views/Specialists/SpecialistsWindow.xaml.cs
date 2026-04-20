using MedHelp.Data;
using MedHelp.Models;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace MedHelp.Views
{
    public partial class SpecialistsWindow : Window
    {
        private Doctor? selectedDoctor;
        private ObservableCollection<Doctor>? allDoctors;
        private string? currentSpecialty;

        public SpecialistsWindow()
        {
            InitializeComponent();
            Loaded += SpecialistsWindow_Loaded;

            dgDoctors.SelectionChanged += DgDoctors_SelectionChanged;
            txtSearch.TextChanged += TxtSearch_TextChanged;
        }

        private async void SpecialistsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadSpecialtiesAsync();
        }

        private async Task LoadSpecialtiesAsync()
        {
            try
            {
                var db = new DatabaseHelper();
                lstSpecialties.ItemsSource = await db.GetAllSpecialtiesAsync();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки специальностей: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                Close();
            }
        }

        private async void LstSpecialties_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstSpecialties.SelectedItem is string selectedSpecialty)
            {
                currentSpecialty = selectedSpecialty;
                await PerformSearch(txtSearch.Text);
            }
        }

        private async Task LoadDoctorsBySpecialty(string specialty)
        {
            try
            {
                var db = new DatabaseHelper();
                allDoctors = new ObservableCollection<Doctor>(await db.GetDoctorsBySpecialtyAsync(specialty));
                dgDoctors.ItemsSource = allDoctors;

                selectedDoctor = null;
                btnDoctorDetails.IsEnabled = false;
                btnBookTicket.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            await PerformSearch(txtSearch.Text);
        }

        private async Task PerformSearch(string filterText)
        {
            if (string.IsNullOrWhiteSpace(filterText))
            {
                if (!string.IsNullOrEmpty(currentSpecialty))
                {
                    await LoadDoctorsBySpecialty(currentSpecialty);
                }
                else
                {
                    dgDoctors.ItemsSource = null;
                    allDoctors = null;
                }
                return;
            }

            try
            {
                var db = new DatabaseHelper();
                List<Doctor> sourceList;

                if (!string.IsNullOrEmpty(currentSpecialty))
                {
                    sourceList = await db.GetDoctorsBySpecialtyAsync(currentSpecialty);
                }
                else
                {
                    sourceList = await db.GetAllDoctorsAsync();
                }

                var filtered = new ObservableCollection<Doctor>(
                    sourceList.Where(d =>
                        (!string.IsNullOrEmpty(d.FullName) && d.FullName.ToLower().Contains(filterText.ToLower())) ||
                        (!string.IsNullOrEmpty(d.Specialty) && d.Specialty.ToLower().Contains(filterText.ToLower()))
                    ).ToList()
                );

                allDoctors = filtered;
                dgDoctors.ItemsSource = allDoctors;

                selectedDoctor = null;
                btnDoctorDetails.IsEnabled = false;
                btnBookTicket.IsEnabled = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка поиска: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Focus();
        }

        private void DgDoctors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDoctor = dgDoctors.SelectedItem as Doctor;
            btnDoctorDetails.IsEnabled = selectedDoctor != null;
            btnBookTicket.IsEnabled = selectedDoctor != null;
        }

        private void DgDoctors_MouseDoubleClick(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            BtnDoctorDetails_Click(sender, new RoutedEventArgs());
        }

        private void BtnDoctorDetails_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDoctor != null)
                new DoctorDetailsWindow(selectedDoctor).ShowDialog();
            else
                MessageBox.Show("Выберите врача.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnBookTicket_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDoctor != null)
                new BookTicketWindow(selectedDoctor).ShowDialog();
            else
                MessageBox.Show("Выберите врача.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
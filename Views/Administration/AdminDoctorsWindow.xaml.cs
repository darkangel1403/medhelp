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
    public partial class AdminDoctorsWindow : Window
    {
        private ObservableCollection<Doctor>? doctorsList;
        private Doctor? selectedDoctor;
        private readonly bool? mode;

        public AdminDoctorsWindow(bool? mode)
        {
            InitializeComponent();
            this.mode = mode;
            Loaded += AdminDoctorsWindow_Loaded;
            txtSearch.TextChanged += TxtSearch_TextChanged;
            if (mode == true) { Title = "Управление врачами"; btnAction.Visibility = Visibility.Collapsed; btnAdd.Content = "Добавить нового"; }
            else if (mode == false) { Title = "Удаление врача"; btnAdd.Visibility = btnEdit.Visibility = Visibility.Collapsed; btnAction.Visibility = Visibility.Visible; }
            else { Title = "Справочник врачей"; btnAction.Visibility = Visibility.Collapsed; }
        }

        private async void AdminDoctorsWindow_Loaded(object sender, RoutedEventArgs e) { await LoadDoctorsAsync(); }

        private async Task LoadDoctorsAsync()
        {
            try { doctorsList = new ObservableCollection<Doctor>(await new DatabaseHelper().GetAllDoctorsAsync()); dgDoctors.ItemsSource = doctorsList; }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (doctorsList == null) return;
            string f = txtSearch.Text.Trim().ToLower();
            dgDoctors.ItemsSource = string.IsNullOrEmpty(f) ? doctorsList : doctorsList.Where(d => d.FullName.ToLower().Contains(f)).ToList();
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e) { txtSearch.Focus(); }

        private void DgDoctors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDoctor = dgDoctors.SelectedItem as Doctor;
            if (mode == false) btnAction.IsEnabled = selectedDoctor != null;
            else if (mode == true) btnEdit.IsEnabled = selectedDoctor != null;
        }

        private async void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDoctor == null) { MessageBox.Show("Выберите врача.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            if (MessageBox.Show($"Удалить {selectedDoctor.FullName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                try
                {
                    if (await new DatabaseHelper().DeleteDoctorAsync(selectedDoctor.DoctorId)) { doctorsList?.Remove(selectedDoctor); txtSearch.Clear(); MessageBox.Show("Удалено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); selectedDoctor = null; btnAction.IsEnabled = false; }
                    else MessageBox.Show("Не удалось удалить.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dlg = new DoctorEditWindow(null);
            if (dlg.ShowDialog() == true && dlg.Doctor != null)
            {
                try { await new DatabaseHelper().AddDoctorAsync(dlg.Doctor); MessageBox.Show("Добавлен.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); await LoadDoctorsAsync(); txtSearch.Clear(); }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDoctor == null) return;
            var dlg = new DoctorEditWindow(new Doctor { DoctorId = selectedDoctor.DoctorId, FullName = selectedDoctor.FullName, Specialty = selectedDoctor.Specialty, CabinetNumber = selectedDoctor.CabinetNumber, ExperienceYears = selectedDoctor.ExperienceYears, Education = selectedDoctor.Education });
            if (dlg.ShowDialog() == true && dlg.Doctor != null)
            {
                try { await new DatabaseHelper().UpdateDoctorAsync(dlg.Doctor); MessageBox.Show("Обновлено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information); await LoadDoctorsAsync(); txtSearch.Clear(); }
                catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
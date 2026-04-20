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

            if (mode == true)
            {
                Title = "Управление врачами (Редактирование)";
                btnAction.Visibility = Visibility.Collapsed;
                btnAdd.Content = "➕ Добавить нового";
            }
            else if (mode == false)
            {
                Title = "Удаление врача";
                btnAdd.Visibility = Visibility.Collapsed;
                btnEdit.Visibility = Visibility.Collapsed;
                btnAction.Visibility = Visibility.Visible;
                btnAction.Content = "🗑️ Удалить врача";
            }
            else
            {
                Title = "Справочник врачей";
                btnAction.Visibility = Visibility.Collapsed;
            }
        }

        private async void AdminDoctorsWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDoctorsAsync();
        }

        private async Task LoadDoctorsAsync()
        {
            try
            {
                var db = new DatabaseHelper();
                var list = await db.GetAllDoctorsAsync();
                doctorsList = new ObservableCollection<Doctor>(list);
                dgDoctors.ItemsSource = doctorsList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка загрузки: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (doctorsList == null) return;

            string filter = txtSearch.Text.Trim().ToLower();

            if (string.IsNullOrEmpty(filter))
            {
                dgDoctors.ItemsSource = doctorsList;
            }
            else
            {
                var filtered = doctorsList.Where(d => d.FullName.ToLower().Contains(filter)).ToList();
                dgDoctors.ItemsSource = filtered;
            }
        }

        private void BtnSearch_Click(object sender, RoutedEventArgs e)
        {
            txtSearch.Focus();
        }

        private void DgDoctors_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            selectedDoctor = dgDoctors.SelectedItem as Doctor;
            if (mode == false)
            {
                btnAction.IsEnabled = selectedDoctor != null;
            }
            else if (mode == true)
            {
                btnEdit.IsEnabled = selectedDoctor != null;
            }
        }

        private async void BtnAction_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDoctor == null)
            {
                MessageBox.Show("Выберите врача из списка для удаления.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            var confirm = MessageBox.Show(
                $"Вы действительно хотите удалить врача:\n{selectedDoctor.FullName}?\nЭто действие нельзя отменить.",
                "Подтверждение удаления",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            if (confirm == MessageBoxResult.Yes)
            {
                try
                {
                    var db = new DatabaseHelper();
                    bool success = await db.DeleteDoctorAsync(selectedDoctor.DoctorId);
                    if (success)
                    {
                        doctorsList?.Remove(selectedDoctor);
                        txtSearch.Clear(); 
                        MessageBox.Show("Врач успешно удалён из базы данных.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        selectedDoctor = null;
                        btnAction.IsEnabled = false;
                    }
                    else
                    {
                        MessageBox.Show("Не удалось удалить врача.\nВозможно, у него есть активные записи или вызовы.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при удалении: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new DoctorEditWindow(null);
            if (dialog.ShowDialog() == true && dialog.Doctor != null)
            {
                try
                {
                    var db = new DatabaseHelper();
                    await db.AddDoctorAsync(dialog.Doctor);
                    MessageBox.Show("Врач успешно добавлен в справочник.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadDoctorsAsync(); 
                    txtSearch.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (selectedDoctor == null) return;
            var dialog = new DoctorEditWindow(new Doctor
            {
                DoctorId = selectedDoctor.DoctorId,
                FullName = selectedDoctor.FullName,
                Specialty = selectedDoctor.Specialty,
                CabinetNumber = selectedDoctor.CabinetNumber,
                ExperienceYears = selectedDoctor.ExperienceYears,
                Education = selectedDoctor.Education
            });
            if (dialog.ShowDialog() == true && dialog.Doctor != null)
            {
                try
                {
                    var db = new DatabaseHelper();
                    await db.UpdateDoctorAsync(dialog.Doctor);
                    MessageBox.Show("Данные врача успешно обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    await LoadDoctorsAsync();
                    txtSearch.Clear();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}

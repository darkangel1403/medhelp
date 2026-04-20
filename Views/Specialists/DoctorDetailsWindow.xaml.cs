using System.Windows;
using MedHelp.Models;

namespace MedHelp.Views
{
    public partial class DoctorDetailsWindow : Window
    {
        public DoctorDetailsWindow(Doctor doctor)
        {
            InitializeComponent();
            if (doctor != null)
            {
                lblFullName.Text = doctor.FullName;
                lblSpecialty.Text = doctor.Specialty;
                lblCabinet.Text = doctor.CabinetNumber;
                lblExperience.Text = $"{doctor.ExperienceYears} лет";
                lblEducation.Text = string.IsNullOrEmpty(doctor.Education) ? "Информация отсутствует." : doctor.Education;
                this.Title = $"MedHelp — {doctor.FullName}";
            }
            else
            {
                MessageBox.Show("Информация о враче недоступна.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                this.Close();
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
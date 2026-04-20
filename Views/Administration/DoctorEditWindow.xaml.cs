using MedHelp.Models;
using System;
using System.Windows;

namespace MedHelp.Views
{
    public partial class DoctorEditWindow : Window
    {
        public Doctor? Doctor { get; private set; }

        public DoctorEditWindow(Doctor? doctor)
        {
            InitializeComponent();
            if (doctor != null)
            {
                Doctor = new Doctor
                {
                    DoctorId = doctor.DoctorId,
                    FullName = doctor.FullName,
                    Specialty = doctor.Specialty,
                    CabinetNumber = doctor.CabinetNumber,
                    ExperienceYears = doctor.ExperienceYears,
                    Education = doctor.Education
                };
                tbFullName.Text = doctor.FullName;
                tbSpecialty.Text = doctor.Specialty;
                tbCabinet.Text = doctor.CabinetNumber;
                tbExp.Text = doctor.ExperienceYears.ToString();
                tbEdu.Text = doctor.Education;
                Title = "Редактирование врача";
            }
            else
            {
                Doctor = new Doctor();
                Title = "Новый врач";
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(tbFullName.Text) || string.IsNullOrWhiteSpace(tbSpecialty.Text))
            {
                MessageBox.Show("Заполните ФИО и Специальность.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (!int.TryParse(tbExp.Text, out int exp)) exp = 0;
            if (Doctor == null) Doctor = new Doctor();
            Doctor.FullName = tbFullName.Text;
            Doctor.Specialty = tbSpecialty.Text;
            Doctor.CabinetNumber = tbCabinet.Text;
            Doctor.ExperienceYears = exp;
            Doctor.Education = tbEdu.Text;
            DialogResult = true;
            Close();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e) => DialogResult = false;
    }
}

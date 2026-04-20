using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using MedHelp.Data;
using MedHelp.Models;
using MedHelp.Services;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class AppointmentsArchiveWindow : Window
    {
        public AppointmentsArchiveWindow()
        {
            InitializeComponent();
            Loaded += AppointmentsArchiveWindow_Loaded;
            InputBindings.Add(new KeyBinding(new RelayCommand(async () => await RunExport()), Key.S, ModifierKeys.Control));
        }

        private async void AppointmentsArchiveWindow_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadArchiveAsync();
        }

        private async Task LoadArchiveAsync()
        {
            try
            {
                var db = new DatabaseHelper();
                var archive = await db.GetArchivedAppointmentsForPatientAsync(SessionManager.CurrentUserId);
                dgArchive.ItemsSource = new ObservableCollection<Appointment>(archive);
            }
            catch (Exception ex) { MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e)
        {
            await RunExport();
        }

        private async Task RunExport()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx|TXT (*.txt)|*.txt|JSON (*.json)|*.json",
                DefaultExt = ".csv",
                FileName = $"Архив_Талоны_{SessionManager.CurrentUserFullName}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string ext = Path.GetExtension(filePath).ToLower();
                try
                {
                    if (ext == ".csv") await ExportToCsv(filePath);
                    else if (ext == ".xlsx") await ExportToExcel(filePath);
                    else if (ext == ".txt") await ExportToTxt(filePath);
                    else if (ext == ".json") await ExportToJson(filePath);
                    else { MessageBox.Show("Неизвестный формат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                    MessageBox.Show("Отчёт сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private async Task ExportToCsv(string filePath)
        {
            var db = new DatabaseHelper();
            var appointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("ИНФОРМАЦИЯ О ПОЛЬЗОВАТЕЛЕ");
                writer.WriteLine("Параметр;Значение");
                writer.WriteLine($"ФИО;{SessionManager.CurrentUserFullName}");
                writer.WriteLine($"Логин;{SessionManager.CurrentUserLogin}");
                writer.WriteLine($"Роль;{SessionManager.CurrentUserRole}");
                writer.WriteLine($"Адрес;{SessionManager.CurrentUserAddress}");
                writer.WriteLine($"Телефон;{SessionManager.CurrentUserPhone}");
                writer.WriteLine();
                writer.WriteLine("ВСЕ ЗАПИСИ НА ПРИЁМ");
                writer.WriteLine("Тип;Врач;Специальность;Дата/Время;Статус");
                foreach (var a in appointments)
                    writer.WriteLine($"Запись;{a.DoctorName};{a.Specialty ?? ""};{a.VisitDate:dd.MM.yyyy HH:mm};{a.Status}");
            }
        }

        private async Task ExportToTxt(string filePath)
        {
            var db = new DatabaseHelper();
            var appointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            using (var writer = new StreamWriter(filePath, false, Encoding.UTF8))
            {
                writer.WriteLine("============================================================");
                writer.WriteLine($"АРХИВ ТАЛОНОВ - {SessionManager.CurrentUserFullName}");
                writer.WriteLine("============================================================");
                writer.WriteLine($"Логин: {SessionManager.CurrentUserLogin}");
                writer.WriteLine($"Роль: {SessionManager.CurrentUserRole}");
                writer.WriteLine($"Адрес: {SessionManager.CurrentUserAddress}");
                writer.WriteLine($"Телефон: {SessionManager.CurrentUserPhone}");
                writer.WriteLine($"Дата отчёта: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
                writer.WriteLine("============================================================\n");
                writer.WriteLine("--- ВСЕ ЗАПИСИ ---\n");
                foreach (var a in appointments)
                {
                    writer.WriteLine($"{a.DoctorName} ({a.Specialty})");
                    writer.WriteLine($"Дата: {a.VisitDate:dd.MM.yyyy HH:mm}");
                    writer.WriteLine($"Статус: {a.Status}");
                    writer.WriteLine();
                }
            }
        }

        private async Task ExportToJson(string filePath)
        {
            var db = new DatabaseHelper();
            var appointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var data = new
            {
                Пользователь = new
                {
                    ФИО = SessionManager.CurrentUserFullName,
                    Логин = SessionManager.CurrentUserLogin,
                    Роль = SessionManager.CurrentUserRole,
                    Адрес = SessionManager.CurrentUserAddress,
                    Телефон = SessionManager.CurrentUserPhone
                },
                Записи = appointments.Select(a => new
                {
                    Врач = a.DoctorName,
                    Специальность = a.Specialty ?? "",
                    Дата = a.VisitDate.ToString("dd.MM.yyyy HH:mm"),
                    Статус = a.Status
                }).ToArray()
            };
            var options = new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping };
            File.WriteAllText(filePath, JsonSerializer.Serialize(data, options), Encoding.UTF8);
        }

        private async Task ExportToExcel(string filePath)
        {
            var db = new DatabaseHelper();
            var appointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            using (var workbook = new XLWorkbook())
            {
                var wsUser = workbook.Worksheets.Add("Пользователь");
                wsUser.Cell(1, 1).Value = "Параметр";
                wsUser.Cell(1, 2).Value = "Значение";
                wsUser.Cell(2, 1).Value = "ФИО"; wsUser.Cell(2, 2).Value = SessionManager.CurrentUserFullName;
                wsUser.Cell(3, 1).Value = "Логин"; wsUser.Cell(3, 2).Value = SessionManager.CurrentUserLogin;
                wsUser.Cell(4, 1).Value = "Роль"; wsUser.Cell(4, 2).Value = SessionManager.CurrentUserRole;
                wsUser.Cell(5, 1).Value = "Адрес"; wsUser.Cell(5, 2).Value = SessionManager.CurrentUserAddress;
                wsUser.Cell(6, 1).Value = "Телефон"; wsUser.Cell(6, 2).Value = SessionManager.CurrentUserPhone;
                wsUser.Columns().AdjustToContents();
                var wsAppts = workbook.Worksheets.Add("Записи");
                wsAppts.Cell(1, 1).Value = "Врач";
                wsAppts.Cell(1, 2).Value = "Специальность";
                wsAppts.Cell(1, 3).Value = "Дата";
                wsAppts.Cell(1, 4).Value = "Статус";
                int row = 2;
                foreach (var a in appointments)
                {
                    wsAppts.Cell(row, 1).Value = a.DoctorName;
                    wsAppts.Cell(row, 2).Value = a.Specialty ?? "";
                    wsAppts.Cell(row, 3).Value = a.VisitDate;
                    wsAppts.Cell(row, 4).Value = a.Status;
                    row++;
                }
                wsAppts.Columns().AdjustToContents();
                workbook.SaveAs(filePath);
            }
        }

        private void BtnSendEmail_Click(object sender, RoutedEventArgs e)
        {
            string subject = $"Архив талонов - {SessionManager.CurrentUserFullName}";
            string body = "Здравствуйте!\nВо вложении ваш архив талонов (файл необходимо прикрепить вручную после экспорта).\nС уважением, MedHelp.";
            Process.Start(new ProcessStartInfo($"mailto:?subject={Uri.EscapeDataString(subject)}&body={Uri.EscapeDataString(body)}") { UseShellExecute = true });
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}

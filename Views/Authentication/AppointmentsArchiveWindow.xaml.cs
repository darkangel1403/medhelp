using ClosedXML.Excel;
using MedHelp.Data;
using MedHelp.Models;
using MedHelp.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
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
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) { RunExport(); e.Handled = true; }
        }

        private async void AppointmentsArchiveWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dgArchive.ItemsSource = new ObservableCollection<Appointment>(await new DatabaseHelper().GetArchivedAppointmentsForPatientAsync(SessionManager.CurrentUserId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void BtnExport_Click(object sender, RoutedEventArgs e) => RunExport();

        private async void RunExport()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV|*.csv|Excel|*.xlsx|TXT|*.txt|JSON|*.json",
                DefaultExt = ".csv",
                FileName = $"Архив_Талоны_{DateTime.Now:yyyyMMdd}"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string ext = Path.GetExtension(dlg.FileName).ToLower();
                    if (ext == ".csv") await ExportToCsv(dlg.FileName);
                    else if (ext == ".xlsx") await ExportToExcel(dlg.FileName);
                    else if (ext == ".txt") await ExportToTxt(dlg.FileName);
                    else if (ext == ".json") await ExportToJson(dlg.FileName);
                    else { MessageBox.Show("Неизвестный формат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }

                    MessageBox.Show("Сохранено.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static async Task ExportToCsv(string f)
        {
            var db = new DatabaseHelper();
            var list = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            using var w = new StreamWriter(f, false, new UTF8Encoding(true));
            await w.WriteLineAsync("Врач;Специальность;Дата;Статус");
            foreach (var a in list)
                await w.WriteLineAsync($"{a.DoctorName};{a.Specialty ?? ""};{a.VisitDate:dd.MM.yyyy HH:mm};{a.Status}");
        }

        private static async Task ExportToTxt(string f)
        {
            var db = new DatabaseHelper();
            var list = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            using var w = new StreamWriter(f, false, new UTF8Encoding(true));
            await w.WriteLineAsync("Архив талонов");
            foreach (var a in list)
                await w.WriteLineAsync($"{a.DoctorName} | {a.VisitDate:dd.MM.yyyy HH:mm} | {a.Status}");
        }

        private static async Task ExportToJson(string f)
        {
            var db = new DatabaseHelper();
            var list = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);

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
                Талоны = list.Select(a => new
                {
                    Врач = a.DoctorName,
                    Специальность = a.Specialty ?? "",
                    Дата = a.VisitDate.ToString("dd.MM.yyyy HH:mm"),
                    Статус = a.Status
                }).ToArray()
            };

            var options = new JsonSerializerOptions
            {
                WriteIndented = true,
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            };

            string json = JsonSerializer.Serialize(data, options);
            await File.WriteAllTextAsync(f, json, new UTF8Encoding(true));
        }

        private static async Task ExportToExcel(string f)
        {
            var db = new DatabaseHelper();
            var appointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);

            using var wb = new XLWorkbook();

            var wsUser = wb.Worksheets.Add("Пользователь");
            wsUser.Cell(1, 1).Value = "Параметр";
            wsUser.Cell(1, 2).Value = "Значение";

            var headerStyle = wsUser.Row(1).Style;
            headerStyle.Font.Bold = true;
            headerStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            headerStyle.Border.BottomBorder = XLBorderStyleValues.Thin;

            wsUser.Cell(2, 1).Value = "ФИО"; wsUser.Cell(2, 2).Value = SessionManager.CurrentUserFullName;
            wsUser.Cell(3, 1).Value = "Логин"; wsUser.Cell(3, 2).Value = SessionManager.CurrentUserLogin;
            wsUser.Cell(4, 1).Value = "Роль"; wsUser.Cell(4, 2).Value = SessionManager.CurrentUserRole;
            wsUser.Cell(5, 1).Value = "Адрес"; wsUser.Cell(5, 2).Value = SessionManager.CurrentUserAddress;
            wsUser.Cell(6, 1).Value = "Телефон"; wsUser.Cell(6, 2).Value = SessionManager.CurrentUserPhone;

            wsUser.Range("A2:B6").Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
            wsUser.Range("A2:B6").Style.Border.InsideBorder = XLBorderStyleValues.Thin;

            wsUser.Columns().AdjustToContents();
            wsUser.Column(1).Width = 20;
            wsUser.Column(2).Width = 40;

            var wsAppts = wb.Worksheets.Add("Архив талонов");

            wsAppts.Cell(1, 1).Value = "Врач";
            wsAppts.Cell(1, 2).Value = "Специальность";
            wsAppts.Cell(1, 3).Value = "Дата и время";
            wsAppts.Cell(1, 4).Value = "Статус";

            var apptHeaderRange = wsAppts.Range("A1:D1");
            apptHeaderRange.Style.Font.Bold = true;
            apptHeaderRange.Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            apptHeaderRange.Style.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            apptHeaderRange.Style.Border.BottomBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var a in appointments)
            {
                wsAppts.Cell(row, 1).Value = a.DoctorName;
                wsAppts.Cell(row, 2).Value = a.Specialty ?? "";
                wsAppts.Cell(row, 3).Value = a.VisitDate;
                wsAppts.Cell(row, 3).Style.DateFormat.Format = "dd.mm.yyyy hh:mm";
                wsAppts.Cell(row, 4).Value = a.Status;

                if (a.Status == "Успешно")
                    wsAppts.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                else if (a.Status == "Отменён")
                    wsAppts.Cell(row, 4).Style.Font.FontColor = XLColor.Red;

                row++;
            }

            if (appointments.Any())
            {
                var dataRange = wsAppts.Range($"A1:D{row - 1}");
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            wsAppts.Columns().AdjustToContents();
            wsAppts.Column(1).Width = 30;
            wsAppts.Column(2).Width = 20;
            wsAppts.Column(3).Width = 22;
            wsAppts.Column(4).Width = 15;

            await Task.Run(() => wb.SaveAs(f));
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
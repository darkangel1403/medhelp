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
    public partial class HomeVisitsArchiveWindow : Window
    {
        public HomeVisitsArchiveWindow()
        {
            InitializeComponent();
            Loaded += HomeVisitsArchiveWindow_Loaded;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape) Close();
            else if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control) { RunExport(); e.Handled = true; }
        }

        private async void HomeVisitsArchiveWindow_Loaded(object sender, RoutedEventArgs e)
        {
            try
            {
                dgArchive.ItemsSource = new ObservableCollection<HomeVisit>(await new DatabaseHelper().GetArchivedHomeVisitsForPatientAsync(SessionManager.CurrentUserId));
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
                FileName = $"Архив_Вызовы_{DateTime.Now:yyyyMMdd}"
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
            var list = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);
            using var w = new StreamWriter(f, false, new UTF8Encoding(true));
            await w.WriteLineAsync("Врач;Дата;Статус;Жалобы");
            foreach (var v in list)
                await w.WriteLineAsync($"{v.DoctorName};{v.RequestDate:dd.MM.yyyy HH:mm};{v.Status};{v.Complaints}");
        }

        private static async Task ExportToTxt(string f)
        {
            var db = new DatabaseHelper();
            var list = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);
            using var w = new StreamWriter(f, false, new UTF8Encoding(true));
            await w.WriteLineAsync("Архив вызовов");
            foreach (var v in list)
                await w.WriteLineAsync($"{v.DoctorName} | {v.RequestDate:dd.MM.yyyy HH:mm} | {v.Status} | {v.Complaints}");
        }

        private static async Task ExportToJson(string f)
        {
            var db = new DatabaseHelper();
            var list = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);

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
                Вызовы = list.Select(v => new
                {
                    Врач = v.DoctorName,
                    Дата = v.RequestDate.ToString("dd.MM.yyyy HH:mm"),
                    Статус = v.Status,
                    Жалобы = v.Complaints,
                    Адрес = v.VisitAddress,
                    Телефон = v.PhoneNumber
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
            var visits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);

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

            var wsVisits = wb.Worksheets.Add("Вызовы");
            wsVisits.Cell(1, 1).Value = "Врач";
            wsVisits.Cell(1, 2).Value = "Дата заявки";
            wsVisits.Cell(1, 3).Value = "Статус";
            wsVisits.Cell(1, 4).Value = "Жалобы";
            wsVisits.Cell(1, 5).Value = "Адрес";
            wsVisits.Cell(1, 6).Value = "Телефон";

            var visitHeaderStyle = wsVisits.Row(1).Style;
            visitHeaderStyle.Font.Bold = true;
            visitHeaderStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            visitHeaderStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            visitHeaderStyle.Border.BottomBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var v in visits)
            {
                wsVisits.Cell(row, 1).Value = v.DoctorName;
                wsVisits.Cell(row, 2).Value = v.RequestDate;
                wsVisits.Cell(row, 2).Style.DateFormat.Format = "dd.mm.yyyy hh:mm";
                wsVisits.Cell(row, 3).Value = v.Status;

                if (v.Status == "Успешно")
                    wsVisits.Cell(row, 3).Style.Font.FontColor = XLColor.Green;
                else if (v.Status == "Отменён")
                    wsVisits.Cell(row, 3).Style.Font.FontColor = XLColor.Red;

                wsVisits.Cell(row, 4).Value = v.Complaints;
                wsVisits.Cell(row, 5).Value = v.VisitAddress;
                wsVisits.Cell(row, 6).Value = v.PhoneNumber;
                row++;
            }

            if (visits.Any())
            {
                var dataRange = wsVisits.Range($"A1:F{row - 1}");
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            wsVisits.Columns().AdjustToContents();
            wsVisits.Column(2).Width = 22;
            wsVisits.Column(4).Width = 50;
            wsVisits.Column(5).Width = 40;

            await Task.Run(() => wb.SaveAs(f));
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
    }
}
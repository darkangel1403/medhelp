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
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Input;

namespace MedHelp.Views
{
    public partial class PersonalAccountWindow : Window
    {
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            WriteIndented = true,
            Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public PersonalAccountWindow()
        {
            InitializeComponent();
            Loaded += PersonalAccountWindow_Loaded;
        }

        private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                RunExport();
                e.Handled = true;
            }
        }

        private async void PersonalAccountWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lblFullName.Text = SessionManager.CurrentUserFullName;
            lblLogin.Text = SessionManager.CurrentUserLogin;
            lblRole.Text = SessionManager.CurrentUserRole;
            lblAddress.Text = SessionManager.CurrentUserAddress;
            lblPhone.Text = SessionManager.CurrentUserPhone;

            try
            {
                await new DatabaseHelper().UpdateAppointmentStatusesAsync();
            }
            catch { }

            await LoadMyAppointmentsAsync();
            await LoadMyHomeVisitsAsync();
        }

        private async Task LoadMyAppointmentsAsync()
        {
            try
            {
                dgMyAppointments.ItemsSource = new ObservableCollection<Appointment>(await new DatabaseHelper().GetAppointmentsForPatientAsync(SessionManager.CurrentUserId));
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                dgMyAppointments.ItemsSource = null;
            }
        }

        private async Task LoadMyHomeVisitsAsync()
        {
            try
            {
                dgMyHomeVisits.ItemsSource = new ObservableCollection<HomeVisit>((await new DatabaseHelper().GetHomeVisitsForPatientAsync(SessionManager.CurrentUserId)).Where(v => v.Status != "Отменён").ToList());
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                dgMyHomeVisits.ItemsSource = null;
            }
        }

        private async void BtnCancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is Appointment appt)
            {
                if (MessageBox.Show($"Отменить запись к {appt.DoctorName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (appt.Status != "Забронирован" && appt.Status != "Подтверждён" && appt.Status != "Ожидает")
                        {
                            MessageBox.Show("Нельзя отменить.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        await new DatabaseHelper().CancelAppointmentByIdAsync(appt.AppointmentId, SessionManager.CurrentUserId);
                        await LoadMyAppointmentsAsync();
                        MessageBox.Show("Запись отменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void BtnCancelHomeVisit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is HomeVisit hv)
            {
                if (MessageBox.Show($"Отменить вызов {hv.DoctorName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (hv.Status != "Ожидает" && hv.Status != "Подтверждён")
                        {
                            MessageBox.Show("Нельзя отменить.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }

                        await new DatabaseHelper().CancelHomeVisitByIdAsync(hv.VisitId, SessionManager.CurrentUserId);
                        await LoadMyHomeVisitsAsync();
                        MessageBox.Show("Заявка отменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void BtnShowArchiveVisits_Click(object sender, RoutedEventArgs e)
        {
            new HomeVisitsArchiveWindow { Owner = this }.ShowDialog();
        }

        private async void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            var win = new EditUserDetailsWindow(SessionManager.CurrentUserFullName, SessionManager.CurrentUserLogin, SessionManager.CurrentUserAddress, SessionManager.CurrentUserPhone);
            if (win.ShowDialog() == true)
            {
                try
                {
                    await new DatabaseHelper().UpdateUserDataAsync(SessionManager.CurrentUserId, win.EditedFullName, win.EditedLogin, win.EditedAddress, win.EditedPhone);
                    SessionManager.CurrentUserFullName = win.EditedFullName;
                    SessionManager.CurrentUserLogin = win.EditedLogin;
                    SessionManager.CurrentUserAddress = win.EditedAddress;
                    SessionManager.CurrentUserPhone = win.EditedPhone;

                    lblFullName.Text = win.EditedFullName;
                    lblLogin.Text = win.EditedLogin;
                    lblAddress.Text = win.EditedAddress;
                    lblPhone.Text = win.EditedPhone;

                    MessageBox.Show("Данные обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private async void BtnExportToFile_Click(object sender, RoutedEventArgs e) => RunExport();

        private async void RunExport()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx|TXT (*.txt)|*.txt|JSON (*.json)|*.json",
                DefaultExt = ".csv",
                FileName = $"Отчет_{SessionManager.CurrentUserFullName}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };

            if (dlg.ShowDialog() == true)
            {
                try
                {
                    string ext = Path.GetExtension(dlg.FileName).ToLower();
                    if (ext == ".csv") await ExportToCsvAsync(dlg.FileName);
                    else if (ext == ".xlsx") await ExportToExcelAsync(dlg.FileName);
                    else if (ext == ".txt") await ExportToTxtAsync(dlg.FileName);
                    else if (ext == ".json") await ExportToJsonAsync(dlg.FileName);
                    else { MessageBox.Show("Неизвестный формат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }

                    MessageBox.Show("Отчёт сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private static async Task ExportToCsvAsync(string f)
        {
            var db = new DatabaseHelper();
            var appts = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var visits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);

            using var w = new StreamWriter(f, false, new UTF8Encoding(true));
            await w.WriteLineAsync("ФИО;Логин;Роль;Адрес;Телефон");
            await w.WriteLineAsync($"{SessionManager.CurrentUserFullName};{SessionManager.CurrentUserLogin};{SessionManager.CurrentUserRole};{SessionManager.CurrentUserAddress};{SessionManager.CurrentUserPhone}");
            await w.WriteLineAsync();
            await w.WriteLineAsync("Записи:Врач;Специальность;Дата;Статус");

            foreach (var a in appts)
                await w.WriteLineAsync($"Запись;{a.DoctorName};{a.Specialty ?? ""};{a.VisitDate:dd.MM.yyyy HH:mm};{a.Status}");

            await w.WriteLineAsync();
            await w.WriteLineAsync("Вызовы:Врач;Дата;Статус;Жалобы;Адрес;Телефон");

            foreach (var v in visits)
                await w.WriteLineAsync($"Вызов;{v.DoctorName};{v.RequestDate:dd.MM.yyyy HH:mm};{v.Status};{v.Complaints};{v.VisitAddress};{v.PhoneNumber}");
        }

        private static async Task ExportToExcelAsync(string f)
        {
            var db = new DatabaseHelper();
            var appts = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
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

            var wsAppts = wb.Worksheets.Add("Записи");
            wsAppts.Cell(1, 1).Value = "Тип";
            wsAppts.Cell(1, 2).Value = "Врач";
            wsAppts.Cell(1, 3).Value = "Специальность";
            wsAppts.Cell(1, 4).Value = "Дата и время";
            wsAppts.Cell(1, 5).Value = "Статус";

            var apptHeaderStyle = wsAppts.Row(1).Style;
            apptHeaderStyle.Font.Bold = true;
            apptHeaderStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            apptHeaderStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            apptHeaderStyle.Border.BottomBorder = XLBorderStyleValues.Thin;

            int row = 2;
            foreach (var a in appts)
            {
                wsAppts.Cell(row, 1).Value = "Запись";
                wsAppts.Cell(row, 2).Value = a.DoctorName;
                wsAppts.Cell(row, 3).Value = a.Specialty ?? "";
                wsAppts.Cell(row, 4).Value = a.VisitDate;
                wsAppts.Cell(row, 4).Style.DateFormat.Format = "dd.mm.yyyy hh:mm";
                wsAppts.Cell(row, 5).Value = a.Status;

                if (a.Status == "Успешно") wsAppts.Cell(row, 5).Style.Font.FontColor = XLColor.Green;
                else if (a.Status == "Отменён") wsAppts.Cell(row, 5).Style.Font.FontColor = XLColor.Red;

                row++;
            }

            if (appts.Any())
            {
                var dataRange = wsAppts.Range($"A1:E{row - 1}");
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            wsAppts.Columns().AdjustToContents();
            wsAppts.Column(4).Width = 22; 

            var wsVisits = wb.Worksheets.Add("Вызовы");
            wsVisits.Cell(1, 1).Value = "Тип";
            wsVisits.Cell(1, 2).Value = "Врач";
            wsVisits.Cell(1, 3).Value = "Дата заявки";
            wsVisits.Cell(1, 4).Value = "Статус";
            wsVisits.Cell(1, 5).Value = "Жалобы";
            wsVisits.Cell(1, 6).Value = "Адрес";
            wsVisits.Cell(1, 7).Value = "Телефон";

            var visitHeaderStyle = wsVisits.Row(1).Style;
            visitHeaderStyle.Font.Bold = true;
            visitHeaderStyle.Alignment.Horizontal = XLAlignmentHorizontalValues.Center;
            visitHeaderStyle.Alignment.Vertical = XLAlignmentVerticalValues.Center;
            visitHeaderStyle.Border.BottomBorder = XLBorderStyleValues.Thin;

            row = 2;
            foreach (var v in visits)
            {
                wsVisits.Cell(row, 1).Value = "Вызов";
                wsVisits.Cell(row, 2).Value = v.DoctorName;
                wsVisits.Cell(row, 3).Value = v.RequestDate;
                wsVisits.Cell(row, 3).Style.DateFormat.Format = "dd.mm.yyyy hh:mm";
                wsVisits.Cell(row, 4).Value = v.Status;

                if (v.Status == "Успешно") wsVisits.Cell(row, 4).Style.Font.FontColor = XLColor.Green;
                else if (v.Status == "Отменён") wsVisits.Cell(row, 4).Style.Font.FontColor = XLColor.Red;

                wsVisits.Cell(row, 5).Value = v.Complaints;
                wsVisits.Cell(row, 6).Value = v.VisitAddress;
                wsVisits.Cell(row, 7).Value = v.PhoneNumber;
                row++;
            }

            if (visits.Any())
            {
                var dataRange = wsVisits.Range($"A1:G{row - 1}");
                dataRange.Style.Border.OutsideBorder = XLBorderStyleValues.Thin;
                dataRange.Style.Border.InsideBorder = XLBorderStyleValues.Thin;
            }

            wsVisits.Columns().AdjustToContents();
            wsVisits.Column(3).Width = 22; 
            wsVisits.Column(5).Width = 50;  

            await Task.Run(() => wb.SaveAs(f));
        }

        private static async Task ExportToTxtAsync(string f)
        {
            var db = new DatabaseHelper();
            var appts = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var visits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);

            using var w = new StreamWriter(f, false, new UTF8Encoding(true));
            await w.WriteLineAsync($"Пациент: {SessionManager.CurrentUserFullName}");
            await w.WriteLineAsync($"Логин: {SessionManager.CurrentUserLogin}");
            await w.WriteLineAsync($"Дата: {DateTime.Now:dd.MM.yyyy HH:mm}");
            await w.WriteLineAsync();
            await w.WriteLineAsync("--- Записи ---");

            foreach (var a in appts)
                await w.WriteLineAsync($"{a.DoctorName} ({a.Specialty}) | {a.VisitDate:dd.MM.yyyy HH:mm} | {a.Status}");

            await w.WriteLineAsync();
            await w.WriteLineAsync("--- Вызовы ---");

            foreach (var v in visits)
                await w.WriteLineAsync($"{v.DoctorName} | {v.RequestDate:dd.MM.yyyy HH:mm} | {v.Status} | {v.Complaints}");
        }

        private static async Task ExportToJsonAsync(string f)
        {
            var db = new DatabaseHelper();
            var appts = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var visits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);

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
                Записи = appts.Select(a => new
                {
                    Врач = a.DoctorName,
                    Специальность = a.Specialty ?? "",
                    Дата = a.VisitDate.ToString("dd.MM.yyyy HH:mm"),
                    Статус = a.Status
                }).ToArray(),
                Вызовы = visits.Select(v => new
                {
                    Врач = v.DoctorName,
                    Дата = v.RequestDate.ToString("dd.MM.yyyy HH:mm"),
                    Статус = v.Status,
                    Жалобы = v.Complaints,
                    Адрес = v.VisitAddress,
                    Телефон = v.PhoneNumber
                }).ToArray()
            };

            string json = JsonSerializer.Serialize(data, JsonOptions);
            await File.WriteAllTextAsync(f, json, new UTF8Encoding(true));
        }

        private void BtnPrintTicket_Click(object sender, RoutedEventArgs e)
        {
            Appointment? appt = null;
            if (sender is Button btn)
            {
                if (btn.DataContext is Appointment a) appt = a;
                else if (dgMyAppointments.SelectedItem is Appointment s) appt = s;
            }

            if (appt == null)
            {
                MessageBox.Show("Выберите запись.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                var doc = new FlowDocument();
                var p = new Paragraph();
                p.Inlines.Add(new Run($"ТАЛОН НА ПРИЁМ\nПациент: {SessionManager.CurrentUserFullName}\nВрач: {appt.DoctorName}\nДата: {appt.VisitDate:dd.MM.yyyy HH:mm}\nСтатус: {appt.Status}"));
                doc.Blocks.Add(p);
                dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, "Талон");
            }
        }

        private void BtnShowAppointmentsArchive_Click(object sender, RoutedEventArgs e)
        {
            new AppointmentsArchiveWindow { Owner = this }.ShowDialog();
        }
    }
}
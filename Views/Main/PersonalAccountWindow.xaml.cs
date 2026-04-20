using ClosedXML.Excel;
using CommunityToolkit.Mvvm.Input;
using MedHelp.Data;
using MedHelp.Models;
using MedHelp.Services;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
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
            Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        };

        public PersonalAccountWindow()
        {
            InitializeComponent();
            Loaded += PersonalAccountWindow_Loaded;
            InputBindings.Add(new KeyBinding(new RelayCommand(async () => await RunExport()), Key.S, ModifierKeys.Control));
        }

        private async void PersonalAccountWindow_Loaded(object sender, RoutedEventArgs e)
        {
            lblFullName.Text = SessionManager.CurrentUserFullName;
            lblLogin.Text = SessionManager.CurrentUserLogin;
            lblRole.Text = SessionManager.CurrentUserRole;
            lblAddress.Text = SessionManager.CurrentUserAddress;
            lblPhone.Text = SessionManager.CurrentUserPhone;
            var db = new DatabaseHelper();
            try { await db.UpdateAppointmentStatusesAsync(); } catch { }
            await LoadMyAppointmentsAsync();
            await LoadMyHomeVisitsAsync();
        }

        private async Task LoadMyAppointmentsAsync()
        {
            try
            {
                var db = new DatabaseHelper();
                var appointments = await db.GetAppointmentsForPatientAsync(SessionManager.CurrentUserId);
                dgMyAppointments.ItemsSource = new ObservableCollection<Appointment>(appointments);
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
                var db = new DatabaseHelper();
                var allVisits = await db.GetHomeVisitsForPatientAsync(SessionManager.CurrentUserId);
                var activeVisits = allVisits.Where(v => v.Status != "Отменён").ToList();
                dgMyHomeVisits.ItemsSource = new ObservableCollection<HomeVisit>(activeVisits);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                dgMyHomeVisits.ItemsSource = null;
            }
        }

        private async void BtnCancelAppointment_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is Appointment appointment)
            {
                var result = MessageBox.Show($"Отменить запись к {appointment.DoctorName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (appointment.Status != "Забронирован" && appointment.Status != "Подтверждён" && appointment.Status != "Ожидает")
                        {
                            MessageBox.Show("Нельзя отменить.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        var db = new DatabaseHelper();
                        bool success = await db.CancelAppointmentByIdAsync(appointment.AppointmentId, SessionManager.CurrentUserId);
                        if (success)
                        {
                            await LoadMyAppointmentsAsync();
                            MessageBox.Show("Запись отменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private async void BtnCancelHomeVisit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.DataContext is HomeVisit homeVisit)
            {
                var result = MessageBox.Show($"Отменить вызов врача {homeVisit.DoctorName}?", "Подтверждение", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        if (homeVisit.Status != "Ожидает" && homeVisit.Status != "Подтверждён")
                        {
                            MessageBox.Show("Нельзя отменить.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        var db = new DatabaseHelper();
                        bool success = await db.CancelHomeVisitByIdAsync(homeVisit.VisitId, SessionManager.CurrentUserId);
                        if (success)
                        {
                            await LoadMyHomeVisitsAsync();
                            MessageBox.Show("Заявка отменена.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                    }
                    catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
                }
            }
        }

        private void BtnShowArchiveVisits_Click(object sender, RoutedEventArgs e)
        {
            new HomeVisitsArchiveWindow { Owner = this }.ShowDialog();
        }

        private async void BtnEditUser_Click(object sender, RoutedEventArgs e)
        {
            var editWindow = new EditUserDetailsWindow(SessionManager.CurrentUserFullName, SessionManager.CurrentUserLogin, SessionManager.CurrentUserAddress, SessionManager.CurrentUserPhone);
            if (editWindow.ShowDialog() == true)
            {
                try
                {
                    var db = new DatabaseHelper();
                    bool success = await db.UpdateUserDataAsync(SessionManager.CurrentUserId, editWindow.EditedFullName, editWindow.EditedLogin, editWindow.EditedAddress, editWindow.EditedPhone);
                    if (success)
                    {
                        SessionManager.CurrentUserFullName = editWindow.EditedFullName;
                        SessionManager.CurrentUserLogin = editWindow.EditedLogin;
                        SessionManager.CurrentUserAddress = editWindow.EditedAddress;
                        SessionManager.CurrentUserPhone = editWindow.EditedPhone;
                        lblFullName.Text = editWindow.EditedFullName;
                        lblLogin.Text = editWindow.EditedLogin;
                        lblAddress.Text = editWindow.EditedAddress;
                        lblPhone.Text = editWindow.EditedPhone;
                        MessageBox.Show("Данные обновлены.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private async void BtnExportToFile_Click(object sender, RoutedEventArgs e)
        {
            await RunExport();
        }

        private async Task RunExport()
        {
            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV (*.csv)|*.csv|Excel (*.xlsx)|*.xlsx|TXT (*.txt)|*.txt|JSON (*.json)|*.json",
                DefaultExt = ".csv",
                FileName = $"Полный_отчет_{SessionManager.CurrentUserFullName}_{DateTime.Now:yyyyMMdd_HHmmss}"
            };
            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;
                string ext = Path.GetExtension(filePath).ToLower();
                try
                {
                    if (ext == ".csv") await ExportToCsvAsync(filePath);
                    else if (ext == ".xlsx") await ExportToExcelAsync(filePath);
                    else if (ext == ".txt") await ExportToTxtAsync(filePath);
                    else if (ext == ".json") await ExportToJsonAsync(filePath);
                    else { MessageBox.Show("Неизвестный формат.", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); return; }
                    MessageBox.Show($"Отчёт сохранён.", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex) { MessageBox.Show(ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error); }
            }
        }

        private static async Task ExportToCsvAsync(string filePath)
        {
            var db = new DatabaseHelper();
            var allAppointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var allVisits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
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
            foreach (var a in allAppointments) writer.WriteLine($"Запись;{a.DoctorName};{a.Specialty ?? ""};{a.VisitDate:dd.MM.yyyy HH:mm};{a.Status}");
            writer.WriteLine();
            writer.WriteLine("ВСЕ ВЫЗОВЫ НА ДОМ");
            writer.WriteLine("Тип;Врач;Дата заявки;Статус;Жалобы;Адрес;Телефон");
            foreach (var v in allVisits) writer.WriteLine($"Вызов;{v.DoctorName};{v.RequestDate:dd.MM.yyyy HH:mm};{v.Status};{v.Complaints};{v.VisitAddress};{v.PhoneNumber}");
        }

        private static async Task ExportToExcelAsync(string filePath)
        {
            var db = new DatabaseHelper();
            var allAppointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var allVisits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);
            using var workbook = new XLWorkbook();
            var wsUser = workbook.Worksheets.Add("Пользователь");
            wsUser.Cell(1, 1).Value = "Параметр"; wsUser.Cell(1, 2).Value = "Значение";
            wsUser.Cell(2, 1).Value = "ФИО"; wsUser.Cell(2, 2).Value = SessionManager.CurrentUserFullName;
            wsUser.Cell(3, 1).Value = "Логин"; wsUser.Cell(3, 2).Value = SessionManager.CurrentUserLogin;
            wsUser.Cell(4, 1).Value = "Роль"; wsUser.Cell(4, 2).Value = SessionManager.CurrentUserRole;
            wsUser.Cell(5, 1).Value = "Адрес"; wsUser.Cell(5, 2).Value = SessionManager.CurrentUserAddress;
            wsUser.Cell(6, 1).Value = "Телефон"; wsUser.Cell(6, 2).Value = SessionManager.CurrentUserPhone;
            wsUser.Columns().AdjustToContents();
            var wsAppts = workbook.Worksheets.Add("Записи на приём");
            wsAppts.Cell(1, 1).Value = "Тип"; wsAppts.Cell(1, 2).Value = "Врач"; wsAppts.Cell(1, 3).Value = "Специальность"; wsAppts.Cell(1, 4).Value = "Дата и время"; wsAppts.Cell(1, 5).Value = "Статус";
            int row = 2;
            foreach (var a in allAppointments)
            {
                wsAppts.Cell(row, 1).Value = "Запись"; wsAppts.Cell(row, 2).Value = a.DoctorName; wsAppts.Cell(row, 3).Value = a.Specialty ?? "";
                wsAppts.Cell(row, 4).Value = a.VisitDate; wsAppts.Cell(row, 4).Style.DateFormat.Format = "dd.mm.yyyy hh:mm";
                wsAppts.Cell(row, 5).Value = a.Status; row++;
            }
            wsAppts.Columns().AdjustToContents();
            var wsVisits = workbook.Worksheets.Add("Вызовы на дом");
            wsVisits.Cell(1, 1).Value = "Тип"; wsVisits.Cell(1, 2).Value = "Врач"; wsVisits.Cell(1, 3).Value = "Дата заявки"; wsVisits.Cell(1, 4).Value = "Статус"; wsVisits.Cell(1, 5).Value = "Жалобы"; wsVisits.Cell(1, 6).Value = "Адрес"; wsVisits.Cell(1, 7).Value = "Телефон";
            row = 2;
            foreach (var v in allVisits)
            {
                wsVisits.Cell(row, 1).Value = "Вызов"; wsVisits.Cell(row, 2).Value = v.DoctorName;
                wsVisits.Cell(row, 3).Value = v.RequestDate; wsVisits.Cell(row, 3).Style.DateFormat.Format = "dd.mm.yyyy hh:mm";
                wsVisits.Cell(row, 4).Value = v.Status; wsVisits.Cell(row, 5).Value = v.Complaints;
                wsVisits.Cell(row, 6).Value = v.VisitAddress; wsVisits.Cell(row, 7).Value = v.PhoneNumber; row++;
            }
            wsVisits.Columns().AdjustToContents();
            workbook.SaveAs(filePath);
        }

        private static async Task ExportToTxtAsync(string filePath)
        {
            var db = new DatabaseHelper();
            var allAppointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var allVisits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);
            using var writer = new StreamWriter(filePath, false, Encoding.UTF8);
            writer.WriteLine("============================================================");
            writer.WriteLine("                  ПОЛНЫЙ ОТЧЁТ ПОЛЬЗОВАТЕЛЯ                 ");
            writer.WriteLine("============================================================");
            writer.WriteLine($"ФИО:      {SessionManager.CurrentUserFullName}");
            writer.WriteLine($"Логин:    {SessionManager.CurrentUserLogin}");
            writer.WriteLine($"Роль:     {SessionManager.CurrentUserRole}");
            writer.WriteLine($"Адрес:    {SessionManager.CurrentUserAddress}");
            writer.WriteLine($"Телефон:  {SessionManager.CurrentUserPhone}");
            writer.WriteLine($"Дата отчёта: {DateTime.Now:dd.MM.yyyy HH:mm:ss}");
            writer.WriteLine("============================================================\n");
            writer.WriteLine("--- ВСЕ ЗАПИСИ НА ПРИЁМ ---");
            if (allAppointments.Count > 0)
            {
                int i = 1;
                foreach (var a in allAppointments)
                {
                    writer.WriteLine($"{i}. Врач: {a.DoctorName} ({a.Specialty ?? "Не указано"})");
                    writer.WriteLine($"   Дата: {a.VisitDate:dd.MM.yyyy HH:mm}");
                    writer.WriteLine($"   Статус: {a.Status}\n"); i++;
                }
            }
            else { writer.WriteLine("Нет записей.\n"); }
            writer.WriteLine("------------------------------------------------------------\n");
            writer.WriteLine("--- ВСЕ ВЫЗОВЫ НА ДОМ ---");
            if (allVisits.Count > 0)
            {
                int i = 1;
                foreach (var v in allVisits)
                {
                    writer.WriteLine($"{i}. Врач: {v.DoctorName}");
                    writer.WriteLine($"   Дата заявки: {v.RequestDate:dd.MM.yyyy HH:mm}");
                    writer.WriteLine($"   Статус: {v.Status}");
                    writer.WriteLine($"   Жалобы: {v.Complaints}\n"); i++;
                }
            }
            else { writer.WriteLine("Нет вызовов."); }
            writer.WriteLine("============================================================");
        }

        private static async Task ExportToJsonAsync(string filePath)
        {
            var db = new DatabaseHelper();
            var allAppointments = await db.GetAllAppointmentsForExportAsync(SessionManager.CurrentUserId);
            var allVisits = await db.GetAllHomeVisitsForExportAsync(SessionManager.CurrentUserId);
            var appointmentsData = allAppointments.Select(a => new { Тип = "Запись на приём", Врач = a.DoctorName, Специальность = a.Specialty ?? "", Дата = a.VisitDate.ToString("dd.MM.yyyy HH:mm"), Статус = a.Status }).ToArray();
            var visitsData = allVisits.Select(v => new { Тип = "Вызов на дом", Врач = v.DoctorName, Дата = v.RequestDate.ToString("dd.MM.yyyy HH:mm"), Статус = v.Status, Жалобы = v.Complaints, Адрес = v.VisitAddress, Телефон = v.PhoneNumber }).ToArray();
            var data = new
            {
                Пользователь = new { ФИО = SessionManager.CurrentUserFullName, Логин = SessionManager.CurrentUserLogin, Роль = SessionManager.CurrentUserRole, Адрес = SessionManager.CurrentUserAddress, Телефон = SessionManager.CurrentUserPhone },
                Все_записи_на_приём = appointmentsData,
                Все_вызовы_на_дом = visitsData
            };
            await File.WriteAllTextAsync(filePath, JsonSerializer.Serialize(data, JsonOptions), Encoding.UTF8);
        }

        private void BtnPrintTicket_Click(object sender, RoutedEventArgs e)
        {
            Appointment? appt = null;
            if (sender is Button btn)
            {
                if (btn.DataContext is Appointment a) appt = a;
                else if (dgMyAppointments.SelectedItem is Appointment s) appt = s;
            }
            if (appt == null) { MessageBox.Show("Выберите запись.", "Внимание", MessageBoxButton.OK, MessageBoxImage.Warning); return; }
            var dlg = new PrintDialog();
            if (dlg.ShowDialog() == true)
            {
                var doc = new FlowDocument();
                var p = new System.Windows.Documents.Paragraph();
                p.Inlines.Add(new Run($"ТАЛОН НА ПРИЁМ\nПациент: {SessionManager.CurrentUserFullName}\nВрач: {appt.DoctorName} ({appt.Specialty})\nДата: {appt.VisitDate:dd.MM.yyyy HH:mm}\nСтатус: {appt.Status}\nПожалуйста, приходите вовремя."));
                doc.Blocks.Add(p);
                dlg.PrintDocument(((IDocumentPaginatorSource)doc).DocumentPaginator, $"Талон_{appt.DoctorName}");
            }
        }

        private void BtnShowAppointmentsArchive_Click(object sender, RoutedEventArgs e)
        {
            new AppointmentsArchiveWindow { Owner = this }.ShowDialog();
        }
    }
}
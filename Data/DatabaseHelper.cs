using MedHelp.Models;
using MedHelp.Services;
using Microsoft.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace MedHelp.Data
{
    public class DatabaseHelper : IDataService<Doctor>, IDataService<User>
    {
        private const string ConnectionString = @"Server=(localdb)\MSSQLLocalDB; Database=MedHelpDB; Integrated Security=true; TrustServerCertificate=true;";

        public async Task<bool> ValidateUserAsync(string login, string password)
        {
            string inputPasswordHash = HashPassword(password);
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT UserId, Login, FullName, Role, Address, PhoneNumber FROM dbo.Users WHERE Login = @login AND PasswordHash = @passwordHash";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@passwordHash", inputPasswordHash);
                await using var reader = await command.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    SessionManager.CurrentUserId = Convert.ToInt32(reader["UserId"]);
                    SessionManager.CurrentUserLogin = reader.GetString("Login");
                    SessionManager.CurrentUserFullName = reader.GetString("FullName");
                    SessionManager.CurrentUserRole = reader.GetString("Role");
                    SessionManager.CurrentUserAddress = reader.IsDBNull("Address") ? string.Empty : reader.GetString("Address");
                    SessionManager.CurrentUserPhone = reader.IsDBNull("PhoneNumber") ? string.Empty : reader.GetString("PhoneNumber");
                    return true;
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка подключения к базе данных.", sqlEx);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("Произошла ошибка при входе.", ex);
            }
            return false;
        }

        public async Task<bool> RegisterUserAsync(string fullName, string login, string phone, string address, string password)
        {
            string passwordHash = HashPassword(password);
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"INSERT INTO dbo.Users (Login, PasswordHash, FullName, Role, Address, PhoneNumber) VALUES (@login, @passwordHash, @fullName, 'User', @address, @phone)";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@login", login);
                command.Parameters.AddWithValue("@passwordHash", passwordHash);
                command.Parameters.AddWithValue("@fullName", fullName);
                command.Parameters.AddWithValue("@address", address);
                command.Parameters.AddWithValue("@phone", phone);
                int rowsAffected = await command.ExecuteNonQueryAsync();
                return rowsAffected > 0;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)
            {
                throw new InvalidOperationException("Пользователь с таким логином уже существует.");
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка при регистрации пользователя.", sqlEx);
            }
        }

        public async Task<List<Doctor>> GetAllAsync() => await GetAllDoctorsAsync();
        public async Task<bool> AddAsync(Doctor item) => await AddDoctorAsync(item);
        public async Task<bool> UpdateAsync(Doctor item) => await UpdateDoctorAsync(item);
        public async Task<bool> DeleteAsync(int id) => await DeleteDoctorAsync(id);

        async Task<List<User>> IDataService<User>.GetAllAsync() => await GetAllUsersAsync();
        async Task<bool> IDataService<User>.AddAsync(User item) => throw new NotImplementedException("Используйте RegisterUserAsync");
        async Task<bool> IDataService<User>.UpdateAsync(User item) => await UpdateUserAsync(item);
        async Task<bool> IDataService<User>.DeleteAsync(int id) => await DeleteUserAsync(id);

        public async Task<List<Doctor>> GetAllDoctorsAsync()
        {
            var doctors = new List<Doctor>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT DoctorId, FullName, Specialty, CabinetNumber, ExperienceYears, Education FROM dbo.Doctors ORDER BY Specialty, FullName";
                await using var command = new SqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    doctors.Add(new Doctor
                    {
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        FullName = reader.GetString("FullName"),
                        Specialty = reader.GetString("Specialty"),
                        CabinetNumber = reader.GetString("CabinetNumber"),
                        ExperienceYears = Convert.ToInt32(reader["ExperienceYears"]),
                        Education = reader.GetString("Education")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки данных о врачах.", sqlEx);
            }
            return doctors;
        }

        public async Task<bool> AddDoctorAsync(Doctor doctor)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"INSERT INTO dbo.Doctors (FullName, Specialty, CabinetNumber, ExperienceYears, Education) VALUES (@fullName, @specialty, @cabinet, @exp, @edu)";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@fullName", doctor.FullName);
                command.Parameters.AddWithValue("@specialty", doctor.Specialty);
                command.Parameters.AddWithValue("@cabinet", doctor.CabinetNumber);
                command.Parameters.AddWithValue("@exp", doctor.ExperienceYears);
                command.Parameters.AddWithValue("@edu", (object?)doctor.Education ?? DBNull.Value);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка при добавлении врача.", sqlEx);
            }
        }

        public async Task<bool> UpdateDoctorAsync(Doctor doctor)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"UPDATE dbo.Doctors SET FullName=@fullName, Specialty=@specialty, CabinetNumber=@cabinet, ExperienceYears=@exp, Education=@edu WHERE DoctorId=@id";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@fullName", doctor.FullName);
                command.Parameters.AddWithValue("@specialty", doctor.Specialty);
                command.Parameters.AddWithValue("@cabinet", doctor.CabinetNumber);
                command.Parameters.AddWithValue("@exp", doctor.ExperienceYears);
                command.Parameters.AddWithValue("@edu", (object?)doctor.Education ?? DBNull.Value);
                command.Parameters.AddWithValue("@id", doctor.DoctorId);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка при обновлении данных врача.", sqlEx);
            }
        }

        public async Task<bool> DeleteDoctorAsync(int doctorId)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                const string deleteVisitsSql = @"DELETE FROM dbo.HomeVisits WHERE DoctorId = @doctorId";
                await using var cmdVisits = new SqlCommand(deleteVisitsSql, connection, transaction);
                cmdVisits.Parameters.AddWithValue("@doctorId", doctorId);
                await cmdVisits.ExecuteNonQueryAsync();

                const string deleteAppointmentsSql = @"DELETE FROM dbo.Appointments WHERE DoctorId = @doctorId";
                await using var cmdAppointments = new SqlCommand(deleteAppointmentsSql, connection, transaction);
                cmdAppointments.Parameters.AddWithValue("@doctorId", doctorId);
                await cmdAppointments.ExecuteNonQueryAsync();

                const string deleteDoctorSql = @"DELETE FROM dbo.Doctors WHERE DoctorId = @doctorId";
                await using var cmdDoctor = new SqlCommand(deleteDoctorSql, connection, transaction);
                cmdDoctor.Parameters.AddWithValue("@doctorId", doctorId);
                int rowsAffected = await cmdDoctor.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    transaction.Commit();
                    return true;
                }
                else
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<User>> GetAllUsersAsync()
        {
            var users = new List<User>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT UserId, Login, FullName, Role, Address, PhoneNumber FROM dbo.Users ORDER BY FullName";
                await using var command = new SqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    users.Add(new User
                    {
                        UserId = Convert.ToInt32(reader["UserId"]),
                        Login = reader.GetString("Login"),
                        FullName = reader.GetString("FullName"),
                        Role = reader.GetString("Role"),
                        Address = reader.IsDBNull("Address") ? string.Empty : reader.GetString("Address"),
                        PhoneNumber = reader.IsDBNull("PhoneNumber") ? string.Empty : reader.GetString("PhoneNumber"),
                        PasswordHash = ""
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки пользователей.", sqlEx);
            }
            return users;
        }

        public async Task<bool> UpdateUserAsync(User user)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"UPDATE dbo.Users SET FullName=@fullName, Login=@login, Address=@address, PhoneNumber=@phone WHERE UserId=@id";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@fullName", user.FullName);
                command.Parameters.AddWithValue("@login", user.Login);
                command.Parameters.AddWithValue("@address", user.Address);
                command.Parameters.AddWithValue("@phone", user.PhoneNumber ?? (object)DBNull.Value);
                command.Parameters.AddWithValue("@id", user.UserId);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка при обновлении пользователя.", sqlEx);
            }
        }

        public async Task<bool> DeleteUserAsync(int userId)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            using var transaction = connection.BeginTransaction();
            try
            {
                const string deleteAppointmentsSql = @"DELETE FROM dbo.Appointments WHERE PatientId = @userId";
                await using var cmdAppointments = new SqlCommand(deleteAppointmentsSql, connection, transaction);
                cmdAppointments.Parameters.AddWithValue("@userId", userId);
                await cmdAppointments.ExecuteNonQueryAsync();

                const string deleteVisitsSql = @"DELETE FROM dbo.HomeVisits WHERE PatientId = @userId";
                await using var cmdVisits = new SqlCommand(deleteVisitsSql, connection, transaction);
                cmdVisits.Parameters.AddWithValue("@userId", userId);
                await cmdVisits.ExecuteNonQueryAsync();

                const string deleteUserSql = @"DELETE FROM dbo.Users WHERE UserId = @userId";
                await using var cmdUser = new SqlCommand(deleteUserSql, connection, transaction);
                cmdUser.Parameters.AddWithValue("@userId", userId);
                int rowsAffected = await cmdUser.ExecuteNonQueryAsync();

                if (rowsAffected > 0)
                {
                    transaction.Commit();
                    return true;
                }
                else
                {
                    transaction.Rollback();
                    return false;
                }
            }
            catch
            {
                transaction.Rollback();
                throw;
            }
        }

        public async Task<List<string>> GetAllSpecialtiesAsync()
        {
            var specialties = new List<string>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT DISTINCT Specialty FROM dbo.Doctors ORDER BY Specialty";
                await using var command = new SqlCommand(sql, connection);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                    specialties.Add(reader.GetString("Specialty"));
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки специальностей.", sqlEx);
            }
            return specialties;
        }

        public async Task<List<Doctor>> GetDoctorsBySpecialtyAsync(string specialty)
        {
            var doctors = new List<Doctor>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT DoctorId, FullName, Specialty, CabinetNumber, ExperienceYears, Education FROM dbo.Doctors WHERE Specialty = @specialty ORDER BY FullName";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@specialty", specialty);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    doctors.Add(new Doctor
                    {
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        FullName = reader.GetString("FullName"),
                        Specialty = reader.GetString("Specialty"),
                        CabinetNumber = reader.GetString("CabinetNumber"),
                        ExperienceYears = Convert.ToInt32(reader["ExperienceYears"]),
                        Education = reader.GetString("Education")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки врачей.", sqlEx);
            }
            return doctors;
        }

        public async Task<List<Appointment>> GetFreeTicketsAsync(int doctorId, DateTime date)
        {
            var freeTickets = new List<Appointment>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT AppointmentId, PatientId, DoctorId, VisitDate, Status, Notes FROM dbo.Appointments WHERE DoctorId = @doctorId AND CAST(VisitDate AS DATE) = @date AND Status = N'Свободен' ORDER BY VisitDate";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@doctorId", doctorId);
                command.Parameters.AddWithValue("@date", date.Date);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    freeTickets.Add(new Appointment
                    {
                        AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                        PatientId = reader["PatientId"] == DBNull.Value ? 0 : Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        VisitDate = (DateTime)reader["VisitDate"],
                        Status = reader.GetString("Status"),
                        Notes = reader["Notes"] == DBNull.Value ? null : reader.GetString("Notes")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки талонов.", sqlEx);
            }
            return freeTickets;
        }

        public async Task<bool> BookAppointmentByIdAsync(int appointmentId, int patientId)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"UPDATE dbo.Appointments SET Status = N'Забронирован', PatientId = @patientId WHERE AppointmentId = @appointmentId AND Status = N'Свободен'; IF @@ROWCOUNT = 0 THROW 50001, 'Талон занят.', 1;";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@appointmentId", appointmentId);
                command.Parameters.AddWithValue("@patientId", patientId);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 50001)
            {
                throw new InvalidOperationException("Талон больше не доступен.");
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка бронирования.", sqlEx);
            }
        }

        public async Task UpdateAppointmentStatusesAsync()
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            const string sql = @"UPDATE dbo.Appointments SET Status = CASE WHEN Status IN (N'Забронирован', N'Подтверждён', N'Ожидает') AND VisitDate <= GETDATE() AND VisitDate > DATEADD(MINUTE, -30, GETDATE()) THEN N'На приёме' WHEN Status IN (N'Забронирован', N'Подтверждён', N'Ожидает', N'На приёме') AND VisitDate <= DATEADD(MINUTE, -30, GETDATE()) THEN N'Успешно' ELSE Status END WHERE Status IN (N'Забронирован', N'Подтверждён', N'Ожидает', N'На приёме')";
            await using var command = new SqlCommand(sql, connection);
            await command.ExecuteNonQueryAsync();
        }

        public async Task<List<Appointment>> GetAppointmentsForPatientAsync(int userId)
        {
            var appointments = new List<Appointment>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.VisitDate, a.Status, a.Notes, d.FullName AS DoctorName, d.Specialty AS DoctorSpecialty FROM dbo.Appointments a INNER JOIN dbo.Doctors d ON a.DoctorId = d.DoctorId WHERE a.PatientId = @userId AND a.Status NOT IN (N'Успешно', N'Отменён') ORDER BY a.VisitDate";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    appointments.Add(new Appointment
                    {
                        AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        VisitDate = (DateTime)reader["VisitDate"],
                        Status = reader.GetString("Status"),
                        Notes = reader["Notes"] == DBNull.Value ? null : reader.GetString("Notes"),
                        DoctorName = reader.GetString("DoctorName"),
                        Specialty = reader.GetString("DoctorSpecialty")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки записей.", sqlEx);
            }
            return appointments;
        }

        public async Task<List<Appointment>> GetArchivedAppointmentsForPatientAsync(int userId)
        {
            var appointments = new List<Appointment>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.VisitDate, a.Status, a.Notes, d.FullName AS DoctorName, d.Specialty AS DoctorSpecialty FROM dbo.Appointments a INNER JOIN dbo.Doctors d ON a.DoctorId = d.DoctorId WHERE a.PatientId = @userId AND (a.Status = N'Успешно' OR a.Status = N'Отменён') ORDER BY a.VisitDate DESC";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    appointments.Add(new Appointment
                    {
                        AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        VisitDate = (DateTime)reader["VisitDate"],
                        Status = reader.GetString("Status"),
                        Notes = reader["Notes"] == DBNull.Value ? null : reader.GetString("Notes"),
                        DoctorName = reader.GetString("DoctorName"),
                        Specialty = reader.GetString("DoctorSpecialty")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки архива.", sqlEx);
            }
            return appointments;
        }

        public async Task<List<Appointment>> GetAllAppointmentsForExportAsync(int userId)
        {
            var appointments = new List<Appointment>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT a.AppointmentId, a.PatientId, a.DoctorId, a.VisitDate, a.Status, a.Notes, d.FullName AS DoctorName, d.Specialty AS DoctorSpecialty FROM dbo.Appointments a INNER JOIN dbo.Doctors d ON a.DoctorId = d.DoctorId WHERE a.PatientId = @userId ORDER BY a.VisitDate DESC";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    appointments.Add(new Appointment
                    {
                        AppointmentId = Convert.ToInt32(reader["AppointmentId"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        VisitDate = (DateTime)reader["VisitDate"],
                        Status = reader.GetString("Status"),
                        Notes = reader["Notes"] == DBNull.Value ? null : reader.GetString("Notes"),
                        DoctorName = reader.GetString("DoctorName"),
                        Specialty = reader.GetString("DoctorSpecialty")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка экспорта.", sqlEx);
            }
            return appointments;
        }

        public async Task<List<HomeVisit>> GetAllHomeVisitsForExportAsync(int userId)
        {
            var homeVisits = new List<HomeVisit>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT h.VisitId, h.PatientId, h.DoctorId, h.VisitAddress, h.PhoneNumber, h.Complaints, h.RequestDate, h.Status, d.FullName AS DoctorName FROM dbo.HomeVisits h INNER JOIN dbo.Doctors d ON h.DoctorId = d.DoctorId WHERE h.PatientId = @userId ORDER BY h.RequestDate DESC";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    homeVisits.Add(new HomeVisit
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        VisitAddress = reader.GetString("VisitAddress"),
                        PhoneNumber = reader.GetString("PhoneNumber"),
                        Complaints = reader.GetString("Complaints"),
                        RequestDate = (DateTime)reader["RequestDate"],
                        Status = reader.GetString("Status"),
                        DoctorName = reader.GetString("DoctorName")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка экспорта вызовов.", sqlEx);
            }
            return homeVisits;
        }

        public async Task<List<HomeVisit>> GetHomeVisitsForPatientAsync(int userId)
        {
            var homeVisits = new List<HomeVisit>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT h.VisitId, h.PatientId, h.DoctorId, h.VisitAddress, h.PhoneNumber, h.Complaints, h.RequestDate, h.Status, d.FullName AS DoctorName FROM dbo.HomeVisits h INNER JOIN dbo.Doctors d ON h.DoctorId = d.DoctorId WHERE h.PatientId = @userId AND h.Status != N'Отменён' ORDER BY h.RequestDate DESC";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    homeVisits.Add(new HomeVisit
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        VisitAddress = reader.GetString("VisitAddress"),
                        PhoneNumber = reader.GetString("PhoneNumber"),
                        Complaints = reader.GetString("Complaints"),
                        RequestDate = (DateTime)reader["RequestDate"],
                        Status = reader.GetString("Status"),
                        DoctorName = reader.GetString("DoctorName")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка загрузки вызовов.", sqlEx);
            }
            return homeVisits;
        }

        public async Task<List<HomeVisit>> GetArchivedHomeVisitsForPatientAsync(int userId)
        {
            var homeVisits = new List<HomeVisit>();
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"SELECT h.VisitId, h.PatientId, h.DoctorId, h.VisitAddress, h.PhoneNumber, h.Complaints, h.RequestDate, h.Status, d.FullName AS DoctorName FROM dbo.HomeVisits h INNER JOIN dbo.Doctors d ON h.DoctorId = d.DoctorId WHERE h.PatientId = @userId AND h.Status = N'Отменён' ORDER BY h.RequestDate DESC";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@userId", userId);
                await using var reader = await command.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    homeVisits.Add(new HomeVisit
                    {
                        VisitId = Convert.ToInt32(reader["VisitId"]),
                        PatientId = Convert.ToInt32(reader["PatientId"]),
                        DoctorId = Convert.ToInt32(reader["DoctorId"]),
                        VisitAddress = reader.GetString("VisitAddress"),
                        PhoneNumber = reader.GetString("PhoneNumber"),
                        Complaints = reader.GetString("Complaints"),
                        RequestDate = (DateTime)reader["RequestDate"],
                        Status = reader.GetString("Status"),
                        DoctorName = reader.GetString("DoctorName")
                    });
                }
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка архива вызовов.", sqlEx);
            }
            return homeVisits;
        }

        public async Task<bool> CancelAppointmentByIdAsync(int appointmentId, int userId)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"UPDATE dbo.Appointments SET Status = N'Отменён' WHERE AppointmentId = @appointmentId AND PatientId = @userId AND Status IN (N'Забронирован', N'Подтверждён', N'Ожидает'); IF @@ROWCOUNT = 0 THROW 50002, 'Нельзя отменить.', 1;";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@appointmentId", appointmentId);
                command.Parameters.AddWithValue("@userId", userId);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 50002)
            {
                throw new InvalidOperationException("Невозможно отменить запись.");
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка отмены.", sqlEx);
            }
        }

        public async Task<bool> CancelHomeVisitByIdAsync(int visitId, int userId)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"UPDATE dbo.HomeVisits SET Status = N'Отменён' WHERE VisitId = @visitId AND PatientId = @userId AND Status IN (N'Ожидает', N'Подтверждён'); IF @@ROWCOUNT = 0 THROW 50003, 'Нельзя отменить.', 1;";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@visitId", visitId);
                command.Parameters.AddWithValue("@userId", userId);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 50003)
            {
                throw new InvalidOperationException("Невозможно отменить заявку.");
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка отмены.", sqlEx);
            }
        }

        public async Task<bool> UpdateUserDataAsync(int userId, string newFullName, string newLogin, string newAddress, string newPhone)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string checkLoginSql = @"SELECT COUNT(*) FROM dbo.Users WHERE Login = @newLogin AND UserId <> @userId";
                await using var checkCmd = new SqlCommand(checkLoginSql, connection);
                checkCmd.Parameters.AddWithValue("@newLogin", newLogin);
                checkCmd.Parameters.AddWithValue("@userId", userId);
                int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                if (count > 0) throw new InvalidOperationException("Такой логин уже занят.");

                const string sql = @"UPDATE dbo.Users SET FullName = @newFullName, Login = @newLogin, Address = @newAddress, PhoneNumber = @newPhone WHERE UserId = @userId";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@newFullName", newFullName);
                command.Parameters.AddWithValue("@newLogin", newLogin);
                command.Parameters.AddWithValue("@newAddress", newAddress);
                command.Parameters.AddWithValue("@newPhone", newPhone);
                command.Parameters.AddWithValue("@userId", userId);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException sqlEx) when (sqlEx.Number == 2627 || sqlEx.Number == 2601)
            {
                throw new InvalidOperationException("Логин занят.");
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Ошибка обновления.", sqlEx);
            }
        }

        public async Task GenerateAndCleanAppointmentsAsync(DateTime startDate, DateTime endDate, TimeSpan startTime = default, TimeSpan endTime = default, int intervalMinutes = 30)
        {
            if (startTime == default) startTime = new TimeSpan(9, 0, 0);
            if (endTime == default) endTime = new TimeSpan(19, 0, 0);

            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();

            var doctors = await GetAllDoctorsAsync();
            if (!doctors.Any()) throw new InvalidOperationException("Нет врачей.");

            const string deleteSql = @"DELETE FROM dbo.Appointments WHERE VisitDate < @nowPlus10 AND Status = N'Свободен'";
            await using var deleteCmd = new SqlCommand(deleteSql, connection);
            deleteCmd.Parameters.AddWithValue("@nowPlus10", DateTime.Now.AddMinutes(10));
            await deleteCmd.ExecuteNonQueryAsync();

            var currentDay = startDate.Date;
            while (currentDay <= endDate.Date)
            {
                if (currentDay.DayOfWeek == DayOfWeek.Saturday || currentDay.DayOfWeek == DayOfWeek.Sunday)
                {
                    currentDay = currentDay.AddDays(1);
                    continue;
                }

                var time = startTime;
                while (time < endTime)
                {
                    var appointmentDateTime = currentDay.Add(time);
                    if (appointmentDateTime >= DateTime.Now.AddMinutes(-10))
                    {
                        foreach (var doctor in doctors)
                        {
                            const string checkSql = @"SELECT COUNT(*) FROM dbo.Appointments WHERE DoctorId = @doctorId AND VisitDate = @visitDate AND Status = N'Свободен'";
                            await using var checkCmd = new SqlCommand(checkSql, connection);
                            checkCmd.Parameters.AddWithValue("@doctorId", doctor.DoctorId);
                            checkCmd.Parameters.AddWithValue("@visitDate", appointmentDateTime);
                            var count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
                            if (count == 0)
                            {
                                const string insertSql = @"INSERT INTO dbo.Appointments (DoctorId, VisitDate, Status, Notes) VALUES (@doctorId, @visitDate, N'Свободен', N'Авто')";
                                await using var insertCmd = new SqlCommand(insertSql, connection);
                                insertCmd.Parameters.AddWithValue("@doctorId", doctor.DoctorId);
                                insertCmd.Parameters.AddWithValue("@visitDate", appointmentDateTime);
                                await insertCmd.ExecuteNonQueryAsync();
                            }
                        }
                    }
                    time = new TimeSpan(time.Hours, time.Minutes + intervalMinutes, time.Seconds);
                }
                currentDay = currentDay.AddDays(1);
            }
        }

        public async Task<bool> CreateHomeVisitAsync(int patientId, int doctorId, string address, string phone, string complaints)
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                const string sql = @"INSERT INTO dbo.HomeVisits (PatientId, DoctorId, VisitAddress, PhoneNumber, Complaints, RequestDate, Status) VALUES (@pId, @dId, @addr, @phone, @compl, GETDATE(), N'Ожидает')";
                await using var command = new SqlCommand(sql, connection);
                command.Parameters.AddWithValue("@pId", patientId);
                command.Parameters.AddWithValue("@dId", doctorId);
                command.Parameters.AddWithValue("@addr", address);
                command.Parameters.AddWithValue("@phone", phone);
                command.Parameters.AddWithValue("@compl", complaints);
                return await command.ExecuteNonQueryAsync() > 0;
            }
            catch (SqlException sqlEx)
            {
                throw new InvalidOperationException("Не удалось создать заявку.", sqlEx);
            }
        }

        public async Task<bool> TestConnectionAsync()
        {
            await using var connection = new SqlConnection(ConnectionString);
            try
            {
                await connection.OpenAsync();
                using var command = new SqlCommand("SELECT 1", connection);
                await command.ExecuteNonQueryAsync();
                return true;
            }
            catch { return false; }
        }

        public async Task<bool> ChangeUserPasswordAsync(int userId, string oldPassword, string newPassword)
        {
            await using var connection = new SqlConnection(ConnectionString);
            await connection.OpenAsync();
            string oldHash = HashPassword(oldPassword);
            const string checkSql = @"SELECT COUNT(*) FROM dbo.Users WHERE UserId = @userId AND PasswordHash = @oldHash";
            await using var checkCmd = new SqlCommand(checkSql, connection);
            checkCmd.Parameters.AddWithValue("@userId", userId);
            checkCmd.Parameters.AddWithValue("@oldHash", oldHash);
            int count = Convert.ToInt32(await checkCmd.ExecuteScalarAsync());
            if (count == 0) return false;

            string newHash = HashPassword(newPassword);
            const string updateSql = @"UPDATE dbo.Users SET PasswordHash = @newHash WHERE UserId = @userId";
            await using var updateCmd = new SqlCommand(updateSql, connection);
            updateCmd.Parameters.AddWithValue("@userId", userId);
            updateCmd.Parameters.AddWithValue("@newHash", newHash);
            return await updateCmd.ExecuteNonQueryAsync() > 0;
        }

        private static string HashPassword(string password)
        {
            using SHA256 sha256Hash = SHA256.Create();
            byte[] bytes = sha256Hash.ComputeHash(Encoding.UTF8.GetBytes(password));
            StringBuilder builder = new StringBuilder();
            foreach (byte b in bytes) builder.Append(b.ToString("x2"));
            return builder.ToString();
        }
    }
}

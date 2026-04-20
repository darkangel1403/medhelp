using System;

namespace MedHelp.Models
{
    public class Appointment
    {
        public int AppointmentId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string? Specialty { get; set; } = string.Empty;
        public DateTime VisitDate { get; set; }
        public string Status { get; set; } = "Свободен";
        public string? Notes { get; set; }
        public string DoctorName { get; set; } = string.Empty;
    }
}

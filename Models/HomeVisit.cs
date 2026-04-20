using System;

namespace MedHelp.Models
{
    public class HomeVisit
    {
        public int VisitId { get; set; }
        public int PatientId { get; set; }
        public int DoctorId { get; set; }
        public string VisitAddress { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Complaints { get; set; } = string.Empty;
        public DateTime RequestDate { get; set; }
        public string Status { get; set; } = "Ожидает";
        public string DoctorName { get; set; } = string.Empty;
    }
}

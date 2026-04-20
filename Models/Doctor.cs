namespace MedHelp.Models
{
    public class Doctor
    {
        public int DoctorId { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Specialty { get; set; } = string.Empty;
        public string CabinetNumber { get; set; } = string.Empty;
        public int ExperienceYears { get; set; }
        public string Education { get; set; } = string.Empty;
    }
}

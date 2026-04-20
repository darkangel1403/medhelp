using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MedHelp.Models
{
    public class Patient
    {
        public int PatientId { get; set; }
        public int? UserId { get; set; } 
        public string CardNumber { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public DateTime BirthDate { get; set; }
        public char Gender { get; private set; } = 'U'; 
        public string Address { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string InsuranceNumber { get; set; } = string.Empty; 
        public byte[] PhotoData { get; set; } = Array.Empty<byte>();

        public void SetGender(char gender)
        {
            if (gender == 'M' || gender == 'F' || gender == 'U')
                Gender = gender;
            else
                throw new ArgumentException("Gender must be 'M', 'F', or 'U'.");
        }
    }
}

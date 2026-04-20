namespace MedHelp.Models
{
    public class User
    {
        public int UserId { get; set; }
        public string Login { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Role { get; set; } = "User";
        public string Address { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; } = string.Empty;
    }
}

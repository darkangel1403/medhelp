namespace MedHelp.Services
{
    public static class SessionManager
    {
        public static int CurrentUserId { get; set; }
        public static string CurrentUserLogin { get; set; } = string.Empty;
        public static string CurrentUserFullName { get; set; } = string.Empty;
        public static string CurrentUserRole { get; set; } = "User";
        public static string CurrentUserAddress { get; set; } = string.Empty;
        public static string CurrentUserPhone { get; set; } = string.Empty;
    }
}

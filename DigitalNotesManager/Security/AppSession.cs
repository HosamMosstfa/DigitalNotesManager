using DigitalNotesManager.Models;

namespace DigitalNotesManager.Security
{
    public static class AppSession
    {
        public static bool IsAuthenticated { get; private set; }
        public static User? CurrentUser { get; private set; }

        public static void Login(User u)
        {
            CurrentUser = u;
            IsAuthenticated = true;
        }

        public static void Logout()
        {
            CurrentUser = null;
            IsAuthenticated = false;
        }
    }
}

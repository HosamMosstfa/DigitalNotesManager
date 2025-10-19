using System.Linq;
using DigitalNotesManager.Data;
using DigitalNotesManager.Models;

namespace DigitalNotesManager.Services
{
    public class UserService
    {
        private readonly NotesDbContext _context;

        public UserService(NotesDbContext context) => _context = context;

        public bool UsernameExists(string username)
        {
            var u = (username ?? "").Trim();
            return _context.Users.Any(x => x.Username == u);
        }

        public User? Authenticate(string username, string password)
        {
            var u = (username ?? "").Trim();

            // لو كنت بتخزن Plain:
             return _context.Users.FirstOrDefault(x => x.Username == u && x.Password == password);

            return null;
        }

        public User? Register(string username, string password)
        {
            var u = (username ?? "").Trim();

            if (UsernameExists(u)) return null;

            var user = new User
            {
                Username = u,
                // لو عندك PasswordHasher:
                // لو مش عندك PasswordHasher ولسه بتخزن Plain (غير مُستحسن):
                 Password = password
            };

            _context.Users.Add(user);
            _context.SaveChanges();
            return user;
        }

        public bool ChangePassword(int userId, string currentPassword, string newPassword, out string error)
        {
            error = string.Empty;
            var user = _context.Users.Find(userId);
            if (user == null)
            {
                error = "User not found.";
                return false;
            }
            // Assuming password is stored as plain text for simplicity; replace with proper hashing in production
            if (user.Password != currentPassword)
            {
                error = "Current password is incorrect.";
                return false;
            }
            user.Password = newPassword;
            _context.SaveChanges();
            return true;
        }
    }
}

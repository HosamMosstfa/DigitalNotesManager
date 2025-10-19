using System.Linq;
using DigitalNotesManager.Models;
using DigitalNotesManager.Security;

namespace DigitalNotesManager.Data
{
    public static class DbInitializer
    {
        public static void EnsureAdmin(NotesDbContext ctx)
        {
            if (!ctx.Users.Any())
            {
                var admin = new User
                {
                    Username = "admin",
                    Password = PasswordHelper.Hash("admin123")
                };
                ctx.Users.Add(admin);
                ctx.SaveChanges();
            }
        }
    }
}

using System;
using System.Windows.Forms;
using DigitalNotesManager.Data;
using DigitalNotesManager.Security;

namespace DigitalNotesManager
{
    internal static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            try
            {
                using var ctx = new NotesDbContext();
                ctx.Database.EnsureCreated(); // Or: ctx.Database.Migrate(); if you use EF migrations
                DbInitializer.EnsureAdmin(ctx); // Seeds admin/admin if Users table is empty
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Database initialization error:\n{ex.Message}",
                    "Startup Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Show login first
            using (var login = new Forms.LoginForm())
            {
                var res = login.ShowDialog();
                if (res == DialogResult.OK && AppSession.IsAuthenticated)
                {
                    Application.Run(new MainForm());
                }
                else
                {
                    // user cancelled or failed login
                    return;
                }
            }
        }
    }
}

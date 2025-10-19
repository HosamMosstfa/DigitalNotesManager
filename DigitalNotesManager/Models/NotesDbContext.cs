using DigitalNotesManager.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Reflection.Emit;

namespace DigitalNotesManager.Data
{
    public class NotesDbContext : DbContext
    {
        public DbSet<User> Users { get; set; }
        public DbSet<Note> Notes { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // Connection to local SQL Server
            optionsBuilder.UseSqlServer(@"Server=HOSSAM\HOSSAM;Database=DigitalNotesDB;Trusted_Connection=True;TrustServerCertificate=True;");
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Table names
            modelBuilder.Entity<User>().ToTable("Users");
            modelBuilder.Entity<Note>().ToTable("Notes");

            // Relationship (1 User -> Many Notes)
            modelBuilder.Entity<User>()
                .HasMany(u => u.Notes)
                .WithOne(n => n.User)
                .HasForeignKey(n => n.UserID)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

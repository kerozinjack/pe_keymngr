using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : IdentityDbContext<ApplicationUser>(options)
    {
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
#pragma warning disable CS0108 // Member hides inherited member; missing new keyword
        public DbSet<Role> CustomRoles { get; set; }
        public DbSet<Key> Keys { get; set; }
        public DbSet<Equipment> Equipments { get; set; }
        public DbSet<Room> Rooms { get; set; }
        public DbSet<MaintenanceWindow> MaintenanceWindows { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<KeyTransaction> KeyTransactions { get; set; }
        public DbSet<Reservation> Reservations { get; set; }
        public DbSet<IssueTicket> IssueTickets { get; set; }
#pragma warning restore CS0108 // Member hides inherited member; missing new keyword
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);
            builder.Entity<Key>()
                .HasMany(k => k.RequiredRoles)
                .WithMany()
                .UsingEntity(j => j.ToTable("KeyRole"));
            builder.Entity<Room>()
                .HasMany(r => r.Equipments)
                .WithMany()
                .UsingEntity(j => j.ToTable("RoomEquipment"));
            builder.Entity<MaintenanceWindow>()
                .HasMany(m => m.AffectedRooms)
                .WithMany()
                .UsingEntity(j => j.ToTable("MaintenanceRoom"));
            builder.Entity<Reservation>()
                .HasMany(r => r.Equipments)
                .WithMany()
                .UsingEntity(j => j.ToTable("ReservationEquipment"));
        }
    }
}

using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    public class Seeder
    {
        public static async Task SeedUsersAsync(IServiceProvider services){
            const string DEFAULT_PASSWORD = "1Q2w3e4r.";
            string _adminPassword = Environment.GetEnvironmentVariable("adminPassword") ?? DEFAULT_PASSWORD;
            string _teacherPassword = Environment.GetEnvironmentVariable("teacherPassword") ?? DEFAULT_PASSWORD;
            string _janitorPassword = Environment.GetEnvironmentVariable("janitorPassword") ?? DEFAULT_PASSWORD;
            string _operationsDirectorPassword = Environment.GetEnvironmentVariable("operationsDirectorPassword") ?? DEFAULT_PASSWORD;

            using IServiceScope? scope = services.CreateScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            UserManager<ApplicationUser> userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

            await context.Database.MigrateAsync();

            if (await context.Users.AnyAsync())
            {
                return;
            }

            Role adminRole = new Role { RoleName = "admin" };
            Role teacherrole = new Role { RoleName = "teacher" };
            Role janitorRole = new Role { RoleName = "janitor" };
            Role opDirectorRole = new Role { RoleName = "Operations Director" };
            context.CustomRoles.AddRange(adminRole, teacherrole, janitorRole, opDirectorRole);

            ApplicationUser admin = new ApplicationUser { Email = "admin@example.com", EmailConfirmed = true, UserName = "admin@example.com" };
            admin.Roles.Add(adminRole);

            ApplicationUser teacher = new ApplicationUser { Email = "teacher@example.com", EmailConfirmed = true, UserName = "teacher@example.com" };
            teacher.Roles.Add(teacherrole);

            ApplicationUser janitor = new ApplicationUser { Email = "janitor@example.com", EmailConfirmed = true, UserName = "janitor@example.com" };
            janitor.Roles.Add(janitorRole);

            ApplicationUser opDir = new ApplicationUser { Email = "director@example.com", EmailConfirmed = true, UserName = "director@example.com" };
            opDir.Roles.Add(opDirectorRole);

            await userManager.CreateAsync(admin, _adminPassword);
            await userManager.CreateAsync(teacher, _teacherPassword);
            await userManager.CreateAsync(janitor, _janitorPassword);
            await userManager.CreateAsync(opDir, _operationsDirectorPassword);
        }

        public static async Task SeedAsync(IServiceProvider services)
        {
            using IServiceScope? scope = services.CreateScope();
            ApplicationDbContext context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();

            if (await context.Users.AnyAsync())
            {
                return;
            }

            await SeedUsersAsync(services);

            Equipment projector = new Equipment { EquipmentName = "Projector" };
            Equipment netswitch = new Equipment { EquipmentName = "Network switch" };
            context.Equipments.AddRange(projector, netswitch);

            Role MasterKey1 = new Role { RoleName = "MasterKey 1" };
            context.CustomRoles.Add(MasterKey1);

            Room room1 = new Room { Building = "I", Floor = "Floor 0", Capacity = 40, RoomName = "Room 1" };
            room1.Equipments.AddRange(projector, netswitch);
            Room room2 = new Room { Building = "I", Floor = "Floor 0", Capacity = 25, RoomName = "Room 2" };
            room2.Equipments.Add(projector);
            Room room3 = new Room { Building = "I", Floor = "Floor 1", Capacity = 10, RoomName = "Room 3" };
            context.Rooms.AddRange(room1, room2, room3);

            Key room1Key = new Key { KeyName = "r1 key" };
            room1Key.AssignedRooms.Add(room1);
            Key room2Key = new Key { KeyName = "r2 key" };
            room2Key.AssignedRooms.Add(room2);
            Key room3Key = new Key { KeyName = "r3 key" };
            room3Key.AssignedRooms.Add(room3);
            Key MasterK1 = new Key { KeyName = "M1" };
            MasterK1.AssignedRooms.AddRange(room1, room2, room3);
            MasterK1.RequiredRoles.Add(MasterKey1);
            context.Keys.AddRange(room1Key, room2Key, room3Key, MasterK1);

            await context.SaveChangesAsync();
        }
    }
}

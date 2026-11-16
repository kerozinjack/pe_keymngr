using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    public sealed record AdminRoleOption(Guid RoleId, string RoleName);

    public sealed record AdminUserRow(
        string UserId,
        string Email,
        bool EmailConfirmed,
        IReadOnlyList<Guid> RoleIds,
        IReadOnlyList<string> RoleNames);

    public sealed record AdminUsersOverview(
        IReadOnlyList<AdminUserRow> Users,
        IReadOnlyList<AdminRoleOption> Roles);

    public sealed record AdminRoleUpdateResult(bool Success, bool Changed, string Message);

    public sealed class AdminUserRoleService(IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        public async Task<AdminUsersOverview> GetOverviewAsync(string? actorId)
        {
            await using ApplicationDbContext db = await dbFactory.CreateDbContextAsync();

            // adminjog ellenorzes 
            if (!await IsAdminAsync(db, actorId))
            {
                throw new UnauthorizedAccessException("A felhasználók kezeléséhez adminisztrátori jogosultság szükséges.");
            }

            List<Role> availableRoles = await db.CustomRoles
                .AsNoTracking()
                .OrderBy(role => role.RoleName)
                .ThenBy(role => role.RoleId)
                .ToListAsync();

            List<ApplicationUser> accounts = await db.Users
                .AsNoTracking()
                .Include(user => user.Roles)
                .OrderBy(user => user.Email)
                .ThenBy(user => user.Id)
                .ToListAsync();

            List<AdminRoleOption> roleOptions = availableRoles
                .Select(role => new AdminRoleOption(role.RoleId, role.RoleName))
                .ToList();

            List<AdminUserRow> users = accounts
                .Select(user => new AdminUserRow(
                    user.Id,
                    user.Email ?? user.UserName ?? user.Id,
                    user.EmailConfirmed,
                    user.Roles.Select(role => role.RoleId).ToList(),
                    user.Roles.Select(role => role.RoleName).OrderBy(name => name).ToList()))
                .ToList();

            return new AdminUsersOverview(users, roleOptions);
        }

        public async Task<AdminRoleUpdateResult> UpdateRolesAsync(
            string? actorId,
            string targetUserId,
            IReadOnlyCollection<Guid> previousRoleIds,
            IReadOnlyCollection<Guid> requestedRoleIds)
        {
            if (string.IsNullOrWhiteSpace(actorId) || string.IsNullOrWhiteSpace(targetUserId))
            {
                return new(false, false, "Hiányzik a bejelentkezett vagy a kiválasztott felhasználó azonosítója.");
            }

            await using ApplicationDbContext db = await dbFactory.CreateDbContextAsync();
            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            ApplicationUser? actor = await db.Users
                .Include(user => user.Roles)
                .FirstOrDefaultAsync(user => user.Id == actorId);

            if (actor is null || !actor.Roles.Any(role => role.RoleName == "admin"))
            {
                return new(false, false, "A módosításhoz adminisztrátori jogosultság szükséges.");
            }

            ApplicationUser? target = await db.Users
                .Include(user => user.Roles)
                .FirstOrDefaultAsync(user => user.Id == targetUserId);

            if (target is null)
            {
                return new(false, false, "A kiválasztott felhasználó már nem található.");
            }

            HashSet<Guid> currentIds = target.Roles.Select(role => role.RoleId).ToHashSet();
            HashSet<Guid> expectedIds = previousRoleIds.ToHashSet();
            HashSet<Guid> desiredIds = requestedRoleIds.ToHashSet();

            if (!currentIds.SetEquals(expectedIds))
            {
                return new(false, false, "A felhasználó jogosultságait időközben módosították. Frissítsd a listát, majd próbáld újra.");
            }

            // letezo szerepkorok 
            List<Role> newRoles = await db.CustomRoles
                .Where(role => desiredIds.Contains(role.RoleId))
                .ToListAsync();

            if (newRoles.Count != desiredIds.Count)
            {
                return new(false, false, "Egy kiválasztott szerepkör időközben megszűnt. Frissítsd a listát.");
            }

            if (currentIds.SetEquals(desiredIds))
            {
                return new(true, false, "Nem történt jogosultságváltozás.");
            }

            bool hadAdminRole = target.Roles.Any(role => role.RoleName == "admin");
            bool willHaveAdminRole = newRoles.Any(role => role.RoleName == "admin");

            if (hadAdminRole && !willHaveAdminRole)
            {
                if (target.Id == actor.Id)
                {
                    return new(false, false, "A saját adminisztrátori jogosultságodat nem vonhatod vissza.");
                }

                bool hasOtherAdmin = await db.Users.AnyAsync(user =>
                    user.Id != target.Id && user.Roles.Any(role => role.RoleName == "admin"));

                if (!hasOtherAdmin)
                {
                    return new(false, false, "A rendszer utolsó adminisztrátorának jogosultsága nem vonható vissza.");
                }
            }

            string oldRoleNames = RoleNames(target.Roles);

            foreach (Role oldRole in target.Roles.Where(role => !desiredIds.Contains(role.RoleId)).ToList())
            {
                target.Roles.Remove(oldRole);
            }

            foreach (Role newRole in newRoles.Where(role => !currentIds.Contains(role.RoleId)))
            {
                target.Roles.Add(newRole);
            }

            // auditlog
            db.AuditLogs.Add(new AuditLog
            {
                LogId = Guid.NewGuid(),
                Date = DateTime.UtcNow,
                Severity = Severity.Info,
                Subsystem = "Felhasználók",
                Text = $"Felhasználó: {SafeName(actor.UserName)} módosította {SafeName(target.UserName)} jogosultságait: " +
                    $"{oldRoleNames} -> {RoleNames(target.Roles)}."
            });

            await db.SaveChangesAsync();
            await transaction.CommitAsync();

            return new(true, true, "A felhasználó jogosultságai sikeresen frissültek.");
        }

        private static async Task<bool> IsAdminAsync(ApplicationDbContext db, string? actorId)
        {
            return !string.IsNullOrWhiteSpace(actorId) &&
                await db.Users.AsNoTracking().AnyAsync(user =>
                    user.Id == actorId && user.Roles.Any(role => role.RoleName == "admin"));
        }

        private static string RoleNames(IEnumerable<Role> roles)
        {
            string[] names = roles.Select(role => SafeName(role.RoleName))
                .OrderBy(name => name)
                .ToArray();
            return names.Length == 0 ? "nincs szerepkör" : string.Join(", ", names);
        }

        private static string SafeName(string? name)
        {
            string value = (name ?? "ismeretlen").Replace('\r', ' ').Replace('\n', ' ').Trim();
            return value.Length > 100 ? value[..100] + "…" : value;
        }
    }
}

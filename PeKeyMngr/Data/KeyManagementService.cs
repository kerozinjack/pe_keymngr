using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    // demonstrációs foglalás dto-k
    public sealed record DemoKeyReservation(
        Guid RoomId,
        string RoomName,
        Guid KeyId,
        string KeyName,
        string ParticipantId,
        string ParticipantName);

    public sealed record ActiveKeyIssue(
        Guid KeyId,
        string KeyName,
        string ParticipantName,
        DateTime IssuedAtUtc,
        Guid TransactionId);

    public sealed record KeyOperationResult(
        bool Success,
        string Message);

    public sealed class KeyManagementService(
        IDbContextFactory<ApplicationDbContext> dbFactory)
    {
        // TODO: A demonstrációs foglalásokat később
        // a valódi foglalási rendszer adataira kell cserélni.
        public async Task<List<DemoKeyReservation>> GetDemoReservationsAsync()
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();

            // oktatok+kulcs+termek lekerese
            var teachers = await db.Users
                .AsNoTracking()
                .Where(u => u.Roles.Any(r => r.RoleName == "teacher"))
                .OrderBy(u => u.UserName)
                .Select(u => new
                {
                    u.Id,
                    u.UserName
                })
                .ToListAsync();

            List<Key> keys = await db.Keys
                .AsNoTracking()
                .Include(k => k.AssignedRooms)
                .Include(k => k.RequiredRoles)
                .ToListAsync();

            // demonstrációs foglalások
            return (from teacher in teachers
                    from key in keys.Where(k => k.RequiredRoles.Count == 0)
                    from room in key.AssignedRooms
                    orderby room.Building, room.Floor, room.RoomName, key.KeyName
                    select new DemoKeyReservation(
                        room.RoomId,
                        $"{room.Building} / {room.Floor} / {room.RoomName}",
                        key.KeyId,
                        key.KeyName,
                        teacher.Id,
                        teacher.UserName ?? teacher.Id))
                .ToList();
        }

        // kulcsok
        public async Task<List<ActiveKeyIssue>> GetActiveIssuesAsync()
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();

            List<KeyTransaction> events = await db.KeyTransactions
                .AsNoTracking()
                .Include(t => t.Key)
                .Include(t => t.Participant)
                .OrderByDescending(t => t.Timestamp)
                .ThenByDescending(t => t.KeyTransactionId)
                .ToListAsync();

            return events
                .GroupBy(t => t.Key.KeyId)
                .Select(g => g.First())
                .Where(t => t.Direction == TransactionDirection.Out)
                .Select(t => new ActiveKeyIssue(
                    t.Key.KeyId,
                    t.Key.KeyName,
                    t.Participant.UserName ?? t.Participant.Id,
                    t.Timestamp,
                    t.KeyTransactionId))
                .OrderBy(t => t.IssuedAtUtc)
                .ToList();
        }

        // kulcskiadas
        public async Task<KeyOperationResult> IssueAsync(
            DemoKeyReservation selection,
            string? operatorId)
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();

            await using IDbContextTransaction tx =
                await db.Database.BeginTransactionAsync();

            ApplicationUser? operatorUser =
                await LoadJanitorAsync(db, operatorId);

            if (operatorUser is null)
            {
                return new(
                    false,
                    "A kulcskiadáshoz portási jogosultság szükséges.");
            }

            Key? key = await db.Keys
                .Include(k => k.AssignedRooms)
                .Include(k => k.RequiredRoles)
                .FirstOrDefaultAsync(k => k.KeyId == selection.KeyId);

            ApplicationUser? participant = await db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u => u.Id == selection.ParticipantId);

            if (key is null || participant is null ||
                !participant.Roles.Any(r => r.RoleName == "teacher") ||
                key.RequiredRoles.Count != 0 ||
                !key.AssignedRooms.Any(r => r.RoomId == selection.RoomId))
            {
                return new(
                    false,
                    "A tesztfoglaláshoz nem található megfelelő kulcs, terem vagy oktató.");
            }

            // TODO: A valódi foglalási rendszer elkészülése után
            // itt kell ellenőrizni a foglalás azonosítóját,
            // tulajdonosát, időpontját, termét és állapotát.

            KeyTransaction? latest = await db.KeyTransactions
                .Where(t => t.Key.KeyId == key.KeyId)
                .OrderByDescending(t => t.Timestamp)
                .ThenByDescending(t => t.KeyTransactionId)
                .FirstOrDefaultAsync();

            // Ha a kulcs már ki van adva, nem adhatjuk ki újra.
            if (latest?.Direction == TransactionDirection.Out)
            {
                return new(false, "Ez a kulcs már ki van adva.");
            }

            // kulcskiadási tranzakció.
            db.KeyTransactions.Add(new KeyTransaction
            {
                KeyTransactionId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Direction = TransactionDirection.Out,
                Key = key,
                AuthorizedBy = operatorUser,
                Participant = participant
            });

            await db.SaveChangesAsync();

            await tx.CommitAsync();

            return new(true, "A kulcskiadás sikeresen rögzítve.");
        }

        // kulcs visszavétele.
        public async Task<KeyOperationResult> ReturnAsync(
            Guid keyId,
            string? operatorId)
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();

            await using IDbContextTransaction tx =
                await db.Database.BeginTransactionAsync();

            ApplicationUser? operatorUser =
                await LoadJanitorAsync(db, operatorId);

            if (operatorUser is null)
            {
                return new(
                    false,
                    "A visszavételhez portási jogosultság szükséges.");
            }

            KeyTransaction? latest = await db.KeyTransactions
                .Include(t => t.Key)
                .Include(t => t.Participant)
                .Where(t => t.Key.KeyId == keyId)
                .OrderByDescending(t => t.Timestamp)
                .ThenByDescending(t => t.KeyTransactionId)
                .FirstOrDefaultAsync();

            if (latest is null ||
                latest.Direction != TransactionDirection.Out)
            {
                return new(false, "Ez a kulcs jelenleg nincs kiadva.");
            }

            // visszavételi tranzakciót.
            db.KeyTransactions.Add(new KeyTransaction
            {
                KeyTransactionId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Direction = TransactionDirection.In,
                Key = latest.Key,
                AuthorizedBy = operatorUser,
                Participant = latest.Participant
            });

            await db.SaveChangesAsync();

            await tx.CommitAsync();

            return new(
                true,
                "A kulcs visszavétele sikeresen rögzítve.");
        }

        private static Task<ApplicationUser?> LoadJanitorAsync(
            ApplicationDbContext db,
            string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.FromResult<ApplicationUser?>(null);
            }

            return db.Users
                .Include(u => u.Roles)
                .FirstOrDefaultAsync(u =>
                    u.Id == id &&
                    u.Roles.Any(r => r.RoleName == "janitor"));
        }
    }
}

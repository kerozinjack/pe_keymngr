using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    internal static class AuditChangeCollector
    {
        public static List<AuditLog> Collect(ApplicationDbContext db, string actor)
        {
            // Az EF Core figyeli, hogy melyik objektum lett Added, Modified vagy Deleted.
            // A DetectChanges() frissíti ezt a listát a változások alapján.
            db.ChangeTracker.DetectChanges();

            List<AuditLog> logs = [];

            // ToList(): pillanatképet készítünk a követett objektumokról.
            // Az elkészült AuditLogokat csak később adja hozzá a DbContext.
            foreach (EntityEntry entry in db.ChangeTracker.Entries().ToList())
            {
                if (entry.Entity is AuditLog)
                {
                    if (entry.State is EntityState.Modified or EntityState.Deleted)
                    {
                        throw new InvalidOperationException(
                            "Az auditnapló-bejegyzés nem módosítható vagy törölhető.");
                    }
                    continue;
                }

                // lenyeges valtoztatasok
                if (entry.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted))
                {
                    continue;
                }

                if (entry.State == EntityState.Modified &&
                    !entry.Properties.Any(property => property.IsModified))
                {
                    continue;
                }

                if (entry.Entity is not (Room or Key or Equipment or IssueTicket or
                    Reservation or MaintenanceWindow or Role or ApplicationUser or KeyTransaction))
                {
                    continue;
                }

                // alapadatok
                if (entry.Entity is ApplicationUser && entry.State == EntityState.Modified)
                {
                    bool visibleUserChange = entry.Properties.Any(property =>
                        (property.Metadata.Name is "UserName" or "Email" or "PhoneNumber")
                        && !Equals(property.OriginalValue, property.CurrentValue));

                    if (!visibleUserChange)
                    {
                        continue;
                    }
                }

                logs.Add(new AuditLog
                {
                    LogId = Guid.NewGuid(),
                    Date = DateTime.UtcNow,
                    Severity = entry.State == EntityState.Deleted
                        ? Severity.Warn
                        : Severity.Info,
                    Subsystem = GetSubsystem(entry.Entity),
                    Text = GetMessage(entry, actor)
                });
            }

            // nem adb mentes, csak visszaadja a logot
            return logs;
        }

        public static Task<List<AuditLog>> CollectAsync(
            ApplicationDbContext db,
            string actor,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Collect(db, actor));
        }

        private static string GetMessage(EntityEntry entry, string actor)
        {
            // kulcskiadas/visszavetel
            if (entry.Entity is KeyTransaction transaction && entry.State == EntityState.Added)
            {
                string operatorName = Clean(transaction.AuthorizedBy?.UserName ?? actor);
                string keyName = Clean(transaction.Key?.KeyName ?? "ismeretlen kulcs");
                string participantName = Clean(transaction.Participant?.UserName ?? "ismeretlen átvevő");

                return transaction.Direction == TransactionDirection.Out
                    ? $"Felhasználó: {operatorName} kiadta a(z) {keyName} kulcsot {participantName} részére."
                    : $"Felhasználó: {operatorName} visszavette a(z) {keyName} kulcsot {participantName} felhasználótól.";
            }

            string verb = entry.State switch
            {
                EntityState.Added => "létrehozta",
                EntityState.Modified => "módosította",
                EntityState.Deleted => "törölte",
                _ => "kezelte"
            };

            //mit modositott
            string target = entry.Entity switch
            {
                Room room => $"a(z) {Clean(room.Building)} / {Clean(room.Floor)} / {Clean(room.RoomName)} termet",
                Key key => $"a(z) {Clean(key.KeyName)} kulcsot",
                Equipment equipment => $"a(z) {Clean(equipment.EquipmentName)} felszerelést",
                IssueTicket ticket => $"a hibajegyet (terem: {Clean(ticket.AffectedRoom?.RoomName ?? "ismeretlen")})",
                Reservation reservation => $"a foglalást (terem: {Clean(reservation.Room?.RoomName ?? "ismeretlen")})",
                MaintenanceWindow => "a karbantartási időszakot",
                Role role => $"a(z) {Clean(role.RoleName)} szerepkört",
                ApplicationUser user => $"a(z) {Clean(user.UserName ?? "ismeretlen")} felhasználót",
                _ => "egy rekordot"
            };

            return $"Felhasználó: {Clean(actor)} {verb} {target}.";
        }

        private static string GetSubsystem(object entity)
        {
            //hol csinalt
            return entity switch
            {
                KeyTransaction => "Kulcskezelés",
                IssueTicket => "Hibajegyek",
                Reservation => "Foglalások",
                Room => "Termek",
                Key => "Kulcsok",
                Equipment => "Felszerelések",
                MaintenanceWindow => "Karbantartások",
                Role => "Szerepkörök",
                ApplicationUser => "Felhasználók",
                _ => "Egyéb"
            };
        }

        private static string Clean(string value)
        {
            string text = value.Replace('\r', ' ').Replace('\n', ' ').Trim();
            return text.Length > 100 ? text[..100] + "…" : text;
        }
    }
}

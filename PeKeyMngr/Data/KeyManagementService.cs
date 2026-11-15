using System.Data;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    // Egy adott foglaláshoz és átvevőhöz használható kulcs.
    public sealed record JanitorKeyOption(Guid KeyId, string KeyName);

    // Valódi foglalás a portásoldal számára. Nincs szükség külön adatbázistáblára.
    public sealed record JanitorReservation(
        Guid ReservationId,
        Guid RoomId,
        string RoomName,
        string Building,
        string Floor,
        string ParticipantId,
        string ParticipantName,
        DateTime StartTime,
        DateTime EndTime,
        List<JanitorKeyOption> Keys)
    {
        // A teremhez fizikailag hozzárendelt ÖSSZES kulcs azonosítója.
        // A teremstátuszhoz kell, függetlenül az adott oktató jogosultságaitól.
        // Ez csak a felületnek szánt adat, NEM új adatbázismező.
        public List<Guid> RoomKeyIds { get; init; } = [];
    }

    // A tényleges KeyTransaction előzményekből meghatározott, jelenleg kiadott kulcs.
    public sealed record ActiveKeyIssue(
        Guid KeyId,
        string KeyName,
        string ParticipantName,
        DateTime IssuedAtUtc,
        Guid TransactionId)
    {
        // Az átvevő azonosítója és a fizikai kulcs által nyitható termek.
        // A frontend így észreveszi, ha az oktatónál már van megfelelő kulcs.
        // Ezek NEM a KeyTransaction modell új mezői.
        public string ParticipantId { get; init; } = string.Empty;
        public List<Guid> AssignedRoomIds { get; init; } = [];
    }

    // Egyszerű eredménytípus: a felületnek nem kell kivételekkel üzenetet továbbítania.
    public sealed record KeyOperationResult(bool Success, string Message);

    public sealed class KeyManagementService(
        IDbContextFactory<ApplicationDbContext> dbFactory) // tesztóra használathoz írd be: JanitorTestClock testClock
    {
        // A kulcskiadás sem a kezdéshez, sem a befejezéshez nincs időben kötve.
        // Csak az adott naphoz tartozó valódi foglalások; nem generálunk kombinációkat.
        // A kulcsok a Key.AssignedRooms kapcsolaton keresztül tartoznak a teremhez.
        public async Task<List<JanitorReservation>> GetTodayReservationsAsync()
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();

            // A valós napot használjuk, kivéve ha Development alatt a portás
            // bekapcsolta a saját, munkamenethez kötött tesztóráját.
            //DateTime today = testClock.Now.Date; // Engedélyezd ha tesztelni akarod a portás dashboardon a holnapi foglalások listázása funkciót
            DateTime today = DateTime.Today;

            DateTime tomorrow = today.AddDays(1);

            List<Reservation> reservations = await db.Reservations
                .AsNoTracking()
                .Include(reservation => reservation.Room)
                .Include(reservation => reservation.RequestedBy)
                    .ThenInclude(user => user.Roles)
                .Where(reservation =>
                    reservation.StartTime < tomorrow && reservation.EndTime > today)
                .OrderBy(reservation => reservation.StartTime)
                .ThenBy(reservation => reservation.Room.Building)
                .ThenBy(reservation => reservation.Room.RoomName)
                .AsSplitQuery()
                .ToListAsync();

            List<Key> keys = await db.Keys
                .AsNoTracking()
                .Include(key => key.AssignedRooms)
                .Include(key => key.RequiredRoles)
                .AsSplitQuery()
                .ToListAsync();

            List<JanitorReservation> result = [];

            foreach (Reservation reservation in reservations)
            {
                // Az adott teremhez rendelt kulcsok közül csak azokat mutatjuk,
                // amelyek valamennyi szükséges szerepkörével rendelkezik az átvevő.
                List<JanitorKeyOption> matchingKeys = keys
                    .Where(key => key.AssignedRooms.Any(room =>
                        room.RoomId == reservation.Room.RoomId))
                    .Where(key => key.RequiredRoles.All(required =>
                        reservation.RequestedBy.Roles.Any(owned =>
                            owned.RoleId == required.RoleId)))
                    .OrderBy(key => key.KeyName)
                    .Select(key => new JanitorKeyOption(key.KeyId, key.KeyName))
                    .ToList();

                // A fizikai készletet és az oktató számára engedélyezett
                // kulcsokat külön tartjuk nyilván. A Master kulcs itt is
                // csak EGY darab kulcs, több AssignedRooms kapcsolattal.
                List<Guid> roomKeyIds = keys
                    .Where(key => key.AssignedRooms.Any(room =>
                        room.RoomId == reservation.Room.RoomId))
                    .Select(key => key.KeyId)
                    .Distinct()
                    .ToList();

                result.Add(new JanitorReservation(
                    reservation.ReservationId,
                    reservation.Room.RoomId,
                    reservation.Room.RoomName,
                    reservation.Room.Building,
                    reservation.Room.Floor,
                    reservation.RequestedBy.Id,
                    reservation.RequestedBy.UserName ?? reservation.RequestedBy.Id,
                    reservation.StartTime,
                    reservation.EndTime,
                    matchingKeys)
                {
                    RoomKeyIds = roomKeyIds
                });
            }

            return result;
        }

        // A kulcstranzakciók utolsó eseményéből derül ki, mely kulcsok vannak kint.
        public async Task<List<ActiveKeyIssue>> GetActiveIssuesAsync()
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();

            List<KeyTransaction> events = await db.KeyTransactions
                .AsNoTracking()
                .Include(transaction => transaction.Key)
                    .ThenInclude(key => key.AssignedRooms)
                .Include(transaction => transaction.Participant)
                .AsSplitQuery()
                .OrderByDescending(transaction => transaction.Timestamp)
                .ThenByDescending(transaction => transaction.KeyTransactionId)
                .ToListAsync();

            return events
                .GroupBy(transaction => transaction.Key.KeyId)
                .Select(group => group.First())
                .Where(transaction => transaction.Direction == TransactionDirection.Out)
                .Select(transaction => new ActiveKeyIssue(
                    transaction.Key.KeyId,
                    transaction.Key.KeyName,
                    transaction.Participant.UserName ?? transaction.Participant.Id,
                    transaction.Timestamp,
                    transaction.KeyTransactionId)
                {
                    ParticipantId = transaction.Participant.Id,
                    AssignedRoomIds = transaction.Key.AssignedRooms
                        .Select(room => room.RoomId)
                        .ToList()
                })
                .OrderBy(transaction => transaction.IssuedAtUtc)
                .ToList();
        }

        // A frontend csak azonosítókat ad át, a jogosultságot és a foglalást
        // az adatbázisból ÚJRA lekérjük, így a megváltoztatott kliensadat sem elég.
        public async Task<KeyOperationResult> IssueAsync(
            Guid reservationId,
            Guid keyId,
            string? operatorId)
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();

            // SQLite alatt a Serializable tranzakció védi a versengő írásokat.
            await using IDbContextTransaction tx =
                await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            ApplicationUser? operatorUser = await LoadJanitorAsync(db, operatorId);
            if (operatorUser is null)
            {
                return new(false, "A kulcskiadáshoz portási jogosultság szükséges.");
            }

            Reservation? reservation = await db.Reservations
                .Include(item => item.Room)
                .Include(item => item.RequestedBy)
                    .ThenInclude(user => user.Roles)
                .FirstOrDefaultAsync(item => item.ReservationId == reservationId);

            if (reservation is null)
            {
                return new(false, "A foglalás nem található. Frissítsd a listát.");
            }

            if (!reservation.RequestedBy.Roles.Any(role => role.RoleName == "teacher"))
            {
                return new(false, "A foglalás átvevője nem rendelkezik oktatói jogosultsággal.");
            }

            // Nincs időablakos tiltás: a létező foglalás kulcsa bármikor kiadható.
            // A portás és az oktató jogosultságát, a terem-kulcs kapcsolatot,
            // valamint a kulcs aktuális kiadottságát továbbra is ellenőrizzük.

            Key? key = await db.Keys
                .Include(item => item.AssignedRooms)
                .Include(item => item.RequiredRoles)
                .FirstOrDefaultAsync(item => item.KeyId == keyId);

            if (key is null ||
                !key.AssignedRooms.Any(room => room.RoomId == reservation.Room.RoomId))
            {
                return new(false, "A kulcs nem tartozik a foglalás terméhez.");
            }

            if (!key.RequiredRoles.All(required =>
                reservation.RequestedBy.Roles.Any(owned => owned.RoleId == required.RoleId)))
            {
                return new(false, "Az oktató nem jogosult ennek a kulcsnak az átvételére.");
            }

            // EGY OKTATÓ / EGY TEREM: ha az oktatónál már van BÁRMELY
            // olyan kiadott fizikai kulcs (akár Master), amely nyitja a
            // kiválasztott termet, nem kaphat másodikat hozzá.
            // Csak a kulcsok utolsó tranzakciója számít; egy régi Out,
            // amelyet később In követett, már nem jelent aktív kiadást.
            // Ez a vizsgálat ugyanebben az adatbázis-tranzakcióban fut.
            List<KeyTransaction> roomKeyEvents = await db.KeyTransactions
                .AsNoTracking()
                .Include(transaction => transaction.Key)
                .Include(transaction => transaction.Participant)
                .Where(transaction => transaction.Key.AssignedRooms.Any(room =>
                    room.RoomId == reservation.Room.RoomId))
                .OrderByDescending(transaction => transaction.Timestamp)
                .ThenByDescending(transaction => transaction.KeyTransactionId)
                .ToListAsync();

            bool participantAlreadyHasRoomKey = roomKeyEvents
                .GroupBy(transaction => transaction.Key.KeyId)
                .Select(group => group.First())
                .Any(transaction =>
                    transaction.Direction == TransactionDirection.Out &&
                    transaction.Participant.Id == reservation.RequestedBy.Id);

            if (participantAlreadyHasRoomKey)
            {
                return new(false,
                    "Az oktatónál már van olyan kiadott kulcs, amely nyitja ezt a termet. " +
                    "Második kulcs csak az előző visszavétele után adható ki.");
            }

            KeyTransaction? latest = await db.KeyTransactions
                .Where(transaction => transaction.Key.KeyId == keyId)
                .OrderByDescending(transaction => transaction.Timestamp)
                .ThenByDescending(transaction => transaction.KeyTransactionId)
                .FirstOrDefaultAsync();

            if (latest?.Direction == TransactionDirection.Out)
            {
                return new(false, "Ez a kulcs már ki van adva.");
            }

            db.KeyTransactions.Add(new KeyTransaction
            {
                KeyTransactionId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow, // A kulcstranzakciók továbbra is UTC-időt használnak.
                Direction = TransactionDirection.Out,
                Key = key,
                AuthorizedBy = operatorUser,
                Participant = reservation.RequestedBy
            });

            try
            {
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateException)
            {
                return new(false, "A kulcskiadás nem menthető. Frissítsd az adatokat és próbáld újra.");
            }
            catch (SqliteException)
            {
                return new(false, "A kulcs állapota időközben megváltozhatott. Frissítsd az adatokat.");
            }

            return new(true, "A kulcskiadás sikeresen rögzítve.");
        }

        // A visszavételhez nem szükséges aktív foglalás: egy lejárt vagy törölt
        // foglalás kulcsát is vissza kell tudni venni.
        public async Task<KeyOperationResult> ReturnAsync(Guid keyId, string? operatorId)
        {
            await using ApplicationDbContext db =
                await dbFactory.CreateDbContextAsync();
            await using IDbContextTransaction tx =
                await db.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            ApplicationUser? operatorUser = await LoadJanitorAsync(db, operatorId);
            if (operatorUser is null)
            {
                return new(false, "A visszavételhez portási jogosultság szükséges.");
            }

            KeyTransaction? latest = await db.KeyTransactions
                .Include(transaction => transaction.Key)
                .Include(transaction => transaction.Participant)
                .Where(transaction => transaction.Key.KeyId == keyId)
                .OrderByDescending(transaction => transaction.Timestamp)
                .ThenByDescending(transaction => transaction.KeyTransactionId)
                .FirstOrDefaultAsync();

            if (latest is null || latest.Direction != TransactionDirection.Out)
            {
                return new(false, "Ez a kulcs jelenleg nincs kiadva.");
            }

            db.KeyTransactions.Add(new KeyTransaction
            {
                KeyTransactionId = Guid.NewGuid(),
                Timestamp = DateTime.UtcNow,
                Direction = TransactionDirection.In,
                Key = latest.Key,
                AuthorizedBy = operatorUser,
                Participant = latest.Participant
            });

            try
            {
                await db.SaveChangesAsync();
                await tx.CommitAsync();
            }
            catch (DbUpdateException)
            {
                return new(false, "A kulcs visszavételét nem sikerült menteni.");
            }
            catch (SqliteException)
            {
                return new(false, "Az adatbázis foglalt. Frissítsd az adatokat és próbáld újra.");
            }

            return new(true, "A kulcs visszavétele sikeresen rögzítve.");
        }

        // A jogosultság ellenőrzését nem csak a Razor oldal végzi.
        private static Task<ApplicationUser?> LoadJanitorAsync(
            ApplicationDbContext db,
            string? id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                return Task.FromResult<ApplicationUser?>(null);
            }

            return db.Users.FirstOrDefaultAsync(user =>
                user.Id == id && user.Roles.Any(role => role.RoleName == "janitor"));
        }
    }
}

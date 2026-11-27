using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;
using PeKeyMngr.Models;

namespace PeKeyMngr.Components.Pages.MaintenanceWindowPages
{
    public sealed record ReservationConflict(
        Guid ReservationId,
        string RoomName,
        DateTime StartTime,
        DateTime EndTime,
        string Requester);

    public static class ReservationConflictQuery
    {
        public static async Task<List<ReservationConflict>> FindAsync(
            ApplicationDbContext context, IReadOnlyCollection<Guid> roomIds, DateTime start, DateTime end)
        {
            List<Reservation> reservations = await context.Reservations
                .AsNoTracking()
                .Include(reservation => reservation.Room)
                .Include(reservation => reservation.RequestedBy)
                .Where(reservation =>
                    !reservation.IsNoShow &&
                    roomIds.Contains(reservation.Room.RoomId) &&
                    reservation.StartTime < end &&
                    reservation.EndTime > start)
                .ToListAsync();

            return reservations
                .Select(reservation => new ReservationConflict(
                    reservation.ReservationId,
                    reservation.Room.RoomName,
                    reservation.StartTime,
                    reservation.EndTime,
                    reservation.RequestedBy.UserName ?? reservation.RequestedBy.Email ?? reservation.RequestedBy.Id))
                .OrderBy(reservation => reservation.ReservationId)
                .ToList();
        }

        public static async Task<bool> DeleteAsync(
            ApplicationDbContext context, IReadOnlyCollection<ReservationConflict> conflicts)
        {
            if (conflicts.Count == 0)
            {
                return true;
            }

            List<Guid> ids = conflicts.Select(conflict => conflict.ReservationId).ToList();
            List<Reservation> reservations = await context.Reservations
                .Include(reservation => reservation.Room)
                .Where(reservation => ids.Contains(reservation.ReservationId))
                .ToListAsync();

            if (reservations.Count != ids.Count)
            {
                throw new DbUpdateConcurrencyException("Az ütköző foglalások időközben megváltoztak.");
            }

            context.Reservations.RemoveRange(reservations);
            try
            {
                await context.SaveChangesAsync();
                return true;
            }
            catch (DbUpdateException exception) when (
                exception.InnerException is SqliteException sqliteException &&
                (sqliteException.SqliteExtendedErrorCode == 787 ||
                 (sqliteException.SqliteExtendedErrorCode == 1811 &&
                  sqliteException.Message.Contains("FOREIGN KEY constraint failed", StringComparison.Ordinal))))
            {
                return false;
            }
        }
    }
}

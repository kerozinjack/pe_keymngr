using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeKeyMngr.Models;

namespace PeKeyMngr.Data
{
    public sealed class NoShowMonitorService(
        IServiceScopeFactory scopeFactory,
        IConfiguration configuration,
        ILogger<NoShowMonitorService> logger) : BackgroundService
    {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            int graceMinutes = configuration.GetValue<int?>("NoShowGracePeriodMinutes") ?? 15;
            if (graceMinutes < 0)
            {
                throw new InvalidOperationException(
                    "NoShowGracePeriodMinutes must not be negative.");
            }

            using PeriodicTimer timer = new(TimeSpan.FromMinutes(1));

            do
            {
                try
                {
                    await CheckReservationsAsync(graceMinutes, stoppingToken);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed to check reservations for no-shows.");
                }
            }
            while (await timer.WaitForNextTickAsync(stoppingToken));
        }

        private async Task CheckReservationsAsync(int graceMinutes, CancellationToken cancellationToken)
        {
            using IServiceScope scope = scopeFactory.CreateScope();
            ApplicationDbContext db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            await using IDbContextTransaction transaction = await db.Database.BeginTransactionAsync(
                IsolationLevel.Serializable, cancellationToken);

            DateTime now = DateTime.Now;
            DateTime today = now.Date;
            DateTime tomorrow = today.AddDays(1);
            DateTime cutoff = now.AddMinutes(-graceMinutes);

            List<Reservation> reservations = await db.Reservations
                .Include(reservation => reservation.Room)
                .Include(reservation => reservation.RequestedBy)
                .Where(reservation =>
                    !reservation.IsNoShow &&
                    reservation.StartTime < tomorrow &&
                    reservation.EndTime > today &&
                    reservation.EndTime > now &&
                    reservation.StartTime < cutoff)
                .ToListAsync(cancellationToken);

            foreach (Reservation reservation in reservations)
            {
                DateTime deadline = reservation.StartTime.AddMinutes(graceMinutes);
                DateTime deadlineUtc = TimeZoneInfo.ConvertTimeToUtc(
                    DateTime.SpecifyKind(deadline, DateTimeKind.Unspecified));

                List<KeyTransaction> events = await db.KeyTransactions
                    .AsNoTracking()
                    .Include(keyEvent => keyEvent.Key)
                    .Include(keyEvent => keyEvent.Participant)
                    .Where(keyEvent =>
                        keyEvent.Timestamp <= deadlineUtc &&
                        keyEvent.Key.AssignedRooms.Any(room =>
                            room.RoomId == reservation.Room.RoomId))
                    .ToListAsync(cancellationToken);

                bool heldAtDeadline = events
                    .GroupBy(keyEvent => keyEvent.Key.KeyId)
                    .Select(group => group
                        .OrderByDescending(keyEvent => keyEvent.Timestamp)
                        .ThenByDescending(keyEvent => keyEvent.KeyTransactionId)
                        .First())
                    .Any(keyEvent =>
                        keyEvent.Direction == TransactionDirection.Out &&
                        keyEvent.Participant.Id == reservation.RequestedBy.Id);

                if (!heldAtDeadline)
                {
                    reservation.IsNoShow = true;
                }
            }

            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
    }
}

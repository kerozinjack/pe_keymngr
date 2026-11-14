using System.Data;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using PeKeyMngr.Data;
using PeKeyMngr.Models;

namespace PeKeyMngr.Components.Pages.ReservationPages
{
    public partial class Edit
    {
        [Inject]
        private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;

        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        [Inject]
        private UserManager<ApplicationUser> UserManager { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [Inject]
        private ILogger<Edit> Logger { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "reservationid")]
        public Guid? ReservationId { get; set; }

        private bool IsLoading { get; set; } = true;
        private bool IsAuthorized { get; set; }
        private bool CanEditReservation { get; set; }
        private bool IsSaving { get; set; }
        private bool isInitialized;
        private string? UserId { get; set; }
        private string? ErrorMessage { get; set; }
        private Reservation? Reservation { get; set; }
        private List<Room> Rooms { get; set; } = [];
        private List<Equipment> Equipment { get; set; } = [];
        private List<BookingInterval> ReservationIntervals { get; set; } = [];
        private List<BookingInterval> MaintenanceIntervals { get; set; } = [];

        private int DurationMinutes => Reservation is null
            ? 45
            : (int)(Reservation.EndTime - Reservation.StartTime).TotalMinutes;

        private IReadOnlyCollection<Guid> InitialEquipmentIds =>
            Reservation?.Equipments.Select(equipment => equipment.EquipmentId).ToList() ?? [];

        protected override async Task OnInitializedAsync()
        {
            await LoadReservationAsync();
            isInitialized = true;
        }

        protected override Task OnParametersSetAsync() =>
            isInitialized ? LoadReservationAsync() : Task.CompletedTask;

        private async Task LoadReservationAsync()
        {
            IsLoading = true;
            IsAuthorized = false;
            CanEditReservation = false;
            UserId = null;
            ErrorMessage = null;
            Reservation = null;
            Rooms = [];
            Equipment = [];
            ReservationIntervals = [];
            MaintenanceIntervals = [];

            try
            {
                AuthenticationState authenticationState =
                    await AuthenticationStateProvider.GetAuthenticationStateAsync();
                UserId = UserManager.GetUserId(authenticationState.User);
                if (UserId is not string userId)
                {
                    return;
                }

                await using ApplicationDbContext context = await DbFactory.CreateDbContextAsync();
                ApplicationUser? user = await context.Users
                    .Include(candidate => candidate.Roles)
                    .FirstOrDefaultAsync(candidate => candidate.Id == userId);
                IsAuthorized = user?.Roles.Any(role => role.RoleName == "teacher") == true;
                if (!IsAuthorized)
                {
                    return;
                }

                if (ReservationId is not Guid reservationId)
                {
                    ErrorMessage = "A foglalás nem található.";
                    NavigationManager.NotFound();
                    return;
                }

                Reservation = await context.Reservations
                    .AsNoTracking()
                    .Include(reservation => reservation.Room)
                    .Include(reservation => reservation.Equipments)
                    .FirstOrDefaultAsync(reservation =>
                        reservation.ReservationId == reservationId &&
                        reservation.RequestedBy.Id == userId);
                if (Reservation is null)
                {
                    ErrorMessage = "A foglalás nem található.";
                    NavigationManager.NotFound();
                    return;
                }

                if (Reservation.StartTime <= DateTime.Now)
                {
                    ErrorMessage = "Már megkezdett foglalást nem lehet szerkeszteni.";
                    return;
                }

                Rooms = await context.Rooms
                    .Include(room => room.Equipments)
                    .AsNoTracking()
                    .OrderBy(room => room.Building)
                    .ThenBy(room => room.Floor)
                    .ThenBy(room => room.RoomName)
                    .ToListAsync();

                Equipment = await context.Equipments
                    .AsNoTracking()
                    .OrderBy(equipment => equipment.EquipmentName)
                    .ToListAsync();

                ReservationIntervals = await context.Reservations
                    .AsNoTracking()
                    .Where(reservation => reservation.ReservationId != reservationId)
                    .Select(reservation => new BookingInterval(
                        reservation.Room.RoomId,
                        reservation.StartTime,
                        reservation.EndTime))
                    .ToListAsync();

                List<MaintenanceWindow> maintenanceWindows = await context.MaintenanceWindows
                    .AsNoTracking()
                    .Include(maintenance => maintenance.AffectedRooms)
                    .ToListAsync();
                MaintenanceIntervals = maintenanceWindows
                    .SelectMany(maintenance => maintenance.AffectedRooms.Select(room =>
                        new BookingInterval(room.RoomId, maintenance.StartTime, maintenance.EndTime)))
                    .ToList();

                CanEditReservation = true;
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task UpdateReservationAsync(ReservationRequest request)
        {
            ErrorMessage = null;
            if (UserId is not string userId || ReservationId is not Guid reservationId)
            {
                NavigationManager.NotFound();
                return;
            }

            IsSaving = true;
            try
            {
                await using ApplicationDbContext context = await DbFactory.CreateDbContextAsync();
                await using IDbContextTransaction transaction =
                    await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

                ApplicationUser? user = await context.Users
                    .Include(candidate => candidate.Roles)
                    .FirstOrDefaultAsync(candidate => candidate.Id == userId);
                if (user?.Roles.Any(role => role.RoleName == "teacher") != true)
                {
                    ErrorMessage = "Nincs jogosultsága foglalást módosítani.";
                    return;
                }

                Reservation? reservationToUpdate = await context.Reservations
                    .Include(reservation => reservation.Equipments)
                    .FirstOrDefaultAsync(reservation =>
                        reservation.ReservationId == reservationId &&
                        reservation.RequestedBy.Id == userId);
                if (reservationToUpdate is null)
                {
                    NavigationManager.NotFound();
                    return;
                }

                if (reservationToUpdate.StartTime <= DateTime.Now)
                {
                    ErrorMessage = "Már megkezdett foglalást nem lehet szerkeszteni.";
                    CanEditReservation = false;
                    return;
                }

                DateTime start = request.StartTime;
                DateTime end = request.EndTime;
                int durationMinutes = (int)(end - start).TotalMinutes;
                int existingDurationMinutes =
                    (int)(reservationToUpdate.EndTime - reservationToUpdate.StartTime).TotalMinutes;
                bool preservingExistingDuration =
                    start == reservationToUpdate.StartTime &&
                    durationMinutes == existingDurationMinutes;

                if ((start != reservationToUpdate.StartTime && start < DateTime.Now) ||
                    start.Hour is < 8 or > 18 ||
                    start.Minute != 0 ||
                    start.Second != 0 ||
                    end <= start ||
                    (durationMinutes % 45 != 0 && !preservingExistingDuration) ||
                    end > start.Date.AddHours(20))
                {
                    ErrorMessage = "A foglalás nem kezdődhet múltbeli időpontban, 08:00 és 18:00 közötti kezdéssel, 45 perces lépésekben, legkésőbb 20:00-ig tarthat.";
                    return;
                }

                Room? room = await context.Rooms
                    .Include(candidate => candidate.Equipments)
                    .FirstOrDefaultAsync(candidate => candidate.RoomId == request.RoomId);
                if (room is null)
                {
                    ErrorMessage = "A kiválasztott terem már nem található.";
                    return;
                }

                List<Guid> equipmentIds = request.SelectedEquipmentIds.Distinct().ToList();
                if (equipmentIds.Count != request.SelectedEquipmentIds.Count ||
                    equipmentIds.Any(id =>
                        room.Equipments.All(equipment => equipment.EquipmentId != id)))
                {
                    ErrorMessage = "Csak a kiválasztott terem felszerelései közül választhat.";
                    return;
                }

                List<Equipment> requestedEquipment = equipmentIds.Count == 0
                    ? []
                    : await context.Equipments
                        .Where(equipment => equipmentIds.Contains(equipment.EquipmentId))
                        .ToListAsync();
                if (requestedEquipment.Count != equipmentIds.Count)
                {
                    ErrorMessage = "A kiválasztott felszerelések listája megváltozott. Frissítse az oldalt, majd próbálja újra.";
                    return;
                }

                bool reservationConflict = await context.Reservations.AnyAsync(reservation =>
                    reservation.ReservationId != reservationId &&
                    reservation.Room.RoomId == room.RoomId &&
                    reservation.StartTime < end &&
                    reservation.EndTime > start);
                if (reservationConflict)
                {
                    ErrorMessage = "A terem erre az időszakra már foglalt.";
                    return;
                }

                bool maintenanceConflict = await context.MaintenanceWindows.AnyAsync(maintenance =>
                    maintenance.StartTime < end &&
                    maintenance.EndTime > start &&
                    maintenance.AffectedRooms.Any(affectedRoom => affectedRoom.RoomId == room.RoomId));
                if (maintenanceConflict)
                {
                    ErrorMessage = "A terem a kiválasztott időszakban karbantartás alatt áll.";
                    return;
                }

                reservationToUpdate.Room = room;
                reservationToUpdate.StartTime = start;
                reservationToUpdate.EndTime = end;
                reservationToUpdate.Equipments.Clear();
                reservationToUpdate.Equipments.AddRange(requestedEquipment);

                try
                {
                    await context.SaveChangesAsync();
                    await transaction.CommitAsync();
                }
                catch (DbUpdateException exception)
                {
                    Logger.LogError(
                        exception,
                        "Failed to update reservation {ReservationId}",
                        reservationId);
                    ErrorMessage = "Nem sikerült elmenteni a foglalást. Próbálja újra.";
                    return;
                }

                NavigationManager.NavigateTo($"/reservations/details?reservationid={reservationId}");
            }
            finally
            {
                IsSaving = false;
            }
        }

        private void ClearErrorMessage() => ErrorMessage = null;
    }
}

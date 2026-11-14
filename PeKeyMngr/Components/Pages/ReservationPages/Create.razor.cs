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
    public partial class Create : IAsyncDisposable
    {
        [Inject]
        private IDbContextFactory<ApplicationDbContext> DbFactory { get; set; } = default!;

        [Inject]
        private AuthenticationStateProvider AuthenticationStateProvider { get; set; } = default!;

        [Inject]
        private UserManager<ApplicationUser> UserManager { get; set; } = default!;

        [Inject]
        private ILogger<Create> Logger { get; set; } = default!;

        [Inject]
        private NavigationManager NavigationManager { get; set; } = default!;

        [SupplyParameterFromQuery(Name = "roomId")]
        public Guid? InitialRoomId { get; set; }

        private ApplicationDbContext context = default!;
        private ApplicationUser? CurrentUser;
        private List<Room> Rooms = new();
        private List<Equipment> Equipment = new();
        private List<BookingInterval> ReservationIntervals = new();
        private List<BookingInterval> MaintenanceIntervals = new();
        private DateTime InitialStartTime { get; set; }
        private int ResetSelectionVersion { get; set; }
        private bool IsLoading { get; set; }
        private bool IsAuthorized { get; set; }
        private bool ShowSuccessModal { get; set; }
        private string ReservationSuccessRoom { get; set; } = "";
        private string ReservationSuccessSchedule { get; set; } = "";
        private string ReservationSuccessEquipment { get; set; } = "";
        private string? ErrorMessage;

        protected override async Task OnParametersSetAsync()
        {
            IsLoading = true;
            try
            {
                if (context is not null)
                {
                    await context.DisposeAsync();
                }

                ResetState();
                await LoadPageAsync();
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ResetState()
        {
            CurrentUser = null;
            Rooms = new();
            Equipment = new();
            ReservationIntervals = new();
            MaintenanceIntervals = new();
            InitialStartTime = default;
            ResetSelectionVersion = 0;
            IsAuthorized = false;
            ShowSuccessModal = false;
            ReservationSuccessRoom = "";
            ReservationSuccessSchedule = "";
            ReservationSuccessEquipment = "";
            ErrorMessage = null;
        }

        private async Task LoadPageAsync()
        {
            context = await DbFactory.CreateDbContextAsync();

            AuthenticationState authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            string? userId = UserManager.GetUserId(authenticationState.User);
            if (userId is null)
            {
                return;
            }

            CurrentUser = await context.Users
                .Include(user => user.Roles)
                .FirstOrDefaultAsync(user => user.Id == userId);

            if (CurrentUser is null)
            {
                return;
            }

            bool isTeacher = CurrentUser.Roles.Any(role => role.RoleName == "teacher");
            IsAuthorized = isTeacher;
            if (!IsAuthorized)
            {
                return;
            }

            DateTime now = DateTime.Now;
            DateTime earliestStart = now.AddHours(24);
            DateTime nextWholeHour = earliestStart.Date.AddHours(earliestStart.Hour);
            if (nextWholeHour <= earliestStart)
            {
                nextWholeHour = nextWholeHour.AddHours(1);
            }

            if (nextWholeHour.Hour < 8)
            {
                nextWholeHour = nextWholeHour.Date.AddHours(8);
            }
            else if (nextWholeHour.Hour > 18)
            {
                nextWholeHour = nextWholeHour.Date.AddDays(1).AddHours(8);
            }

            InitialStartTime = nextWholeHour;

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

            var reservationRanges = await context.Reservations
                .AsNoTracking()
                .Select(reservation => new
                {
                    RoomId = reservation.Room.RoomId,
                    reservation.StartTime,
                    reservation.EndTime
                })
                .ToListAsync();
            ReservationIntervals = reservationRanges
                .Select(reservation => new BookingInterval(
                    reservation.RoomId,
                    reservation.StartTime,
                    reservation.EndTime))
                .ToList();

            List<MaintenanceWindow> maintenanceWindows = await context.MaintenanceWindows
                .Include(maintenance => maintenance.AffectedRooms)
                .AsNoTracking()
                .ToListAsync();
            MaintenanceIntervals = maintenanceWindows
                .SelectMany(maintenance => maintenance.AffectedRooms.Select(room =>
                    new BookingInterval(room.RoomId, maintenance.StartTime, maintenance.EndTime)))
                .ToList();
        }

        private async Task CreateReservationAsync(ReservationRequest request)
        {
            ErrorMessage = null;

            AuthenticationState authenticationState = await AuthenticationStateProvider.GetAuthenticationStateAsync();
            string? currentUserId = UserManager.GetUserId(authenticationState.User);
            if (currentUserId is null)
            {
                ErrorMessage = "A bejelentkezési munkamenet lejárt. Jelentkezzen be újra.";
                return;
            }

            ApplicationUser? currentUser = await context.Users
                .Include(user => user.Roles)
                .FirstOrDefaultAsync(user => user.Id == currentUserId);
            if (currentUser is null)
            {
                ErrorMessage = "A bejelentkezett felhasználó nem található.";
                return;
            }

            bool isTeacher = currentUser.Roles.Any(role => role.RoleName == "teacher");
            if (!isTeacher)
            {
                ErrorMessage = "Nincs jogosultsága foglalást indítani.";
                return;
            }

            if (request.StartTime <= DateTime.Now.AddHours(24))
            {
                ErrorMessage = "A foglalás kezdése legalább 24 órával később lehet.";
                return;
            }

            DateTime start = request.StartTime;
            DateTime end = request.EndTime;
            int durationMinutes = (int)(end - start).TotalMinutes;
            if (start.Date < DateTime.Today ||
                start.Hour is < 8 or > 18 ||
                start.Minute != 0 ||
                start.Second != 0 ||
                end <= start ||
                durationMinutes % 45 != 0 ||
                end > start.Date.AddHours(20))
            {
                ErrorMessage = "A foglalás 08:00 és 18:00 közötti kezdéssel, 45 perces lépésekben, legkésőbb 20:00-ig tarthat.";
                return;
            }
            Guid roomId = request.RoomId;

            Room? room = await context.Rooms
                .Include(candidate => candidate.Equipments)
                .FirstOrDefaultAsync(candidate => candidate.RoomId == roomId);
            if (room is null)
            {
                ErrorMessage = "A kiválasztott terem már nem található.";
                return;
            }

            List<Guid> selectedEquipmentIds = request.SelectedEquipmentIds.Distinct().ToList();
            if (selectedEquipmentIds.Count != request.SelectedEquipmentIds.Count)
            {
                ErrorMessage = "A kiválasztott felszerelések listája érvénytelen.";
                return;
            }
            if (selectedEquipmentIds.Any(id => room.Equipments.Any(equipment => equipment.EquipmentId == id)))
            {
                ErrorMessage = "A kiválasztott terem saját felszerelése nem igényelhető extra eszközként.";
                return;
            }

            List<Equipment> requestedEquipment = selectedEquipmentIds.Count == 0
                ? new()
                : await context.Equipments
                    .Where(equipment => selectedEquipmentIds.Contains(equipment.EquipmentId))
                    .ToListAsync();
            if (requestedEquipment.Count != selectedEquipmentIds.Count)
            {
                ErrorMessage = "A kiválasztott felszerelések listája megváltozott. Frissítse az oldalt, majd próbálja újra.";
                return;
            }

            await using IDbContextTransaction transaction =
                await context.Database.BeginTransactionAsync(IsolationLevel.Serializable);

            bool reservationConflict = await context.Reservations.AnyAsync(reservation =>
                reservation.Room.RoomId == roomId &&
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
                maintenance.AffectedRooms.Any(affectedRoom => affectedRoom.RoomId == roomId));
            if (maintenanceConflict)
            {
                ErrorMessage = "A terem a kiválasztott időszakban karbantartás alatt áll.";
                return;
            }

            context.Reservations.Add(new Reservation
            {
                Room = room,
                RequestedBy = currentUser,
                StartTime = start,
                EndTime = end,
                Equipments = requestedEquipment
            });

            try
            {
                await context.SaveChangesAsync();
                await transaction.CommitAsync();
            }
            catch (DbUpdateException exception)
            {
                Logger.LogError(exception, "Failed to create a reservation for room {RoomId}", roomId);
                ErrorMessage = "Nem sikerült elmenteni a foglalást. Próbálja újra.";
                return;
            }

            ReservationIntervals.Add(new BookingInterval(roomId, start, end));
            ReservationSuccessRoom = room.RoomName;
            ReservationSuccessSchedule = $"{start:yyyy.MM.dd. HH:mm} – {end:yyyy.MM.dd. HH:mm}";
            ReservationSuccessEquipment = requestedEquipment.Count == 0
                ? "Nem lett felszerelés igényelve."
                : string.Join(", ", requestedEquipment.Select(equipment => equipment.EquipmentName));
            ShowSuccessModal = true;
            ResetSelectionVersion++;
        }

        private void CloseSuccessModal()
        {
            ShowSuccessModal = false;
        }

        private void NavigateToReservations()
        {
            NavigationManager.NavigateTo("/reservations");
        }

        private void ClearErrorMessage()
        {
            ErrorMessage = null;
        }

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
        }

    }
}

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
    public sealed record RoomOption(Room Room, string AvailabilityClass, string AvailabilityLabel, bool IsAvailable);

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

        [SupplyParameterFromQuery(Name = "reservationid")]
        public Guid? ReservationId { get; set; }

        private bool IsEditMode =>
            new Uri(NavigationManager.Uri).AbsolutePath.TrimEnd('/').EndsWith(
                "/reservations/edit",
                StringComparison.OrdinalIgnoreCase);

        private ApplicationDbContext context = default!;
        private ApplicationUser? CurrentUser;
        private List<Room> Rooms = new();
        private List<EquipmentOption> EquipmentOptions = new();
        private List<BookingInterval> ReservationIntervals = new();
        private List<BookingInterval> MaintenanceIntervals = new();
        private DateTime SelectedDate { get; set; }
        private int StartHour { get; set; }
        private int DurationMinutes { get; set; } = 45;
        private int? ExistingDurationMinutes { get; set; }
        private DateTime? ExistingStartTime { get; set; }
        private Guid? SelectedRoomId { get; set; }
        private bool IsLoading { get; set; }
        private bool IsAuthorized { get; set; }
        private bool CanEditReservation { get; set; }
        private bool ShowSuccessModal { get; set; }
        private string ReservationSuccessRoom { get; set; } = "";
        private string ReservationSuccessSchedule { get; set; } = "";
        private string ReservationSuccessEquipment { get; set; } = "";
        private string? ErrorMessage;

        private List<RoomOption> RoomOptions =>
            Rooms.Select(room =>
            {
                RoomAvailability availability = GetAvailability(room.RoomId);
                return new RoomOption(
                    room,
                    availability.ToString().ToLowerInvariant(),
                    GetAvailabilityLabel(availability),
                    availability == RoomAvailability.Available);
            }).ToList();

        private List<int> AvailableStartHours
        {
            get
            {
                List<int> startHours = SelectedDate.Date == DateTime.Today
                    ? GetStartHours(Math.Max(8, DateTime.Now.Hour + 1))
                    : SelectedDate.Date > DateTime.Today
                        ? Enumerable.Range(8, 11).ToList()
                        : new();

                return startHours
                    .Where(hour => !IsWithinStartCutoff(SelectedDate.Date.AddHours(hour)))
                    .ToList();
            }
        }

        private IEnumerable<int> AvailableDurations
        {
            get
            {
                IEnumerable<int> durations = StartHour >= 8 && StartHour <= 18
                    ? Enumerable.Range(1, (20 - StartHour) * 60 / 45)
                        .Select(intervalCount => intervalCount * 45)
                    : Enumerable.Empty<int>();

                if (IsEditMode &&
                    ExistingDurationMinutes is int existingDuration &&
                    existingDuration % 45 != 0 &&
                    ExistingStartTime?.Date == SelectedDate.Date &&
                    ExistingStartTime?.Hour == StartHour)
                {
                    durations = durations.Append(existingDuration).OrderBy(duration => duration);
                }

                return durations;
            }
        }

        private List<EquipmentOption> AvailableExtraEquipmentOptions =>
            EquipmentOptions
                .Where(option => SelectedRoom is null ||
                                 SelectedRoom.Equipments.All(equipment => equipment.EquipmentId != option.Id))
                .ToList();

        private Room? SelectedRoom =>
            SelectedRoomId is null
                ? null
                : Rooms.FirstOrDefault(room => room.RoomId == SelectedRoomId.Value);

        private bool CanSubmit
        {
            get
            {
                if (!IsAuthorized ||
                    (IsEditMode && !CanEditReservation) ||
                    !TryGetSchedule(out DateTime start, out _) ||
                    start < DateTime.Now ||
                    IsWithinStartCutoff(start) ||
                    SelectedRoomId is not Guid roomId)
                {
                    return false;
                }

                return GetAvailability(roomId) == RoomAvailability.Available &&
                       CurrentUser is not null;
            }
        }

        private string ScheduleSummary
        {
            get
            {
                DateTime requestedStart = SelectedDate.Date.AddHours(StartHour);
                if (requestedStart < DateTime.Now)
                {
                    return "A foglalás kezdete nem lehet múltbeli.";
                }

                if (IsWithinStartCutoff(requestedStart))
                {
                    return "A foglalást legalább 24 órával a kezdés előtt kell rögzíteni.";
                }

                return TryGetSchedule(out DateTime start, out DateTime end)
                    ? $"{start:g} – {end:t}"
                    : "Adjon meg érvényes kezdést és 45 perces lépésekben megadható időtartamot.";
            }
        }

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
            EquipmentOptions = new();
            ReservationIntervals = new();
            MaintenanceIntervals = new();
            SelectedDate = default;
            StartHour = 0;
            DurationMinutes = 45;
            ExistingDurationMinutes = null;
            ExistingStartTime = null;
            SelectedRoomId = null;
            IsAuthorized = false;
            CanEditReservation = false;
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

            SelectedDate = nextWholeHour.Date;
            StartHour = nextWholeHour.Hour;
            DurationMinutes = 45;
            HashSet<Guid> existingEquipmentIds = [];

            if (IsEditMode)
            {
                if (ReservationId is not Guid reservationId)
                {
                    NavigationManager.NotFound();
                    return;
                }

                Reservation? reservation = await context.Reservations
                    .AsNoTracking()
                    .Include(item => item.Room)
                    .Include(item => item.Equipments)
                    .FirstOrDefaultAsync(item =>
                        item.ReservationId == reservationId &&
                        item.RequestedBy.Id == userId);

                if (reservation is null)
                {
                    NavigationManager.NotFound();
                    return;
                }

                if (reservation.StartTime <= DateTime.Now.AddHours(24))
                {
                    ErrorMessage = "A foglalás a kezdés előtti 24 órában már nem módosítható.";
                    return;
                }

                CanEditReservation = true;
                ExistingStartTime = reservation.StartTime;
                ExistingDurationMinutes = (int)(reservation.EndTime - reservation.StartTime).TotalMinutes;
                SelectedDate = reservation.StartTime.Date;
                StartHour = reservation.StartTime.Hour;
                DurationMinutes = ExistingDurationMinutes.Value;
                SelectedRoomId = reservation.Room.RoomId;
                existingEquipmentIds = reservation.Equipments
                    .Select(equipment => equipment.EquipmentId)
                    .ToHashSet();
            }

            Rooms = await context.Rooms
                .Include(room => room.Equipments)
                .AsNoTracking()
                .OrderBy(room => room.Building)
                .ThenBy(room => room.Floor)
                .ThenBy(room => room.RoomName)
                .ToListAsync();

            EquipmentOptions = await context.Equipments
                .AsNoTracking()
                .OrderBy(equipment => equipment.EquipmentName)
                .Select(equipment => new EquipmentOption(equipment.EquipmentId, equipment.EquipmentName))
                .ToListAsync();

            IQueryable<Reservation> reservationsQuery = context.Reservations.AsNoTracking();
            if (IsEditMode && ReservationId is Guid currentReservationId)
            {
                reservationsQuery = reservationsQuery
                    .Where(reservation => reservation.ReservationId != currentReservationId);
            }

            var reservationRanges = await reservationsQuery
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

            foreach (EquipmentOption option in EquipmentOptions)
            {
                option.Selected = existingEquipmentIds.Contains(option.Id);
            }

            List<MaintenanceWindow> maintenanceWindows = await context.MaintenanceWindows
                .Include(maintenance => maintenance.AffectedRooms)
                .AsNoTracking()
                .ToListAsync();
            MaintenanceIntervals = maintenanceWindows
                .SelectMany(maintenance => maintenance.AffectedRooms.Select(room =>
                    new BookingInterval(room.RoomId, maintenance.StartTime, maintenance.EndTime)))
                .ToList();

            if (!IsEditMode && InitialRoomId is Guid initialRoomId)
            {
                Room? initialRoom = Rooms.FirstOrDefault(room => room.RoomId == initialRoomId);
                if (initialRoom is not null && GetAvailability(initialRoomId) == RoomAvailability.Available)
                {
                    SelectedRoomId = initialRoom.RoomId;
                }
            }
        }

        private async Task CreateReservationAsync()
        {
            ErrorMessage = null;
            if (IsEditMode && !CanEditReservation)
            {
                ErrorMessage = "A foglalás a kezdés előtti 24 órában már nem módosítható.";
                return;
            }

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

            if (IsWithinStartCutoff(SelectedDate.Date.AddHours(StartHour)))
            {
                ErrorMessage = "A foglalás kezdése legalább 24 órával később lehet.";
                return;
            }

            if (!TryGetSchedule(out DateTime start, out DateTime end))
            {
                ErrorMessage = "A foglalás 08:00 és 18:00 közötti kezdéssel, 45 perces lépésekben, legkésőbb 20:00-ig tarthat.";
                return;
            }

            if (start.Minute != 0 || start.Second != 0 || start < DateTime.Now)
            {
                ErrorMessage = "A kezdés egész órára essen, és ne legyen múltbeli.";
                return;
            }

            if (SelectedRoomId is not Guid roomId)
            {
                ErrorMessage = "Válasszon termet.";
                return;
            }

            Room? room = await context.Rooms
                .Include(candidate => candidate.Equipments)
                .FirstOrDefaultAsync(candidate => candidate.RoomId == roomId);
            if (room is null)
            {
                ErrorMessage = "A kiválasztott terem már nem található.";
                return;
            }

            List<Guid> selectedEquipmentIds = EquipmentOptions
                .Where(option => option.Selected)
                .Select(option => option.Id)
                .ToList();
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

            Reservation? reservationToUpdate = null;
            if (IsEditMode)
            {
                if (ReservationId is not Guid reservationId)
                {
                    NavigationManager.NotFound();
                    return;
                }

                reservationToUpdate = await context.Reservations
                    .Include(reservation => reservation.Equipments)
                    .FirstOrDefaultAsync(reservation =>
                        reservation.ReservationId == reservationId &&
                        reservation.RequestedBy.Id == currentUser.Id);

                if (reservationToUpdate is null)
                {
                    NavigationManager.NotFound();
                    return;
                }

                if (reservationToUpdate.StartTime <= DateTime.Now.AddHours(24))
                {
                    CanEditReservation = false;
                    ErrorMessage = "A foglalás a kezdés előtti 24 órában már nem módosítható.";
                    return;
                }
            }

            IQueryable<Reservation> reservationsForConflict = context.Reservations
                .Where(reservation =>
                reservation.Room.RoomId == roomId &&
                reservation.StartTime < end &&
                reservation.EndTime > start);
            if (ReservationId is Guid editedReservationId && IsEditMode)
            {
                reservationsForConflict = reservationsForConflict
                    .Where(reservation => reservation.ReservationId != editedReservationId);
            }

            bool reservationConflict = await reservationsForConflict.AnyAsync();
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

            if (reservationToUpdate is null)
            {
                context.Reservations.Add(new Reservation
                {
                    Room = room,
                    RequestedBy = currentUser,
                    StartTime = start,
                    EndTime = end,
                    Equipments = requestedEquipment
                });
            }
            else
            {
                reservationToUpdate.Room = room;
                reservationToUpdate.StartTime = start;
                reservationToUpdate.EndTime = end;
                reservationToUpdate.Equipments.Clear();
                reservationToUpdate.Equipments.AddRange(requestedEquipment);
            }

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

            if (IsEditMode && ReservationId is Guid updatedReservationId)
            {
                NavigationManager.NavigateTo($"/reservations/details?reservationid={updatedReservationId}");
                return;
            }

            ReservationIntervals.Add(new BookingInterval(roomId, start, end));
            ReservationSuccessRoom = room.RoomName;
            ReservationSuccessSchedule = $"{start:yyyy.MM.dd. HH:mm} – {end:yyyy.MM.dd. HH:mm}";
            ReservationSuccessEquipment = requestedEquipment.Count == 0
                ? "Nem lett felszerelés igényelve."
                : string.Join(", ", requestedEquipment.Select(equipment => equipment.EquipmentName));
            ShowSuccessModal = true;
            SelectedRoomId = null;
            ClearSelectedEquipment();
        }

        private bool TryGetSchedule(out DateTime start, out DateTime end)
        {
            start = SelectedDate.Date.AddHours(StartHour);
            end = start.AddMinutes(DurationMinutes);
            return SelectedDate.Date >= DateTime.Today &&
                   AvailableStartHours.Contains(StartHour) &&
                   StartHour >= 8 &&
                   StartHour <= 18 &&
                   AvailableDurations.Contains(DurationMinutes) &&
                   end <= SelectedDate.Date.AddHours(20);
        }

        private static List<int> GetStartHours(int firstHour) =>
            firstHour <= 18
                ? Enumerable.Range(firstHour, 19 - firstHour).ToList()
                : new();

        private static bool IsWithinStartCutoff(DateTime start) =>
            start <= DateTime.Now.AddHours(24);

        private static string FormatDuration(int durationMinutes)
        {
            if (durationMinutes % 45 != 0)
            {
                return $"{durationMinutes} perc (meglévő foglalás)";
            }

            int intervalCount = durationMinutes / 45;
            return intervalCount == 1
                ? "1 × 45 perc"
                : $"{intervalCount} × 45 perc ({durationMinutes} perc)";
        }

        private RoomAvailability GetAvailability(Guid roomId)
        {
            if (!TryGetSchedule(out DateTime start, out DateTime end) || start < DateTime.Now)
            {
                return RoomAvailability.Unknown;
            }

            if (MaintenanceIntervals.Any(interval =>
                    interval.RoomId == roomId && interval.StartTime < end && interval.EndTime > start))
            {
                return RoomAvailability.Maintenance;
            }

            if (ReservationIntervals.Any(interval =>
                    interval.RoomId == roomId && interval.StartTime < end && interval.EndTime > start))
            {
                return RoomAvailability.Reserved;
            }

            return RoomAvailability.Available;
        }

        private static string GetAvailabilityLabel(RoomAvailability availability) =>
            availability switch
            {
                RoomAvailability.Available => "Szabad",
                RoomAvailability.Reserved => "Foglalt",
                RoomAvailability.Maintenance => "Karbantartás alatt",
                _ => "Adjon meg érvényes időpontot"
            };

        private Task SelectRoomAsync(Guid? roomId)
        {
            if (roomId is Guid selectedRoomId &&
                GetAvailability(selectedRoomId) != RoomAvailability.Available)
            {
                return Task.CompletedTask;
            }

            if (SelectedRoomId != roomId)
            {
                ClearSelectedEquipment();
            }

            SelectedRoomId = roomId;
            ErrorMessage = null;
            return Task.CompletedTask;
        }

        private void ScheduleChanged()
        {
            ErrorMessage = null;
            if (SelectedDate.Date < DateTime.Today)
            {
                SelectedDate = DateTime.Today;
            }

            if (AvailableStartHours.Count > 0 && !AvailableStartHours.Contains(StartHour))
            {
                StartHour = AvailableStartHours[0];
            }

            if (AvailableDurations.Any() && !AvailableDurations.Contains(DurationMinutes))
            {
                DurationMinutes = AvailableDurations.First();
            }

            if (SelectedRoomId is Guid roomId &&
                GetAvailability(roomId) != RoomAvailability.Available)
            {
                SelectedRoomId = null;
                ClearSelectedEquipment();
            }
        }

        private void StartHourChanged() => ScheduleChanged();

        private void CloseSuccessModal()
        {
            ShowSuccessModal = false;
        }

        private void NavigateToReservations()
        {
            NavigationManager.NavigateTo("/reservations");
        }

        private void ClearSelectedEquipment()
        {
            foreach (EquipmentOption option in EquipmentOptions)
            {
                option.Selected = false;
            }
        }

        private void ClearErrorMessage()
        {
            ErrorMessage = null;
        }

        public async ValueTask DisposeAsync()
        {
            await context.DisposeAsync();
        }

        private sealed class EquipmentOption(Guid id, string name)
        {
            public Guid Id { get; } = id;
            public string Name { get; } = name;
            public bool Selected { get; set; }
        }

        private sealed record BookingInterval(Guid RoomId, DateTime StartTime, DateTime EndTime);

        private enum RoomAvailability
        {
            Unknown,
            Available,
            Reserved,
            Maintenance
        }
    }
}

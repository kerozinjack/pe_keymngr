using Microsoft.AspNetCore.Components;
using PeKeyMngr.Models;

namespace PeKeyMngr.Components.Pages.ReservationPages
{
    public sealed record RoomOption(Room Room, string AvailabilityClass, string AvailabilityLabel, bool IsAvailable);

    public sealed record BookingInterval(Guid RoomId, DateTime StartTime, DateTime EndTime);

    public sealed record ReservationRequest(
        DateTime StartTime,
        DateTime EndTime,
        Guid RoomId,
        IReadOnlyCollection<Guid> SelectedEquipmentIds);

    public partial class ReservationForm
    {
        [Parameter, EditorRequired]
        public IReadOnlyList<Room> Rooms { get; set; } = Array.Empty<Room>();

        [Parameter, EditorRequired]
        public IReadOnlyList<Equipment> Equipment { get; set; } = Array.Empty<Equipment>();

        [Parameter, EditorRequired]
        public IReadOnlyList<BookingInterval> ReservationIntervals { get; set; } =
            Array.Empty<BookingInterval>();

        [Parameter, EditorRequired]
        public IReadOnlyList<BookingInterval> MaintenanceIntervals { get; set; } =
            Array.Empty<BookingInterval>();

        [Parameter]
        public DateTime InitialStartTime { get; set; }

        [Parameter]
        public int InitialDurationMinutes { get; set; } = 45;

        [Parameter]
        public Guid? InitialRoomId { get; set; }

        [Parameter]
        public IReadOnlyCollection<Guid> InitialEquipmentIds { get; set; } = Array.Empty<Guid>();

        [Parameter]
        public int ResetSelectionVersion { get; set; }

        [Parameter]
        public string? ErrorMessage { get; set; }

        [Parameter]
        public string ScheduleSectionTitle { get; set; } = "Időpont és foglaló";

        [Parameter]
        public string SubmitText { get; set; } = "Foglalás létrehozása";

        [Parameter]
        public bool IsSaving { get; set; }

        [Parameter]
        public EventCallback<ReservationRequest> OnSubmit { get; set; }

        [Parameter]
        public EventCallback OnErrorCleared { get; set; }

        private bool isInitialized;
        private int lastResetSelectionVersion;
        private string? LocalErrorMessage { get; set; }
        private DateTime SelectedDate { get; set; }
        private int StartHour { get; set; }
        private int DurationMinutes { get; set; } = 45;
        private Guid? SelectedRoomId { get; set; }
        private List<EquipmentOption> EquipmentOptions { get; set; } = [];

        private string? DisplayErrorMessage => LocalErrorMessage ?? ErrorMessage;

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

        private List<int> AvailableStartHours =>
            (SelectedDate.Date == DateTime.Today
                ? GetStartHours(Math.Max(8, DateTime.Now.Hour + 1))
                : SelectedDate.Date > DateTime.Today
                    ? Enumerable.Range(8, 11).ToList()
                    : new())
            .Where(hour => !IsWithinStartCutoff(SelectedDate.Date.AddHours(hour)))
            .ToList();

        private IEnumerable<int> AvailableDurations
        {
            get
            {
                IEnumerable<int> durations = StartHour is >= 8 and <= 18
                ? Enumerable.Range(1, (20 - StartHour) * 60 / 45)
                    .Select(intervalCount => intervalCount * 45)
                : Enumerable.Empty<int>();

                if (InitialDurationMinutes % 45 != 0 &&
                    SelectedDate.Date == InitialStartTime.Date &&
                    StartHour == InitialStartTime.Hour &&
                    InitialStartTime != default)
                {
                    durations = durations.Append(InitialDurationMinutes).OrderBy(duration => duration);
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
            SelectedRoomId is Guid roomId
                ? Rooms.FirstOrDefault(room => room.RoomId == roomId)
                : null;

        private bool CanSubmit
        {
            get
            {
                if (!TryGetSchedule(out DateTime start, out _) ||
                    start < DateTime.Now ||
                    IsWithinStartCutoff(start) ||
                    SelectedRoomId is not Guid roomId)
                {
                    return false;
                }

                return GetAvailability(roomId) == RoomAvailability.Available;
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

        protected override void OnParametersSet()
        {
            if (!isInitialized)
            {
                SelectedDate = InitialStartTime.Date;
                StartHour = InitialStartTime.Hour;
                DurationMinutes = InitialDurationMinutes;
                SelectedRoomId = InitialRoomId;
                HashSet<Guid> selectedEquipmentIds = InitialEquipmentIds.ToHashSet();
                EquipmentOptions = Equipment
                    .Select(equipment => new EquipmentOption(
                        equipment.EquipmentId,
                        equipment.EquipmentName,
                        selectedEquipmentIds.Contains(equipment.EquipmentId)))
                    .ToList();
                isInitialized = true;
            }
            else if (lastResetSelectionVersion != ResetSelectionVersion)
            {
                SelectedRoomId = null;
                ClearSelectedEquipment();
            }

            lastResetSelectionVersion = ResetSelectionVersion;

            if (SelectedRoomId is Guid roomId &&
                GetAvailability(roomId) != RoomAvailability.Available)
            {
                SelectedRoomId = null;
            }
        }

        private async Task SubmitAsync()
        {
            LocalErrorMessage = null;

            if (!TryGetSchedule(out DateTime start, out DateTime end))
            {
                LocalErrorMessage = "A foglalás 08:00 és 18:00 közötti kezdéssel, 45 perces lépésekben, legkésőbb 20:00-ig tarthat.";
                return;
            }

            if (start < DateTime.Now)
            {
                LocalErrorMessage = "A foglalás kezdete nem lehet múltbeli.";
                return;
            }

            if (IsWithinStartCutoff(start))
            {
                LocalErrorMessage = "A foglalás kezdése legalább 24 órával később lehet.";
                return;
            }

            if (SelectedRoomId is not Guid roomId)
            {
                LocalErrorMessage = "Válasszon szabad termet.";
                return;
            }

            if (GetAvailability(roomId) != RoomAvailability.Available)
            {
                LocalErrorMessage = "A terem erre az időszakra nem szabad.";
                return;
            }

            if (SelectedRoom is Room room &&
                EquipmentOptions.Any(option =>
                    option.Selected &&
                    room.Equipments.Any(equipment => equipment.EquipmentId == option.Id)))
            {
                LocalErrorMessage = "A terem saját felszerelése nem igényelhető extra eszközként.";
                return;
            }

            await OnSubmit.InvokeAsync(new ReservationRequest(
                start,
                end,
                roomId,
                EquipmentOptions.Where(option => option.Selected).Select(option => option.Id).ToList()));
        }

        private bool TryGetSchedule(out DateTime start, out DateTime end)
        {
            start = SelectedDate.Date.AddHours(StartHour);
            end = start.AddMinutes(DurationMinutes);
            return SelectedDate.Date >= DateTime.Today &&
                   AvailableStartHours.Contains(StartHour) &&
                   StartHour is >= 8 and <= 18 &&
                   AvailableDurations.Contains(DurationMinutes) &&
                   end <= SelectedDate.Date.AddHours(20);
        }

        private static List<int> GetStartHours(int firstHour) =>
            firstHour <= 18
                ? Enumerable.Range(firstHour, 19 - firstHour).ToList()
                : [];

        private static bool IsWithinStartCutoff(DateTime start) =>
            start <= DateTime.Now.AddHours(24);

        private static string FormatDuration(int durationMinutes)
        {
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
            return ClearErrorMessage();
        }

        private async Task ScheduleChanged()
        {
            LocalErrorMessage = null;
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

            await OnErrorCleared.InvokeAsync();
        }

        private Task ClearErrorMessage()
        {
            LocalErrorMessage = null;
            return OnErrorCleared.InvokeAsync();
        }

        private void ClearSelectedEquipment()
        {
            foreach (EquipmentOption option in EquipmentOptions)
            {
                option.Selected = false;
            }
        }

        private sealed class EquipmentOption(Guid id, string name, bool selected)
        {
            public Guid Id { get; } = id;
            public string Name { get; } = name;
            public bool Selected { get; set; } = selected;
        }

        private enum RoomAvailability
        {
            Unknown,
            Available,
            Reserved,
            Maintenance
        }
    }
}

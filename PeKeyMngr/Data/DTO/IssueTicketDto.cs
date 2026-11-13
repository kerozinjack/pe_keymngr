using PeKeyMngr.Models;
using System.ComponentModel.DataAnnotations;

namespace PeKeyMngr.Data.DTO
{
    public class IssueTicketDto
    {
        public string TicketId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string AffectedRoom { get; set; } = string.Empty;
        [StringLength(200, ErrorMessage = "A probléma leírása 50 és 200 karakter között kell legyen", MinimumLength = 20)]
        public string IssueDescription { get; set; } = string.Empty;

        public IssueTicket? ToIssueTicket(ApplicationUser user, ApplicationDbContext ctx)
        {
            Console.WriteLine($"AffectedRoom: '{AffectedRoom}'");
            Console.WriteLine($"Is valid GUID: {Guid.TryParse(AffectedRoom, out Guid roomId)}");

            List<Room> allRooms = ctx.Rooms.ToList();

            Console.WriteLine($"Rooms count: {allRooms.Count}");

            foreach (Room r in allRooms)
            {
                Console.WriteLine($"Room: {r.RoomName}, ID: {r.RoomId}");
            }

            Room? room = ctx.Rooms.FirstOrDefault(r => r.RoomId == roomId);

            Console.WriteLine($"Found room: {room != null}");

            if (room == null)
            {
                return null;
            }

            return new IssueTicket
            {
                Timestamp = Timestamp,
                ReportedBy = user,
                AffectedRoom = room,
                IssueDescription = IssueDescription
            };
        }

        public void FromIssueTicket(IssueTicket? ticket)
        {
            if (ticket == null)
            {
                return;
            }
            Timestamp = ticket.Timestamp;
            AffectedRoom = ticket.AffectedRoom.RoomId.ToString("D").ToLowerInvariant();
            IssueDescription = ticket.IssueDescription;
        }
    }
}
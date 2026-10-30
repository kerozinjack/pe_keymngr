using PeKeyMngr.Models;

namespace PeKeyMngr.Data.DTO
{
    public class IssueTicketDto
    {
        public string TicketId { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; } = DateTime.Now;
        public string AffectedRoom { get; set; } = string.Empty;
        public string IssueDescription { get; set; } = string.Empty;

        public IssueTicket? ToIssueTicket(ApplicationUser user, ApplicationDbContext ctx)
        {
            Console.WriteLine(AffectedRoom);
            Room? room = ctx.Rooms.FirstOrDefault(r => r.RoomId.ToString() == AffectedRoom.ToUpper());
            if (room == null)
            {
                return null;
            }
            return new IssueTicket
            {
                Timestamp = Timestamp, ReportedBy = user, AffectedRoom = room, IssueDescription = IssueDescription
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
using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;

namespace PeKeyMngr.Models
{
    [PrimaryKey("TicketId")]
    public class IssueTicket
    {
        public Guid TicketId { get; set; }
        public DateTime Timestamp { get; set; }
        public required ApplicationUser ReportedBy { get; set; }
        public required Room AffectedRoom { get; set; }
        public string IssueDescription { get; set; } = string.Empty;
    }
}

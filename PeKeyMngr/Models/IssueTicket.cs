using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;
using System.ComponentModel.DataAnnotations;

namespace PeKeyMngr.Models
{
    [PrimaryKey("TicketId")]
    public class IssueTicket
    {
        public Guid TicketId { get; set; }
        public DateTime Timestamp { get; set; }
        public required ApplicationUser ReportedBy { get; set; }
        public required Room AffectedRoom { get; set; }
        [StringLength(500, ErrorMessage = "A hiba leírásának minimum 20 és maximum 500 karakternek kell lennie", MinimumLength = 20)]
        public string IssueDescription { get; set; } = string.Empty;
    }
}

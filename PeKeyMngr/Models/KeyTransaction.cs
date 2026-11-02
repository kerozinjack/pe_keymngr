using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;

namespace PeKeyMngr.Models
{
    public enum TransactionDirection
    {
        In, Out
    }

    [PrimaryKey("KeyTransactionId")]
    public class KeyTransaction
    {
        public Guid KeyTransactionId { get; set; }
        public DateTime Timestamp { get; set; }
        public TransactionDirection Direction { get; set; }
        public required Key Key { get; set; }
        public required ApplicationUser AuthorizedBy { get; set; }
        public required ApplicationUser Participant { get; set; }
    }
}

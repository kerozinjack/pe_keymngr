using Microsoft.EntityFrameworkCore;

namespace PeKeyMngr.Models
{
    public enum Severity
    {
        Info, Warn, Error, Critical
    }
    [PrimaryKey("LogId")]
    public class AuditLog
    {
        public Guid LogId { get; set; }
        public Severity Severity { get; set; }
        public string Subsystem { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
    }
}

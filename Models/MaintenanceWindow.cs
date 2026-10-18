using Microsoft.EntityFrameworkCore;

namespace PeKeyMngr.Models
{
    [PrimaryKey("MaintenanceWindowId")]
    public class MaintenanceWindow
    {
        public Guid MaintenanceWindowId { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<Room> AffectedRooms { get; set; } = [];
    }
}

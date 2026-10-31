using Microsoft.EntityFrameworkCore;
using PeKeyMngr.Data;

namespace PeKeyMngr.Models
{
    [PrimaryKey("ReservationId")]
    public class Reservation
    {
        public Guid ReservationId { get; set; }
        public required Room Room { get; set; }
        public required ApplicationUser RequestedBy { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime EndTime { get; set; }
        public List<Equipment> Equipments { get; set; } = [];
    }
}

using Microsoft.EntityFrameworkCore;

namespace PeKeyMngr.Models
{
    [PrimaryKey("RoomId")]
    public class Room
    {
        public Guid RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string Building { get; set; } = string.Empty;
        public string Floor { get; set; } = string.Empty;
        public int Capacity { get; set; }
        public List<Equipment> Equipments = [];
    }
}

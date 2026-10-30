using Microsoft.EntityFrameworkCore;

namespace PeKeyMngr.Models
{
    [PrimaryKey("KeyId")]
    public class Key
    {
        public Guid KeyId { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public List<Room> AssignedRooms { get; set; } = [];
        public List<Role> RequiredRoles { get; set; } = [];
    }
}

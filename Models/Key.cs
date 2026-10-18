using Microsoft.EntityFrameworkCore;

namespace PeKeyMngr.Models
{
    [PrimaryKey("KeyId")]
    public class Key
    {
        public Guid KeyId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public List<Role> RequiredRoles { get; set; } = [];
    }
}

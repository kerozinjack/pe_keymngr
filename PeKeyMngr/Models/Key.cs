using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PeKeyMngr.Models
{
    [PrimaryKey("KeyId")]
    public class Key
    {
        public Guid KeyId { get; set; }
        [StringLength(100, ErrorMessage = "A kulcs nevének minimum 2 és maximum 100 karakter kell legyen", MinimumLength = 2)]
        public string KeyName { get; set; } = string.Empty;
        public List<Room> AssignedRooms { get; set; } = [];
        public List<Role> RequiredRoles { get; set; } = [];
    }
}

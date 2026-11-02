using Microsoft.EntityFrameworkCore;

namespace PeKeyMngr.Models
{
    [PrimaryKey("RoleId")]
    public class Role
    {
        public Guid RoleId { get; set; }
        public string RoleName { get; set; } = string.Empty;
    }
}

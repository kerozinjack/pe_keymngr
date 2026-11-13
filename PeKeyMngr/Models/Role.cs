using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PeKeyMngr.Models
{
    [PrimaryKey("RoleId")]
    public class Role
    {
        public Guid RoleId { get; set; }
        [StringLength(50, ErrorMessage = "A jog nevének legalább 3 és maximum 50 karakter kell legyen",  MinimumLength = 3)]
        public string RoleName { get; set; } = string.Empty;
    }
}

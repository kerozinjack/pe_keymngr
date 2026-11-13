using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PeKeyMngr.Models
{
    [PrimaryKey("EquipmentId")]
    public class Equipment
    {
        public Guid EquipmentId { get; set; }
        [StringLength(100, ErrorMessage = "A felszerelésnek a hossza legalább 3 és maximum 100 karakter kell legyen",  MinimumLength = 3)]
        public string EquipmentName { get; set; } = string.Empty;
    }
}

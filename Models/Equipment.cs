using Microsoft.EntityFrameworkCore;

namespace PeKeyMngr.Models
{
    [PrimaryKey("EquipmentId")]
    public class Equipment
    {
        public Guid EquipmentId { get; set; }
        public string EquipmentName { get; set; } = string.Empty;
    }
}

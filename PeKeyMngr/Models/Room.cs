using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace PeKeyMngr.Models
{
    [PrimaryKey("RoomId")]
    public class Room
    {
        public Guid RoomId { get; set; }
        [StringLength(50, ErrorMessage = "A szoba nevének legalább 2 és maximum 50 karakternek kell lennie", MinimumLength = 2)]
        public string RoomName { get; set; } = string.Empty;
        [StringLength(50, ErrorMessage = "Az épület neve maximum 50 karakter lehet")]
        public string Building { get; set; } = string.Empty;
        [StringLength(50, ErrorMessage = "A szint legalább 2 és maximum 50 karakter lehet", MinimumLength = 2)]
        public string Floor { get; set; } = string.Empty;
        [Range(minimum: 1, maximum: 1200, ErrorMessage = "A férőhelyek száma 1 és 1200 között kell lennie")]
        public int Capacity { get; set; }
        public List<Equipment> Equipments = [];
    }
}

using PeKeyMngr.Models;

namespace PeKeyMngr.Data.DTO
{
    public class KeyDto
    {
        public Guid KeyId { get; set; }
        public string KeyName { get; set; } = string.Empty;
        public List<Guid> AssignedRoomIds { get; set; } = [];
        public List<Guid> RequiredRoleIds { get; set; } = [];

        public void FromKey(Key? key)
        {
            if (key == null)
            {
                return;
            }
            KeyId = key.KeyId;
            KeyName = key.KeyName;
            AssignedRoomIds = key.AssignedRooms.Select(room => room.RoomId).ToList();
            RequiredRoleIds = key.RequiredRoles.Select(role => role.RoleId).ToList();
        }

        public Key ToKey(ApplicationDbContext context)
        {
            Key tmp = new Key() { KeyId = KeyId, KeyName = KeyName, AssignedRooms = [], RequiredRoles = [] };
            foreach (Guid assignedRoomId in AssignedRoomIds)
            {
                Room? x = context.Rooms.Find(assignedRoomId);
                if (x != null)
                {
                    tmp.AssignedRooms.Add(x);
                }
            }

            foreach (Guid requiredRoleId in RequiredRoleIds)
            {
                Role? x = context.CustomRoles.Find(requiredRoleId);
                if (x != null)
                {
                    tmp.RequiredRoles.Add(x);
                }
            }
            return tmp;
        }
    }
}
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChatApi.Model
{
   
    public class Connection
    {
        public string ConnectionID { get; set; }
        public string UserAgent { get; set; }
        public bool Connected { get; set; }
    }
 
    public class ConversationRoom
    {
        [Key]
        public int id { get; set; }
        public string RoomName { get; set; }
        public virtual ICollection<UserInRoom> UsersInRoom { get; set; }
    }

    public class UserInRoom
    {
        [Key]
        public int id { get; set; }
        public int user_id { get; set; }
        public int room_id { get; set; }
        public virtual ICollection<GroupConnection> GroupConnections { get; set; }

    }
    public class GroupConnection
    {
        [Key]
        public int Id { get; set; }
        public string ConnectionID { get; set; }
        public string UserAgent { get; set; }
        public bool Connected { get; set; }
    }

    public class ChatMessages
    {
        [Key]
        public int id { get; set; }
        public int sender_id { get; set; }
        public int receiver_id { get; set; }
        public string content { get; set; }
        public DateTime send_at { get; set; }
    }
    public class RoomChatMessages
    {
        [Key]
        public int id { get; set; }
        public int room_id { get; set; }
        public int sender_id { get; set; }
        public int receiver_id { get; set; }
        public string content { get; set; }
        public DateTime send_at { get; set; }
    }
}

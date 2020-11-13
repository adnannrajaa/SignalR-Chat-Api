using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using SignalRChatApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SignalRChatApi.Services
{
    [Authorize]
    public class ChatHub : Hub
    {
        private readonly static ConnectionMapping<string> _connections =
            new ConnectionMapping<string>();
        private readonly ApplicationDbContext _context;
        public ChatHub(ApplicationDbContext context)
        {
            _context = context;
        }
        public void SendMessage(string who, string message)
        {
            var user = Context.User.Identities.FirstOrDefault();
            var user_name = user.Claims.Where(e => e.Type == "user_name").Select(e => e.Value).FirstOrDefault();
            int sender_id =Convert.ToInt32( user.Claims.Where(e => e.Type == "user_id").Select(e => e.Value).FirstOrDefault());
            var id = Convert.ToInt32(who);
            var ObjFromDb = _context.Users.Where(s=>s.id == id).FirstOrDefault();
            if (ObjFromDb == null)
            {
                //Clients.Caller.showErrorMessage("Could not find that user.");
            }
            else
            {
                _context.Entry(ObjFromDb)
                    .Collection(u => u.Connections)
                    .Query()
                    .Where(c => c.Connected == true)
                    .Load();

                if (ObjFromDb.Connections == null)
                {
                    //Clients.Caller.showErrorMessage("The user is no longer connected.");
                }
                else
                {
                    ChatMessages chat = new ChatMessages();
                    chat.sender_id = sender_id;
                    chat.receiver_id = id;
                    chat.content = message;
                    chat.send_at = DateTime.Now;
                    _context.ChatMessages.Add(chat);

                    _context.SaveChanges();
                    foreach (var connection in ObjFromDb.Connections)
                    {
                        Clients.Client(connection.ConnectionID).SendAsync("ReceiveMessage", user_name, message);
                    }
                }
            }
           
        }
        public void SendGroupMessage(string groupName, string message)
        {
            var user = Context.User.Identities.FirstOrDefault();
            var user_name = user.Claims.Where(e => e.Type == "user_name").Select(e => e.Value).FirstOrDefault();
            int  sender_id = Convert.ToInt32(user.Claims.Where(e => e.Type == "user_id").Select(e => e.Value).FirstOrDefault());
            var RoomName = _context.Rooms.Where(s => s.RoomName == groupName).FirstOrDefault();

            RoomChatMessages roomChat = new RoomChatMessages();
            roomChat.sender_id = sender_id;
            roomChat.content = message;
            roomChat.send_at = DateTime.Now;
            roomChat.room_id = RoomName.id;
            _context.GroupChat.Add(roomChat);
            _context.SaveChanges();

            Clients.Group(groupName).SendAsync("ReceiveMessage", user_name, message);
        }

        public override async Task OnConnectedAsync()
        {
            var Claim = Context.User.Identities.FirstOrDefault();
            var user_name = Claim.Claims.Where(e => e.Type == "user_name").Select(e => e.Value).FirstOrDefault();
            //_connections.Add(user_id, Context.ConnectionId);
            var user = _context.Users
                    .Include(u => u.Connections)
                    .SingleOrDefault(u => u.user_name == user_name);

            if (user == null)
            {
                user = new User
                {
                    user_name = user_name,
                    Connections = new List<Connection>()
                };
                _context.Users.Add(user);
            }

            user.Connections.Add(new Connection
            {
                ConnectionID = Context.ConnectionId,
                UserAgent = "User-Agent",
                Connected = true
            });
            _context.SaveChanges();
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception ex)
        {
            var Claim = Context.User.Identities.FirstOrDefault();
            var user_id = Claim.Claims.Where(e => e.Type == "user_id").Select(e => e.Value).FirstOrDefault();
            var activeConnection = _context.Connections.Where(s => s.ConnectionID == Context.ConnectionId).FirstOrDefault();
            if(activeConnection != null)
            {
                _context.Connections.Remove(activeConnection);

            }
            var connection = _context.Connections.Find(Context.ConnectionId);
            connection.Connected = false;
            _context.SaveChanges(); 
            await base.OnDisconnectedAsync(ex);
        }
        public async Task AddToGroup(string groupName)
        {
            var Claim = Context.User.Identities.FirstOrDefault();
            var userName = Claim.Claims.Where(e => e.Type == "user_name").Select(e => e.Value).FirstOrDefault();
            int userId = Convert.ToInt32(Claim.Claims.Where(e => e.Type == "user_id").Select(e => e.Value).FirstOrDefault());

            // Retrieve Room.
            var Room = _context.Rooms.Where(s => s.RoomName == groupName).FirstOrDefault();
            if (Room == null)
            {
                Room = new ConversationRoom
                {
                    RoomName = groupName
                };
                _context.Rooms.Add(Room);
                _context.SaveChanges();
            }
            var UserRoom = _context.RoomUsers.Where(s => s.room_id == Room.id && s.user_id == userId).FirstOrDefault();
            if (UserRoom == null)
            {
                UserRoom = new UserInRoom
                {
                    user_id = userId,
                    room_id = Room.id,
                    GroupConnections = new List<GroupConnection>()
                };
                _context.RoomUsers.Add(UserRoom);
                _context.SaveChanges();

            }
            else
            {
                UserRoom.GroupConnections = new List<GroupConnection>();
            }
            UserRoom.GroupConnections.Add(new GroupConnection
            {
                ConnectionID = Context.ConnectionId,
                UserAgent = "User-Agent",
                Connected = true
            });
            _context.SaveChanges();


            // Add to each assigned group.
            var user = _context.RoomUsers
                .Include(u => u.GroupConnections)
                .Where(u => u.room_id == Room.id).ToList();

             await Groups.AddToGroupAsync(Context.ConnectionId, groupName);
            await Clients.Group(groupName).SendAsync("Send", $"{userName} has joined the group {groupName}.");
        }

        public async Task RemoveFromGroup(string groupName)
        {
            var Claim = Context.User.Identities.FirstOrDefault();
            var userName = Claim.Claims.Where(e => e.Type == "user_name").Select(e => e.Value).FirstOrDefault();
            int userId = Convert.ToInt32(Claim.Claims.Where(e => e.Type == "user_id").Select(e => e.Value).FirstOrDefault());

            // Retrieve room.
            var room = _context.Rooms.Find(groupName);
            if (room != null)
            {
                var userInRoom = _context.RoomUsers.Where(s => s.room_id == room.id && s.user_id == userId);

                foreach(var item in userInRoom)
                {
                    _context.RoomUsers.Remove(item);
                }
                
                _context.SaveChanges();

                await Groups.RemoveFromGroupAsync(Context.ConnectionId, groupName);
                await Clients.Group(groupName).SendAsync("Send", $"{userName} has left the group {groupName}.");

            }
        }
        //public override async Task OnReconnectedAsyc()
        //{
        //    string name = Context.User.Identity.Name;

        //    if (!_connections.GetConnections(name).Contains(Context.ConnectionId))
        //    {
        //        _connections.Add(name, Context.ConnectionId);
        //    }

        //    await base.OnReconnectedAsync();
        //}
    }
}

using Microsoft.EntityFrameworkCore;
using SignalRChatApi.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SignalRChatApi
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Connection> Connections { get; set; }
        public DbSet<ConversationRoom> Rooms { get; set; }
        public DbSet<UserInRoom> RoomUsers { get; set; }
        public DbSet<GroupConnection> GroupConnections { get; set; }

        public DbSet<ChatMessages> ChatMessages { get; set; }
        public DbSet<RoomChatMessages> GroupChat { get; set; }

    }
}

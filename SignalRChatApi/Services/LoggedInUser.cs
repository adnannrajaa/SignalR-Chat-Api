using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace SignalRChatApi.Services
{
    public static class LoggedInUser
    {
        public static string UserName { get; set; }
        public static int UserId { get; set; }
    }
}

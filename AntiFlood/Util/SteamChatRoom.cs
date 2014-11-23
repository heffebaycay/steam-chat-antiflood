using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiFlood
{
    public class SteamChatRoom
    {
        public UInt64 chatId { get; set; }

        public UInt64 groupId { get; set; }

        public string roomName { get; set; }

        public SteamChatRoom(UInt64 pChatId, UInt64 pGroupId, string pRoomName)
        {
            chatId = pChatId;
            groupId = pGroupId;
            roomName = pRoomName;
        }
    }
}

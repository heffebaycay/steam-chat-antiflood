using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steam4NET;

namespace AntiFlood.Util
{
    public class SteamPlayer
    {
        public uint accountID { get; set; }

        public int nbFlagged { get; set; }

        public DateTime lastMsgTime { get; set; }

        public int lastMessageHash { get; set; }

        public SteamPlayer(uint gAccountId, DateTime gLastMsgTime, int gNbFlagged, int gMsgHash)
        {
            accountID = gAccountId;
            lastMsgTime = gLastMsgTime;
            nbFlagged = gNbFlagged;
            lastMessageHash = gMsgHash;
        }

        public void raiseNbFlagged(int offset = 1)
        {
            nbFlagged += offset;
        }

        public void lowerNbFlagged()
        {
            if (nbFlagged > 0)
            {
                nbFlagged--;
            } 
        }
    }
}

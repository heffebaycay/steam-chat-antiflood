using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AntiFlood.Util
{
    class SteamException : Exception
    {
        public SteamException(string msg) : base(msg)
        {
            
        }

        public SteamException(string msg, Exception ex) : base(msg, ex)
        {
            
        }
    }
}

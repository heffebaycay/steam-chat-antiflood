using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Steam4NET;
using System.Runtime.InteropServices;

namespace AntiFlood.Util
{
    public static class SteamContext
    {
        public static IClientEngine pClientEngine { get; private set; }
        public static IClientFriends pClientFriends { get; private set; }
        public static ISteamClient010 pSteamClient { get; private set; }
        public static ISteamFriends002 pSteamFriends { get; private set; }
        public static ISteam005 pSteam005 { get; private set; }


        public static int hPipe { get; set; }
        public static int hUser { get; set; }

        public static void Initialize()
        {
            if ( !Steamworks.Load( true ) )
                throw new SteamException( "Unable to load steamclient library." );

            try
            {
                pClientEngine = Steamworks.CreateInterface<IClientEngine>();
            }
            catch ( Exception ex )
            {
                throw new SteamException( "Unable to get IClientEngine interface", ex );
            }

            try
            {
                pSteamClient = Steamworks.CreateInterface<ISteamClient010>();
            }
            catch ( Exception ex )
            {
                throw new SteamException( "Unable to get ISteamClient interface", ex );
            }

            hPipe = pSteamClient.CreateSteamPipe();
            if ( hPipe == 0 )
                throw new SteamException( "Unable to acquire a Steam pipe." );
            hUser = pSteamClient.ConnectToGlobalUser( hPipe );
            if ( hUser == 0 )
                throw new SteamException( "Unable to connect to global user." );

            pSteamFriends = pSteamClient.GetISteamFriends<ISteamFriends002>( hUser, hPipe );
            if ( pSteamFriends == null )
                throw new SteamException( "Unable to get ISteamFriends interface." );
            pClientFriends = pClientEngine.GetIClientFriends<IClientFriends>( hUser, hPipe );
            if ( pClientFriends == null )
                throw new SteamException( "Unable to get IClientFriends interface." );


        }

        public static void Shutdown()
        {
            if ( pSteamClient != null )
            {
                if ( hUser != 0 )
                    pSteamClient.ReleaseUser( hPipe, hUser );
                if ( hPipe != 0 )
                    pSteamClient.BReleaseSteamPipe( hPipe );
            }

        }

        public static CSteamID GroupIDToChatID(CSteamID groupID)
        {
            CSteamID chatID = new CSteamID( groupID.AccountID, 0x80000, groupID.AccountUniverse, EAccountType.k_EAccountTypeChat );
            return chatID;
        }

        public static CSteamID ChatIDToGroupID(CSteamID chatID)
        {
            CSteamID groupID = new CSteamID( chatID.AccountID, 0, chatID.AccountUniverse, EAccountType.k_EAccountTypeClan );
            return groupID;
        }

        public static bool IsChatRoomOpened(CSteamID chatID)
        {
            int nbChatRooms = pClientFriends.GetChatRoomCount();
            for ( int i = 0; i < nbChatRooms; i++ )
            {
                UInt64 tmpChatID = 0;
                tmpChatID = pClientFriends.GetChatRoomByIndex(i);
                if ( tmpChatID == chatID.ConvertToUint64() )
                {
                    return true;
                }
            }
            return false;
        }

        public static bool isChatOfficer(CSteamID chatID, CSteamID userID)
        {
            UInt32 mDetails = 0;
            UInt32 mDetailsLocal = 0;
            pClientFriends.BGetChatRoomMemberDetails(chatID, userID, ref mDetails, ref mDetailsLocal);
            if ( mDetails == 1 || mDetails == 2 || mDetails == 8 )
            {
                return true;
            }
            else
            {
                return false;
            }
        }

    }
}

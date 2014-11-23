using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using AntiFlood.Util;
using Steam4NET;

namespace AntiFlood
{
    class Program
    {
        static void Main(string[] args)
        {

            Console.WriteLine( "-------------------------------------------------------------------------" );
            Console.WriteLine( "\t\t\tSteam ChatRoom Antiflood v" + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString() );
            Console.WriteLine( "  Written by Heffebaycay using Steam4NET" );
            Console.WriteLine( "-------------------------------------------------------------------------" );


            Settings sets;

            try
            {
                sets = Settings.Load(Settings.BackingFile);
            }
            catch (FileNotFoundException)
            {
                sets = new Settings();
                sets.Save();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to load settings: " + ex.Message + "\n\nResetting to defaults");
                sets = new Settings();
                sets.Save();
            }

            sets.Check();

            Console.WriteLine( " ------- " );
            Console.WriteLine( "     Using the following settings:" );
            Console.WriteLine( "\tWarning threshold: " + sets.warningLimit );
            Console.WriteLine( "\tFlood delay (milliseconds): " + sets.floodDelay );
            Console.WriteLine( "\tVerbose mode: " + sets.bVerbose );
            Console.WriteLine( "\tScore (Limit = " + sets.warningLimit + " ):" );
            Console.WriteLine( "\t    Flood / Identical Msg :    " + sets.floodScore + " / "
                                                                            + sets.identicalMsgScore );
            Console.WriteLine( " ------- \n" );


            try
            {
                SteamContext.Initialize();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to initialize SteamContext: " + ex.Message + "\n\n" + ex.ToString());
                return;
            }


            // Getting the list of open chatrooms
            List<SteamChatRoom> chatRooms = new List<SteamChatRoom>();
            for (int i = 0; i < SteamContext.pClientFriends.GetChatRoomCount(); i++)
            {
                CSteamID tmpChatId = SteamContext.pClientFriends.GetChatRoomByIndex(i);
                string tmpChatRoomName = SteamContext.pClientFriends.GetChatRoomName(tmpChatId);

                var tmpChatRoom = new SteamChatRoom(tmpChatId.ConvertToUint64(), SteamContext.ChatIDToGroupID(tmpChatId), tmpChatRoomName);

                chatRooms.Add(tmpChatRoom);
            }

            // Default groupID/chatID is the summer camp group: 103582791432188903
            CSteamID groupID = new CSteamID( (UInt64)sets.defaultGroup );
            CSteamID chatID = SteamContext.GroupIDToChatID(groupID);

            if (chatRooms.Count > 0)
            {
                bool bChoiceOK = false;
                while (!bChoiceOK)
                {
                    Console.WriteLine("You are currently in the following chatrooms:");
                    for (int i = 0; i < chatRooms.Count; i++)
                    {
                        Console.WriteLine("\t{0}. {1}", i+1, chatRooms[i].roomName);
                    }
                    Console.WriteLine("Please pick a chatroom to monitor: ");
                    string sChoice = Console.ReadLine();
                    int iChoice = 0;

                    if (Int32.TryParse(sChoice, out iChoice))
                    {
                        iChoice--;
                        if (iChoice < chatRooms.Count)
                        {
                            chatID = new CSteamID(chatRooms[iChoice].chatId);
                            groupID = new CSteamID(chatRooms[iChoice].groupId);
                            bChoiceOK = true;
                        }
                        else
                        {
                            Console.WriteLine("Invalid choice");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Invalid choice");
                    }
                }
            }
            else
            {
                // If the chat room window is not opened, then we open it automatically
                if (!SteamContext.IsChatRoomOpened((chatID)))
                {
                    SteamContext.pClientFriends.JoinChatRoom(chatID);
                }
            }

            string chatName = SteamContext.pClientFriends.GetChatRoomName(chatID);
            int iSleep = 0;
            while (chatName.Length == 0)
            {
                iSleep++;
                if (iSleep == 60)
                {
                    Console.WriteLine("Error: Failed to get chat room name (invalid group?)");
                    return;
                }
                System.Threading.Thread.Sleep(50);
                chatName = SteamContext.pClientFriends.GetChatRoomName(chatID);
            }

            Console.WriteLine("Monitoring chatroom: {0} ({1})", chatName, groupID.ConvertToUint64());

            // Initializing memberlist
            var playerList = new List<SteamPlayer>();

            Console.WriteLine("Started listening to callbacks.\n");
            
            var callbackMsg = new CallbackMsg_t();
            while (true)
            {
                if (Steamworks.GetCallback(SteamContext.hPipe, ref callbackMsg))
                {
                    switch (callbackMsg.m_iCallback)
                    {
                        case ChatMemberStateChange_t.k_iCallback:
                        {
                            var stateChangeInfo =
                                (ChatMemberStateChange_t)
                                    Marshal.PtrToStructure( callbackMsg.m_pubParam, typeof( ChatMemberStateChange_t ) );
                            CSteamID ulSteamIDChat = new CSteamID( stateChangeInfo.m_ulSteamIDChat );
                            if ( ulSteamIDChat != chatID )
                            {
                                // We need to check that the ChatMemberStateChange callback came
                                // from the chatroom we are monitoring
                                break;
                            }

                            EChatMemberStateChange changeType = stateChangeInfo.m_rgfChatMemberStateChange;
                            if ( changeType != EChatMemberStateChange.k_EChatMemberStateChangeEntered )
                            {
                                // User left the chatroom, so we're going to try to remove him from the list
                                var steamID = new CSteamID( stateChangeInfo.m_ulSteamIDUserChanged );
                                uint accountID = steamID.AccountID;
                                int playerIndex = playerList.FindIndex( item => item.accountID == accountID );
                                if ( playerIndex > -1 )
                                {
                                    // Index > -1, so we found a matching player
                                    playerList.RemoveAt( playerIndex );
                                }
                            }
                                
                            break;
                        }
                        case ChatRoomMsg_t.k_iCallback:
                        {
                            var chatMsgInfo =
                                (ChatRoomMsg_t)Marshal.PtrToStructure( callbackMsg.m_pubParam, typeof( ChatRoomMsg_t ) );
                            var ulSteamIDChat = new CSteamID( chatMsgInfo.m_ulSteamIDChat );
                            if ( ulSteamIDChat != chatID )
                            {
                                // We need to check that the ChatMemberStateChange callback came
                                // from the chatroom we are monitoring
                                break;
                            }

                            var chatterID = new CSteamID(chatMsgInfo.m_ulSteamIDUser);

                            EChatEntryType chatType = EChatEntryType.k_EChatEntryTypeInvalid;
                            var steamIDUser = new CSteamID();
                            byte[] msgData = new byte[1024 * 4];

                            int iLength = SteamContext.pClientFriends.GetChatRoomEntry(chatID,
                                (int) chatMsgInfo.m_iChatID, ref steamIDUser , msgData, msgData.Length, ref chatType);

                            iLength = Clamp(iLength, 1, msgData.Length);


                            if (chatType == EChatEntryType.k_EChatEntryTypeEmote ||
                                chatType == EChatEntryType.k_EChatEntryTypeChatMsg)
                            {
                                string msg = Encoding.UTF8.GetString(msgData, 0, iLength);
                                msg = msg.Substring(0, msg.Length - 1);
                                msg = msg.Replace("\b", "");
                                msg = msg.Replace("\u2022", "");
                                int msgHash = msg.GetHashCode();

                                int playerIndex = playerList.FindIndex(item => item.accountID == chatterID.AccountID);
                                bool bNewPlayer = false;
                                if (playerIndex == -1)
                                {
                                    // User not found: we need to add this user to the playerList
                                    bNewPlayer = true;
                                    var tmpPlayer = new SteamPlayer(chatterID.AccountID, DateTime.Now, 0, msgHash);
                                    playerList.Add(tmpPlayer);
                                    playerIndex = playerList.FindIndex(item => item.accountID == chatterID.AccountID);
                                }

                                // Let's check whether the last message from this user was sent fewer than 1500ms ago
                                TimeSpan span = DateTime.Now - playerList[playerIndex].lastMsgTime;
                                string persona = SteamContext.pSteamFriends.GetFriendPersonaName(chatterID);
                                persona = CleanPersona(persona);
                                string timestamp = String.Format("{0}:{1} - ", DateTime.Now.Hour, DateTime.Now.Minute);

                                bool kicked = false;
                                bool warned = false;

                                if (!bNewPlayer)
                                {
                                    if (span.TotalMilliseconds > 3000)
                                    {
                                        playerList[playerIndex].lowerNbFlagged();
                                    }
                                    else if (playerList[playerIndex].lastMessageHash == msgHash)
                                    {
                                        // Message is identical to the last one sent
                                        playerList[playerIndex].raiseNbFlagged(sets.identicalMsgScore);
                                        warned = true;
                                        Console.WriteLine("{0}{1}: {2} warning points (identical msg)", timestamp, persona, playerList[playerIndex].nbFlagged);
                                    }
                                    else if (span.TotalMilliseconds <= sets.floodDelay)
                                    {
                                        playerList[playerIndex].raiseNbFlagged(sets.floodScore);
                                        warned = true;
                                        Console.WriteLine("{0}{1}: {2} warning points (flood)", timestamp, persona, playerList[playerIndex].nbFlagged);

                                    }


                                    if(sets.bBeepOnMention == true && sets.triggers.Any(s => msg.ToLower().Contains(s)))
                                    {
                                        // Trigger !
                                        Console.Beep(5000, 800);
                                        Console.WriteLine("[INFO] You were mentioned in the following message by {0}: ", persona);
                                        Console.WriteLine("\t" + msg);
                                        Console.WriteLine();
                                    }

                                    if (warned && sets.bVerbose)
                                    {
                                        // Verbose mode on: printing message detail
                                        Console.WriteLine("\t{0}: {1}\n", persona, msg);
                                    }

                                    if (playerList[playerIndex].nbFlagged > sets.warningLimit &&
                                        !SteamContext.isChatOfficer(chatID, chatterID))
                                    {
                                        Console.Write("\t\tKicking {0}...", persona);
                                        if (SteamContext.pClientFriends.KickChatMember(chatID, chatterID))
                                        {
                                            Console.Write(" User kicked!\n");
                                            kicked = true;
                                        }
                                        else
                                        {
                                            Console.Write(" Failed!\n");
                                        }
                                    }

                                    if (!kicked)
                                    {
                                        playerList[playerIndex].lastMsgTime = DateTime.Now;
                                        playerList[playerIndex].lastMessageHash = msgHash;
                                    }
                                }
                            }

                            break;
                        }
                    }
                    Steamworks.FreeLastCallback(SteamContext.hPipe);

                }
                System.Threading.Thread.Sleep(5);
            }


        }

        static int Clamp(int value, int min, int max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        static string CleanPersona(string persona)
        {
            string res = persona.Replace("\b", "");
            res = res.Replace("\u2022", "");

            return res;
        }
    }
}

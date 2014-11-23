/// SETTING CLASS : Completely taken from Voided's ChatLog project
namespace AntiFlood
{
    using System;
    using System.Collections.Generic;

    public class Settings : Serializable<Settings>
    {
        public const string BackingFile = "settings.xml";

        public bool bVerbose;
        public int floodDelay;
        public int warningLimit;
        public ulong defaultGroup;

        public int floodScore;
        public int identicalMsgScore;

        public List<string> triggers;
        public bool bBeepOnMention;

        public Settings()
        {
            bVerbose = true;

            floodDelay = 700;

            warningLimit = 12;

            defaultGroup = 103582791432188903;

            floodScore = 3;
            identicalMsgScore = 1;

            bBeepOnMention = false;

            triggers = new List<string>();
            triggers.Add( "heffebaycay" );
            triggers.Add( "heffy" );
        }

        public void Check()
        {
            if ( floodDelay > 2000 )
            {
                floodDelay = 1000;
                Console.WriteLine( "[WARNING] Flood delay value was greater than 2000ms. Value set to 1000ms" );
            }

            for ( int i = 0; i < triggers.Count; i++ )
            {
                triggers[i] = triggers[i].ToLower();
            }
        }


        public bool Save()
        {
            return base.Save( BackingFile );
        }

    }
}

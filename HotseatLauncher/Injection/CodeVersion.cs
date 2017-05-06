using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotseatLauncher
{
    class CodeVersion
    {
        public static IEnumerable<CodeVersion> Versions { get { return versions; } }
        static CodeVersion[] versions = new CodeVersion[]
        {
            new CodeVersion(0)
        };

        public byte ID { get; private set; }
        public string Name { get; private set; }
        public MD5Hash Hash { get; private set; }
        public string InstallRegKey { get; private set; }
        public string InstallRegValue { get; private set; }
        public string ProcessStartPath { get; private set; }

        // code addresses

        public int ShowErrAddress { get; private set; }
        public int WindowedAddress { get; private set; }
        public int NoAudioAddress { get; private set; }

        public int ModAddress { get; private set; }
        public int CampaignAddress { get; private set; }
        public int FactionAddress { get; private set; }

        public int CampaignDifficulty { get; private set; }
        public int BattleDifficulty { get; private set; }
        public int ArcadeBattles { get; private set; }
        public int AutoManage { get; private set; }
        public int NoBattleTimeLimit { get; private set; }
        public int ShortCampaign { get; private set; }

        public int UnlockFactionsAddress { get; private set; }
        public int BetweenFactionTurnsAddress { get; private set; }
        public int ControlFactionSelfCheckAddress { get; private set; }
        public int StartGameAddress { get; private set; }
        public int FinishedLoadingAddress { get; private set; }

        private CodeVersion(byte id)
        {
            this.ID = id;
            switch (id)
            {
                case 0:
                    Name = "Steam - Rome: Total War 1.0";
                    Hash = new MD5Hash(new byte[] { 0xA9, 0xFE, 0x41, 0x88, 0x6B, 0xD4, 0x60, 0x77, 0x5F, 0x68, 0xAA, 0xCA, 0x41, 0xD9, 0xA0, 0xC4 });
                    InstallRegKey = @"HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\Steam App 4760";
                    InstallRegValue = @"InstallLocation";
                    ProcessStartPath = "steam://rungameid/4760";

                    ShowErrAddress = 0x409B64;
                    WindowedAddress = 0x408DF0;
                    NoAudioAddress = 0x408E48;

                    ModAddress = 0x4090D4;
                    CampaignAddress = 0x40946D;
                    FactionAddress = 0x126AE6C;
                    // launch parameter check is never reached: FactionAddress = 0x40B810;

                    CampaignDifficulty = 0x126AE70;
                    BattleDifficulty = 0x126AE74;
                    ArcadeBattles = 0x2943850;
                    AutoManage = 0x29450B6;
                    NoBattleTimeLimit = 0x2945083; 
                    ShortCampaign = 0x294508D;
                    // follow ai characters enabled 0x2945098
                    // advice level 0x126AE8C

                    UnlockFactionsAddress = 0x44B7FB;
                    BetweenFactionTurnsAddress = 0x4CB864; //0x4535DE;
                    // script factionTurnEnd = 0x4CBAEE;
                    // script factionTurnStart = 0x4CB864;
                    // start of faction id increase call : 0x45334A
                    ControlFactionSelfCheckAddress = 0xDE7CD3;
                    StartGameAddress = 0x404FD0; // for loading
                    FinishedLoadingAddress = 0x8C8689;
                    break;
                default:
                    throw new Exception("Unsupported id");
            }
        }
    }
}

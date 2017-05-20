using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;
using System.Windows;

namespace HotseatLauncher
{
    partial class Session
    {
        FileWatcher sessionFileWatcher;

        CodeVersion version;
        public CodeVersion Version { get { return version; } }

        string modName;
        public string ModName { get { return modName; } }
        public ModFolder GetModFolder() { return installation?.GetMod(modName); }

        string campaignName;
        public string CampaignName { get { return campaignName; } }
        public Campaign GetCampaign() { return GetModFolder()?.GetCampaign(campaignName); }

        Difficulty difficulty;
        public Difficulty Difficulty { get { return difficulty; } }

        bool autoSolve;
        public bool AutoSolve { get { return autoSolve; } }

        bool autoManage;
        public bool AutoManage { get { return autoManage; } }

        bool shortCampaign;
        public bool ShortCampaign { get { return shortCampaign; } }

        bool arcadeBattles;
        public bool ArcadeBattles { get { return arcadeBattles; } }

        bool noBattleTimeLimit;
        public bool NoBattleTimeLimit { get { return noBattleTimeLimit; } }

        int turn = 1; // uint?
        public int Turn { get { return turn; } }
        public string TurnString
        {
            get
            {
                if (lastPlayedFactionIndex != 0 && (currentFactionIndex == startFactionIndex || lastPlayedFactionIndex == NextPlayerFaction(startFactionIndex)))
                    return (turn + 1).ToString();

                return turn.ToString();
            }
        }

        byte startFactionIndex;
        public FactionInfo GetStartFaction() { return GetModFolder()?.GetFaction(startFactionIndex); }

        byte lastPlayedFactionIndex;
        public FactionInfo GetLastPlayedFaction() { return GetModFolder()?.GetFaction(lastPlayedFactionIndex); }
        
        void InitGameInfo()
        {
            sessionFileWatcher = new FileWatcher(GameFile);
            sessionFileWatcher.AfterWrite += () => Application.Current.Dispatcher.Invoke((Action)ReadGameFile);
            sessionFileWatcher.AfterDelete += () => Application.Current.Dispatcher.Invoke((Action)ReadGameFile);
            sessionFileWatcher.Enabled = true;
        }

        void SaveGameInfo()
        {
            Directory.CreateDirectory(GamePath);

            // game infos & compressed save
            using (WatcherStream ws = new WatcherStream(sessionFileWatcher, FileMode.Create, FileAccess.Write))
            {
                ws.Write(version);
                ws.Write(modName);
                ws.Write(campaignName);

                // game info
                ws.Write(difficulty);
                ws.Write(autoSolve);
                ws.Write(autoManage);
                ws.Write(shortCampaign);
                ws.Write(arcadeBattles);
                ws.Write(noBattleTimeLimit);
                ws.Write(startFactionIndex);
                ws.Write(lastPlayedFactionIndex);
                ws.Write(turn);
            }
        }

        void ReadGameFile()
        {
            if (File.Exists(GameFile))
            {
                CodeVersion newVersion;
                string newMod, newCampaign;
                using (WatcherStream ws = new WatcherStream(sessionFileWatcher, FileMode.Open, FileAccess.Read))
                {
                    ws.Read(out newVersion);
                    ws.Read(out newMod);
                    ws.Read(out newCampaign);

                    ws.Read(out difficulty);
                    ws.Read(out autoSolve);
                    ws.Read(out autoManage);
                    ws.Read(out shortCampaign);
                    ws.Read(out arcadeBattles);
                    ws.Read(out noBattleTimeLimit);
                    ws.Read(out startFactionIndex);
                    ws.Read(out lastPlayedFactionIndex);
                    ws.Read(out turn);
                }

                if ((version != null && newVersion != version) 
                    || (modName != null && !string.Equals(newMod, modName, StringComparison.OrdinalIgnoreCase)) 
                    || (campaignName != null && !string.Equals(newCampaign, campaignName, StringComparison.OrdinalIgnoreCase))) // version, mod or campaign changed
                {
                    installation = null;
                    throw new NotSupportedException("Player faction should change too!");
                }

                version = newVersion;
                modName = newMod;
                campaignName = newCampaign;
            }
            else
            {
                version = null;
                installation = null;
                modName = null;
                campaignName = null;

                difficulty = Difficulty.Unset;
                autoSolve = false;
                autoManage = false;
                shortCampaign = false;
                startFactionIndex = 0;
                lastPlayedFactionIndex = 0;
            }

            UpdateCurrentFaction();
        }
    }
}

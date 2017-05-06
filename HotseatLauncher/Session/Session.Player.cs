using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace HotseatLauncher
{
    partial class Session
    {
        #region Installation

        Installation installation;
        public Installation Installation { get { return installation; } }

        public bool IsAcceptableInstallation(Installation installation)
        {
            return installation == null || (installation.Version == Version && installation.GetMod(modName)?.GetCampaign(campaignName) != null);
        }

        public void SetInstallation(Installation installation)
        {
            if (!IsAcceptableInstallation(installation))
                installation = null;

            this.installation = installation;
            SavePlayerInfo();

            UpdateCurrentFaction();
        }

        #endregion

        // path to transfer folder
        string transferPath;
        public string TransferPath { get { return this.transferPath; } }

        // name
        string rename;
        public string Rename { get { return rename; } }

        // in case of duplicates, 'name' is changed
        string name;
        public string Name { get { return name; } }

        // our own faction
        byte playerFactionIndex;
        public FactionInfo GetPlayerFaction() { return GetModFolder()?.GetFaction(playerFactionIndex); }
        public string PlayerFactionString { get { return GetPlayerFaction()?.Name; } }

        void SavePlayerInfo()
        {
            Directory.CreateDirectory(PlayerPath);

            using (FileStream fs = new FileStream(PlayerFile, FileMode.Create, FileAccess.Write, FileShare.None))
            using (WatcherStream ws = new WatcherStream(fs))
            {
                // read once
                ws.Write(GamePath);
                ws.Write(installation);

                // info
                ws.Write(playerFactionIndex);
            }
        }

        void ReadPlayerInfoFile(string playerFile)
        {
            if (!File.Exists(playerFile))
                throw new FileNotFoundException("Player info file not found!");

            if (transferPath != null)
                throw new NotSupportedException("Session already has a transfer path!");

            string str;
            using (FileStream fs = new FileStream(playerFile, FileMode.Open, FileAccess.Read))
            using (WatcherStream ws = new WatcherStream(fs))
            {
                ws.Read(out str);
                ws.Read(out installation);

                ws.Read(out playerFactionIndex);
            }

            this.name = Path.GetFileName(str);
            this.transferPath = Path.GetDirectoryName(str);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.ComponentModel;

namespace HotseatLauncher
{
    partial class Session : INotifyPropertyChanged, IDisposable
    {
        #region Sessions Dictionary
        // game path, session
        static Dictionary<string, Session> sessions = new Dictionary<string, Session>(StringComparer.OrdinalIgnoreCase);
        public static IEnumerable<Session> Sessions { get { return sessions.Values; } }

        public static bool ContainsSession(string name)
        {
            return sessions.Values.FirstOrDefault(s => string.Equals(s.Name, name, StringComparison.OrdinalIgnoreCase)) != null;
        }

        #endregion

        byte currentFactionIndex;
        public FactionInfo GetCurrentFaction() { return this.GetModFolder()?.GetFaction(currentFactionIndex); }
        public string CurrentFactionString { get { return GetCurrentFaction()?.Name; } }

        #region Status

        public enum Status
        {
            None,
            Ready,
            Warn,
        }

        public Status State
        {
            get
            {
                if (version != null)
                {
                    if (installation == null)
                        return Status.Warn;

                    if (currentFactionIndex != 0 && currentFactionIndex == playerFactionIndex)
                        return Status.Ready;
                }

                return Status.None;
            }
        }
        public string StateString
        {
            get
            {
                switch (State)
                {
                    case Status.None: return "";
                    case Status.Ready: return "✔";
                    case Status.Warn: return "!";
                    default: return "???";
                }
            }
        }

        #endregion

        public event PropertyChangedEventHandler PropertyChanged;
        public static event PropertyChangedEventHandler sPropertyChanged;

        #region Paths

        public const string PlayerFileName = "Player.bin";
        public const string GameFileName = "Game.bin";
        public const string FactionsFileName = "Factions.bin";

        // some paths
        public string GamePath { get { return Path.Combine(this.transferPath, this.name); } }
        public string GameFile { get { return Path.Combine(GamePath, GameFileName); } }
        public string FactionsFile { get { return Path.Combine(GamePath, FactionsFileName); } }

        public string PlayerPath { get { return Path.GetFullPath(this.rename); } }
        public string PlayerFile { get { return Path.Combine(PlayerPath, PlayerFileName); } }

        #endregion

        #region Constructor

        public void Dispose()
        {
            sessionFileWatcher.Dispose();
            factionsFileWatcher.Dispose();
        }

        #endregion

        #region Creation

        // create a new game session
        public static Session Create(CreationArgs args)
        {
            Session session = new Session();
            session.name = args.Name;
            session.rename = args.Name;
            session.transferPath = args.TransferPath;

            session.installation = args.Installation;
            session.version = args.Installation.Version;
            session.modName = args.Mod.Name;
            session.campaignName = args.Campaign.Name;

            session.difficulty = args.Difficulty;
            session.startFactionIndex = args.StartFaction.Index;
            session.lastPlayedFactionIndex = 0;
            session.playerFactionIndex = args.StartFaction.Index;
            session.autoSolve = args.AutoSolve;
            session.autoManage = args.AutoManage;

            session.takenFactionIndices.Add(args.StartFaction.Index);
            session.freeFactionIndices.AddRange(args.FreeFactions.Where(f => f != args.StartFaction).Select(f => f.Index));

            session.InitGameInfo();
            session.InitFactionsInfo();

            session.SavePlayerInfo();
            session.SaveGameInfo();
            session.SaveFactionsFile();

            session.UpdateCurrentFaction();

            sessions.Add(session.GamePath, session);

            return session;
        }

        // Open from session path
        public static Session AddExisting(string gamePath, string rename)
        {
            gamePath = Path.GetFullPath(gamePath);

            Session session;
            if (sessions.TryGetValue(gamePath, out session))
                return session;

            if (!Directory.Exists(gamePath))
                throw new DirectoryNotFoundException("GamePath does not exist!");

            session = new Session();
            session.name = Path.GetFileName(gamePath);
            session.transferPath = Path.GetDirectoryName(gamePath);
            session.rename = rename;

            session.InitGameInfo();
            session.InitFactionsInfo();

            session.ReadGameFile();
            session.ReadFactionsFile();
            session.SavePlayerInfo();

            sessions.Add(gamePath, session);

            return session;
        }

        // load all local player infos
        public static void LoadSessions()
        {
            foreach (string dir in Directory.EnumerateDirectories(Directory.GetCurrentDirectory()))
            {
                if (sessions.Values.FirstOrDefault(s => string.Equals(s.PlayerPath, dir, StringComparison.OrdinalIgnoreCase)) != null)
                    continue; // already loaded

                string playerFile = Path.Combine(dir, PlayerFileName);
                if (!File.Exists(playerFile))
                    continue;

                Session session = new Session();
                session.rename = Path.GetFileName(dir);
                session.ReadPlayerInfoFile(playerFile);

                session.InitGameInfo();
                session.InitFactionsInfo();

                session.ReadGameFile();
                session.ReadFactionsFile();

                session.SetInstallation(session.installation);

                sessions.Add(session.GamePath, session);
            }
        }

        #endregion

        #region Remove & Delete

        public void Remove(bool deleteSaves)
        {
            ChooseFaction(null);

            DirectoryInfo playerInfoDir = new DirectoryInfo(PlayerPath);

            File.Delete(PlayerFile);

            if (deleteSaves)
            {
                foreach (FileInfo fi in playerInfoDir.EnumerateFiles("*.sav"))
                    fi.Delete();
            }

            if (playerInfoDir.EnumerateFileSystemInfos().FirstOrDefault() == null)
                playerInfoDir.Delete();

            sessions.Remove(this.GamePath);

            this.Dispose();
        }

        public bool HasSaveGamesStored()
        {
            //if (Directory.Exists(PlayerPath))
            //    return Directory.EnumerateFiles(PlayerPath, "*.sav").FirstOrDefault() != null;
            return false;
        }

        #endregion

        #region Start Rome

        public void StartGame()
        {
            File.Delete(OutputSaveGamePath);
            if (lastPlayedFactionIndex != 0)
            {
                if (!DecompressSave())
                {
                    Debug.ShowWarning("No save game found!");
                    return;
                }
            }
            Injection.StartGame(this, Exited);
        }

        void Exited()
        {
            try
            {
                bool saved = CompressSave();

                File.Delete(InputSaveGamePath);
                File.Delete(OutputSaveGamePath);

                if (!saved)
                    return;

                lastPlayedFactionIndex = playerFactionIndex;
                SaveGameInfo();

                UpdateCurrentFaction();
            }
            catch (Exception e)
            {
                Debug.ShowException(e);
            }
        }

        #endregion
    }
}

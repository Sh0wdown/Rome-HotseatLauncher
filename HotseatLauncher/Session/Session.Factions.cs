using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.ComponentModel;

namespace HotseatLauncher
{
    partial class Session
    {
        FileWatcher factionsFileWatcher;

        List<byte> freeFactionIndices = new List<byte>();
        public IEnumerable<FactionInfo> GetFreeFactions() { return GetFactions(freeFactionIndices); }

        List<byte> takenFactionIndices = new List<byte>();
        public IEnumerable<FactionInfo> GetTakenFactions() { return GetFactions(takenFactionIndices); }

        public static event Action<Session> OnFactionsChange;

        byte NextPlayerFaction(byte index, bool all = false)
        {
            index = SwitchIndex(index);
            IEnumerable<byte> indices = takenFactionIndices;
            if (all) indices = indices.Concat(freeFactionIndices);

            byte result = byte.MaxValue;
            foreach (byte i in indices.Select(i => SwitchIndex(i)))
                if (i > index && i < result)
                    result = i;

            if (result == byte.MaxValue)
            {
                result = index;
                foreach (byte i in indices.Select(i => SwitchIndex(i)))
                    if (i < index && i < result)
                        result = i;
            }
            return SwitchIndex(result);
        }

        byte PrevPlayerFaction(byte index, bool all = false)
        {
            index = SwitchIndex(index);
            IEnumerable<byte> indices = takenFactionIndices;
            if (all) indices = indices.Concat(freeFactionIndices);

            byte result = byte.MinValue;
            foreach (byte i in indices.Select(i => SwitchIndex(i)))
                if (i < index && i > result)
                    result = i;

            if (result != byte.MinValue)
            {
                result = index;
                foreach (byte i in indices.Select(i => SwitchIndex(i)))
                    if (i > index && i > result)
                        result = i;
            }
            return SwitchIndex(result);
        }

        IEnumerable<FactionInfo> GetFactions(IEnumerable<byte> indices)
        {
            ModFolder mod = GetModFolder();
            if (mod == null)
                return Enumerable.Empty<FactionInfo>();

            return indices.Select(i => mod.GetFaction(i));
        }

        void InitFactionsInfo()
        {
            factionsFileWatcher = new FileWatcher(FactionsFile);
            factionsFileWatcher.AfterWrite += () => Application.Current.Dispatcher.Invoke((Action)ReadFactionsFile);
            factionsFileWatcher.AfterDelete += () => Application.Current.Dispatcher.Invoke((Action)ReadFactionsFile);
            factionsFileWatcher.Enabled = true;
        }

        void SaveFactionsFile()
        {
            Directory.CreateDirectory(GamePath);

            using (WatcherStream ws = new WatcherStream(factionsFileWatcher, FileMode.Create, FileAccess.Write))
            {
                ws.Write((byte)freeFactionIndices.Count);
                freeFactionIndices.ForEach(i => ws.Write(i));

                ws.Write((byte)takenFactionIndices.Count);
                takenFactionIndices.ForEach(i => ws.Write(i));
            }
        }

        void ReadFactionsFile()
        {
            freeFactionIndices.Clear();
            takenFactionIndices.Clear();

            if (File.Exists(FactionsFile))
            {
                using (WatcherStream ws = new WatcherStream(factionsFileWatcher, FileMode.Open, FileAccess.Read))
                {
                    byte count, index;

                    ws.Read(out count);
                    for (int i = 0; i < count; i++)
                    {
                        ws.Read(out index);
                        freeFactionIndices.Add(index);
                    }

                    ws.Read(out count);
                    for (int i = 0; i < count; i++)
                    {
                        ws.Read(out index);
                        takenFactionIndices.Add(index);
                    }
                }
            }

            // in case of desyncs // fixme: warning
            if (playerFactionIndex != 0)
            {
                if (freeFactionIndices.Contains(playerFactionIndex))
                    playerFactionIndex = 0;
                if (!takenFactionIndices.Contains(playerFactionIndex))
                    takenFactionIndices.Add(playerFactionIndex);
            }

            if (OnFactionsChange != null)
                OnFactionsChange(this);

            UpdateCurrentFaction();
        }

        #region (Current) Faction Stuff

        void UpdateCurrentFaction()
        {
            byte newIndex;
            if (startFactionIndex == 0 || takenFactionIndices.Count == 0)
            {
                newIndex = 0;
            }
            else if (lastPlayedFactionIndex == 0)
            {
                newIndex = startFactionIndex;
            }
            else
            {
                newIndex = NextPlayerFaction(lastPlayedFactionIndex);
            }

            currentFactionIndex = newIndex;

            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(null));

            if (sPropertyChanged != null)
                sPropertyChanged(this, new PropertyChangedEventArgs(null));
        }

        // startfaction is switched with index 1 faction
        public byte SwitchIndex(byte factionIndex)
        {
            if (factionIndex != 0 && startFactionIndex != 0)
            {
                if (factionIndex == startFactionIndex)
                {
                    return 1;
                }
                else if (factionIndex == 1)
                {
                    return startFactionIndex;
                }
            }
            return factionIndex;
        }

        #endregion

        #region Choose Faction

        public void ChooseFaction(FactionInfo faction)
        {
            byte factionIndex = (byte)(faction == null ? 0 : faction.Index);
            if (factionIndex == playerFactionIndex)
                return;

            if (factionIndex == 0)
            {
                freeFactionIndices.Add(playerFactionIndex);
                takenFactionIndices.Remove(playerFactionIndex);
            }
            else
            {
                if (!freeFactionIndices.Contains(factionIndex))
                {
                    Debug.ShowWarning("Faction is already taken!");
                    return;
                }
                if (playerFactionIndex != 0)
                {
                    freeFactionIndices.Add(playerFactionIndex);
                    takenFactionIndices.Remove(playerFactionIndex);
                }
                freeFactionIndices.Remove(factionIndex);
                takenFactionIndices.Add(factionIndex);

                if (lastPlayedFactionIndex == 0 && startFactionIndex == 0)
                {
                    startFactionIndex = factionIndex;
                    SaveGameInfo();
                }
            }

            SaveFactionsFile();

            if (lastPlayedFactionIndex == 0 && playerFactionIndex == startFactionIndex) // needs a new start faction
            {
                if (factionIndex == 0)
                {
                    startFactionIndex = takenFactionIndices.Count > 0 ? takenFactionIndices[0] : (byte)0;
                    SaveGameInfo();
                }
                else
                {
                    startFactionIndex = factionIndex;
                    SaveGameInfo();
                }
            }
            playerFactionIndex = factionIndex;

            SavePlayerInfo();

            UpdateCurrentFaction();
        }

        #endregion
    }
}

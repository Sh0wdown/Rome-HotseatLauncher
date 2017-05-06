using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HotseatLauncher
{
    class FactionInfo
    {
        byte index;
        public byte Index { get { return this.index; } }
        public byte RomeIndex { get { return (byte)(this.index - 1); } }

        string name;
        public string Name { get { return this.name; } }

        private FactionInfo(string name)
        {
            this.name = name;
        }

        public static IEnumerable<FactionInfo> LoadFactions(string modFolder)
        {
            string file = Path.Combine(modFolder, "data/descr_sm_factions.txt");
            if (File.Exists(file))
            {
                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs))
                {
                    FactionInfo faction = null;

                    string line;
                    while ((line = sr.ReadRomeLine()) != null)
                    {
                        if (line.Length == 0)
                            continue;

                        if (line.StartsWith("faction", StringComparison.OrdinalIgnoreCase)) // new faction
                        {
                            if (faction != null) // save current faction
                            {
                                if (faction.index != 0)
                                    yield return faction;
                                faction = null;
                            }

                            string nameEntry = line.Substring("faction".Length);

                            int commaIndex = nameEntry.IndexOf(','); // remove additional information (BI)
                            if (commaIndex >= 0)
                                nameEntry = nameEntry.Remove(commaIndex);

                            nameEntry = nameEntry.Trim();
                            if (nameEntry.Length == 0)
                                continue;

                            faction = new FactionInfo(nameEntry);
                        }
                        else
                        {
                            if (faction == null)
                                continue;

                            if (line.StartsWith("standard_index", StringComparison.OrdinalIgnoreCase))
                            {
                                byte index;
                                if (byte.TryParse(line.Substring("standard_index".Length), out index))
                                    faction.index = (byte)(index + 1);
                            }
                        }
                    }

                    if (faction != null && faction.index != 0)
                        yield return faction;
                }
            }
        }

        public override string ToString()
        {
            return string.Format("FactionInfo [{0}]: {1}", this.index, this.name);
        }
    }
}

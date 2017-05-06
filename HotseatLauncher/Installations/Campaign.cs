using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HotseatLauncher
{
    class Campaign
    {
        string name;
        public string Name { get { return this.name; } }

        List<CampaignFaction> factions;
        public IEnumerable<CampaignFaction> Factions { get { return this.factions; } }

        private Campaign(string name, List<CampaignFaction> factions)
        {
            this.name = name;
            this.factions = factions;
        }

        public static IEnumerable<Campaign> LoadCampaigns(string modFolder, IEnumerable<FactionInfo> factions)
        {
            foreach (string dir in Directory.EnumerateDirectories(Path.Combine(modFolder, "data/world/maps/campaign")))
            {
                string file = Path.Combine(dir, "descr_strat.txt");
                if (!File.Exists(file))
                    continue;

                string campaignName = null;
                List<FactionInfo> playables = new List<FactionInfo>();
                List<FactionInfo> unlockables = new List<FactionInfo>();
                List<FactionInfo> nonPlayables = new List<FactionInfo>();

                using (FileStream fs = new FileStream(file, FileMode.Open, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs))
                {
                    string line;
                    while ((line = sr.ReadRomeLine()) != null)
                    {
                        if (line.Length == 0)
                            continue;

                        if (line.StartsWith("campaign", StringComparison.OrdinalIgnoreCase)) // new faction
                        {
                            campaignName = line.Substring("campaign".Length).Trim();
                        }
                        else if (line.StartsWith("playable", StringComparison.OrdinalIgnoreCase))
                        {
                            ReadFactions(sr, factions, playables);
                        }
                        else if (line.StartsWith("unlockable", StringComparison.OrdinalIgnoreCase))
                        {
                            ReadFactions(sr, factions, unlockables);
                        }
                        else if (line.StartsWith("nonplayable", StringComparison.OrdinalIgnoreCase))
                        {
                            ReadFactions(sr, factions, nonPlayables);
                        }
                    }
                }

                int factionCount = playables.Count + unlockables.Count + nonPlayables.Count;
                if (string.IsNullOrWhiteSpace(campaignName) || factionCount == 0)
                    continue;

                List<CampaignFaction> list = new List<CampaignFaction>(factionCount);
                playables.ForEach(fi => list.Add(new CampaignFaction(fi, true)));
                unlockables.ForEach(fi => list.Add(new CampaignFaction(fi, true)));
                nonPlayables.ForEach(fi => list.Add(new CampaignFaction(fi, false)));
                yield return new Campaign(campaignName, list);
            }
        }

        static void ReadFactions(StreamReader sr, IEnumerable<FactionInfo> factions, List<FactionInfo> output)
        {
            output.Clear();
            string line;
            while ((line = sr.ReadRomeLine()) != null)
            {
                if (line.Length == 0)
                    continue;

                if (string.Equals(line, "end", StringComparison.OrdinalIgnoreCase))
                    break;

                FactionInfo factionInfo = factions.FirstOrDefault(f => string.Equals(line, f.Name, StringComparison.OrdinalIgnoreCase));
                if (factionInfo != null)
                    output.Add(factionInfo);
            }
        }
    }
}

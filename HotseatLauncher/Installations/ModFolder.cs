using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HotseatLauncher
{
    class ModFolder
    {
        public const string DefaultName = "Default";

        string fullPath;
        public string FullPath { get { return this.fullPath; } }

        string name;
        public string Name { get { return this.name; } }

        List<Campaign> campaigns;
        public IEnumerable<Campaign> Campaigns { get { return campaigns; } }

        public Campaign GetCampaign(string name)
        {
            return campaigns.Find(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        List<FactionInfo> factions;
        public IEnumerable<FactionInfo> Factions { get { return factions; } }

        public FactionInfo GetFaction(byte index)
        {
            return factions.Find(f => f.Index == index);
        }

        private ModFolder(string path, string name, List<Campaign> campaigns, List<FactionInfo> factions)
        {
            if (!Directory.Exists(path))
                throw new DirectoryNotFoundException("path");
            if (string.IsNullOrWhiteSpace(name))
                throw new ArgumentException("Name is null or white space.");

            this.fullPath = path;
            this.name = name;

            this.campaigns = campaigns;
            this.factions = factions;
        }

        public static IEnumerable<ModFolder> LoadMods(string path)
        {
            // default game
            ModFolder mod = LoadMod(path, DefaultName);
            if (mod != null)
                yield return mod;
            
            foreach (string dir in Directory.EnumerateDirectories(path))
            {
                mod = LoadMod(dir, Path.GetFileName(dir));
                if (mod != null)
                    yield return mod;
            }
        }

        static ModFolder LoadMod(string path, string name)
        {
            if (!Directory.Exists(Path.Combine(path, "data")))
                return null;

            List<FactionInfo> factions = new List<FactionInfo>(FactionInfo.LoadFactions(path));
            if (factions.Count == 0)
                return null;

            List<Campaign> campaigns = new List<Campaign>(Campaign.LoadCampaigns(path, factions));
            if (campaigns.Count == 0)
                return null;

            return new ModFolder(path, name, campaigns, factions);
        }
    }
}

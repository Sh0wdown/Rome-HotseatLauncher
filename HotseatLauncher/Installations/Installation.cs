using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Security.Cryptography;

namespace HotseatLauncher
{
    class Installation
    {
        string filePath;
        public string FilePath { get { return filePath; } }
        public string DirPath { get { return Path.GetDirectoryName(filePath); } }

        CodeVersion version;
        public CodeVersion Version { get { return version; } }

        List<ModFolder> mods;
        public IEnumerable<ModFolder> Mods { get { return this.mods; } }

        public ModFolder GetMod(string name)
        {
            return mods.Find(m => string.Equals(m.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        private Installation(string filePath, CodeVersion version, IEnumerable<ModFolder> mods)
        {
            this.filePath = filePath;
            this.version = version;
            this.mods = new List<ModFolder>(mods);
        }

        public static Installation LoadInstallation(string filePath)
        {
            if (!string.IsNullOrWhiteSpace(filePath) && File.Exists(filePath))
            {
                MD5Hash hash = new MD5Hash(filePath);

                foreach (CodeVersion version in CodeVersion.Versions)
                {
                    if (version.Hash == hash)
                    {
                        string dirPath = Path.GetDirectoryName(filePath);
                        foreach (Installation i in Settings.Installations)
                            if (string.Equals(i.DirPath, dirPath, StringComparison.OrdinalIgnoreCase))
                            {
                                if (i.version == version)
                                    return null;

                                return new Installation(filePath, version, i.mods);
                            }
                        return new Installation(filePath, version, ModFolder.LoadMods(dirPath));
                    }
                }
            }
            return null;
        }
    }
}

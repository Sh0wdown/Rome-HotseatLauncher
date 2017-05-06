using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace HotseatLauncher
{
    static class Settings
    {
        const string configPath = "HotseatLauncher.cfg";

        #region Installations Dictionary

        static Dictionary<string, Installation> installations = new Dictionary<string, Installation>(StringComparer.OrdinalIgnoreCase);
        public static IEnumerable<Installation> Installations { get { return installations.Values; } }

        public static bool TryGetInstallation(string filePath, out Installation installation)
        {
            return installations.TryGetValue(filePath, out installation);
        }

        #endregion

        public static bool WindowedMode = false;
        public static bool DisableAudio = false;
        public static bool ShowErrors = true;

        public static void AddInstallPaths(string path)
        {
            if (!string.IsNullOrWhiteSpace(path))
            {
                path = Path.GetFullPath(path);
                foreach (string file in Directory.EnumerateFiles(path, "*.exe"))
                    AddInstallation(file);
            }
        }

        public static void AddInstallation(string file)
        {
            if (!installations.ContainsKey(file))
            {
                Installation installation = Installation.LoadInstallation(file);
                if (installation != null)
                    installations.Add(file, installation);
            }
        }

        static string lastTransferPath = "";
        public static string LastTransferPath
        {
            get { return lastTransferPath; }
            set
            {
                if (!string.IsNullOrWhiteSpace(value))
                    lastTransferPath = Path.GetFullPath(value);
            }
        }

        public static void Save()
        {
            try
            {
                using (FileStream fs = new FileStream(configPath, FileMode.Create, FileAccess.Write))
                using (StreamWriter sw = new StreamWriter(fs))
                {
                    sw.WriteLine(WindowedMode);
                    sw.WriteLine(DisableAudio);
                    sw.WriteLine(ShowErrors);

                    sw.WriteLine(lastTransferPath);

                    foreach (string path in installations.Keys)
                        sw.Write(path);
                }
            }
            catch (Exception e)
            {
                Debug.ShowException(e);
            }
        }

        public static void Load()
        {
            try
            {
                if (!File.Exists(configPath))
                    return;

                using (FileStream fs = new FileStream(configPath, FileMode.Open, FileAccess.Read))
                using (StreamReader sr = new StreamReader(fs))
                {
                    bool.TryParse(sr.ReadLine(), out WindowedMode);
                    bool.TryParse(sr.ReadLine(), out DisableAudio);
                    bool.TryParse(sr.ReadLine(), out ShowErrors);

                    LastTransferPath = sr.ReadLine();

                    string line;
                    while ((line = sr.ReadLine()) != null)
                        AddInstallation(line);
                }
            }
            catch (Exception e)
            {
                Debug.ShowException(e);
            }
        }
    }
}

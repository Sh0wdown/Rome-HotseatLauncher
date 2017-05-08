using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Compression;

namespace HotseatLauncher
{
    partial class Session
    {
        // fixme: watch save or put into game info file
        public const string InputSaveFileName = "input.sav";
        public const string OutputSaveFileName = "output.sav";
        public const string SaveFileName = "save.bin";

        public string InputSaveGamePath { get { return Path.Combine(PlayerPath, InputSaveFileName); } }
        public string OutputSaveGamePath { get { return Path.Combine(PlayerPath, OutputSaveFileName); } }
        public string SavePath { get { return Path.Combine(GamePath, SaveFileName); } }

        bool CompressSave()
        {
            if (!File.Exists(OutputSaveGamePath))
                return false;

            using (FileStream output = new FileStream(OutputSaveGamePath, FileMode.Open, FileAccess.Read))
            {
                // read turn number
                output.Position = 3827;
                byte[] buf = new byte[4];
                output.Read(buf, 0, 4);
                turn = BitConverter.ToInt32(buf, 0) + 1;
                output.Position = 0;

                // pack
                using (FileStream save = new FileStream(SavePath, FileMode.Create, FileAccess.Write))
                using (DeflateStream ds = new DeflateStream(save, CompressionMode.Compress))
                {
                    output.CopyTo(ds);
                }
            }
            return true;
        }

        bool DecompressSave()
        {
            if (!File.Exists(SavePath))
                return false;

            using (FileStream input = new FileStream(InputSaveGamePath, FileMode.Create, FileAccess.Write))
            {
                using (FileStream save = new FileStream(SavePath, FileMode.Open, FileAccess.Read))
                using (DeflateStream ds = new DeflateStream(save, CompressionMode.Decompress))
                {
                    ds.CopyTo(input);
                }
                // set faction indices
                input.Position = 1273;
                input.WriteByte(GetCurrentFaction().RomeIndex);
                input.Position = 3508;
                input.WriteByte(GetCurrentFaction().RomeIndex);
            }
            return true;
        }
    }
}

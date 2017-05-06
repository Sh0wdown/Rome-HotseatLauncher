using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotseatLauncher
{
    class CreationArgs : EventArgs
    {
        public Installation Installation;
        public ModFolder Mod;
        public Campaign Campaign;
        public string Name;
        public string TransferPath;
        public Difficulty Difficulty;
        public bool AutoSolve;
        public bool AutoManage;
        public FactionInfo StartFaction;
        public IEnumerable<FactionInfo> FreeFactions;
    }
}

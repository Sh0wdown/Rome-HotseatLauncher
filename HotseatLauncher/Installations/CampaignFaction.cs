using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotseatLauncher
{
    class CampaignFaction
    {
        FactionInfo faction;
        public FactionInfo Faction { get { return faction; } }

        bool playable;
        public bool Playable { get { return playable; } }

        public CampaignFaction(FactionInfo faction, bool playable)
        {
            this.faction = faction;
            this.playable = playable;
        }
    }
}

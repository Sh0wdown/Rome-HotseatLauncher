using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace HotseatLauncher
{
    struct Difficulty
    {
        public const byte Unset = 0xFF;
        public const byte Easy = 0;
        public const byte Medium = 1;
        public const byte Hard = 2;
        public const byte VeryHard = 3;

        byte value;

        public Difficulty(byte value)
        {
            this.value = value < 4 ? value : Unset;
        }

        public override string ToString()
        {
            switch (value)
            {
                case Easy: return "Easy";
                case Medium: return "Medium";
                case Hard: return "Hard";
                case VeryHard: return "Very Hard";
            }
            return "Unset";
        }

        public static implicit operator Difficulty(byte value)
        {
            return new Difficulty(value);
        }

        public static implicit operator byte(Difficulty difficulty)
        {
            return difficulty.value;
        }
    }
}

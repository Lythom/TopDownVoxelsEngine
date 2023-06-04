using System;

namespace Shared
{
    [Flags]
    public enum Equipment
    {
        None = 0,
        Novice = 1,
        Apprentice = 2,
        Mage = 4,
        Master = 8
    }
}
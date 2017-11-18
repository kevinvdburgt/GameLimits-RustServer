using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Kits", "Game Limits", "1.0.0")]
    [Description("The kits module")]

    public class GameLimitsKits : RustPlugin
    {
        private class Kit
        {
            public string command;
            public int cooldown;
        }
    }
}

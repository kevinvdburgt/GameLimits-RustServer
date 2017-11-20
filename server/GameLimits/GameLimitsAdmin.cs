using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;

namespace Oxide.Plugins
{
    [Info("Admin", "Game Limits", "1.0.0")]
    [Description("The admin module")]

    public class GameLimitsAdmin : RustPlugin
    {
        #region Server Events
        void OnRconCommand(IPAddress ip, string command, string[] args)
        {
            if (command == "glpi")
            {
                Puts($"Game Limits Plugin Information:\n\n" +
                    $"Online Player: {BasePlayer.activePlayerList.Count}\n" +
                    $"Sleepers: {BasePlayer.sleepingPlayerList.Count}\n" +
                    $"Active Choppers: {GameObject.FindObjectsOfType<BaseHelicopter>().Length}\n" +
                    $"");

            }
        }
        #endregion
    }
}

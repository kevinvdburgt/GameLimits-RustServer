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
            if (command == "admin.info")
            {
                string onlinePlayerInfo = "";

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (player == null)
                        continue;

                    PlayerInfo info = playerInfo[player.userID];

                    onlinePlayerInfo += $"\nDisplayname: {player.displayName}\n" +
                        $"SteamID: {player.UserIDString}\n" +
                        $"DatabaseID: {info.id}\n" +
                        $"RP: {info.rewardPoints}\n" +
                        //$"subscriptions: {info.subscriptions}\n" +
                        //$"cooldowns: {info.cooldowns}\n" +
                        $"IP Address: {player.net.connection.ipaddress}\n";
                }

                Puts($"Game Limits Plugin Information:\n\n" +
                    $"Online Player: {BasePlayer.activePlayerList.Count}\n" +
                    $"Sleepers: {BasePlayer.sleepingPlayerList.Count}\n" +
                    $"Active Choppers: {GameObject.FindObjectsOfType<BaseHelicopter>().Length}\n" +
                    $"\n" +
                    $"Online player info:{onlinePlayerInfo}");
            }
        }

        [ChatCommand("a")]
        private void OnChatCommandTest(BasePlayer player, string command, string[] args)
        {
            
        }
        #endregion

        #region GUI
        private void CreateGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_admin");

            var container = UI.CreateElementContainer("gl_admin", "0 0 0 .95", "0.2 0.2", "0.8 0.8", true);

            UI.CreatePanel(ref container, "gl_admin", "1 1 1 .01", "0 0", "1 0.96", false);
        }
        #endregion
    }
}

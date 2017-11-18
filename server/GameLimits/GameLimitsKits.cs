using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;

namespace Oxide.Plugins
{
    [Info("Kits", "Game Limits", "1.0.0")]
    [Description("The kits module")]

    public class GameLimitsKits : RustPlugin
    {
        private Dictionary<string, KitItem> kitItems = new Dictionary<string, KitItem>();

        #region Server Events
        private void Init()
        {
            InitializeKits();
        }

        private void OnRconCommand(IPAddress ip, string command, string[] args)
        {
            if (command != "kits.reload")
                return;

            InitializeKits();
        }

        [ChatCommand("k")]
        private void OnChatCommandK(BasePlayer player, string command, string[] args)
        {
            OnChatCommandKitp(player, command, args);
        }

        [ChatCommand("kit")]
        private void OnChatCommandKitp(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            PlayerInfo info = playerInfo[player.userID];

            if (args.Length == 0)
            {
                string kits = "Kits:";

                foreach (KeyValuePair<string, KitItem> item in kitItems)
                {
                    kits += $"\n/kit {item.Key}";

                    if (info.HasSubscription($"kit_{item.Key}") || AllowKit(player, item.Key))
                    {
                        if (info.HasCooldown($"kit_{item.Key}") > 0)
                            kits += $" <color=#aaa>cooldown: {(info.HasCooldown($"kit_{ item.Key}"))} seconds</color>";
                        else
                            kits += $" <color=#0d0>available!</color>";
                    }
                    else
                        kits += " <color=#aaa>Buy kit access in the shop.</color>";
                }

                player.ChatMessage(kits);
            }
            else
                Kit(player, Convert.ToString(args[0]));
        }
        #endregion

        #region Functions
        private bool AllowKit(BasePlayer player, string kit)
        {
            PlayerInfo info = playerInfo[player.userID];

            if (kit == "starter")
                return true;

            if (info.HasSubscription($"kit_{kit}"))
                return true;

            return false;
        }

        private void Kit(BasePlayer player, string kit)
        {
            if (player == null)
                return;

            PlayerInfo info = playerInfo[player.userID];

            // Check if the kit exists
            if (!kitItems.ContainsKey(kit))
            {
                player.ChatMessage("<color=#d00>ERROR</color> That kit does not exists.");
                return;
            }

            KitItem item = kitItems[kit];

            // Check for the permissions
            if (!info.HasSubscription($"kit_{kit}") && !AllowKit(player, kit))
            {
                player.ChatMessage("<color=#d00>ERROR</color> You dont have permissions for this kit, you can buy them in the shop.");
                return;
            }

            // Check for the cooldown
            int cooldown = info.HasCooldown($"kit_{kit}");
            if (cooldown > 0)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> Wait {cooldown} seconds.");
                return;
            }

            // Add the cooldown
            info.AddCooldown($"kit_{kit}", item.cooldown);

            // Execute the command
            string[] commands = item.command.Split('|');
            foreach (string command in commands)
                GameLimits.ExecuteCommand(player, command);
        }

        private void InitializeKits()
        {
            if (!MReady())
            {
                Puts("Waiting for the MySQL server..");
                timer.Once(2f, () => InitializeKits());
                return;
            }

            kitItems.Clear();

            MQuery(MBuild("SELECT * FROM kits;"), records =>
            {
                if (records.Count == 0)
                {
                    Puts("No kit items to load..");
                    return;
                }

                foreach (var record in records)
                {
                    kitItems.Add(Convert.ToString(record["key"]), new KitItem()
                    {
                        command = Convert.ToString(record["command"]),
                        cooldown = Convert.ToInt32(record["cooldown"]),
                        id = Convert.ToInt32(record["id"]),
                    });
                }

                Puts($"Loaded {records.Count} kit items");
            });
        }
        #endregion

        #region Classes
        private class KitItem
        {
            public string command = null;
            public int cooldown = 0;
            public int id = 0;
        }
        #endregion
    }
}

using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using Oxide.Game.Rust.Cui;

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
                // Display the GUI
                CreateGUI(player, 0);

                string kits = "Kits:";

                foreach (KeyValuePair<string, KitItem> item in kitItems)
                {
                    kits += $"\n/kit {item.Key}";

                    if (info.HasSubscription($"kit_{item.Key}") || AllowKit(player, item.Key))
                    {
                        if (info.HasCooldown($"kit_{item.Key}") > 0)
                            kits += $" <color=#aaa>cooldown: {FormatTime(info.HasCooldown($"kit_{ item.Key}"))} seconds</color>";
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

        [ConsoleCommand("kits")]
        private void OnConsoleCommandInfoClose(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            var player = (BasePlayer)arg.Connection.player;

            switch (arg.Args[0])
            {
                case "open":
                    if (arg.Args.Length < 2)
                        break;

                    CreateGUI(player, Convert.ToInt32(arg.Args[1]));
                    break;

                case "close":
                    DestroyGUI(player);
                    break;

                case "activate":
                    if (arg.Args.Length < 3)
                        break;

                    Kit(player, Convert.ToString(arg.Args[1]));

                    CreateGUI(player, 0);
                    break;
            }
        }
        #endregion

        #region Functions
        private bool AllowKit(BasePlayer player, string kit)
        {
            PlayerInfo info = playerInfo[player.userID];

            if (kit == "starter")
                return true;

            if (info.HasSubscription($"kit_{kit}") || info.HasSubscription("vip_pro") || info.HasSubscription("vip_elite"))
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

            // Add logging
            MInsert(MBuild("INSERT INTO kits_history (user_id, kit_id) VALUES (@0, @1);", info.id, item.id));
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

        #region GUI
        void CreateGUI(BasePlayer player, int index = 0)
        {
            DestroyGUI(player);

            PlayerInfo info = playerInfo[player.userID];

            // Main container
            var container = UI.CreateElementContainer("gl_kits", "0 0 0 0.99", "0.35 0.05", "0.65 0.95", true);

            // Close button
            UI.CreateButton(ref container, "gl_kits", "0.8 0.2 0.2 1", "Close", 12, "0.1 0", "0.9 0.04", "kits close");

            // Index buttons
            if (index > 0)
                UI.CreateButton(ref container, "gl_kits", "0.12 0.38 0.57 1", "Prev", 12, "0 0", "0.09 0.04", $"kits open {index - 1}");

            if (kitItems.Count - (index * 15) > 15)
                UI.CreateButton(ref container, "gl_kits", "0.12 0.38 0.57 1", "Next", 12, "0.91 0", "0.997 0.04", $"kits open {index + 1}");

            int i = 0;
            foreach (KeyValuePair<string, KitItem> item in kitItems.Skip(index * 15).Take(15))
            {
                Vector2 dim = new Vector2(1f, 0.05f);
                Vector2 ori = new Vector2(0.0f, 1f);
                float offset = (0.013f + dim.y) * (i + 1);
                Vector2 off = new Vector2(0f, offset);
                Vector2 min = ori - off;
                Vector2 max = min + dim;

                string available = "";
                if (info.HasSubscription($"kit_{item.Key}") || AllowKit(player, item.Key))
                {
                    if (info.HasCooldown($"kit_{item.Key}") > 0)
                        available += $"<color=#aaa>Cooldown: {FormatTime(info.HasCooldown($"kit_{ item.Key}"))}</color>";
                    else
                        available += $"<color=#0d0>Available!</color>";
                }
                else
                    available += "<color=#aaa>Buy kit access in the shop.</color>";

                UI.CreatePanel(ref container, "gl_kits", "1 1 1 0.02", $"{min.x} {min.y}", $"{max.x} {max.y}");
                UI.CreateLabel(ref container, "gl_kits", $"Kit {item.Key}", 12, $"{min.x + 0.02} {min.y + 0.02}", $"{max.x} {max.y}", TextAnchor.MiddleLeft, "1 1 1 1", 0);

                UI.CreateLabel(ref container, "gl_kits", $"{available}", 11, $"{min.x + 0.02} {min.y - 0.02}", $"{max.x} {max.y}", TextAnchor.MiddleLeft, "1 1 1 1", 0);

                UI.CreateButton(ref container, "gl_kits", "0.12 0.38 0.57 1", $"Activate", 12, $"{min.x + 0.8} {min.y + 0.01}", $"{max.x - 0.02} {max.y - 0.01}", $"kits activate {item.Key} {index}");

                i++;
            }

            CuiHelper.AddUi(player, container);
        }


        void DestroyGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_kits");
        }
        #endregion
    }
}

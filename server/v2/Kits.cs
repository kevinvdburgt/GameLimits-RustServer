using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using UnityEngine;
using static Oxide.Plugins.Core;

namespace Oxide.Plugins
{
    [Info("Kits", "Game Limits", "2.0.0")]
    [Description("The kits plugin")]

    public class Kits : RustPlugin
    {
        private Dictionary<string, KitItem> kitItems = new Dictionary<string, KitItem>();

        #region Oxide Hooks
        private void Init()
        {
            LoadKits();
        }

        [ChatCommand("k")]
        private void OnChatCommandK(BasePlayer player, string command, string[] args)
        {
            OnChatCommandKit(player, command, args);
        }

        [ChatCommand("kit")]
        private void OnChatCommandKit(BasePlayer player, string command, string[] args)
        {
            PlayerData.PData pdata = PlayerData.Get(player);
            if (player == null || pdata == null)
                return;

            if (args.Length == 0)
            {
                CreateUI(player);

                string kits = "Kits:";

                foreach (KeyValuePair<string, KitItem> item in kitItems)
                {
                    kits += $"\n/kit {item.Key}";

                    if (pdata.HasSubscription($"kit_{item.Key}") || HasAccess(player, item.Key))
                    {
                        int cooldown = pdata.HasCooldown($"kit_{item.Key}");
                        if (cooldown > 0)
                            kits += $" <color=#aaa>cooldown: {Helper.TimeFormat.Long(cooldown)}</color>";
                        else
                            kits += $" <color=#0d0>available!</color>";
                    }
                    else
                        kits += " <color=#aaa>Buy kit access in the shop.</color>";
                }

                player.ChatMessage(kits);
            }
            else
                ActivateKit(player, Convert.ToString(args[0]));
        }

        [ConsoleCommand("kits")]
        private void OnConsoleCommandInfoClose(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            var player = (BasePlayer)arg.Connection.player;

            switch (arg.Args[0])
            {
                case "close":
                    DestroyUI(player);
                    break;

                case "activate":
                    if (arg.Args.Length != 2)
                        break;

                    ActivateKit(player, Convert.ToString(arg.Args[1]));
                    break;
            }
        }
        #endregion

        #region Functions
        private void LoadKits()
        {
            if (!Database.Ready())
            {
                timer.In(1f, () => LoadKits());
                return;
            }

            kitItems.Clear();

            Database.Query(Database.Build("SELECT * FROM kits;"), records =>
            {
                foreach (var record in records)
                {
                    kitItems.Add(Convert.ToString(record["key"]), new KitItem()
                    {
                        command = Convert.ToString(record["command"]),
                        cooldown = Convert.ToInt32(record["cooldown"]),
                        id = Convert.ToInt32(record["id"]),
                    });
                }

                Puts($"Loaded {records.Count} kits");
            });
        }

        private bool HasAccess(BasePlayer player, string kit)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null)
                return false;

            if (kit == "starter")
                return true;

            if (Helper.HasMinimumVipRank(pdata, "vip_pro") || pdata.HasSubscription($"kit_{kit}"))
                return true;

            return false;
        }

        private void ActivateKit(BasePlayer player, string kit)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null)
                return;

            // Check if the kit exists
            if (!kitItems.ContainsKey(kit))
            {
                player.ChatMessage("<color=#d00>Error</color> thit kit does not exists.");
                return;
            }

            KitItem item = kitItems[kit];

            // Check if the user has access to this kit
            if (!HasAccess(player, kit))
            {
                player.ChatMessage("<color=#d00>Error</color> you dont have permissions for this kit, you can buy them in the shop by typing: /s");
                return;
            }

            // Check for cooldowns
            int cooldown = pdata.HasCooldown($"kit_{kit}");
            if (cooldown > 0)
            {
                player.ChatMessage($"<color=#d00>Error</color> cooldown: {Helper.TimeFormat.Long(cooldown)}.");
                return;
            }

            // Add the cooldown
            pdata.AddCooldown($"kit_{kit}", item.cooldown);

            // Execute the kit commands
            string[] commands = item.command.Split('|');
            foreach (string command in commands)
                Helper.ExecuteCommand(player, command);

            player.ChatMessage($"Kit activated!");

            // Add usage log
            Database.Insert(Database.Build("INSERT INTO kits_history (user_id, kit_id) VALUES (@0, @1);", pdata.id, item.id));

            Puts($"[{pdata.id}:{player.UserIDString}] activated kit {kit}");
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player)
        {
            if (player == null)
                return;

            DestroyUI(player);

            CuiElementContainer container = Helper.UI.Container("ui_kits", "0 0 0 .99", "0.35 0.05", "0.65 0.95", true);

            Helper.UI.Add(player, container);

            UpdateUI(player);
        }

        private void UpdateUI(BasePlayer player, int index = 0)
        {
            Helper.UI.Destroy(player, "ui_kits_toolbar");
            Helper.UI.Destroy(player, "ui_kits_entries");

            // Create the home items themself
            CuiElementContainer entriesContainer = Helper.UI.Container("ui_kits_entries", "0 0 0 0", "0 0.04", "0.997 1", false, "ui_kits");
            int i = 0;
            foreach (var kit in kitItems.Skip(index * 15).Take(15))
            {
                Vector2 dimension = new Vector2(0.997f, 0.05f);
                Vector2 origin = new Vector2(0.0f, 1f);
                Vector2 offset = new Vector2(0f, (0.013f + dimension.y) * (i + 1));
                Vector2 min = origin - offset;
                Vector2 max = min + dimension;

                Helper.UI.Panel(ref entriesContainer, "ui_kits_entries", "1 1 1 0.02", $"{min.x} {min.y}", $"{max.x} {max.y}");
                Helper.UI.Label(ref entriesContainer, "ui_kits_entries", "1 1 1 1", $"Kit {kit.Key}", 12, $"{min.x + 0.02} {min.y}", $"{max.x} {max.y}", TextAnchor.MiddleLeft);
                Helper.UI.Button(ref entriesContainer, "ui_kits_entries", "0.12 0.38 0.57 1", "Activate", 12, $"{min.x + 0.8} {min.y + 0.01}", $"{max.x - 0.02} {max.y - 0.01}", $"kits activate {kit.Key}");

                i++;
            }
            
            // Create toolbar container
            CuiElementContainer toolbarContainer = Helper.UI.Container("ui_kits_toolbar", "0 0 0 0", "0 0", "0.997 0.04", false, "ui_kits");

            if (index > 0)
                Helper.UI.Button(ref toolbarContainer, "ui_kits_toolbar", "0.12 0.38 0.57 1", "< Previous", 12, "0 0", "0.2 0.96", $"kits index {index - 1}");

            if (kitItems.Count - (index * 15) > 15)
                Helper.UI.Button(ref toolbarContainer, "ui_kits_toolbar", "0.12 0.38 0.57 1", "Next >", 12, "0.8 0", "0.997 0.96", $"kits index {index + 1}");

            Helper.UI.Button(ref toolbarContainer, "ui_kits_toolbar", "0.8 0.2 0.2 1", "Close Kits", 12, $"{(index > 0 ? "0.21" : "0")} 0", $"{(kitItems.Count - (index * 15) > 15 ? "0.79" : "0.997")} 0.96", $"kits close");

            Helper.UI.Add(player, entriesContainer);
            Helper.UI.Add(player, toolbarContainer);
        }

        private void DestroyUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_kits");
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

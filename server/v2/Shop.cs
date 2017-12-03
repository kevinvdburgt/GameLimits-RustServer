using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("Shop", "Game Limits", "2.0.0")]
    [Description("The shop plugin")]

    public class Shop : RustPlugin
    {
        private Dictionary<string, ShopItem> items = new Dictionary<string, ShopItem>();

        #region Oxide Hooks
        [ChatCommand("s")]
        private void OnChatCommandS(BasePlayer player, string command, string[] args)
        {
            CreateUI(player);
        }

        [ConsoleCommand("shop")]
        private void OnConsoleCommandShop(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            BasePlayer player = (BasePlayer)arg.Connection.player;

            switch (arg.GetString(0))
            {
                case "close":
                    DestoryUI(player);
                    break;

                case "category":
                    UpdateTabsUI(player, arg.GetString(1, "popular"));
                    break;
            }
        }
        
        private void Init()
        {
            // Test code
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestoryUI(player);
            }

            LoadItems();
        }
        #endregion

        #region Functions
        private void LoadItems()
        {
            if (!Database.Ready())
            {
                timer.In(5f, () => LoadItems());
                return;
            }

            items.Clear();

            Database.Query(Database.Build("SELECT * FROM shop_items ORDER BY sort ASC;"), records =>
            {
                foreach (var record in records)
                {
                    items.Add(Convert.ToString(record["key"]), new ShopItem()
                    {
                        id = Convert.ToInt32(record["id"]),
                        name = Convert.ToString(record["name"]),
                        description = Convert.ToString(record["description"]),
                        image = Convert.ToString(record["image"]),
                        command = Convert.ToString(record["command"]),
                        category = Convert.ToString(record["category"]),
                        price = Convert.ToInt32(record["price"]),
                        type = Convert.ToInt16(record["type"]),
                        amount = Convert.ToInt32(record["amount"]),
                    });
                }

                Puts($"Loaded {records.Count} shop items");
            });
        }
        #endregion

        #region UI
        public void CreateUI(BasePlayer player)
        {
            if (player == null)
                return;

            DestoryUI(player);
            
            // Create the main container
            CuiElementContainer container = Helper.UI.Container("ui_shop", "0 0 0 .99", "0.05 0.05", "0.95 0.95", true);

            // Create a close shop button
            Helper.UI.Button(ref container, "ui_shop", "0.8 0.2 0.2 1", "Close Shop", 12, "0.9 0.96", "0.999 0.999", "shop close");

            // Display the container
            Helper.UI.Add(player, container);

            UpdateTabsUI(player, "popular");
            UpdateStatusUI(player, "You can buy more RP at https://rust.gamelimits.com/");
        }

        public void DestoryUI(BasePlayer player)
        {
            if (player == null)
                return;

            Helper.UI.Destroy(player, "ui_shop");
        }

        public void UpdateTabsUI(BasePlayer player, string category)
        {
            Helper.UI.Destroy(player, "ui_shop_tabs");

            CuiElementContainer container = Helper.UI.Container("ui_shop_tabs", "0 0 0 0", "0 0.96", "0.9 0.999", false, "ui_shop");

            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "Popular", 12, "0.00 0", "0.1 0.97", "shop category popular");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "Weapons", 12, "0.11 0", "0.21 0.97", "shop category weapons");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "Ammunition", 12, "0.22 0", "0.32 0.97", "shop category ammunition");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "Construction", 12, "0.33 0", "0.43 0.97", "shop category construction");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "Tools", 12, "0.44 0", "0.54 0.97", "shop category tools");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "Misc", 12, "0.55 0", "0.65 0.97", "shop category misc");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "Kits", 12, "0.66 0", "0.76 0.97", "shop category kits");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "VIP", 12, "0.77 0", "0.87 0.97", "shop category vip");
            Helper.UI.Button(ref container, "ui_shop_tabs", "0.12 0.38 0.57 1", "VIPx", 12, "0.88 0", "0.98 0.97", "shop category vipx");

            switch (category)
            {
                case "popular":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0 0", "0.1 0.03");
                    break;

                case "weapons":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.11 0", "0.21 0.03");
                    break;

                case "ammunition":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.22 0", "0.32 0.03");
                    break;

                case "construction":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.33 0", "0.43 0.03");
                    break;

                case "tools":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.44 0", "0.54 0.03");
                    break;

                case "misc":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.55 0", "0.65 0.03");
                    break;

                case "kits":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.66 0", "0.76 0.03");
                    break;

                case "vip":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.77 0", "0.87 0.03");
                    break;

                case "vipx":
                    Helper.UI.Panel(ref container, "ui_shop_tabs", "1 1 1 1", "0.88 0", "0.98 0.03");
                    break;
            }

            Helper.UI.Add(player, container);
        }

        public void UpdateStatusUI(BasePlayer player, string message)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null)
                return;

            Helper.UI.Destroy(player, "ui_shop_status");

            CuiElementContainer container = Helper.UI.Container("ui_shop_status", "1 1 1 0.01", "0 0", "0.999 0.04", false, "ui_shop");

            Helper.UI.Label(ref container, "ui_shop_status", "1 1 1 1", $"You have <color=#0E84B7>{pdata.rewardPoints}</color> Reward Points", 12, "0.01 0", "1 1", TextAnchor.MiddleLeft);

            Helper.UI.Label(ref container, "ui_shop_status", "1 1 1 1", message, 12, "0 0", "0.99 1", TextAnchor.MiddleRight);

            Helper.UI.Add(player, container);
        }
        #endregion

        #region Classes
        public class ShopItem
        {
            public int id;
            public string name;
            public string description;
            public string image;
            public string command;
            public string category;
            public int price;
            public short type;
            public int amount;

            public static class Type
            {
                public static short LIST = 0;
                public static short GRID = 1;
            }
        }
        #endregion
    }
}

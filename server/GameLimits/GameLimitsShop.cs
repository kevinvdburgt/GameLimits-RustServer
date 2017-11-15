﻿using static Oxide.Plugins.GameLimits;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Shop", "Game Limits", "1.0.0")]
    [Description("The shop module")]

    public class GameLimitsShop : RustPlugin
    {
        private Dictionary<string, ShopItem> ShopItems = new Dictionary<string, ShopItem>();

        public void InitializeShop()
        {
            ShopItems.Clear();



            // Adding shop items
            ShopItems.Add("vip_default", new ShopItem()
            {
                name = "VIP",
                description = "VIP Member, 10 RP each hour, 50 teleports per day",
                command = "shop buy vip_default",
                price = 200,
                type = ShopItem.Type.GRID,
            });

            ShopItems.Add("vip_pro", new ShopItem()
            {
                name = "VIP Pro",
                description = "VIP Member, 10 RP each hour, 50 teleports per day, access to all kits",
                command = "shop buy vip_pro",
                price = 900,
                type = ShopItem.Type.GRID,
            });

            ShopItems.Add("vip_elite", new ShopItem()
            {
                name = "VIP Elite",
                description = "VIP Member, 10 RP each hour, 50 teleports per day, access to all kits",
                command = "shop buy vip_elite",
                price = 1200,
                type = ShopItem.Type.GRID,
            });
        }

        [ChatCommand("s")]
        private void OnChatCommandS(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            CreateGUI(player, "popular");
        }

        [ChatCommand("shop")]
        private void OnChatCommandShop(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            CreateGUI(player, "popular");
        }

        [ConsoleCommand("shop")]
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

                    CreateGUI(player, arg.Args[1]);
                    break;

                case "close":
                    DestroyGUI(player);
                    break;
            }
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyGUI(player);
            }
        }

        void Init()
        {
            InitializeShop();
        }

        public void CreateGUI(BasePlayer player, string page)
        {
            DestroyGUI(player);

            // Main container
            var container = UI.CreateElementContainer("gl_shop", "0 0 0 .99", "0.05 0.05", "0.95 0.95", true);

            // Close shop button
            UI.CreateButton(ref container, "gl_shop", "0.8 0.2 0.2 1", "Close Shop", 12, "0.9 0.95", "0.999 0.999", "shop close");

            // Pages
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "Popular", 12, $"0 {(page == "popular" ? "0.95" : "0.96")}", "0.1 0.999", "shop open popular");
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "Weapons", 12, $"0.11 {(page == "weapons" ? "0.95" : "0.96")}", "0.21 0.999", "shop open weapons");
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "Ammunition", 12, $"0.22 {(page == "ammunition" ? "0.95" : "0.96")}", "0.32 0.999", "shop open ammunition");
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "Construction", 12, $"0.33 {(page == "construction" ? "0.95" : "0.96")}", "0.43 0.999", "shop open construction");
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "Tools", 12, $"0.44 {(page == "tools" ? "0.95" : "0.96")}", "0.54 0.999", "shop open tools");
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "Misc", 12, $"0.55 {(page == "misc" ? "0.95" : "0.96")}", "0.65 0.999", "shop open misc");
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "Kits", 12, $"0.66 {(page == "kits" ? "0.95" : "0.96")}", "0.76 0.999", "shop open kits");
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "VIP Packages", 12, $"0.77 {(page == "vip_packages" ? "0.95" : "0.96")}", "0.87 0.999", "shop open vip_packages");

            // Create the shop GUI
            var itemContainer = UI.CreateElementContainer("gl_shop_item", "0 0 0 0", "0.0 0.05", "0.999 0.949", true, "gl_shop");
            bool created = CreateShopGUI(player, ref itemContainer, ShopItems);
            if (!created)
                UI.CreateLabel(ref itemContainer, "gl_shop_item", "Failed to load shop items (ERR: MIXED_SHOPITEM_TYPES)", 16, "0 0", "1 1", TextAnchor.MiddleCenter, "1 0 0 1", 1);

            CuiHelper.AddUi(player, container);
            CuiHelper.AddUi(player, itemContainer);
        }

        public void DestroyGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_shop");
        }

        public bool CreateShopGUI(BasePlayer player, ref CuiElementContainer container, Dictionary<string, ShopItem> items)
        {
            if (items.Count == 0)
                return false;

            // Check if all shop items are the same type, if not, it is kinda impossible to render.
            short type = -1;
            foreach (KeyValuePair<string, ShopItem> item in items)
                if (type == -1)
                    type = item.Value.type;
                else if (type != item.Value.type)
                    return false;

            int index = 0;
            foreach (KeyValuePair<string, ShopItem> item in items)
                if (type == ShopItem.Type.LIST)
                    CreateShopListGUI(ref container, index++, item);
                else if (type == ShopItem.Type.GRID)
                    CreateShopGridGUI(ref container, index++, item);

            return true;
        }

        private void CreateShopListGUI(ref CuiElementContainer container, int index, KeyValuePair<string, ShopItem> item)
        {
            Vector2 dim = new Vector2(0.989f, 0.08f);
            Vector2 ori = new Vector2(0.005f, 0.999f);
            float offset = (0.012f + dim.y) * (index + 1);
            Vector2 off = new Vector2(0f, offset);
            Vector2 min = ori - off;
            Vector2 max = min + dim;

            UI.CreatePanel(ref container, "gl_shop_item", "1 1 1 0.02", $"{min.x} {min.y}", $"{max.x} {max.y}");
            UI.CreateLabel(ref container, "gl_shop_item", $"{item.Value.name}", 12, $"{min.x + 0.012} {min.y + 0.026}", $"{max.x} {max.y}", TextAnchor.MiddleLeft, "1 1 1 1", 0);
            UI.CreateLabel(ref container, "gl_shop_item", $"{item.Value.description}", 12, $"{min.x + 0.012} {min.y - 0.026}", $"{max.x} {max.y}", TextAnchor.MiddleLeft, "0.7 0.7 0.7 1", 1);
            UI.CreateButton(ref container, "gl_shop_item", "0.12 0.38 0.57 1", $"Costs: {item.Value.price}", 12, $"{min.x + 0.9} {min.y + 0.02}", $"{max.x - 0.012} {max.y - 0.02}", $"shop buy {item.Key}");
        }

        private void CreateShopGridGUI(ref CuiElementContainer container, int index, KeyValuePair<string, ShopItem> item)
        {
            Vector2 dim = new Vector2(0.105f, 0.23f);
            Vector2 ori = new Vector2(0.007f, 0.75f);
            float offsetX = (0.005f + dim.x) * (index % 9);
            float offsetY = (0.01f + dim.y) * (index / 9);

            Vector2 off = new Vector2(offsetX, -offsetY);
            Vector2 min = ori + off;
            Vector2 max = min + dim;

            UI.CreatePanel(ref container, "gl_shop_item", "1 1 1 0.02", $"{min.x} {min.y}", $"{max.x} {max.y}");
            UI.CreateLabel(ref container, "gl_shop_item", "Shotgun Trap", 12, $"{min.x} {min.y}", $"{max.x} {max.y - 0.08}", TextAnchor.MiddleCenter, "1 1 1 1", 0);
            UI.LoadUrlImage(ref container, "gl_shop_item", "http://i.imgur.com/iNFOxbT.png", $"{min.x + 0.022} {min.y + 0.095}", $"{max.x - 0.022} {max.y - 0.015}");
            UI.CreateButton(ref container, "gl_shop_item", "0.12 0.38 0.57 1", $"Costs: {item.Value.price}", 12, $"{min.x + 0.01} {min.y + 0.017}", $"{max.x - 0.01} {max.y - 0.18}", "shop buy xx");
        }

        public class ShopItem
        {
            public string name = null;
            public string description = null;
            public string image = null;
            public string command = null;
            public string category = null;
            public int price = 0;
            public short type = Type.LIST;
            public int amount = 1;

            public static class Type
            {
                public static short LIST = 0;
                public static short GRID = 1;
            };
        }
    }
}

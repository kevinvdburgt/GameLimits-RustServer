﻿using static Oxide.Plugins.GameLimits;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Shop", "Game Limits", "1.0.0")]
    [Description("The shop module")]

    public class GameLimitsShop : RustPlugin
    {
        private Dictionary<string, ShopItem> ShopItems = new Dictionary<string, ShopItem>();
        
        #region Server Events
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

                case "buy":
                    if (arg.Args.Length < 2)
                        break;

                    BuyItem(player, arg.Args[1]);
                    break;
            }
        }

        void OnRconCommand(IPAddress ip, string command, string[] args)
        {
            if (command != "shop.reload")
                return;

            InitializeShop();
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
        #endregion

        #region GUI
        public void CreateGUI(BasePlayer player, string page)
        {
            DestroyGUI(player);

            if (player == null || !playerInfo.ContainsKey(player.userID))
                return;

            PlayerInfo info = playerInfo[player.userID];

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
            UI.CreateButton(ref container, "gl_shop", "0.12 0.38 0.57 1", "VIP Packages", 12, $"0.77 {(page == "vip" ? "0.95" : "0.96")}", "0.87 0.999", "shop open vip");

            // Create the shop GUI
            var itemContainer = UI.CreateElementContainer("gl_shop_item", "0 0 0 0", "0.0 0.05", "0.999 0.949", true, "gl_shop");
            var filter = ShopItems.Where(item => item.Value.category == page).ToDictionary(p => p.Key, p => p.Value);
            string created = CreateShopGUI(player, ref itemContainer, filter);
            if (created != null)
                UI.CreateLabel(ref itemContainer, "gl_shop_item", $"Failed to load shop items (ERR: {created})", 16, "0 0", "1 1", TextAnchor.MiddleCenter, "1 0 0 1", 1);

            CuiHelper.AddUi(player, container);
            CuiHelper.AddUi(player, itemContainer);

            UpdateInfoGUI(player);

            info.LoadRewardPoints(() => UpdateInfoGUI(player));
        }

        public void DestroyGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_shop");
        }

        public void UpdateInfoGUI(BasePlayer player, string message = null)
        {
            PlayerInfo info = playerInfo[player.userID];

            UI.Destroy(player, "gl_shop_info");

            var container = UI.CreateElementContainer("gl_shop_info", "1 1 1 .01", "0.0 0.0", "0.999 0.05", true, "gl_shop");

            UI.CreateLabel(ref container, "gl_shop_info", $"You have {info.rewardPoints} RP", 14, "0.01 0", "1 1", TextAnchor.MiddleLeft, "1 1 1 1", 0, "gl_shop_info_text");

            if (message != null)
                UI.CreateLabel(ref container, "gl_shop_info", message, 14, "0 0", "0.99 1", TextAnchor.MiddleRight, "1 1 1 1", 0, "gl_shop_info_text");

            CuiHelper.AddUi(player, container);
        }

        public string CreateShopGUI(BasePlayer player, ref CuiElementContainer container, Dictionary<string, ShopItem> items)
        {
            if (items.Count == 0)
                return "NO_ITEMS";

            // Check if all shop items are the same type, if not, it is kinda impossible to render.
            short type = -1;
            foreach (KeyValuePair<string, ShopItem> item in items)
                if (type == -1)
                    type = item.Value.type;
                else if (type != item.Value.type)
                    return "MIXED_SHOPITEM_TYPES";

            int index = 0;
            foreach (KeyValuePair<string, ShopItem> item in items)
                if (type == ShopItem.Type.LIST)
                    CreateShopListGUI(ref container, index++, item);
                else if (type == ShopItem.Type.GRID)
                    CreateShopGridGUI(ref container, index++, item);

            return null;
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
            UI.CreateLabel(ref container, "gl_shop_item", $"{item.Value.name}", 12, $"{min.x} {min.y}", $"{max.x} {max.y - 0.08}", TextAnchor.MiddleCenter, "1 1 1 1", 0);
            if (item.Value.image != null)
                UI.LoadUrlImage(ref container, "gl_shop_item", item.Value.image, $"{min.x + 0.022} {min.y + 0.095}", $"{max.x - 0.022} {max.y - 0.015}");
            if (item.Value.amount > 1)
                UI.CreateLabel(ref container, "gl_shop_item", $"{item.Value.amount}x", 12, $"{min.x} {min.y}", $"{max.x - 0.005} {max.y - 0.005}", TextAnchor.UpperRight, "1 1 1 1", 0);
            UI.CreateButton(ref container, "gl_shop_item", "0.12 0.38 0.57 1", $"Costs: {item.Value.price}", 12, $"{min.x + 0.01} {min.y + 0.017}", $"{max.x - 0.01} {max.y - 0.18}", $"shop buy {item.Key}");
        }
        #endregion

        #region Functions
        public void InitializeShop()
        {
            if (!MReady())
            {
                Puts("Waiting for the MySQL server..");
                timer.Once(2f, () => InitializeShop());
                return;
            }

            ShopItems.Clear();

            MQuery(MBuild("SELECT * FROM shop_items ORDER BY sort ASC;"), records =>
            {
                if (records.Count == 0)
                {
                    Puts("No shop items to load..");
                    return;
                }

                foreach (var record in records)
                {
                    ShopItems.Add(Convert.ToString(record["key"]), new ShopItem()
                    {
                        name = Convert.ToString(record["name"]),
                        description = Convert.ToString(record["description"]),
                        image = Convert.ToString(record["image"]),
                        command = Convert.ToString(record["command"]),
                        category = Convert.ToString(record["category"]),
                        price = Convert.ToInt32(record["price"]),
                        type = Convert.ToInt16(record["type"]),
                        amount = Convert.ToInt32(record["amount"]),
                        id = Convert.ToInt32(record["id"]),
                    });
                }

                Puts($"Loaded {records.Count} shop items");
            });
        }

        public void BuyItem(BasePlayer player, string productId)
        {
            // Check if the item exists
            if (!ShopItems.ContainsKey(productId))
                return;

            PlayerInfo info = playerInfo[player.userID];
            ShopItem product = ShopItems[productId];

            // Check if there are enough reward points on the users account
            MQuery(MBuild("SELECT SUM(points) AS points FROM reward_points WHERE user_id=@0;", info.id), records =>
            {
                if (records.Count == 0)
                {
                    Puts("Failed to fetch when buying item");
                    return;
                }

                int points = 0;
                if (records[0]["points"].ToString().Length > 0)
                    points = Convert.ToInt32(records[0]["points"]);

                if (product.price > points)
                {
                    UpdateInfoGUI(player, $"<color=#B70E30>Not enough Reward Points</color> visit https://rust.gamelimits.com/ to buy Reward Points.");
                    return;
                }

                MInsert(MBuild("INSERT INTO reward_points (user_id, points, description) VALUES (@0, @1, @2);", info.id, -product.price, $"Bought {product.name} from the shop"), done =>
                {
                    string[] commands = product.command.Split('|');
                    foreach (string command in commands)
                        GameLimits.ExecuteCommand(player, command);

                    info.LoadRewardPoints(() => UpdateInfoGUI(player, $"<color=#0E84B7>Bought</color> {product.name}"));
                });

                MInsert(MBuild("INSERT INTO shop_history (user_id, shop_item_id) VALUES (@0, @1);", info.id, product.id));
            });
        }
        #endregion

        #region Classes
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
            public int id = 0;

            public static class Type
            {
                public static short LIST = 0;
                public static short GRID = 1;
            };
        }
        #endregion
    }
}

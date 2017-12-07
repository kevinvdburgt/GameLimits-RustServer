using Oxide.Game.Rust.Cui;
using System.Net;
using static Oxide.Plugins.Core;

namespace Oxide.Plugins
{
    [Info("Info", "Game Limits", "2.0.0")]
    [Description("The info plugin")]

    public class Info : RustPlugin
    {
        #region Oxide Hooks
        [ChatCommand("i")]
        private void OnChatCommantI(BasePlayer player, string command, string[] args)
        {
            OnChatCommantInfo(player, command, args);
        }

        [ChatCommand("info")]
        private void OnChatCommantInfo(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            CreateUI(player);
        }

        [ConsoleCommand("cinfo")]
        private void OnConsoleCommandCinfo(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            BasePlayer player = (BasePlayer)arg.Connection.player;
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null)
                return;

            switch (arg.GetString(0))
            {
                case "close":
                    DestroyUI(player);
                    break;

                case "page":
                    UpdatePageUI(player, arg.GetString(1, "welcome"));
                    break;

                case "settings":
                    pdata.SetSetting(arg.GetString(1), arg.GetString(2));
                    UpdateSettingsUI(player);
                    break;
            }
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player, string page = "welcome")
        {
            if (player == null)
                return;

            DestroyUI(player);

            // Create the main container
            CuiElementContainer container = Helper.UI.Container("ui_info", "0 0 0 .99", "0.05 0.05", "0.95 0.95", true);

            // Create a close shop button
            Helper.UI.Button(ref container, "ui_info", "0.8 0.2 0.2 1", "Close", 12, "0.9 0.96", "0.999 0.999", "cinfo close");

            // Display the container
            Helper.UI.Add(player, container);

            UpdatePageUI(player, page);
        }

        private void DestroyUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_info");
        }

        private void UpdatePageUI(BasePlayer player, string page)
        {
            Helper.UI.Destroy(player, "ui_info_tabs");
            Helper.UI.Destroy(player, "ui_info_page");

            CuiElementContainer container = Helper.UI.Container("ui_info_tabs", "0 0 0 0", "0 0.96", "0.9 0.999", false, "ui_info");
            CuiElementContainer containerPage = Helper.UI.Container("ui_info_page", "0 0 0 0", "0 0", "0.999 0.955", false, "ui_info");

            Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Welcome", 12, "0.00 0", "0.1 0.97", "cinfo page welcome");
            Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Rules", 12, "0.11 0", "0.21 0.97", "cinfo page rules");
            Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Commands", 12, "0.22 0", "0.32 0.97", "cinfo page commands");
            //Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Construction", 12, "0.33 0", "0.43 0.97", "shop category construction");
            //Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Tools", 12, "0.44 0", "0.54 0.97", "shop category tools");
            //Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Components", 12, "0.55 0", "0.65 0.97", "shop category components");
            //Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Misc", 12, "0.66 0", "0.76 0.97", "shop category misc");
            //Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Kits", 12, "0.77 0", "0.87 0.97", "shop category kits");
            Helper.UI.Button(ref container, "ui_info_tabs", "0.12 0.38 0.57 1", "Settings", 12, "0.88 0", "0.98 0.97", "cinfo page settings");

            switch (page)
            {
                case "welcome":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0 0", "0.1 0.03");
                    break;

                case "rules":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.11 0", "0.21 0.03");
                    break;

                case "commands":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.22 0", "0.32 0.03");
                    break;

                case "construction":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.33 0", "0.43 0.03");
                    break;

                case "tools":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.44 0", "0.54 0.03");
                    break;

                case "components":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.55 0", "0.65 0.03");
                    break;

                case "misc":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.66 0", "0.76 0.03");
                    break;

                case "kits":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.77 0", "0.87 0.03");
                    break;

                case "settings":
                    Helper.UI.Panel(ref container, "ui_info_tabs", "1 1 1 1", "0.88 0", "0.98 0.03");
                    break;
            }

            Helper.UI.Add(player, container);

            UpdateContentUI(player, page, ref containerPage);

            Helper.UI.Add(player, containerPage);

            if (page == "settings")
                UpdateSettingsUI(player);

            // UpdateItemsUI(player, category);
        }

        private void UpdateContentUI(BasePlayer player, string page, ref CuiElementContainer container)
        {
            Helper.UI.Destroy(player, "ui_info_settings_1");
            Helper.UI.Destroy(player, "ui_info_settings_2");
            Helper.UI.Destroy(player, "ui_info_settings_3");

            string text;

            switch (page)
            {
                case "welcome":
                    text = $"Hi there <color=#f00>{player.displayName}</color>, welcome to the Game Limits Rust server!\n" +
                        $"\n" +
                        $"Wiped on: {Core.Data.currentWipe}\n" +
                        $"Next wipe: {Core.Data.nextWipe}\n" +
                        $"\n" +
                        $"Visit <color=#0E84B7>rust.gamelimits.com</color> for more information - and our mods\n" +
                        $"\n" +
                        $"\n" +
                        $"Join our discord server: <color=#0E84B7>rust.gamelimits.com/discord</color>" +
                        $"\n" +
                        $"\n" +
                        $"Bug Bounty: found an exploit or bug that significantly influences gameplay? Report to an admin and receive 500 RP!";

                    Helper.UI.Label(ref container, "ui_info_page", "1 1 1 1", text, 14, "0.01 0.01", "0.99 0.99", UnityEngine.TextAnchor.UpperLeft);
                    break;

                case "rules":
                    text = $"We don't have many rules on this server, but we do ask you not to:\n" +
                        $"\n" +
                        $" - Spam in the char;\n" +
                        $" - Advertise any goods, services, other resources, media or events not releated to the game;\n" +
                        $" - Hack, cheat or script;\n" +
                        $" - Use glitches to get an unfair advantage.\n" +
                        $"\n" +
                        $"We might ban you if we think it's necessary. Ban appeals can be requested in our Discord server.";

                    Helper.UI.Label(ref container, "ui_info_page", "1 1 1 1", text, 14, "0.01 0.01", "0.99 0.99", UnityEngine.TextAnchor.UpperLeft);
                    break;

                case "commands":
                    text = "Our server commands:\n" +
                        "\n" +
                        "<color=#aaa>/i /info</color> Opens this information window\n" +
                        "<color=#aaa>/s /shop</color> Opens the ingame shop\n" +
                        "<color=#aaa>/f /friend</color> Opens the friend menu\n" +
                        "<color=#aaa>/r /remove</color> Remove placed entities\n" +
                        "<color=#aaa>/k /kit</color> Use kits\n" +
                        "<color=#aaa>/h /home</color> Manage home locations and teleportation\n" +
                        "\n" +
                        "For a more detailed overview of our commands, visit our site at <color=#0E84B7>rust.gamelimits.com/mods</color>";

                    Helper.UI.Label(ref container, "ui_info_page", "1 1 1 1", text, 14, "0.01 0.01", "0.99 0.99", UnityEngine.TextAnchor.UpperLeft);
                    break;
            }
        }

        private void UpdateSettingsUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_info_settings_1");
            Helper.UI.Destroy(player, "ui_info_settings_2");
            Helper.UI.Destroy(player, "ui_info_settings_3");

            PlayerData.PData pdata = PlayerData.Get(player);

            if (pdata == null)
                return;

            CuiElementContainer row1 = Helper.UI.Container("ui_info_settings_1", "0 0 0 0", "0 0", "0.333 0.955", false, "ui_info");
            CuiElementContainer row2 = Helper.UI.Container("ui_info_settings_2", "0 0 0 0", "0.333 0", "0.666 0.955", false, "ui_info");
            CuiElementContainer row3 = Helper.UI.Container("ui_info_settings_3", "0 0 0 0", "0.666 0", "0.999 0.955", false, "ui_info");

            Helper.AnchorPosition.Anchor anchor;

            // Countdown notification
            anchor = Helper.AnchorPosition.List(0, 0.05f, 0.975f, 0.01f, 0.02f);
            Helper.UI.Panel(ref row1, "ui_info_settings_1", "1 1 1 .01", anchor.smin, anchor.smax);
            Helper.UI.Label(ref row1, "ui_info_settings_1", "1 1 1 1", "Display countdown notifications", 12, $"{anchor.vmin.x + 0.02f} {anchor.vmin.y}", $"{anchor.vmax.x} {anchor.vmax.y}", UnityEngine.TextAnchor.MiddleLeft);
            if (pdata.displayTimedNotifications)
                Helper.UI.Button(ref row1, "ui_info_settings_1", "0.41 0.5 0.25 1", "Yes", 12, $"{anchor.vmin.x + 0.8f} {anchor.vmin.y + 0.01}", $"{anchor.vmax.x - 0.02} {anchor.vmax.y - 0.01}", $"cinfo settings displayTimedNotifications false");
            else
                Helper.UI.Button(ref row1, "ui_info_settings_1", "0.57 0.21 0.11 1", "No", 12, $"{anchor.vmin.x + 0.8f} {anchor.vmin.y + 0.01}", $"{anchor.vmax.x - 0.02} {anchor.vmax.y - 0.01}", $"cinfo settings displayTimedNotifications true");

            //// Countdown notifications - Teleport
            //if (pdata.displayTimedNotifications)
            //{
            //    anchor = Helper.AnchorPosition.List(1, 0.05f, 0.975f, 0.01f, 0.02f);
            //    Helper.UI.Panel(ref row1, "ui_info_settings_1", "1 1 1 .01", anchor.smin, anchor.smax);
            //    Helper.UI.Label(ref row1, "ui_info_settings_1", "1 1 1 1", "Display countdown notifications (teleport cooldown)", 12, $"{anchor.vmin.x + 0.02f} {anchor.vmin.y}", $"{anchor.vmax.x} {anchor.vmax.y}", UnityEngine.TextAnchor.MiddleLeft);
            //    if (true)
            //        Helper.UI.Button(ref row1, "ui_info_settings_1", "0.41 0.5 0.25 1", "Yes", 12, $"{anchor.vmin.x + 0.8f} {anchor.vmin.y + 0.01}", $"{anchor.vmax.x - 0.02} {anchor.vmax.y - 0.01}", $"cinfo settings displayTimedNotifications false");
            //    else
            //        Helper.UI.Button(ref row1, "ui_info_settings_1", "0.57 0.21 0.11 1", "No", 12, $"{anchor.vmin.x + 0.8f} {anchor.vmin.y + 0.01}", $"{anchor.vmax.x - 0.02} {anchor.vmax.y - 0.01}", $"cinfo settings displayTimedNotifications true");

            //    anchor = Helper.AnchorPosition.List(2, 0.05f, 0.975f, 0.01f, 0.02f);
            //    Helper.UI.Panel(ref row1, "ui_info_settings_1", "1 1 1 .01", anchor.smin, anchor.smax);
            //    Helper.UI.Label(ref row1, "ui_info_settings_1", "1 1 1 1", "Display countdown notifications (kit cooldown)", 12, $"{anchor.vmin.x + 0.02f} {anchor.vmin.y}", $"{anchor.vmax.x} {anchor.vmax.y}", UnityEngine.TextAnchor.MiddleLeft);
            //    if (false)
            //        Helper.UI.Button(ref row1, "ui_info_settings_1", "0.41 0.5 0.25 1", "Yes", 12, $"{anchor.vmin.x + 0.8f} {anchor.vmin.y + 0.01}", $"{anchor.vmax.x - 0.02} {anchor.vmax.y - 0.01}", $"cinfo settings displayTimedNotifications false");
            //    else
            //        Helper.UI.Button(ref row1, "ui_info_settings_1", "0.57 0.21 0.11 1", "No", 12, $"{anchor.vmin.x + 0.8f} {anchor.vmin.y + 0.01}", $"{anchor.vmax.x - 0.02} {anchor.vmax.y - 0.01}", $"cinfo settings displayTimedNotifications true");
            //}

            Helper.UI.Add(player, row1);
            Helper.UI.Add(player, row2);
            Helper.UI.Add(player, row3);
        }
        #endregion

        #region Classes
        
        #endregion
    }
}

﻿using static Oxide.Plugins.GameLimits;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Info", "Game Limits", "1.0.0")]
    [Description("The info module")]

    public class GameLimitsInfo : RustPlugin
    {
        #region Server Events
        [ChatCommand("i")]
        private void OnChatCommandI(BasePlayer player, string command, string[] args)
        {
            OnChatCommandInfo(player, command, args);
        }

        [ChatCommand("info")]
        private void OnChatCommandInfo(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            CreateGUI(player);
        }

        [ConsoleCommand("info")]
        private void OnConsoleCommandInfo(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            BasePlayer player = (BasePlayer)arg.Connection.player;

            switch (arg.Args[0])
            {
                case "open":
                    CreateGUI(player, arg.GetString(1, "general"));
                    break;

                case "close":
                    DestroyGUI(player);
                    break;
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player == null)
                return;

            if (player.HasPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot))
            {
                timer.Once(2f, () => OnPlayerInit(player));
                return;
            }

            CreateGUI(player);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
                if (player != null)
                    DestroyGUI(player);
        }
        #endregion

        private void Content(ref CuiElementContainer container, BasePlayer player, string page)
        {
            string text = "";
            switch (page)
            {
                case "general":
                    text = $"Hi <color=#f00>{player.displayName}</color>, welcome to the Game Limits Rust server!\n\n" +
                        "Wiped on: 16 november\nNext wipe: 7 december\n\n" +
                        "This server is still under heavy development!\n\n" +
                        "Visit <color=#0E84B7>rust.gamelimits.com</color> for more information.\n\n" +
                        "Bug Bounty: found an exploit or bug that significantly influences gameplay? Report to an admin and receive 500 RP!";

                    UI.CreateLabel(ref container, "gl_info", text, 14, "0.01 0.01", "0.99 0.92", TextAnchor.UpperLeft, "1 1 1 1");
                    break;

                case "rules":
                    text = "We don't have many rules, but we do ask you to not:\n\n" +
                        " - Spam the chat;\n" +
                        " - Advertise any goods, services, other resources, media or events not releated to the game;\n" +
                        " - Hack, cheat or script;\n" +
                        " - Use glitches to get an unfair advantage.\n\n" +
                        "We might ban you if we think it's necessary. Ban appeals can be requested in our Discord server.";

                    UI.CreateLabel(ref container, "gl_info", text, 14, "0.01 0.01", "0.99 0.92", TextAnchor.UpperLeft, "1 1 1 1");
                    break;

                case "commands":
                    text = "Our server commands:\n\n" +
                        "<color=#aaa>/i /info</color> Opens this information window\n" +
                        "<color=#aaa>/s /shop</color> Opens the ingame shop\n" +
                        "<color=#aaa>/f /friend</color> Opens the friend menu\n" +
                        "<color=#aaa>/r /remove</color> Remove placed entities\n" +
                        "<color=#aaa>/k /kit</color> Use kits\n" +
                        "<color=#aaa>/h /home</color> Manage home locations and teleportation\n" +
                        // "<color=#aaa>/t /tpr \"name\"</color> Requests a teleport to another player\n" +
                        "";

                    UI.CreateLabel(ref container, "gl_info", text, 14, "0.01 0.01", "0.99 0.92", TextAnchor.UpperLeft, "1 1 1 1");
                    break;

                default:
                    text = $"Sorry. the page <color=#f00>{page}</color> does not exists :(";
                    UI.CreateLabel(ref container, "gl_info", text, 14, "0.01 0.01", "0.99 0.92", TextAnchor.MiddleCenter, "1 1 1 1");
                    break;
            }
        }

        public void CreateGUI(BasePlayer player, string page = "general")
        {
            DestroyGUI(player);

            // Main container
            var container = UI.CreateElementContainer("gl_info", "0 0 0 .99", "0.05 0.05", "0.95 0.95", true);

            // Close Info Button
            UI.CreateButton(ref container, "gl_info", "0.8 0.2 0.2 1", "Close", 12, "0.9 0.96", "0.999 0.999", "info close");

            // Pages
            UI.CreateButton(ref container, "gl_info", "0.12 0.38 0.57 1", "General", 12, $"0 {(page == "general" ? "0.95" : "0.96")}", "0.1 0.999", "info open general");
            UI.CreateButton(ref container, "gl_info", "0.12 0.38 0.57 1", "Rules", 12, $"0.11 {(page == "rules" ? "0.95" : "0.96")}", "0.21 0.999", "info open rules");
            UI.CreateButton(ref container, "gl_info", "0.12 0.38 0.57 1", "Commands", 12, $"0.22 {(page == "commands" ? "0.95" : "0.96")}", "0.32 0.999", "info open commands");
            //UI.CreateButton(ref container, "gl_info", "0.12 0.38 0.57 1", "Website", 12, $"0.33 {(page == "website" ? "0.95" : "0.96")}", "0.43 0.999", "info open website");
            //UI.CreateButton(ref container, "gl_info", "0.12 0.38 0.57 1", "Reward Points", 12, $"0.44 {(page == "rp" ? "0.95" : "0.96")}", "0.54 0.999", "info open rp");
            //UI.CreateButton(ref container, "gl_info", "0.12 0.38 0.57 1", "Game Limits", 12, $"0.55 {(page == "gamelimits" ? "0.95" : "0.96")}", "0.65 0.999", "info open gamelimits");

            // Content
            Content(ref container, player, page);

            CuiHelper.AddUi(player, container);
        }

        public void DestroyGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "gl_info");
        }
    }
}

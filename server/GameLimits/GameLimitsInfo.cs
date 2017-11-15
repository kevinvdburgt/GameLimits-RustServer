using static Oxide.Plugins.GameLimits;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Info", "Game Limits", "1.0.0")]
    [Description("The info module")]

    public class GameLimitsInfo : RustPlugin
    {
        [ChatCommand("info")]
        private void OnChatCommandInfo(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            CreateGUI(player);
        }

        [ConsoleCommand("info_close")]
        private void OnConsoleCommandInfoClose(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null)
                return;

            var player = (BasePlayer)arg.Connection.player;

            DestroyGUI(player);
        }

        [ConsoleCommand("info_open")]
        private void OnConsoleCommandInfoSwitch(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            var player = (BasePlayer)arg.Connection.player;

            CreateGUI(player, arg.GetString(0, ""));
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
            {
                DestroyGUI(player);
            }
        }

        private string GeneratePage(BasePlayer player, string page)
        {
            switch (page)
            {
                case "general":
                    return $"Hi <color=#f00>{player.displayName}</color>, welcome to the Game Limits Rust server!\n\n" +
                        "Wiped on: 11 november\nNext wipe: 14 november";

                case "rules":
                    return "We dont have a lot of rules, however the following is strictly forbidden and will results in a instant ban by our automated systems:\n\n" +
                        " - Spamming the chat.\n" +
                        " - Advertising of any goods or services, other resources, media or events not releated to the game.\n" +
                        " - Hacking, cheating or scripting.\n" +
                        " - Using glitched to get an unfair advantage.";

                case "commands":
                    return "<color=#888>/info</color> opens this screen.";

                case "website":
                    return "Visit our website as https://rust.gamelimits.com/\n\n" +
                        "You also can read the chatlogs over there";

                default:
                    return $"Sorry. the page <color=#f00>{page}</color> does not exists :(";
            }
        }

        public void CreateGUI(BasePlayer player, string page = "general")
        {
            DestroyGUI(player);

            var container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 .99",
                        },
                        RectTransform = {
                            AnchorMin = "0.05 0.05",
                            AnchorMax = "0.95 0.95"
                        },
                        CursorEnabled = true
                    },
                    new CuiElement().Parent = "Overall",
                    "gl_info"
                }
            };

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "info_close",
                    Color = "0.8 0.2 0.2 1",
                },
                RectTransform =
                {
                    AnchorMin = "0.9 0.95",
                    AnchorMax = "0.999 0.999"
                },
                Text =
                {
                    Text = "Close",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter
                }
            }, "gl_info");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "info_open general",
                    Color = "0.12 0.38 0.57 1",
                },
                RectTransform =
                {
                    AnchorMin = "0 0.95",
                    AnchorMax = "0.1 0.999"
                },
                Text =
                {
                    Text = "General",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter
                }
            }, "gl_info");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "info_open rules",
                    Color = "0.12 0.38 0.57 1",
                },
                RectTransform =
                {
                    AnchorMin = "0.11 0.95",
                    AnchorMax = "0.21 0.999"
                },
                Text =
                {
                    Text = "Rules",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter
                }
            }, "gl_info");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "info_open commands",
                    Color = "0.12 0.38 0.57 1",
                },
                RectTransform =
                {
                    AnchorMin = "0.22 0.95",
                    AnchorMax = "0.32 0.999"
                },
                Text =
                {
                    Text = "Commands",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter
                }
            }, "gl_info");

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = "info_open website",
                    Color = "0.12 0.38 0.57 1",
                },
                RectTransform =
                {
                    AnchorMin = "0.33 0.95",
                    AnchorMax = "0.43 0.999"
                },
                Text =
                {
                    Text = "Website",
                    FontSize = 12,
                    Align = TextAnchor.MiddleCenter
                }
            }, "gl_info");

            container.Add(new CuiLabel
            {
                Text =
                {
                    Text = GeneratePage(player, page),
                    FontSize = 16,
                    Align = TextAnchor.UpperLeft
                },
                RectTransform =
                {
                    AnchorMin = "0.01 0.01",
                    AnchorMax = "0.99 0.92"
                }
            }, "gl_info");

            CuiHelper.AddUi(player, container);
        }

        public void DestroyGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "gl_info");
        }
    }
}
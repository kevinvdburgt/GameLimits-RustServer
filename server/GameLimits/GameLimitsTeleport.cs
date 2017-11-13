using Oxide.Game.Rust.Cui;
using System;
using System.Collections.Generic;
using static Oxide.Plugins.GameLimits;

namespace Oxide.Plugins
{
    [Info("Teleport", "Game Limits", "1.0.0")]
    [Description("The teleport module")]

    public class GameLimitsTeleport : RustPlugin
    {
        void Init()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                CreateTeleportRequestGUI(player);
            }
        }

        [ChatCommand("tpr")]
        private void OnChatCommandInfo(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            CreateTeleportRequestGUI(player);
        }

        void Unload()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                DestroyTeleportRequestGUI(player);
            }
        }

        [ConsoleCommand("gl_teleport_request_ui")]
        private void OnConsoleCommandInfoClose(ConsoleSystem.Arg args)
        {
            if (args.Connection == null || args.Connection.player == null)
                return;

            var player = (BasePlayer)args.Connection.player;

            switch (args.Args[0])
            {
                case "page":
                    int index = Convert.ToInt32(args.Args[1]);

                    if (index < 0)
                        return;

                    CreateTeleportRequestGUI(player, index);
                    break;

                case "close":
                    DestroyTeleportRequestGUI(player);
                    break;
            }

        }

        #region GUI Teleport Request
        void CreateTeleportRequestGUI(BasePlayer player, int index = 0)
        {
            DestroyTeleportRequestGUI(player);
            
            List<string> targets = new List<string>();

            foreach (var p in BasePlayer.activePlayerList)
                targets.Add(p.displayName);

            var container = new CuiElementContainer
            {
                {
                    new CuiPanel
                    {
                        Image =
                        {
                            Color = "0 0 0 .99"
                        },
                        RectTransform =
                        {
                            AnchorMin = "0.4 0.2",
                            AnchorMax = "0.6 0.9"
                        },
                        CursorEnabled = true
                    },
                    new CuiElement().Parent = "Overall",
                    "gl_teleport_request"
                }
            };

            int i = 0;
            foreach (var target in targets.GetRange(20 * index, 20 * index + 20))
            {
                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"gl_teleport_request_ui tpr {target}",
                        Color = "0.12 0.38 0.57 1"
                    },
                    RectTransform =
                    {
                        AnchorMin = "0.02 " + (1 - (i * 0.05) - 0.05).ToString("0.####"),
                        AnchorMax = "0.98 " + (1 - (i * 0.05) - 0.01).ToString("0.####")
                    },
                    Text =
                    {
                        Text = targets[i],
                        FontSize = 12,
                        Align = UnityEngine.TextAnchor.MiddleCenter
                    }
                }, "gl_teleport_request");

                i++;
            }

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = $"gl_teleport_request_ui close",
                    Color = "0.8 0.2 0.2 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.15 0.01",
                    AnchorMax = "0.85 0.05"
                },
                Text =
                {
                    Text = "Close",
                    FontSize = 12,
                    Align = UnityEngine.TextAnchor.MiddleCenter
                }
            }, "gl_teleport_request");

            if (index > 0)
            {
                container.Add(new CuiButton
                {
                    Button =
                    {
                        Command = $"gl_teleport_request_ui page {index - 1}",
                        Color = "0.8 0.2 0.2 1"
                    },
                        RectTransform =
                    {
                        AnchorMin = "0.02 0.01",
                        AnchorMax = "0.13 0.05"
                    },
                        Text =
                    {
                        Text = "<<",
                        FontSize = 12,
                        Align = UnityEngine.TextAnchor.MiddleCenter
                    }
                }, "gl_teleport_request");
            }

            container.Add(new CuiButton
            {
                Button =
                {
                    Command = $"gl_teleport_request_ui page {index + 1}",
                    Color = "0.8 0.2 0.2 1"
                },
                RectTransform =
                {
                    AnchorMin = "0.87 0.01",
                    AnchorMax = "0.98 0.05"
                },
                Text =
                {
                    Text = ">>",
                    FontSize = 12,
                    Align = UnityEngine.TextAnchor.MiddleCenter
                }
            }, "gl_teleport_request");

            CuiHelper.AddUi(player, container);
        }

        void DestroyTeleportRequestGUI(BasePlayer player)
        {
            CuiHelper.DestroyUi(player, "gl_teleport_request");
        }
        #endregion
    }
}
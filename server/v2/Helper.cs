using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("Helper", "Game Limits", "2.0.0")]
    [Description("The helper plugin")]

    public class Helper : RustPlugin
    {
        #region Classes
        public static class UI
        {
            public static void Add(BasePlayer player, string json)
            {
                if (player == null)
                    return;

                CuiHelper.AddUi(player, json);
            }

            public static void Add(BasePlayer player, List<CuiElement> list)
            {
                if (player == null)
                    return;

                CuiHelper.AddUi(player, list);
            }

            public static void Destroy(BasePlayer player, string elem)
            {
                if (player == null)
                    return;

                CuiHelper.DestroyUi(player, elem);
            }

            public static CuiElementContainer Container(string panel, string color, string aMin, string aMax, bool cursor = false, string parent = "Overlay")
            {
                return new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image =
                            {
                                Color = color,
                            },
                            RectTransform =
                            {
                                AnchorMin = aMin,
                                AnchorMax = aMax,
                            },
                            CursorEnabled = cursor,
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
            }

            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0.0f)
            {
                container.Add(new CuiButton
                {
                    Button =
                    {
                        Color = color,
                        Command = command,
                        FadeIn = fadein,
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax,
                    },
                    Text =
                    {
                        Text = text,
                        FontSize = size,
                        Align = align,
                    },
                }, panel, CuiHelper.GetGuid());
            }

            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0.0f, string guid = null)
            {
                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = text,
                        FontSize = size,
                        Align = align,
                        Color = color,
                        FadeIn = fadein,
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax,
                    },
                }, panel, guid == null ? CuiHelper.GetGuid() : guid);
            }

            public static void Panel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false, string guid = null)
            {
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = color,
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax,
                    },
                    CursorEnabled = cursor,
                }, panel, guid == null ? CuiHelper.GetGuid() : guid);
            }

            public static void Image(ref CuiElementContainer container, string panel, string url, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = url,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax,
                        },
                    },
                });
            }

            public static void ImageRaw(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = png,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax,
                        },
                    },
                });
            }
        }

        public static class TimeFormat
        {
            public static string Short(TimeSpan ts)
            {
                if (ts.Days > 0)
                    return string.Format("{0}D {1}H", ts.Days, ts.Hours);

                if (ts.Hours > 0)
                    return string.Format("{0}H {1}M", ts.Hours, ts.Minutes);

                if (ts.Minutes > 0)
                    return string.Format("{0}M {1}S", ts.Minutes, ts.Seconds);

                return string.Format("{0}S", ts.TotalSeconds);
            }
        }
        #endregion

        #region Functions
        public static void ExecuteCommand(BasePlayer player, string command)
        {

        }

        public static int Timestamp()
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }
        #endregion
    }
}

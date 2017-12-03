using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Notifications", "Game Limits", "2.0.0")]
    [Description("The notifications plugin")]

    public class Notifications : RustPlugin
    {
        private static Dictionary<BasePlayer, Dictionary<string, TimedNotification>> timedNotifications = new Dictionary<BasePlayer, Dictionary<string, TimedNotification>>();

        #region Oxide Hooks
        void Init()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestoryUI(player);

            timer.Every(1f, () =>
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    UpdatePlayer(player);
            });
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                DestoryUI(player);
        }

        [ChatCommand("a")]
        private void OnChatCommandA(BasePlayer player, string command, string[] args)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            
            int sec = pdata.HasCooldown("test");
            Puts($"test cooldown: {sec}");

            pdata.AddCooldown("test", 120);


            AddTimedNotification(player, "raid", "COMBAT BLOCK", 30, "0.8 0 0.02 1");
            AddTimedNotification(player, "raid", "Cooldown: Teleport Home", 60 * 5, "0.8 0 0.02 1");
            // AddTimedNotification(player, "remove", "Remove Tool", 30, "0.3 0.3 0.3 1");
        }
        [ChatCommand("aa")]
        private void OnChatCommandAa(BasePlayer player, string command, string[] args)
        {
            RemoveTimedNotification(player, "raid");
        }
        #endregion

        #region Functions
        public static void AddTimedNotification(BasePlayer player, string key, string message, int seconds, string color)
        {
            if (!timedNotifications.ContainsKey(player))
                timedNotifications.Add(player, new Dictionary<string, TimedNotification>());
            
            // Check if the notification already exists
            if (timedNotifications[player].ContainsKey(key))
            {
                timedNotifications[player][key].message = message;
                timedNotifications[player][key].expires = Helper.Timestamp() + seconds;
                timedNotifications[player][key].color = color;
            }
            else
            {
                timedNotifications[player].Add(key, new TimedNotification()
                {
                    message = message,
                    expires = Helper.Timestamp() + seconds,
                    color = color,
                });
            }
            
            UpdatePlayer(player, true);
        }

        public static void RemoveTimedNotification(BasePlayer player, string key)
        {
            if (!timedNotifications.ContainsKey(player) || !timedNotifications[player].ContainsKey(key))
                return;

            timedNotifications[player].Remove(key);

            // Remove the player from the dictionary when there arent any timed notifications left
            if (timedNotifications[player].Count == 0)
                timedNotifications.Remove(player);

            UpdatePlayer(player, true);
        }

        private static void UpdatePlayer(BasePlayer player, bool force = false)
        {
            if (force)
            {
                Helper.UI.Destroy(player, "ui_notifications_times");
            }

            if (timedNotifications.ContainsKey(player))
            {
                if (!force)
                    Helper.UI.Destroy(player, "ui_notifications_times");
                
                // Render notifications
                CuiElementContainer container = Helper.UI.Container("ui_notifications_times", "0 0 0 0", "0.85 0.4", "0.990 0.85", false, "Hud");

                int index = 0;
                int currentTimestamp = Helper.Timestamp();
                
                foreach (var notification in timedNotifications[player])
                    BuildNotificationBlock(ref container, index++, notification.Value);

                // Remove expired timers
                foreach (var notification in timedNotifications[player].Where(k => k.Value.expires <= currentTimestamp).ToList())
                    timedNotifications[player].Remove(notification.Key);

                Helper.UI.Add(player, container);
            }
        }

        private static void DestoryUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_notifications_times");
        }

        private static void BuildNotificationBlock(ref CuiElementContainer container, int index, TimedNotification notification)
        {
            Vector2 dimension = new Vector2(1f, 0.07f);
            Vector2 origin = new Vector2(0f, 1f);
            Vector2 offset = new Vector2(0f, (0.015f + dimension.y) * (index + 1));
            Vector2 min = origin - offset;
            Vector2 max = min + dimension;

            Helper.UI.Panel(ref container, "ui_notifications_times", notification.color, $"{min.x} {min.y}", $"{max.x} {max.y}");

            // Message
            Helper.UI.Label(ref container, "ui_notifications_times", "1 1 1 1", notification.message, 12, $"{min.x} {min.y}", $"{max.x - 0.28} {max.y}");

            // Timer
            Helper.UI.Panel(ref container, "ui_notifications_times", "0 0 0 .64", $"{min.x + 0.72} {min.y}", $"{max.x} {max.y}");
            Helper.UI.Label(ref container, "ui_notifications_times", "1 1 1 1", Helper.TimeFormat.Short(TimeSpan.FromSeconds(notification.expires - Helper.Timestamp())), 12, $"{min.x + 0.72} {min.y}", $"{max.x} {max.y}");
        }
        #endregion

        #region Classes
        public class TimedNotification
        {
            public string message;
            public int expires;
            public string color;
        }
        #endregion
    }
}

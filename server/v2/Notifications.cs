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

















        /*private static Dictionary<BasePlayer, Dictionary<string, TimedNotification>> timedNotifications = new Dictionary<BasePlayer, Dictionary<string, TimedNotification>>();

        #region Oxide Hooks
        void Init()
        {
            //timer.Every(1f, () => UpdateTimedNotifications());

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                CreateUI(player);
        }
        #endregion

        #region Functions
        public static void SetTimedNotification(BasePlayer player, string name, string message, int seconds)
        {
            if (!timedNotifications.ContainsKey(player) && seconds > 0)
                timedNotifications.Add(player, new Dictionary<string, TimedNotification>());

            timedNotifications[player].Add(name, new TimedNotification()
            {
                message = message,
                expires = Helper.Timestamp() + seconds,
            });
        }

        public static void RemoveTimedNotification(BasePlayer player, string name)
        {
            if (!timedNotifications.ContainsKey(player) || !timedNotifications[player].ContainsKey(name))
                return;

            timedNotifications[player].Remove(name);

            if (timedNotifications[player].Count == 0)
                timedNotifications.Remove(player);
        }

        private void UpdateTimedNotifications()
        {
            int currentTimestamp = Helper.Timestamp();

            foreach (var notifications in timedNotifications)
            {
                int index = 0;
                foreach (var notification in notifications.Value)
                {
                    index++;

                    // Render the notification

                    // Remove expired notifications
                    if (notification.Value.expires < currentTimestamp)
                    {
                        Helper.UI.Destroy(notifications.Key, $"ui_timednotification_{notification.Key}");
                        RemoveTimedNotification(notifications.Key, notification.Key);
                    }
                }
            }
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player)
        {
            if (player == null)
                return;

            DestroyUI(player);

            // Create the timed notifications container
            CuiElementContainer container = Helper.UI.Container("ui_timednotifications", "1 0 0 .1", "0.85 0.4", "0.990 0.85");


            // Create notification block
            BuildNotificationBlock(ref container, 0, "Foo Bar 1", 100);
            BuildNotificationBlock(ref container, 1, "Foo Bar 2", 100);
            BuildNotificationBlock(ref container, 2, "Foo Bar 3", 100);
            BuildNotificationBlock(ref container, 3, "Foo Bar 4", 100);

            Helper.UI.Add(player, container);


            //CuiElementContainer nc = Helper.UI.Container("ui_timednotification", "0 1 0 .4", "0 0", "1 0.08", false, "ui_timednotifications");

            //Helper.UI.Add(player, nc);



            //DestoryUI(player);

            //// Create the main container
            //CuiElementContainer container = Helper.UI.Container("ui_shop", "0 0 0 .99", "0.05 0.05", "0.95 0.95", true);

            //// Create a close shop button
            //Helper.UI.Button(ref container, "ui_shop", "0.8 0.2 0.2 1", "Close Shop", 12, "0.9 0.96", "0.999 0.999", "shop close");

            //// Display the container
            //Helper.UI.Add(player, container);

            //UpdateTabsUI(player, "popular");
            //UpdateStatusUI(player, "You can buy more RP at https://rust.gamelimits.com/");
        }

        private void BuildNotificationBlock(ref CuiElementContainer container, int index, string message, int timestamp)
        {
            Vector2 dimension = new Vector2(1f, 0.08f);
            Vector2 origin = new Vector2(0f, 1f);
            Vector2 offset = new Vector2(0f, (0.015f + dimension.y) * (index + 1));
            Vector2 min = origin - offset;
            Vector2 max = min + dimension;

            Helper.UI.Panel(ref container, "ui_timednotifications", "0 1 0 .4", $"{min.x} {min.y}", $"{max.x} {max.y}");
        }

        private void DestroyUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_timednotifications");
        }
        #endregion

        #region Classes
        public class TimedNotification
        {
            public string message;
            public int expires;
            public Action callback;
        }
        #endregion*/
    }
}

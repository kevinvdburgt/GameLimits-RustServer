using System;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static Oxide.Plugins.Core;

namespace Oxide.Plugins
{
    [Info("Playtime", "Game Limits", "2.0.0")]
    [Description("The playtime plugin")]

    public class Playtime : RustPlugin
    {
        private static Dictionary<ulong, TimeInfo> timeInfo = new Dictionary<ulong, TimeInfo>();

        #region Oxide Hooks
        private void Init()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                LoadPlayer(player);

            Tick();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            LoadPlayer(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            UnloadPlayer(player);
        }
        #endregion

        #region Functions
        private void LoadPlayer(BasePlayer player)
        {
            if (player == null)
                return;

            PlayerData.PData pdata = PlayerData.Get(player);

            if (pdata == null)
            {
                Puts($"Loading for [{player.UserIDString}] has been delayed, waiting for the PlayerData plugin");
                timer.Once(1f, () => LoadPlayer(player));
                return;
            }

            // Remove the old data first
            UnloadPlayer(player);

            Database.Query(Database.Build("SELECT * FROM users WHERE id = @0 LIMIT 1;", pdata.id), records =>
            {
                if (records.Count == 0)
                {
                    Puts($"Could not load the player times for [{pdata.id}:{player.UserIDString}]");
                    return;
                }

                timeInfo.Add(player.userID, new TimeInfo()
                {
                    playTime = Convert.ToInt32(records[0]["playtime"]),
                    afkTime = Convert.ToInt32(records[0]["afktime"]),
                    rewardTime = Convert.ToInt32(records[0]["rewardtime"]),
                    lastTime = Helper.Timestamp(),
                    lastPosition = player.transform.position,
                });

                Puts($"Loaded [{pdata.id}:{player.UserIDString}] playtime: {Helper.TimeFormat.Short(Convert.ToInt32(records[0]["playtime"]))}, afktime: {Helper.TimeFormat.Short(Convert.ToInt32(records[0]["afktime"]))}");
            });
        }

        private void SavePlayer(BasePlayer player)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null || !timeInfo.ContainsKey(player.userID))
                return;

            TimeInfo ti = timeInfo[player.userID];

            Database.NonQuery(Database.Build("UPDATE users SET playtime=@0, afktime=@1, rewardtime=@2 WHERE id=@3 LIMIT 1;", ti.playTime, ti.afkTime, ti.rewardTime, pdata.id));
        }

        private void UnloadPlayer(BasePlayer player)
        {
            if (player == null || timeInfo.ContainsKey(player.userID))
                timeInfo.Remove(player.userID);
        }

        private void Tick()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null || !timeInfo.ContainsKey(player.userID) || PlayerData.Get(player) == null)
                    continue;

                TimeInfo ti = timeInfo[player.userID];

                // Update the time
                int time = Helper.Timestamp();
                int diffTime = time - ti.lastTime;
                ti.lastTime = time;

                // Update the position
                Vector3 position = player.transform.position;
                Vector3 diffPosition = position - ti.lastPosition;
                ti.lastPosition = position;

                // Apply the tick times
                if (diffPosition.magnitude > 0.5f)
                    ti.playTime += diffTime;
                else
                    ti.afkTime += diffTime;

                if ((ti.playTime + ti.afkTime) - ti.rewardTime > ti.rewardInterval)
                {
                    ti.rewardTime = (ti.playTime + ti.afkTime);
                    Reward(player);
                }

                SavePlayer(player);
            }

            timer.Once(60f, () => Tick());
        }

        private void Reward(BasePlayer player)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null || !timeInfo.ContainsKey(player.userID))
                return;

            int rewardPoints = 2;

            if (Helper.HasMinimumVipRank(pdata, "vip"))
                rewardPoints = 10;

            pdata.GiveRewardPoints(rewardPoints, $"You have been rewarded {rewardPoints} RP for playing on this server.", points =>
            {
                player.ChatMessage($"You have been rewarded <color=#0E84B7>{rewardPoints} RP</color> for playing on this server.\n" +
                    $"You have <color=#0E84B7>{points} Reward Points (RP)</color>. Type /s to spend them in the shop");

                Puts($"[{pdata.id}:{player.UserIDString}] has been rewarded {rewardPoints} RP for playing on this server (total: {points} RP)");
            });
        }
        #endregion

        #region Classes
        private class TimeInfo
        {
            public int rewardInterval = 60;
            public int playTime = 0;
            public int afkTime = 0;
            public int rewardTime = 0;
            public int lastTime = 0;
            public Vector3 lastPosition;
        }
        #endregion
    }
}

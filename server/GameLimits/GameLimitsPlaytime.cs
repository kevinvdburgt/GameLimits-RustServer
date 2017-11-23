using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Playtime", "Game Limits", "1.0.0")]
    [Description("The playtime module")]

    public class GameLimitsPlaytime : RustPlugin
    {
        private Dictionary<ulong, TimeInfo> timeInfo = new Dictionary<ulong, TimeInfo>();

        #region Server Hooks
        private void Init()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player != null)
                    OnPlayerInit(player);

            Tick();
        }

        private void OnPlayerInit(BasePlayer player)
        {
            LoadPlayer(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null || !timeInfo.ContainsKey(player.userID))
                return;

            timeInfo.Remove(player.userID);
        }
        #endregion

        #region Functions
        private void LoadPlayer(BasePlayer player)
        {
            if (player == null)
                return;

            if (!playerInfo.ContainsKey(player.userID))
            {
                Puts($"Waiting for player {player.UserIDString} to be loaded in the core module");
                timer.Once(1f, () => LoadPlayer(player));
                return;
            }

            PlayerInfo info = playerInfo[player.userID];

            MQuery(MBuild("SELECT * FROM users WHERE id=@0 LIMIT 1;", info.id), records =>
            {
                if (records.Count == 0)
                {
                    Puts($"[Error] Could not the player times for {player.UserIDString}");
                    return;
                }

                timeInfo.Add(player.userID, new TimeInfo()
                {
                    playTime = Convert.ToInt32(records[0]["playtime"]),
                    afkTime = Convert.ToInt32(records[0]["afktime"]),
                    rewardTime = Convert.ToInt32(records[0]["rewardtime"]),
                    lastTime = Timestamp(),
                    lastPosition = player.transform.position,
                });
            });
        }

        private void SavePlayer(BasePlayer player)
        {
            if (player == null || !timeInfo.ContainsKey(player.userID) || !playerInfo.ContainsKey(player.userID))
                return;

            TimeInfo tinfo = timeInfo[player.userID];
            PlayerInfo pinfo = playerInfo[player.userID];

            MNonQuery(MBuild("UPDATE users SET playtime=@0, afktime=@1, rewardtime=@2 WHERE id=@3 LIMIT 1;", tinfo.playTime, tinfo.afkTime, tinfo.rewardTime, pinfo.id));
        }

        private void Tick()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null || !timeInfo.ContainsKey(player.userID) || !playerInfo.ContainsKey(player.userID))
                    continue;

                TimeInfo info = timeInfo[player.userID];

                // Update the time
                int time = Timestamp();
                int diffTime = time - info.lastTime;
                info.lastTime = time;

                // Update the position
                Vector3 position = player.transform.position;
                Vector3 diffPosition = position - info.lastPosition;
                info.lastPosition = position;

                // Apply the tick times
                if (diffPosition.magnitude > 0.5f)
                    info.playTime += diffTime;
                else
                    info.afkTime += diffTime;

                if ((info.playTime + info.afkTime) - info.rewardTime > info.rewardInterval)
                {
                    info.rewardTime = (info.playTime + info.afkTime);
                    Reward(player);
                    Puts($"Reward mark at {(info.playTime + info.afkTime)} seconds ({info.playTime} playtime, {info.afkTime} afktime) for {player.displayName}");
                }

                // @TODO: Maybe move this to another place?
                SavePlayer(player);
            }

            timer.Once(60f, () => Tick());
        }

        private void Reward(BasePlayer player)
        {
            if (player == null || !playerInfo.ContainsKey(player.userID))
                return;

            PlayerInfo info = playerInfo[player.userID];
            int rewardPoints = 2;

            if (info.HasSubscription("vip"))
                rewardPoints = 10;

            info.GiveRewardPoints(rewardPoints, $"You have been rewarded {rewardPoints} RP for playing on this server.");
            PrintToChat(player, $"You have been rewarded <color=#0E84B7>{rewardPoints} RP</color> for playing on this server.");
        }
        #endregion

        #region Classes
        private class TimeInfo
        {
            public int rewardInterval = 60 * 60;
            public int playTime = 0;
            public int afkTime = 0;
            public int rewardTime = 0;
            public int lastTime;
            public Vector3 lastPosition;
        }
        #endregion
    }
}

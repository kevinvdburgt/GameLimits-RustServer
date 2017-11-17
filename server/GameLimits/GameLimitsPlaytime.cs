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
        //private Dictionary<ulong, TimeInfo> timeInfo = new Dictionary<ulong, TimeInfo>();
        //private int lastTick = 0;

        //#region Server Hooks
        //private void Init()
        //{
        //    foreach (BasePlayer player in BasePlayer.activePlayerList)
        //        OnPlayerInit(player);

        //    Tick();
        //}

        //private void Unload()
        //{
        //    foreach (BasePlayer player in BasePlayer.activePlayerList)
        //        OnPlayerDisconnected(player, null);
        //}

        //private void OnPlayerInit(BasePlayer player)
        //{
        //    if (player == null)
        //        return;

        //    if (!playerInfo.ContainsKey(player.userID))
        //    {
        //        timer.Once(1f, () => OnPlayerInit(player));
        //        return;
        //    }

        //    MQuery(MBuild("SELECT * FROM users WHERE id=@0 LIMIT 1;", playerInfo[player.userID].id), records =>
        //    {
        //        if (records.Count == 0)
        //            return;

        //        timeInfo.Add(player.userID, new TimeInfo()
        //        {
        //            playtime = Convert.ToDouble(records[0]["playtime"]),
        //            afktime = Convert.ToDouble(records[0]["afktime"]),
        //            rewardtime = Convert.ToDouble(records[0]["rewardtime"]),
        //            lastTime = GrabCurrentTime(),
        //            lastPos = player.transform.position,
        //        });
        //    });
        //}

        //private void OnPlayerDisconnected(BasePlayer player, string reason)
        //{
        //    timeInfo.Remove(player.userID);
        //}
        //#endregion

        //#region Functions
        //private static double GrabCurrentTime() => DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;

        //private void Tick()
        //{
        //    foreach (BasePlayer player in BasePlayer.activePlayerList)
        //    {
        //        if (player == null || !timeInfo.ContainsKey(player.userID) || !playerInfo.ContainsKey(player.userID))
        //            continue;

        //        TimeInfo tinfo = timeInfo[player.userID];
        //        PlayerInfo pinfo = playerInfo[player.userID];

        //        // Update time
        //        double time = GrabCurrentTime();
        //        double timeDiff = time - tinfo.lastTime;
        //        tinfo.lastTime = time;

        //        // Update position
        //        Vector3 pos = player.transform.position;
        //        Vector3 posDiff = pos - tinfo.lastPos;
        //        tinfo.lastPos = pos;

        //        if (posDiff.magnitude > 0.5f)
        //            tinfo.playtime += timeDiff;
        //        else
        //            tinfo.afktime += timeDiff;

        //        // Hourly rewards
        //        //double rewardInterval = 60 * 60;
        //        double rewardInterval = 60;
        //        if ((tinfo.playtime + tinfo.afktime) - tinfo.rewardtime > rewardInterval)
        //        {
        //            Reward(player);
        //            tinfo.rewardtime += rewardInterval;
        //        }

        //        // Save into the database
        //        MNonQuery(MBuild("UPDATE users SET playtime=@0, afktime=@1, rewardtime=@2 WHERE id=@3 LIMIT 1;", tinfo.playtime, tinfo.afktime, tinfo.rewardtime, pinfo.id));
        //    }
            
        //    timer.Once(60f, () => Tick());
        //}

        ///// <summary>
        ///// Called when the player hass been on the server each hour
        ///// </summary>
        ///// <param name="player"></param>
        //private void Reward(BasePlayer player)
        //{
        //    int reward = 20;

        //    PlayerInfo pinfo = playerInfo[player.userID];

        //    pinfo.GiveRewardPoints(reward, "Deposited for playing each hour on the server");

        //    PrintToChat(player, $"<color=\"#DD32AA\">{reward} RP</color>have been deposited to your account!");
        //}
        //#endregion

        //#region Classes
        //private class TimeInfo
        //{
        //    public double playtime = 0;
        //    public double afktime = 0;
        //    public double rewardtime = 0;
        //    public double lastTime;
        //    public Vector3 lastPos;
        //}
        //#endregion
    }
}

using Oxide.Core.Database;
using System;
using System.Collections.Generic;
using System.Net;
using static Oxide.Plugins.Core;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("PlayerData", "Game Limits", "2.0.0")]
    [Description("The player data plugin")]

    public class PlayerData : RustPlugin
    {
        static PlayerData plug;

        public static Dictionary<ulong, PData> datas = new Dictionary<ulong, PData>();

        #region Oxide Hooks
        private void Init()
        {
            plug = this;
        }
        #endregion

        #region Functions
        public static void Load(BasePlayer player, bool created = false)
        {
            if (player == null)
                return;

            if (!created)
                plug.Puts($"Loading player [{player.UserIDString}]");

            Database.Query(Database.Build("SELECT * FROM users WHERE steam_id = @0 LIMIT 1;", player.UserIDString), records =>
            {
                // Check if the user has been created, but still not able to find
                if (records.Count == 0 && created == true)
                {
                    plug.Puts($"Cannot fint he player after creation. Player has been kicked!");
                    player.Kick("Account creation failed, contact us at rust.gamelimits.com/discord");
                    return;
                }

                // Check if the user does not exists, in that case, create a new user and restart the loading process
                if (records.Count == 0)
                {
                    plug.Puts($"Creating new player record [{player.UserIDString}]");
                    Database.Insert(Database.Build("INSERT INTO users (steam_id, display_name) VALUES (@0, @1);", player.UserIDString, player.displayName), result => Load(player, true));
                    return;
                }

                PData pdata = new PData()
                {
                    id = Convert.ToUInt32(records[0]["id"]),
                    admin = Convert.ToBoolean(records[0]["is_admin"]),
                };

                datas.Add(player.userID, pdata);

                pdata.LoadRewardPoints();
                pdata.LoadSubscriptions();
                pdata.LoadCooldowns();
                pdata.LoadSettings();

                plug.Puts($"Player loaded (id: {pdata.id})");
            });
        }

        public static void Unload(BasePlayer player)
        {
            if (player == null || Get(player) == null)
                return;

            datas.Remove(player.userID);
        }

        public static PData Get(BasePlayer player)
        {
            if (player == null)
                return null;

            return Get(player.userID);
        }

        public static PData Get(ulong userID)
        {
            if (!datas.ContainsKey(userID))
                return null;

            return datas[userID];
        }
        #endregion

        #region Classes
        public class PData
        {
            public uint id = 0;
            public bool admin = false;
            public int rewardPoints = 0;
            public Dictionary<string, int> subscriptions = new Dictionary<string, int>();
            public Dictionary<string, int> cooldowns = new Dictionary<string, int>();

            // Settings
            public bool displayTimedNotifications = true;

            public void LoadRewardPoints(Action<int> callback = null)
            {
                Database.Query(Database.Build("SELECT SUM(points) AS points FROM reward_points WHERE user_id=@0;", id), records =>
                {
                    if (records.Count == 0)
                    {
                        callback?.Invoke(0);
                        return;
                    }

                    int points = 0;
                    if (records[0]["points"].ToString().Length > 0)
                        points = Convert.ToInt32(records[0]["points"]);

                    rewardPoints = points;

                    callback?.Invoke(points);
                });
            }
            public void GiveRewardPoints(int amount, string message = null, Action<int> callback = null)
            {
                Database.Insert(Database.Build("INSERT INTO reward_points (user_id, points, description) VALUES (@0, @1, @2);", id, amount, message), done =>
                {
                    LoadRewardPoints(points =>
                    {
                        callback?.Invoke(points);
                    });
                });
            }

            public bool HasSubscription(string name)
            {
                return subscriptions.ContainsKey(name) && subscriptions[name] > Helper.Timestamp();
            }
            public void AddSubscription(string name, int seconds)
            {
                Database.Query(Database.Build("SELECT id, UNIX_TIMESTAMP(expires_at) AS expires_at FROM subscriptions WHERE user_id=@0 AND name=@1 AND expires_at > NOW();", id, name), records =>
                {
                    int time = Helper.Timestamp() + seconds;
                    if (records.Count == 0)
                        Database.Insert(Database.Build("INSERT INTO subscriptions (user_id, name, expires_at) VALUES (@0, @1, FROM_UNIXTIME(@2));", id, name, time), index => LoadSubscriptions());
                    else
                        Database.NonQuery(Database.Build("UPDATE subscriptions SET expires_at=FROM_UNIXTIME(@0) WHERE id=@1 LIMIT 1;", Convert.ToInt32(records[0]["expires_at"]) + seconds, Convert.ToInt32(records[0]["id"])), index => LoadSubscriptions());
                });
            }
            public void LoadSubscriptions()
            {
                Database.Query(Database.Build("SELECT name, UNIX_TIMESTAMP(expires_at) AS expires_at FROM subscriptions WHERE user_id=@0 AND expires_at > NOW();", id), records =>
                {
                    subscriptions.Clear();
                    foreach (var record in records)
                        subscriptions.Add(Convert.ToString(record["name"]), Convert.ToInt32(record["expires_at"]));
                });
            }

            public int HasCooldown(string name)
            {
                int timestamp = Helper.Timestamp();

                if (cooldowns.ContainsKey(name) && cooldowns[name] > timestamp)
                    return cooldowns[name] - timestamp;

                return 0;
            }
            public void AddCooldown(string name, int seconds)
            {
                Database.Query(Database.Build("SELECT id, UNIX_TIMESTAMP(expires_at) AS expires_at FROM cooldowns WHERE user_id=@0 AND name=@1 AND expires_at > NOW();", id, name), records =>
                {
                    int time = Helper.Timestamp() + seconds;
                    if (records.Count == 0)
                        Database.Insert(Database.Build("INSERT INTO cooldowns (user_id, name, expires_at) VALUES (@0, @1, FROM_UNIXTIME(@2));", id, name, time), index => LoadCooldowns());
                    else
                        Database.NonQuery(Database.Build("UPDATE cooldowns SET expires_at=FROM_UNIXTIME(@0) WHERE id=@1 LIMIT 1;", Convert.ToInt32(records[0]["expires_at"]) + seconds, Convert.ToInt32(records[0]["id"])), index => LoadCooldowns());
                });
            }
            public void LoadCooldowns()
            {
                Database.Query(Database.Build("SELECT name, UNIX_TIMESTAMP(expires_at) AS expires_at FROM cooldowns WHERE user_id=@0 AND expires_at > NOW();", id), records =>
                {
                    cooldowns.Clear();
                    foreach (var record in records)
                        cooldowns.Add(Convert.ToString(record["name"]), Convert.ToInt32(record["expires_at"]));
                });
            }

            public void SetSetting(string setting, string data, bool restore = false)
            {
                switch (setting)
                {
                    case "displayTimedNotifications":
                        displayTimedNotifications = (data == "true");
                        break;
                }

                if (restore)
                {
                    plug.Puts($"Setting restored: {setting} to {data}");
                    return;
                }

                Database.Query(Database.Build("SELECT * FROM user_settings WHERE user_id=@0 AND setting=@1 LIMIT 1;", id, setting), records =>
                {
                    if (records.Count == 0)
                    {
                        Database.NonQuery(Database.Build("INSERT INTO user_settings (user_id, setting, data) VALUES (@0, @1, @2);", id, setting, data));
                        plug.Puts($"[{id}] Changed setting {setting} to {data} (first time)");
                    }
                    else
                    {
                        Database.NonQuery(Database.Build("UPDATE user_settings SET data=@0 WHERE user_id=@1 AND setting=@2 LIMIT 1;", data, id, setting));
                        plug.Puts($"[{id}] Changed setting {setting} to {data}");
                    }
                });
            }
            public void GetSetting(string setting, string defaultString, Action<string> callback)
            {
                Database.Query(Database.Build("SELECT data FROM user_settings WHERE user_id=@0 AND setting=@1 LIMIT 1;", id, setting), records => 
                {
                    if (records.Count == 0)
                    {
                        callback?.Invoke(defaultString);
                        return;
                    }

                    callback?.Invoke(records[0]["data"].ToString());
                });
            }
            public void LoadSettings()
            {
                Database.Query(Database.Build("SELECT * FROM user_settings WHERE user_id=@0;", id), records =>
                {
                    foreach (var record in records)
                        SetSetting(Convert.ToString(record["setting"]), Convert.ToString(record["data"]), true);
                });
            }
        }
        #endregion
    }
}

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
                };

                datas.Add(player.userID, pdata);

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
            public int rewardPoints = 0;
        }
        #endregion
    }
}

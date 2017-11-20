// Requires: GameLimitsChat
// Requires: GameLimitsFriends
// Requires: GameLimitsInfo
// Requires: GameLimitsShop
// Requires: GameLimitsCars
// Requires: GameLimitsSkills
// Requires: GameLimitsStacks
// Requires: GameLimitsPlaytime
// Requires: GameLimitsSpawn
// Requires: GameLimitsRemove
// Requires: GameLimitsKits
// Requires: GameLimitsAdmin
// Requires: GameLimitsKillfeed
// Requires: GameLimitsTeleport

using Oxide.Core.Database;
using Oxide.Core;
using System.Collections.Generic;
using System;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Globalization;

namespace Oxide.Plugins
{
    [Info("Core", "Game Limits", "1.0.0")]
    [Description("The core plugin for Game Limits")]

    public class GameLimits : RustPlugin
    {
        #region MySQL
        public static Connection mysqlConnection;
        private static readonly Core.MySql.Libraries.MySql mysql = Interface.GetMod().GetLibrary<Core.MySql.Libraries.MySql>();
        #endregion

        #region Dictionaries
        public static Dictionary<ulong, PlayerInfo> playerInfo = new Dictionary<ulong, PlayerInfo>();
        public static Dictionary<ulong, HashSet<ulong>> playerFriends = new Dictionary<ulong, HashSet<ulong>>();
        #endregion

        #region Configurations
        //private static string MYSQL_HOST = "rust.gamelimits.com";
        //private static string MYSQL_USER = "gamelimits";
        //private static string MYSQL_PASS = "osduhfgp9as7d8yasd";
        //private static string MYSQL_DB = "gamelimits";
        private static string MYSQL_HOST = "192.168.1.101";
        private static string MYSQL_USER = "root";
        private static string MYSQL_PASS = "";
        private static string MYSQL_DB = "project_rust_gl";
        #endregion

        #region Classes
        public class PlayerInfo
        {
            /// <summary>
            /// The database player id
            /// </summary>
            public int id;

            /// <summary>
            /// Set to true, when the player is an admin
            /// </summary>
            public bool admin = false;

            /// <summary>
            /// The amount of reward points
            /// </summary>
            public int rewardPoints = 0;

            /// <summary>
            /// Load the reward points from the database
            /// </summary>
            public void LoadRewardPoints(Action callback = null)
            {
                MQuery(MBuild("SELECT SUM(points) AS points FROM reward_points WHERE user_id=@0;", id), records =>
                {
                    if (records.Count == 0)
                        return;

                    int points = 0;
                    if (records[0]["points"].ToString().Length > 0)
                        points = Convert.ToInt32(records[0]["points"]);

                    rewardPoints = points;

                    callback?.Invoke();
                });
            }

            /// <summary>
            /// Give the user a amount of reward points!
            /// </summary>
            /// <param name="amout"></param>
            /// <param name="message"></param>
            /// <param name="callback"></param>
            public void GiveRewardPoints(int amount, string message = null, Action callback = null)
            {
                MInsert(MBuild("INSERT INTO reward_points (user_id, points, description) VALUES (@0, @1, @2);", id, amount, message), done => {
                    LoadRewardPoints(() =>
                    {
                        callback?.Invoke();
                    });
                });
            }

            /// <summary>
            /// A dictionary with subscription names and their expiry dates
            /// </summary>
            public Dictionary<string, int> subscriptions = new Dictionary<string, int>();

            /// <summary>
            /// Check if the user has an active subscription on his name
            /// </summary>
            /// <param name="name"></param>
            /// <returns></returns>
            public bool HasSubscription(string name)
            {
                int timestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds;

                if (subscriptions.ContainsKey(name) && subscriptions[name] > timestamp)
                    return true;

                return false;
            }

            /// <summary>
            /// Add a subscription to the user
            /// </summary>
            /// <param name="name"></param>
            /// <param name="seconds"></param>
            public void AddSubscription(string name, int seconds)
            {
                MQuery(MBuild("SELECT id, UNIX_TIMESTAMP(expires_at) AS expires_at FROM subscriptions WHERE user_id=@0 AND name=@1 AND expires_at > NOW();", id, name), records =>
                {
                    int timestamp = (int)(DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1))).TotalSeconds + seconds;
                    if (records.Count == 0)
                        MInsert(MBuild("INSERT INTO subscriptions (user_id, name, expires_at) VALUES (@0, @1, FROM_UNIXTIME(@2));", id, name, timestamp), (int i) => LoadSubscriptions());
                    else if (records.Count == 1)
                        MNonQuery(MBuild("UPDATE subscriptions SET expires_at=FROM_UNIXTIME(@0) WHERE id=@1 LIMIT 1;", Convert.ToInt32(records[0]["expires_at"]) + seconds, Convert.ToInt32(records[0]["id"])), (int i) => LoadSubscriptions());
                });
            }

            /// <summary>
            /// Load the user subscriptions
            /// </summary>
            public void LoadSubscriptions()
            {
                MQuery(MBuild("SELECT name, UNIX_TIMESTAMP(expires_at) AS expires_at FROM subscriptions WHERE user_id=@0 AND expires_at > NOW();", id), records =>
                {
                    subscriptions.Clear();
                    foreach (var record in records)
                        subscriptions.Add(Convert.ToString(record["name"]), Convert.ToInt32(record["expires_at"]));
                });
            }


            public Dictionary<string, int> cooldowns = new Dictionary<string, int>();

            public int HasCooldown(string name)
            {
                int timestamp = Timestamp();

                if (cooldowns.ContainsKey(name) && cooldowns[name] > timestamp)
                    return cooldowns[name] - timestamp;

                return 0;
            }

            public void AddCooldown(string name, int seconds)
            {
                MQuery(MBuild("SELECT id, UNIX_TIMESTAMP(expires_at) AS expires_at FROM cooldowns WHERE user_id=@0 AND name=@1 AND expires_at > NOW();", id, name), records =>
                {
                    int timestamp = Timestamp() + seconds;
                    if (records.Count == 0)
                        MInsert(MBuild("INSERT INTO cooldowns (user_id, name, expires_at) VALUES (@0, @1, FROM_UNIXTIME(@2));", id, name, timestamp), (int i) => LoadCooldowns());
                    else if (records.Count == 1)
                        MNonQuery(MBuild("UPDATE cooldowns SET expires_at=FROM_UNIXTIME(@0) WHERE id=@1 LIMIT 1;", Convert.ToInt32(records[0]["expires_at"]) + seconds, Convert.ToInt32(records[0]["id"])), (int i) => LoadCooldowns());
                });
            }

            public void LoadCooldowns()
            {
                MQuery(MBuild("SELECT name, UNIX_TIMESTAMP(expires_at) AS expires_at FROM cooldowns WHERE user_id=@0 AND expires_at > NOW();", id), records =>
                {
                    cooldowns.Clear();
                    foreach (var record in records)
                        cooldowns.Add(Convert.ToString(record["name"]), Convert.ToInt32(record["expires_at"]));
                });
            }
        }

        public static class UI
        {
            public static void Add(BasePlayer player, List<CuiElement> elements)
            {
                CuiHelper.AddUi(player, elements);
            }

            public static void Destroy(BasePlayer player, string name)
            {
                CuiHelper.DestroyUi(player, name);
            }

            public static CuiElementContainer CreateElementContainer(string panel, string color, string aMin, string aMax, bool cursor = false, string parent = "Overlay")
            {
                var element = new CuiElementContainer()
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
                            CursorEnabled = cursor
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };

                return element;
            }

            public static void CreateButton(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0.0f)
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

            public static void CreateLabel(ref CuiElementContainer container, string panel, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, string color = null, float fadein = 0.0f, string guid = null)
            {
                if (guid == null)
                    guid = CuiHelper.GetGuid();

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
                    }
                }, panel, guid);
            }

            public static void CreatePanel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false, string guid = null)
            {
                if (guid == null)
                    guid = CuiHelper.GetGuid();

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
                }, panel, guid);
            }

            public static void LoadRawImage(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = png
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax,
                        }
                    }
                });
            }

            public static void LoadUrlImage(ref CuiElementContainer container, string panel, string url, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = url
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax,
                        }
                    }
                });
            }
        }
        #endregion

        #region Helpers
        public static bool HasMinimumVipRank(PlayerInfo info, string rank)
        {
            if (rank == "vip" && (info.HasSubscription("vip") || info.HasSubscription("vip_pro") || info.HasSubscription("vip_elite")))
                return true;

            if (rank == "vip_pro" && (info.HasSubscription("vip_pro") || info.HasSubscription("vip_elite")))
                return true;

            if (rank == "vip_elite" && (info.HasSubscription("vip_elite")))
                return true;

            return false;
        }

        public static bool HasFriend(ulong player, ulong friendId)
        {
            if (!playerFriends.ContainsKey(player) || !playerFriends[player].Contains(friendId))
                return false;

            return true;
        }

        public static int Timestamp()
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        public static List<BasePlayer> FindOnlinePlayers(string search)
        {
            List<BasePlayer> players = new List<BasePlayer>();
            if (string.IsNullOrEmpty(search))
                return players;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.UserIDString.Equals(search))
                    players.Add(player);
                else if (!string.IsNullOrEmpty(player.displayName) && player.displayName.Contains(search, CompareOptions.IgnoreCase))
                    players.Add(player);
                else if (player.net?.connection != null && player.net.connection.ipaddress.Equals(search))
                    players.Add(player);
            return players;
        }
        #endregion

        #region MySQL Helpers
        public static Sql MBuild(string sql)
        {
            return MBuild(sql, null);
        }

        public static bool MReady()
        {
            if (mysqlConnection == null || mysqlConnection.Con == null)
                return false;

            return true;
        }

        public static Sql MBuild(string sql, params object[] args)
        {
            return Sql.Builder.Append(sql, args);
        }

        public static void MQuery(Sql sql, Action<List<Dictionary<string, object>>> callback)
        {
            mysql.Query(sql, mysqlConnection, callback);
        }

        public static void MNonQuery(Sql sql, Action<int> callback = null)
        {
            mysql.ExecuteNonQuery(sql, mysqlConnection, callback);
        }

        public static void MInsert(Sql sql, Action<int> callback = null)
        {
            mysql.Insert(sql, mysqlConnection, callback);
        }

        public static void MDelete(Sql sql, Action<int> callback = null)
        {
            mysql.Delete(sql, mysqlConnection, callback);
        }
        #endregion

        #region Server Events
        void Init()
        {
            // Setup the MySQL connection
            MySQLSetup();

            // Load all the friends
            LoadFriends();

            // Load all connected players
            foreach (var player in BasePlayer.activePlayerList)
            {
                if (player == null)
                    return;

                LoadPlayer(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player == null)
                return;

            LoadPlayer(player);
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null || !playerInfo.ContainsKey(player.userID))
                return;

            playerInfo.Remove(player.userID);
        }
        #endregion

        #region Functions
        /// <summary>
        /// Setup and create a MySQL connection
        /// </summary>
        void MySQLSetup()
        {
            Puts("Connecting to the database...");

            try
            {
                mysqlConnection = mysql.OpenDb(MYSQL_HOST, 3306, MYSQL_DB, MYSQL_USER, MYSQL_PASS, this);
                if (mysqlConnection == null && mysqlConnection.Con == null)
                {
                    Puts("Cannot connect to the server");
                    return;
                }

                MNonQuery(MBuild("SELECT 1=1;"));
            }
            catch (Exception e)
            {
                Puts($"Exception {e.Message}");
                return;
            }


            Puts("Connection successfull");
        }

        /// <summary>
        /// Load all friends and place them in the player friend dictionary
        /// </summary>
        void LoadFriends()
        {
            Puts("Loading friends list from the database..");

            MQuery(MBuild("SELECT a.steam_id AS uid, b.steam_id AS with_uid FROM friends LEFT JOIN users AS a ON friends.user_id = a.id LEFT JOIN users AS b ON friends.with_user_id = b.id;"), records =>
            {
                Puts($"Found {records.Count} friend records!");

                playerFriends.Clear();

                if (records.Count == 0)
                    return;

                foreach (var record in records)
                {
                    ulong uid = Convert.ToUInt64(record["uid"]);
                    ulong with_uid = Convert.ToUInt64(record["with_uid"]);

                    if (playerFriends.ContainsKey(uid))
                        playerFriends[uid].Add(with_uid);
                    else
                        playerFriends.Add(uid, new HashSet<ulong> { with_uid });
                }
            });
        }

        /// <summary>
        /// Load the selected player from the database, if it doenst exists create one
        /// </summary>
        /// <param name="player">The player object</param>
        /// <param name="created">Pass true when the player is created!</param>
        void LoadPlayer(BasePlayer player, bool created = false)
        {
            if (!created)
                Puts($"Loading player information for [{player.displayName}]");

            MQuery(MBuild("SELECT * FROM users WHERE steam_id=@0 LIMIT 1;", player.UserIDString), records =>
            {
                if (records.Count == 0 && created == true)
                {
                    Puts($"Cannot find the player after creation. Player is kicked!");
                    player.Kick("Account creation failed. Please contact us at rust.gamelimits.com");
                    return;
                }

                if (records.Count == 0)
                {
                    Puts($"Creating new player record for [{player.displayName}]");
                    MInsert(MBuild("INSERT INTO users (steam_id, display_name) VALUES (@0, @1)", player.UserIDString, player.displayName));
                    LoadPlayer(player, true);
                    return;
                }

                PlayerInfo info = new PlayerInfo()
                {
                    id = Convert.ToInt32(records[0]["id"]),
                    admin = Convert.ToBoolean(records[0]["is_admin"]),
                };

                playerInfo.Add(player.userID, info);

                info.LoadSubscriptions();
                info.LoadRewardPoints();
                info.LoadCooldowns();

                Puts($"Player loaded from the database [{player.displayName}] (id: {info.id})");
            });
        }

        void SavePlayer(BasePlayer player)
        {
            if (player == null || !playerInfo.ContainsKey(player.userID))
                return;
        }

        public static bool ExecuteCommand(BasePlayer player, string command)
        {
            string[] args = command.Split(' ');

            if (player == null || args.Length == 0 || !playerInfo.ContainsKey(player.userID))
                return false;

            PlayerInfo info = playerInfo[player.userID];

            switch (args[0])
            {
                // Add subscriptions
                // syntax: subscription <name> <duration in seconds>
                case "subscription":
                    if (args.Length != 3)
                        return false;

                    info.AddSubscription(Convert.ToString(args[1]), Convert.ToInt32(args[2]));
                    break;

                // Give item
                // syntax: give <itemname> [amount] [inventory]
                case "give":
                    if (args.Length == 1)
                        return false;

                    // Parameters
                    int amount = args.Length == 3 ? Convert.ToInt32(args[2]) : 1;
                    ItemContainer inventory = player.inventory.containerMain;
                    if (args.Length == 4 && Convert.ToString(args[3]) == "belt")
                        inventory = player.inventory.containerBelt;
                    else if (args.Length == 4 && Convert.ToString(args[3]) == "wear")
                        inventory = player.inventory.containerWear;

                    ItemDefinition definition = ItemManager.FindItemDefinition(Convert.ToString(args[1]));
                    if (definition == null)
                        return false;

                    Item item = ItemManager.CreateByItemID(definition.itemid, amount);
                    
                    if (inventory.itemList.Count >= 24)
                        item.Drop(player.transform.position, new Vector3(0, 1f, 0));
                    else
                        player.inventory.GiveItem(ItemManager.CreateByItemID(definition.itemid, amount), inventory);
                    break;

                // Call the helicopter to the caller
                // syntax: helicopter
                case "helicopter":
                    BaseHelicopter helicopter = (BaseHelicopter)GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                    if (helicopter == null)
                        return false;

                    var ai = helicopter?.GetComponent<PatrolHelicopterAI>() ?? null;
                    if (ai == null)
                        return false;

                    ai.SetInitialDestination(player.transform.position + new Vector3(0, 25f, 0));

                    helicopter.Spawn();
                    break;
            }

            return true;
        }
        #endregion
    }
}


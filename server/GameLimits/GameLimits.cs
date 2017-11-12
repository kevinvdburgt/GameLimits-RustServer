// Requires: GameLimitsChat
// Requires: GameLimitsFriends

using Oxide.Core.Database;
using Oxide.Core;
using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("Core", "Game Limits", "1.0.0")]
    [Description("The core plugin for Game Limits")]

    public class GameLimits : RustPlugin
    {
        #region MySQL
        private static Connection mysqlConnection;
        private static readonly Core.MySql.Libraries.MySql mysql = Interface.GetMod().GetLibrary<Core.MySql.Libraries.MySql>();
        #endregion

        #region Dictionaries
        public static Dictionary<ulong, PlayerInfo> playerInfo = new Dictionary<ulong, PlayerInfo>();
        public static Dictionary<ulong, HashSet<ulong>> playerFriends = new Dictionary<ulong, HashSet<ulong>>();
        #endregion

        #region Configurations
        private static string MYSQL_HOST = "rust.gamelimits.com";
        private static string MYSQL_USER = "gamelimits";
        private static string MYSQL_PASS = "osduhfgp9as7d8yasd";
        private static string MYSQL_DB = "gamelimits";
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
            /// Check if the player has VIP privileges
            /// </summary>
            /// <returns>Return true when the that is the case</returns>
            public bool IsVip()
            {
                return true;
            }
        }

        public static class LogType
        {
            /// <summary>
            /// Use for default messages
            /// </summary>
            public static ushort DEFAULT = 0;

            /// <summary>
            /// Use for success messages
            /// </summary>
            public static ushort OK = 1;

            /// <summary>
            /// Use for error messages
            /// </summary>
            public static ushort ERROR = 2;

            /// <summary>
            /// Use for info / pending messages
            /// </summary>
            public static ushort INFO = 3;
        }
        #endregion

        #region Helpers
        public static void Log(string module, string message, ushort state = 0, bool print = true)
        {
            switch (state)
            {
                case 1:
                    Console.ForegroundColor = ConsoleColor.Green;
                    break;

                case 2:
                    Console.ForegroundColor = ConsoleColor.Red;
                    break;

                case 3:
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    break;
            }

            if (!print)
                return;

            Console.Write($"[{module}] ");

            if (state > 0)
                Console.ResetColor();

            Console.WriteLine(message);
        }
        #endregion

        #region MySQL Helpers
        public static Sql MBuild(string sql)
        {
            return MBuild(sql, null);
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
        #endregion

        #region Functions
        /// <summary>
        /// Setup and create a MySQL connection
        /// </summary>
        void MySQLSetup()
        {
            Log("MySQL", "Connecting to the database...", LogType.INFO);

            try
            {
                mysqlConnection = mysql.OpenDb(MYSQL_HOST, 3306, MYSQL_DB, MYSQL_USER, MYSQL_PASS, this);
                if (mysqlConnection == null && mysqlConnection.Con == null)
                {
                    Log("MySQL", "Cannot connect to the server", LogType.ERROR);
                    return;
                }

                MNonQuery(MBuild("SELECT 1=1;"));
            }
            catch (Exception e)
            {
                Log("MySQL", $"Exception {e.Message}", LogType.ERROR);
                return;
            }


            Log("MySQL", "Connection successfull", LogType.OK);
        }

        /// <summary>
        /// Load all friends and place them in the player friend dictionary
        /// </summary>
        void LoadFriends()
        {
            Log("Friends", "Loading friends list from the database..", LogType.INFO);

            MQuery(MBuild("SELECT a.steam_id AS uid, b.steam_id AS with_uid FROM friends LEFT JOIN users AS a ON friends.user_id = a.id LEFT JOIN users AS b ON friends.with_user_id = b.id;"), records =>
            {
                Log("Friends", $"Found {records.Count} friend records!", LogType.OK);

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
                Log("Player", $"Loading player information for [{player.displayName}]", LogType.INFO);

            MQuery(MBuild("SELECT * FROM users WHERE steam_id=@0 LIMIT 1;", player.UserIDString), records =>
            {
                if (records.Count == 0 && created == true)
                {
                    Log("Player", $"Cannot find the player after creation. Player is kicked!", LogType.ERROR);
                    player.Kick("Account creation failed. Please contact us at rust.gamelimits.com");
                    return;
                }

                if (records.Count == 0)
                {
                    Log("Player", $"Creating new player record for [{player.displayName}]", LogType.INFO);
                    MInsert(MBuild("INSERT INTO users (steam_id, display_name) VALUES (@0, @1)", player.UserIDString, player.displayName));
                    LoadPlayer(player, true);
                    return;
                }

                PlayerInfo info = new PlayerInfo()
                {
                    id = Convert.ToInt32(records[0]["id"]),
                    admin = Convert.ToBoolean(records[0]["is_admin"])
                };

                playerInfo.Add(player.userID, info);

                Log("Player", $"Player loaded from the database [{player.displayName}] (id: {info.id})", LogType.INFO);
            });
        }
        #endregion
    }
}
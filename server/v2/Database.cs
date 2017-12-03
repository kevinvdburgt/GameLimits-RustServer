using Oxide.Core.Database;
using System;
using System.Collections.Generic;
using System.Net;
using static Oxide.Plugins.Core;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Database", "Game Limits", "2.0.0")]
    [Description("The database plugin")]

    public class Database : RustPlugin
    {
        private static Connection connection;
        private static readonly Oxide.Core.MySql.Libraries.MySql mysql = Interface.GetMod().GetLibrary<Oxide.Core.MySql.Libraries.MySql>();

        private static string MYSQL_HOST = "192.168.1.101";
        private static string MYSQL_USER = "root";
        private static string MYSQL_PASS = "";
        private static string MYSQL_DB = "project_rust_gl";

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            Connect();
        }
        #endregion

        #region Functions
        private void Connect()
        {
            Puts("Connecting..");

            try
            {
                connection = mysql.OpenDb(MYSQL_HOST, 3306, MYSQL_DB, MYSQL_USER, MYSQL_PASS, this);
                if (connection == null || connection.Con == null)
                {
                    Puts("Cannot connect to the database server");
                    return;
                }

                NonQuery(Build("SELECT 1=1;"));
            }
            catch (Exception e)
            {
                Puts($"Exception {e.Message}");
                return;
            }

            Puts("Connected!");
        }

        public static Sql Build(string sql)
        {
            return Build(sql, null);
        }

        public static Sql Build(string sql, params object[] args)
        {
            return Sql.Builder.Append(sql, args);
        }

        public static void Query(Sql sql, Action<List<Dictionary<string, object>>> callback)
        {
            mysql.Query(sql, connection, callback);
        }

        public static void NonQuery(Sql sql, Action<int> callback = null)
        {
            mysql.ExecuteNonQuery(sql, connection, callback);
        }

        public static void Insert(Sql sql, Action<int> callback = null)
        {
            mysql.Insert(sql, connection, callback);
        }

        public static void Delete(Sql sql, Action<int> callback = null)
        {
            mysql.Delete(sql, connection, callback);
        }

        public static bool Ready()
        {
            return connection != null && connection.Con != null;
        }
        #endregion
    }
}

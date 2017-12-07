using System.Net;
using static Oxide.Plugins.Core;

namespace Oxide.Plugins
{
    [Info("Chat", "Game Limits", "2.0.0")]
    [Description("The chat plugin")]

    public class Chat : RustPlugin
    {
        #region Oxide Hooks
        private object OnPlayerChat(ConsoleSystem.Arg args)
        {
            BasePlayer player = (BasePlayer)args.Connection.player;
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null)
                return null;

            // The player data has to be loaded in order to send chat messages
            if (pdata == null)
            {
                player.ChatMessage("<color=#D00>Error</color> chat muted until your player data has been loaded!");
                return false;
            }

            // Player tags
            string tags = "";
            if (Helper.HasMinimumVipRank(pdata, "vip"))
                tags += "<color=#88B71B>[VIP]</color> ";

            // Compose the player message
            string message = string.Join(" ", args.Args);

            // Broadcast to ingame chat
            BroadcastChat($"{tags}<color=#32AADD>{player.displayName}</color> {message}", player.userID);

            // Broadcast to server console
            Puts($"[ingame] [{player.displayName}] {message}");

            // Store the chatmessage into the database
            Database.Insert(Database.Build("INSERT INTO chatlogs (user_id, display_name, message) VALUES (@0, @1, @2);", pdata.id, player.displayName, message));

            return false;
        }

        private void OnRconCommand(IPAddress ip, string command, string[] args)
        {
            if (command == "chat.admin" && args != null)
            {
                string message = string.Join(" ", args);
                BroadcastChat($"<color=#B70E84>Admin</color> {message}");
                Puts($"[rcon] [admin] {message}");
                Database.Insert(Database.Build("INSERT INTO chatlogs (display_name, message) VALUES (@0, @1);", "Admin", message));
            }
        }
        #endregion

        #region Functions
        public static void BroadcastChat(string message, ulong userID = 0)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                if (player == null)
                    continue;

                player.SendConsoleCommand("chat.add", userID, message);
            }
        }
        #endregion
    }
}

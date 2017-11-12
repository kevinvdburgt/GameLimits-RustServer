﻿using static Oxide.Plugins.GameLimits;
using System.Net;

namespace Oxide.Plugins
{
    [Info("Chat", "Game Limits", "1.0.0")]
    [Description("The chat module")]

    public class GameLimitsChat : RustPlugin
    {
        #region Server Events
        object OnPlayerChat(ConsoleSystem.Arg args)
        {
            var player = (BasePlayer)args.Connection.player;

            if (player == null)
                return null;

            // The player has to be loaded, when logging the chat, we have to know the player's database id
            if (!playerInfo.ContainsKey(player.userID))
            {
                player.ChatMessage("<color=#d00>ERROR</color>  We are still loading your player info, when this message keeps popping up, please contact us at our Discord server. Or visit our website at rust.gamelimits.com for more information.");
                return false;
            }

            PlayerInfo info = playerInfo[player.userID];

            // Check if the player has special permissions on the server
            string tags = "";

            if (info.IsVip())
                tags += "<color=#88B71B>[VIP]</color> ";

            // Send the chat to the game
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                if (p == null)
                    continue;

                p.SendConsoleCommand("chat.add", player.userID, $"{tags}<color=#0E84B7>{player.displayName}</color> {string.Join(" ", args.Args)}");
            }

            // Send the chat to the console
            Log("Chat", $"[{player.displayName}] {string.Join(" ", args.Args)}", LogType.DEFAULT, false);
            Puts($"[{player.displayName}] {string.Join(" ", args.Args)}"); // Using Puts because RCON needs this info..

            // Send the chat to the database
            MInsert(MBuild("INSERT INTO chatlogs (user_id, display_name, message) VALUES (@0, @1, @2);", info.id, player.displayName, string.Join(" ", args.Args)));

            return false;
        }

        void OnRconCommand(IPAddress ip, string command, string[] args)
        {
            if (command != "gl_chat_admin" || args == null)
                return;

            SendAdminMessage(string.Join(" ", args));
        }
        #endregion

        void SendAdminMessage(string message)
        {
            // Send the chat to the game
            foreach (BasePlayer p in BasePlayer.activePlayerList)
            {
                if (p == null)
                    continue;

                p.SendConsoleCommand("chat.add", 0, $"<color=#B70E84>Admin</color> {message}");
            }

            // Send the chat to the console
            Log("Chat", $"[Admin] {message}");

            // Send the chat to the database
            MInsert(MBuild("INSERT INTO chatlogs (display_name, message) VALUES (@0, @1);", "Admin", message));
        }
    }
}
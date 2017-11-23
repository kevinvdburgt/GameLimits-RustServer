using Oxide.Game.Rust.Cui;
using ProtoBuf;
using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using static Oxide.Plugins.GameLimits;

namespace Oxide.Plugins
{
    [Info("Friends", "Game Limits", "1.0.0")]
    [Description("The friends module")]

    public class GameLimitsFriends : RustPlugin
    {
        #region Server Events
        [ConsoleCommand("friend")]
        private void OnConsoleCommandInfoClose(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            var player = (BasePlayer)arg.Connection.player;
            PlayerInfo info = playerInfo[player.userID];

            switch (arg.Args[0])
            {
                case "open":
                    if (arg.Args.Length < 2)
                        break;
                    
                    GetFriends(player, friends => 
                        CreateGUI(player, friends, Convert.ToInt32(arg.Args[1])), true);
                    break;

                case "close":
                    DestroyGUI(player);
                    break;

                case "add":
                    if (arg.Args.Length < 2)
                        break;

                    DestroyGUI(player);

                    MQuery(MBuild("SELECT * FROM users WHERE steam_id=@0 LIMIT 1;", Convert.ToUInt64(arg.Args[1])), records =>
                    {
                        if (records.Count == 0)
                        {
                            SendReply(player, $"Player not found");
                            return;
                        }

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == info.id)
                        {
                            SendReply(player, "You cannot add yourself to your friendlist.");
                            return;
                        }

                        AddFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result == 0)
                                SendReply(player, $"{records[0]["display_name"]} is already in your friends list.");
                            else
                                SendReply(player, $"{records[0]["display_name"]} added to your friendlist.");
                        });
                    });
                    break;

                case "remove":
                    if (arg.Args.Length < 2)
                        break;

                    DestroyGUI(player);

                    MQuery(MBuild("SELECT * FROM users WHERE steam_id=@0 LIMIT 1;", Convert.ToUInt64(arg.Args[1])), records =>
                    {
                        if (records.Count == 0)
                        {
                            SendReply(player, $"Player not found");
                            return;
                        }

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == info.id)
                        {
                            SendReply(player, "You cannot remove yourself from your friendlist.");
                            return;
                        }

                        RemoveFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result == 0)
                                SendReply(player, $"{records[0]["display_name"]} is not in your friendlist.");
                            else
                                SendReply(player, $"{records[0]["display_name"]} removed from your friendlist.");
                        });
                    });
                    break;
            }
        }

        [ChatCommand("f")]
        private void OnFChatCommand(BasePlayer player, string command, string[] args)
        {
            OnFriendChatCommand(player, command, args);
        }

        [ChatCommand("friend")]
        private void OnFriendChatCommand(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            // Check the syntax
            string syntax = "/friend list <color=#999>Shows a list of your friends</color>\n" +
                "/friend add \"name\" <color=#999>Add a friend to your friendlist</color>\n" +
                "/friend remove \"name\" <color=#999>Removes a friend from your friendlist</color>";

            if (args.Length < 1 || args.Length == 1 && !(args[0].ToLower() == "list"))
            {
                GetFriends(player, friends =>
                    CreateGUI(player, friends, 0), true);
                SendReply(player, syntax);
                return;
            }

            // The player has to be loaded, when logging the chat, we have to know the player's database id
            if (!playerInfo.ContainsKey(player.userID))
            {
                player.ChatMessage("<color=#d00>ERROR</color>  We are still loading your player info, when this message keeps popping up, please contact us at our Discord server. Or visit our website at rust.gamelimits.com for more information.");
                return;
            }

            PlayerInfo info = playerInfo[player.userID];

            switch (args[0].ToLower())
            {
                case "list":
                    MQuery(MBuild("SELECT users.display_name AS display_name, users.steam_id AS uid FROM friends LEFT JOIN users ON friends.with_user_id = users.id WHERE friends.user_id = @0;", info.id), records =>
                    {
                        if (records.Count == 0)
                        {
                            SendReply(player, "You dont have any friends, you can add them with /friend add \"name\"");
                            return;
                        }

                        string friendList = "";

                        foreach (var record in records)
                        {
                            ulong uid = Convert.ToUInt64(record["uid"]);

                            if (playerInfo.ContainsKey(uid))
                                friendList += $"- {record["display_name"]} <color=#0D0>(online)</color>\n";
                            else
                                friendList += $"- {record["display_name"]} <color=#999>(offline)</color>\n";
                        }

                        SendReply(player, friendList.TrimEnd('\n'));
                    });
                    break;

                case "add":
                    // Check if the user exists
                    string searchParam = $"%{args[1]}%";
                    MQuery(MBuild("SELECT * FROM users WHERE display_name LIKE @0;", searchParam), records =>
                    {
                        if (records.Count == 0)
                        {
                            SendReply(player, $"Player \"{args[1]}\" not found");
                            return;
                        }

                        if (records.Count > 1)
                        {
                            SendReply(player, $"Found multiple players with \"{args[1]}\", please be more specific.");
                            return;
                        }

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == info.id)
                        {
                            SendReply(player, "You cannot add yourself to your friendlist.");
                            return;
                        }

                        AddFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result == 0)
                                SendReply(player, $"{records[0]["display_name"]} is already in your friends list.");
                            else
                                SendReply(player, $"{records[0]["display_name"]} added to your friendlist.");
                        });
                    });
                    break;

                case "remove":
                    // Check if the user exists
                    MQuery(MBuild("SELECT * FROM users WHERE display_name LIKE @0;", $"%{args[1]}%"), records =>
                    {
                        if (records.Count == 0)
                        {
                            SendReply(player, $"Player \"{args[1]}\" not found");
                            return;
                        }

                        if (records.Count > 1)
                        {
                            SendReply(player, $"Found multiple players with \"{args[1]}\", please be more specific.");
                            return;
                        }

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == info.id)
                        {
                            SendReply(player, "You cannot remove yourself from your friendlist.");
                            return;
                        }

                        RemoveFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result == 0)
                                SendReply(player, $"{records[0]["display_name"]} is not in your friendlist.");
                            else
                                SendReply(player, $"{records[0]["display_name"]} removed from your friendlist.");
                        });
                    });
                    break;

                default:
                    SendReply(player, syntax);
                    return;
            }
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity target)
        {
            if (!(target is BasePlayer) || turret.OwnerID <= 0)
                return null;

            var player = (BasePlayer)target;

            if (turret.IsAuthed(player) || !HasFriend(turret.OwnerID, player.userID))
                return null;

            turret.authorizedPlayers.Add(new PlayerNameID
            {
                userid = player.userID,
                username = player.displayName
            });
            return false;
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        {
            if (!(@lock is CodeLock) || @lock.GetParentEntity().OwnerID <= 0)
                return null;

            if (HasFriend(@lock.GetParentEntity().OwnerID, player.userID))
            {
                var codeLock = (CodeLock)@lock;
                var whitelistPlayers = (List<ulong>)codeLock.whitelistPlayers;
                if (!whitelistPlayers.Contains(player.userID))
                    whitelistPlayers.Add(player.userID);
            }
            return null;
        }
        #endregion

        #region Functions
        void AddFriend(BasePlayer player, ulong friendUid, int friendId, Action<int> callback = null)
        {
            PlayerInfo info = playerInfo[player.userID];

            MQuery(MBuild("SELECT * FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", info.id, friendId), records =>
            {
                if (records.Count == 1)
                {
                    callback?.Invoke(0);
                    return;
                }

                if (playerFriends.ContainsKey(player.userID))
                    playerFriends[player.userID].Add(friendUid);
                else
                    playerFriends.Add(player.userID, new HashSet<ulong> { friendUid });

                MInsert(MBuild("INSERT INTO friends (user_id, with_user_id) VALUES (@0, @1);", info.id, friendId), callback);
            });
        }

        void RemoveFriend(BasePlayer player, ulong friendUid, int friendId, Action<int> callback = null)
        {
            PlayerInfo info = playerInfo[player.userID];

            MQuery(MBuild("SELECT * FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", info.id, friendId), records =>
            {
                if (records.Count == 0)
                {
                    callback?.Invoke(0);
                    return;
                }

                if (playerFriends.ContainsKey(player.userID))
                    playerFriends[player.userID].Remove(friendUid);

                // When there are no users connected to this as friend, remove..
                if (playerFriends[player.userID].Count == 0)
                    playerFriends.Remove(player.userID);

                // Remove old friend from the turret whitelist
                var turrets = UnityEngine.Object.FindObjectsOfType<AutoTurret>();
                foreach (AutoTurret turret in turrets)
                {
                    if (turret.OwnerID != player.userID)
                        continue;

                    turret.authorizedPlayers.RemoveAll(a => a.userid == friendUid);
                }

                // Remove old friend from the codelock shares
                var codeLocks = UnityEngine.Object.FindObjectsOfType<CodeLock>();
                foreach (CodeLock codeLock in codeLocks)
                {
                    var entity = codeLock.GetParentEntity();
                    if (entity == null || entity.OwnerID != player.userID)
                        continue;

                    var whitelistPlayers = (List<ulong>)codeLock.whitelistPlayers;
                    whitelistPlayers.RemoveAll(a => a == friendUid);
                }

                MDelete(MBuild("DELETE FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", info.id, friendId), callback);
            });
        }

        void GetFriends(BasePlayer player, Action<List<FriendItem>> callback, bool includeNonFriends = false)
        {
            if (player == null || !playerInfo.ContainsKey(player.userID))
                return;

            PlayerInfo info = playerInfo[player.userID];

            MQuery(MBuild("SELECT users.display_name AS display_name, users.steam_id AS uid FROM friends LEFT JOIN users ON friends.with_user_id = users.id WHERE friends.user_id = @0;", info.id), records =>
            {
                List<FriendItem> friends = new List<FriendItem>();

                foreach (var record in records)
                {
                    ulong uid = Convert.ToUInt64(record["uid"]);

                    friends.Add(new FriendItem()
                    {
                        userId = uid,
                        displayName = Convert.ToString(record["display_name"]),
                        isFriend = true,
                        isOnline = playerInfo.ContainsKey(uid),
                    });
                }

                if (includeNonFriends)
                    foreach (BasePlayer p in BasePlayer.activePlayerList)
                        if (player != null && p != player && !HasFriend(player.userID, p.userID))
                            friends.Add(new FriendItem()
                            {
                                userId = p.userID,
                                displayName = p.displayName,
                                isFriend = false,
                                isOnline = false,
                            });

                callback(friends);
            });
        }
        #endregion

        #region GUI
        void CreateGUI(BasePlayer player, List<FriendItem> players, int index = 0)
        {
            DestroyGUI(player);

            // Main container
            var container = UI.CreateElementContainer("gl_friends", "0 0 0 0.99", "0.35 0.05", "0.65 0.95", true);

            // Close button
            UI.CreateButton(ref container, "gl_friends", "0.8 0.2 0.2 1", "Close", 12, $"{(index > 0 ? "0.1" : "0")} 0", $"{(players.Count - (index * 15) > 15 ? "0.9" : "0.997")} 0.04", "friend close");

            // Index buttons
            if (index > 0)
                UI.CreateButton(ref container, "gl_friends", "0.12 0.38 0.57 1", "Prev", 12, "0 0", "0.09 0.04", $"friend open {index - 1}");

            if (players.Count - (index * 15) > 15)
                UI.CreateButton(ref container, "gl_friends", "0.12 0.38 0.57 1", "Next", 12, "0.91 0", "0.997 0.04", $"friend open {index + 1}");

            int i = 0;
            foreach (FriendItem item in players.Skip(index * 15).Take(15))
                CreateRecordGUI(ref container, item, i++);

            CuiHelper.AddUi(player, container);
        }


        void DestroyGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_friends");
        }

        void CreateRecordGUI(ref CuiElementContainer container, FriendItem friend, int index)
        {
            Vector2 dim = new Vector2(1f, 0.05f);
            Vector2 ori = new Vector2(0.0f, 1f);
            float offset = (0.013f + dim.y) * (index + 1);
            Vector2 off = new Vector2(0f, offset);
            Vector2 min = ori - off;
            Vector2 max = min + dim;

            UI.CreatePanel(ref container, "gl_friends", "1 1 1 0.01", $"{min.x} {min.y}", $"{max.x} {max.y}");
            UI.CreateLabel(ref container, "gl_friends", $"{friend.displayName}{(friend.isFriend && friend.isOnline ? "  <color=#0D0>(Online)</color>" : "")}", 12, $"{min.x + 0.02} {min.y}", $"{max.x} {max.y}", TextAnchor.MiddleLeft, "1 1 1 1", 0);

            if (friend.isFriend)
                UI.CreateButton(ref container, "gl_friends", "0.57 0.21 0.11 1", $"Remove", 12, $"{min.x + 0.8} {min.y + 0.01}", $"{max.x - 0.02} {max.y - 0.01}", $"friend remove {friend.userId}");
            else
                UI.CreateButton(ref container, "gl_friends", "0.41 0.5 0.25 1", $"Add", 12, $"{min.x + 0.8} {min.y + 0.01}", $"{max.x - 0.02} {max.y - 0.01}", $"friend add {friend.userId}");
        }
        #endregion

        public class FriendItem
        {
            public ulong userId;
            public string displayName;
            public bool isFriend = false;
            public bool isOnline = false;
        }
    }
}

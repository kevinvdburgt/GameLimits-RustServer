using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Friends", "Game Limits", "2.0.0")]
    [Description("The friends plugin")]

    public class Friends : RustPlugin
    {
        private static Friends plug;

        public static Dictionary<ulong, HashSet<ulong>> friends = new Dictionary<ulong, HashSet<ulong>>();

        #region Oxide Hooks
        private void Init()
        {
            plug = this;
            LoadFriends();
        }

        [ConsoleCommand("friend")]
        private void OnConsoleCommand(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            BasePlayer player = (BasePlayer)arg.Connection.player;
            PlayerData.PData pdata = PlayerData.Get(player);

            if (pdata == null)
                return;

            switch (arg.GetString(0))
            {
                case "index":
                    GetFriends(player, players => UpdateUI(player, players, arg.GetInt(1)), true);
                    break;

                case "close":
                    DestroyUI(player);
                    break;

                case "add":
                    DestroyUI(player);

                    Database.Query(Database.Build("SELECT * FROM users WHERE steam_id=@0 LIMIT 1;", arg.GetUInt64(1)), records =>
                    {
                        if (records.Count == 0)
                            return;

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == pdata.id)
                        {
                            player.ChatMessage("You cannot add yourself to your friendlist.");
                            return;
                        }

                        AddFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result > 0)
                                player.ChatMessage($"{records[0]["display_name"]} added to your friendlist.");
                            else if (result == 0)
                                player.ChatMessage($"{records[0]["display_name"]} is already in your friends list.");
                            else
                                player.ChatMessage($"Something went wrong (error code: {result})");
                        });
                    });
                    break;

                case "remove":
                    DestroyUI(player);

                    Database.Query(Database.Build("SELECT * FROM users WHERE steam_id=@0 LIMIT 1;", arg.GetUInt64(1)), records =>
                    {
                        if (records.Count == 0)
                            return;

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == pdata.id)
                        {
                            player.ChatMessage("You cannot remove yourself from your friendlist.");
                            return;
                        }

                        RemoveFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result > 0)
                                player.ChatMessage($"{records[0]["display_name"]} removed from your friendlist.");
                            else if (result == 0)
                                player.ChatMessage($"{records[0]["display_name"]} is not in your friendlist.");
                            else
                                player.ChatMessage($"Something went wrong (error code: {result})");
                        });
                    });
                    break;
            }
        }

        [ChatCommand("f")]
        private void OnChatCommandF(BasePlayer player, string command, string[] args)
        {
            OnChatCommandFriend(player, command, args);
        }

        [ChatCommand("friend")]
        private void OnChatCommandFriend(BasePlayer player, string command, string[] args)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null)
                return;

            // Check the syntax
            string syntax = "/friend list <color=#999>Shows a list of your friends</color>\n" +
                "/friend add \"name\" <color=#999>Add a friend to your friendlist</color>\n" +
                "/friend remove \"name\" <color=#999>Removes a friend from your friendlist</color>";

            if (args.Length < 1 || args.Length == 1 && !(args[0].ToLower() == "list"))
            {
                GetFriends(player, players => CreateUI(player, players), true);
                SendReply(player, syntax);
                return;
            }
            
            switch (args[0].ToLower())
            {
                case "list":
                    Database.Query(Database.Build("SELECT users.display_name AS display_name, users.steam_id AS uid FROM friends LEFT JOIN users ON friends.with_user_id = users.id WHERE friends.user_id = @0;", pdata.id), records =>
                    {
                        if (records.Count == 0)
                        {
                            player.ChatMessage("You dont have any friends, you can add them with /friend add \"name\"");
                            return;
                        }

                        string list = "";

                        foreach (var record in records)
                        {
                            ulong uid = Convert.ToUInt64(record["uid"]);

                            if (PlayerData.Get(uid) == null)
                                list += $"- {record["display_name"]} <color=#999>(offline)</color>\n";
                            else
                                list += $"- {record["display_name"]} <color=#0D0>(online)</color>\n";

                        }

                        player.ChatMessage(list.TrimEnd('\n'));
                    });

                    break;

                case "add":
                    Database.Query(Database.Build("SELECT * FROM users WHERE display_name LIKE @0;", $"%{args[1]}%"), records =>
                    {
                        

                        if (records.Count == 0)
                        {
                            player.ChatMessage("Player \"{args[1]}\" not found");
                            return;
                        }

                        if (records.Count > 1)
                        {
                            player.ChatMessage($"Found multiple players with \"{args[1]}\", please be more specific.");
                            return;
                        }

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == pdata.id)
                        {
                            player.ChatMessage("You cannot add yourself to your friendlist.");
                            return;
                        }

                        AddFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result > 0)
                                player.ChatMessage($"{records[0]["display_name"]} added to your friendlist.");
                            else if (result == 0)
                                player.ChatMessage($"{records[0]["display_name"]} is already in your friends list.");
                            else
                                player.ChatMessage($"Something went wrong (error code: {result})");
                        });
                    });
                    break;

                case "remove":
                    Database.Query(Database.Build("SELECT * FROM users WHERE display_name LIKE @0;", $"%{args[1]}%"), records =>
                    {
                        if (records.Count == 0)
                        {
                            player.ChatMessage("Player \"{args[1]}\" not found");
                            return;
                        }

                        if (records.Count > 1)
                        {
                            player.ChatMessage($"Found multiple players with \"{args[1]}\", please be more specific.");
                            return;
                        }

                        int foundUser = Convert.ToInt32(records[0]["id"]);
                        ulong foundUserId = Convert.ToUInt64(records[0]["steam_id"]);

                        if (foundUser == pdata.id)
                        {
                            player.ChatMessage("You cannot remove yourself from your friendlist.");
                            return;
                        }

                        RemoveFriend(player, foundUserId, foundUser, result =>
                        {
                            if (result > 0)
                                player.ChatMessage($"{records[0]["display_name"]} removed from your friendlist.");
                            else if (result == 0)
                                player.ChatMessage($"{records[0]["display_name"]} is not in your friendlist.");
                            else
                                player.ChatMessage($"Something went wrong (error code: {result})");
                        });
                    });
                    break;

                default:
                    player.ChatMessage(syntax);
                    break;
            }
        }

        private object OnTurretTarget(AutoTurret turret, BaseCombatEntity target)
        {
            if (!(target is BasePlayer) || turret.OwnerID <= 0)
                return null;

            BasePlayer player = (BasePlayer)target;

            if (turret.IsAuthed(player) || !HasFriend(turret.OwnerID, player.userID))
                return null;

            turret.authorizedPlayers.Add(new ProtoBuf.PlayerNameID
            {
                userid = player.userID,
                username = player.displayName,
            });

            return false;
        }

        private object CanUseLockedEntity(BasePlayer player, BaseLock @lock)
        {
            if (!(@lock is CodeLock) || @lock.GetParentEntity().OwnerID <= 0)
                return null;

            if (HasFriend(@lock.GetParentEntity().OwnerID, player.userID))
            {
                CodeLock codeLock = (CodeLock)@lock;
                List<ulong> whitelisted = (List<ulong>)codeLock.whitelistPlayers;
                if (whitelisted.Contains(player.userID))
                    whitelisted.Add(player.userID);
            }

            return null;
        }
        #endregion

        #region Functions
        private void LoadFriends()
        {
            if (!Database.Ready())
            {
                timer.In(1f, () => LoadFriends());
                return;
            }

            Database.Query(Database.Build("SELECT a.steam_id AS uid, b.steam_id AS with_uid FROM friends LEFT JOIN users AS a ON friends.user_id = a.id LEFT JOIN users AS b ON friends.with_user_id = b.id;"), records =>
            {
                friends.Clear();

                foreach (var record in records)
                {
                    ulong uid = Convert.ToUInt64(record["uid"]);
                    ulong with_uid = Convert.ToUInt64(record["with_uid"]);

                    if (friends.ContainsKey(uid))
                        friends[uid].Add(with_uid);
                    else
                        friends.Add(uid, new HashSet<ulong> { with_uid });
                }

                Puts($"Loaded {records.Count} friendships.");
            });
        }

        public static bool HasFriend(ulong userUid, ulong friendUid)
        {
            return friends.ContainsKey(userUid) && friends[userUid].Contains(friendUid);
        }

        public static void AddFriend(BasePlayer player, ulong friendUid, int friendId, Action<int> callback = null)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null)
            {
                callback?.Invoke(-1);
                return;
            }

            Database.Query(Database.Build("SELECT * FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", pdata.id, friendId), records =>
            {
                if (records.Count == 1)
                {
                    callback?.Invoke(0);
                    return;
                }

                if (friends.ContainsKey(player.userID))
                    friends[player.userID].Add(friendUid);
                else
                    friends.Add(player.userID, new HashSet<ulong> { friendUid });

                plug.Puts($"[{pdata.id}:{player.UserIDString}] added [{friendId}:{friendUid}] to their friendlist.");

                Database.Insert(Database.Build("INSERT INTO friends (user_id, with_user_id) VALUES (@0, @1);", pdata.id, friendId), callback);
            });
        }

        public static void RemoveFriend(BasePlayer player, ulong friendUid, int friendId, Action<int> callback = null)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null)
            {
                callback?.Invoke(-1);
                return;
            }

            Database.Query(Database.Build("SELECT * FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", pdata.id, friendId), records =>
            {
                if (records.Count == 0)
                {
                    callback?.Invoke(0);
                    return;
                }

                if (friends.ContainsKey(player.userID))
                    friends[player.userID].Remove(friendUid);

                if (friends.ContainsKey(player.userID) && friends[player.userID].Count == 0)
                    friends.Remove(player.userID);

                // Remove from their turrets
                foreach (AutoTurret turret in UnityEngine.Object.FindObjectsOfType<AutoTurret>())
                    if (turret.OwnerID == player.userID)
                        turret.authorizedPlayers.RemoveAll(u => u.userid == friendUid);

                // Remove from their codelocks
                foreach (CodeLock codeLock in UnityEngine.Object.FindObjectsOfType<CodeLock>())
                {
                    BaseEntity entity = codeLock.GetParentEntity();
                    if (entity == null || entity.OwnerID != player.userID)
                        continue;

                    codeLock.whitelistPlayers.RemoveAll(u => u == friendUid);
                }

                plug.Puts($"[{pdata.id}:{player.UserIDString}] removed [{friendId}:{friendUid}] from their friendlist.");

                Database.Delete(Database.Build("DELETE FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", pdata.id, friendId), callback);
            });
        }

        public static void GetFriends(BasePlayer player, Action<List<FriendItem>> callback, bool includeNonFriends = false)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            List<FriendItem> friendList = new List<FriendItem>();

            if (player == null || pdata == null)
            {
                callback?.Invoke(friendList);
                return;
            }

            Database.Query(Database.Build("SELECT users.display_name AS display_name, users.steam_id AS uid FROM friends LEFT JOIN users ON friends.with_user_id = users.id WHERE friends.user_id = @0;", pdata.id), records => 
            {
                foreach (var record in records)
                {
                    ulong uid = Convert.ToUInt64(record["uid"]);

                    friendList.Add(new FriendItem()
                    {
                        userId = uid,
                        displayName = Convert.ToString(record["display_name"]),
                        isFriend = true,
                        isOnline = PlayerData.datas.ContainsKey(uid),
                    });
                }

                if (includeNonFriends)
                    foreach (BasePlayer p in BasePlayer.activePlayerList)
                        if (p != null && p != player && !HasFriend(player.userID, p.userID))
                            friendList.Add(new FriendItem()
                            {
                                userId = p.userID,
                                displayName = p.displayName,
                                isFriend = false,
                                isOnline = false,
                            });

                callback?.Invoke(friendList);
            });
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player, List<FriendItem> players)
        {
            if (player == null)
                return;

            DestroyUI(player);

            CuiElementContainer container = Helper.UI.Container("ui_friends", "0 0 0 .99", "0.35 0.05", "0.65 0.95", true);

            Helper.UI.Add(player, container);

            UpdateUI(player, players);
        }

        private void DestroyUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_friends");
        }

        private void UpdateUI(BasePlayer player, List<FriendItem> players, int index = 0)
        {
            Helper.UI.Destroy(player, "ui_friends_toolbar");
            Helper.UI.Destroy(player, "ui_friends_entries");

            // Create the home items themself
            CuiElementContainer entriesContainer = Helper.UI.Container("ui_friends_entries", "0 0 0 0", "0 0.04", "0.997 1", false, "ui_friends");
            int i = 0;
            foreach (var friend in players.Skip(index * 15).Take(15))
            {
                Vector2 dimension = new Vector2(0.997f, 0.05f);
                Vector2 origin = new Vector2(0.0f, 1f);
                Vector2 offset = new Vector2(0f, (0.013f + dimension.y) * (i + 1));
                Vector2 min = origin - offset;
                Vector2 max = min + dimension;

                Helper.UI.Panel(ref entriesContainer, "ui_friends_entries", "1 1 1 0.02", $"{min.x} {min.y}", $"{max.x} {max.y}");
                Helper.UI.Label(ref entriesContainer, "ui_friends_entries", "1 1 1 1", $"{friend.displayName}{(friend.isFriend && friend.isOnline ? "  <color=#0D0>(Online)</color>" : "")}", 12, $"{min.x + 0.02} {min.y}", $"{max.x} {max.y}", TextAnchor.MiddleLeft);

                if (friend.isFriend)
                    Helper.UI.Button(ref entriesContainer, "ui_friends_entries", "0.57 0.21 0.11 1", "Remove", 12, $"{min.x + 0.8} {min.y + 0.01}", $"{max.x - 0.02} {max.y - 0.01}", $"friend remove {friend.userId}");
                else
                    Helper.UI.Button(ref entriesContainer, "ui_friends_entries", "0.41 0.5 0.25 1", "Add", 12, $"{min.x + 0.8} {min.y + 0.01}", $"{max.x - 0.02} {max.y - 0.01}", $"friend add {friend.userId}");

                i++;
            }

            // Create toolbar container
            CuiElementContainer toolbarContainer = Helper.UI.Container("ui_friends_toolbar", "0 0 0 0", "0 0", "0.997 0.04", false, "ui_friends");

            if (index > 0)
                Helper.UI.Button(ref toolbarContainer, "ui_friends_toolbar", "0.12 0.38 0.57 1", "< Previous", 12, "0 0", "0.2 0.96", $"friend index {index - 1}");

            if (players.Count - (index * 15) > 15)
                Helper.UI.Button(ref toolbarContainer, "ui_friends_toolbar", "0.12 0.38 0.57 1", "Next >", 12, "0.8 0", "0.997 0.96", $"friend index {index + 1}");

            Helper.UI.Button(ref toolbarContainer, "ui_friends_toolbar", "0.8 0.2 0.2 1", "Close Friendslist", 12, $"{(index > 0 ? "0.21" : "0")} 0", $"{(players.Count - (index * 15) > 15 ? "0.79" : "0.997")} 0.96", $"friend close");

            Helper.UI.Add(player, entriesContainer);
            Helper.UI.Add(player, toolbarContainer);
        }
        #endregion

        #region Classes
        public class FriendItem
        {
            public ulong userId;
            public string displayName;
            public bool isFriend = false;
            public bool isOnline = false;
        }
        #endregion
    }
}

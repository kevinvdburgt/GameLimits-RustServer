using ProtoBuf;
using System;
using System.Collections.Generic;
using static Oxide.Plugins.GameLimits;

namespace Oxide.Plugins
{
    [Info("Friends", "Game Limits", "1.0.0")]
    [Description("The friends module")]

    public class GameLimitsFriends : RustPlugin
    {
        #region Server Events
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

                        MQuery(MBuild("SELECT * FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", info.id, foundUser), records2 =>
                        {
                            if (records2.Count > 0)
                            {
                                SendReply(player, $"{records[0]["display_name"]} is already in your friends list.");
                                return;
                            }

                            MInsert(MBuild("INSERT INTO friends (user_id, with_user_id) VALUES (@0, @1);", info.id, foundUser), records3 => 
                            {
                                SendReply(player, $"{records[0]["display_name"]} added to your friends list.");

                                AddFriend(player, foundUserId);
                            });
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

                        MQuery(MBuild("SELECT * FROM friends WHERE user_id=@0 AND with_user_id=@1 LIMIT 1;", info.id, foundUser), records2 =>
                        {
                            if (records2.Count == 0)
                            {
                                SendReply(player, $"{records[0]["display_name"]} is not on your friends list.");
                                return;
                            }

                            MDelete(MBuild("DELETE FROM friends WHERE id=@0 LIMIT 1;", records2[0]["id"]), records3 =>
                            {
                                SendReply(player, $"{records[0]["display_name"]} removed from your friends list.");

                                RemoveFriend(player, foundUserId);
                            });
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
                var whitelistPlayers = (List<ulong>) codeLock.whitelistPlayers;
                if (!whitelistPlayers.Contains(player.userID))
                    whitelistPlayers.Add(player.userID);
            }
            return null;
        }
        #endregion

        #region Functions
        void AddFriend(BasePlayer player, ulong friendUid)
        {
            if (playerFriends.ContainsKey(player.userID))
                playerFriends[player.userID].Add(friendUid);
            else
                playerFriends.Add(player.userID, new HashSet<ulong> { friendUid });
        }

        void RemoveFriend(BasePlayer player, ulong friendUid)
        {
            if (playerFriends.ContainsKey(player.userID))
                playerFriends[player.userID].Remove(friendUid);

            // When there are no users connected to this as friend, remove..
            if (playerFriends[player.userID].Count == 0)
                playerFriends.Remove(player.userID);
        }
        #endregion
    }
}

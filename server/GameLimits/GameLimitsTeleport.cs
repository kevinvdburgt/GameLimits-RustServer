using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Teleport", "Game Limits", "1.0.0")]
    [Description("The teleport module")]

    public class GameLimitsTeleport : RustPlugin
    {
        private readonly int triggerLayer = LayerMask.GetMask("Trigger");
        private Dictionary<ulong, Dictionary<string, Vector3>> homes = new Dictionary<ulong, Dictionary<string, Vector3>>();
        private List<TeleportRequestInfo> teleportRequests = new List<TeleportRequestInfo>();

        #region Functions
        private string CanTeleportFrom(BasePlayer player)
        {
            if (!player.IsAlive())
                return "dead";
            if (player.IsWounded())
                return "wounded";
            if (!player.CanBuild())
                return "building blocked";
            if (player.IsSwimming())
                return "swimming";
            if (player.inventory.crafting.queue.Count > 0)
                return "crafting";
            return null;
        }

        private string CanTeleportTo(BasePlayer player, Vector3 position)
        {
            List<Collider> colliders = Facepunch.Pool.GetList<Collider>();
            Vis.Colliders(position, 0.1f, colliders, triggerLayer);
            foreach (Collider collider in colliders)
            {
                BuildingPrivlidge tc = collider.GetComponentInParent<BuildingPrivlidge>();
                if (tc == null)
                    continue;

                if (!tc.IsAuthed(player))
                {
                    Facepunch.Pool.FreeList(ref colliders);
                    return "building blocked";
                }
            }
            return null;
        }

        private void AddHome(BasePlayer player, string name)
        {
            if (player == null || !homes.ContainsKey(player.userID))
                return;

            PlayerInfo info = playerInfo[player.userID];

            if (homes[player.userID].ContainsKey(name))
            {
                player.ChatMessage($"<color=#d00>ERROR</color> The home with the name \"{name}\" already exists.");
                return;
            }

            string teleportFrom = CanTeleportFrom(player);
            if (teleportFrom != null)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> Cannot create homepoint ({teleportFrom}).");
                return;
            }

            if (!HasMinimumVipRank(info, "vip") && homes[player.userID].Count >= 1)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> Unable to set your home here, you have reached the maximum of 1 home!");
                return;
            }
            else if (HasMinimumVipRank(info, "vip") && homes[player.userID].Count >= 3)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> Unable to set your home here, you have reached the maximum of 3 homes!");
                return;
            }

            homes[player.userID].Add(name, player.transform.position);

            MInsert(MBuild("INSERT INTO homes (user_id, name, x, y, z) VALUES (@0, @1, @2, @3, @4);",
                info.id,
                name,
                player.transform.position.x,
                player.transform.position.y,
                player.transform.position.z));

            player.ChatMessage($"Your homepoint \"{name}\" has been added.");
        }

        private void DeleteHome(BasePlayer player, string name)
        {
            if (player == null || !homes.ContainsKey(player.userID))
                return;

            PlayerInfo info = playerInfo[player.userID];

            if (!homes[player.userID].ContainsKey(name))
            {
                player.ChatMessage($"<color=#d00>ERROR</color> The home with the name \"{name}\" doest not exists.");
                return;
            }

            homes[player.userID].Remove(name);

            MDelete(MBuild("DELETE FROM homes WHERE user_id=@0 AND name=@1 LIMIT 1;", info.id, name));

            player.ChatMessage($"Your homepoint \"{name}\" has been deleted.");
        }

        private void TeleportHome(BasePlayer player, string name)
        {
            if (player == null || !homes.ContainsKey(player.userID))
                return;

            PlayerInfo info = playerInfo[player.userID];

            if (!homes[player.userID].ContainsKey(name))
            {
                player.ChatMessage($"<color=#d00>ERROR</color> The home with the name \"{name}\" doest not exists.");
                return;
            }

            int cooldown = info.HasCooldown("teleport_home");
            if (cooldown > 0)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> Teleport cooldown {FormatTime(cooldown)}.");
                return;
            }

            Vector3 to = homes[player.userID][name];

            string teleportFrom = CanTeleportFrom(player);
            if (teleportFrom != null)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> You cannot teleport from your current location ({teleportFrom}).");
                return;
            }

            string teleportTo = CanTeleportTo(player, to);
            if (teleportTo != null)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> You cannot teleport to your home ({teleportTo}).");
                return;
            }

            Teleport(player, to, 10);
        }

        private void Teleport(BasePlayer player, Vector3 target, int countdown = 0)
        {
            if (player == null)
                return;

            if (countdown > 0)
            {
                player.ChatMessage($"Teleporting in {FormatTime(countdown)}");
                timer.Once(countdown, () => Teleport(player, target));
                return;
            }

            PlayerInfo info = playerInfo[player.userID];
            
            string teleportFrom = CanTeleportFrom(player);
            if (teleportFrom != null)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> You cannot teleport from your current location ({teleportFrom}).");
                return;
            }

            string teleportTo = CanTeleportTo(player, target);
            if (teleportTo != null)
            {
                player.ChatMessage($"<color=#d00>ERROR</color> You cannot teleport to your home ({teleportTo}).");
                return;
            }

            if (HasMinimumVipRank(info, "vip"))
                info.AddCooldown("teleport_home", 60 * 5);
            else
                info.AddCooldown("teleport_home", 60 * 20);

            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "StartLoading");

            if (!player.IsSleeping())
            {
                player.SetPlayerFlag(BasePlayer.PlayerFlags.Sleeping, true);
                if (!BasePlayer.sleepingPlayerList.Contains(player))
                    BasePlayer.sleepingPlayerList.Add(player);
                player.CancelInvoke("InventoryUpdate");
            }

            player.MovePosition(target);
            if (player.net?.connection != null)
                player.ClientRPCPlayer(null, player, "ForcePositionTo", target);
            if (player.net?.connection != null)
                player.SetPlayerFlag(BasePlayer.PlayerFlags.ReceivingSnapshot, true);
            player.UpdateNetworkGroup();
            //player.UpdatePlayerCollider(true);
            player.SendNetworkUpdateImmediate(false);
            if (player.net?.connection == null)
                return;

            try
            {
                player.ClearEntityQueue(null);
            }
            catch { }
            player.SendFullSnapshot();
        }

        private void TeleportRequest(BasePlayer player, BasePlayer target)
        {

        }

        private void AcceptTeleportRequest(BasePlayer player)
        {

        }

        private void DenyTeleportRequest(BasePlayer player)
        {

        }

        private void CancelTeleportRequest(BasePlayer player)
        {

        }

        private void LoadPlayer(BasePlayer player)
        {
            if (player == null || homes.ContainsKey(player.userID))
                return;

            if (!playerInfo.ContainsKey(player.userID))
            {
                timer.Once(0.5f, () => LoadPlayer(player));
                return;
            }

            PlayerInfo info = playerInfo[player.userID];

            MQuery(MBuild("SELECT * FROM homes WHERE user_id=@0;", info.id), records =>
            {
                Dictionary<string, Vector3> playerHomes = new Dictionary<string, Vector3>();

                foreach (var record in records)
                {
                    playerHomes.Add(Convert.ToString(record["name"]), new Vector3(
                        Convert.ToSingle(record["x"]),
                        Convert.ToSingle(record["y"]),
                        Convert.ToSingle(record["z"])));
                }

                homes.Add(player.userID, playerHomes);

                Puts($"Loaded {records.Count} homes for [{info.id}:{player.UserIDString}]");
            });
        }
        #endregion

        #region Classes
        private class TeleportRequestInfo
        {
            public BasePlayer player;
            public BasePlayer target;
            public Timer timeoutTimer;
        }
        #endregion

        #region Server Events
        [ChatCommand("h")]
        private void OnChatCommandH(BasePlayer player, string command, string[] args)
        {
            OnChatCommandHome(player, command, args);
        }

        [ChatCommand("home")]
        private void OnChatCommandHome(BasePlayer player, string command, string[] args)
        {
            if (player == null || !playerInfo.ContainsKey(player.userID))
                return;

            string syntax = "/home \"name\" <color=#999>Start the teleport to your home</color>\n" +
                "/home list <color=#999>Shows a list of your homes</color>\n" +
                "/home add \"name\" <color=#999>Add a home to your homelist</color>\n" +
                "/home remove \"name\" <color=#999>Removes a home from your homelist</color>";

            if (args.Length < 1)
            {
                CreateHomeGUI(player);
                SendReply(player, syntax);
                return;
            }

            switch (args[0])
            {
                case "list":
                    if (homes[player.userID].Count == 0)
                    {
                        SendReply(player, "You dont have any homes, you can add them with /home add [name]");
                        return;
                    }

                    string homeList = "Your homes:\n";

                    foreach (KeyValuePair<string, Vector3> home in homes[player.userID])
                        homeList += $"- {home.Key}\n";

                    SendReply(player, homeList.TrimEnd('\n'));
                    return;

                case "add":
                    if (args.Length != 2)
                        break;

                    AddHome(player, args[1]);
                    return;

                case "remove":
                    if (args.Length != 2)
                        break;

                    DeleteHome(player, args[1]);
                    return;
            }

            if (args.Length != 1)
                SendReply(player, syntax);

            TeleportHome(player, args[0]);
        }

        [ConsoleCommand("teleportgl")]
        private void OnConsoleCommandTeleport(ConsoleSystem.Arg arg)
        {
            if (arg.Connection == null || arg.Connection.player == null || !arg.HasArgs())
                return;

            var player = (BasePlayer)arg.Connection.player;

            Puts(string.Join(" ", arg.Args));

            if (arg.Args[0] == "home")
            {
                switch (arg.Args[1])
                {
                    case "open":
                        if (arg.Args.Length != 3)
                            break;

                        CreateHomeGUI(player, Convert.ToInt32(arg.Args[2]));
                        break;

                    case "add":
                        if (arg.Args.Length != 3)
                            break;

                        AddHome(player, arg.Args[2]);

                        DestroyHomeGUI(player);
                        break;

                    case "remove":
                        if (arg.Args.Length != 3)
                            break;

                        DeleteHome(player, arg.Args[2]);

                        CreateHomeGUI(player);
                        break;

                    case "teleport":
                        if (arg.Args.Length != 3)
                            break;

                        TeleportHome(player, arg.Args[2]);

                        DestroyHomeGUI(player);
                        break;

                    case "close":
                        DestroyHomeGUI(player);
                        break;
                }
            }
        }

        [ChatCommand("t")]
        private void OnChatCommandT(BasePlayer player, string command, string[] args)
        {
            OnChatCommandTpr(player, command, args);
        }

        [ChatCommand("tpr")]
        private void OnChatCommandTpr(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            // @TODO: Check pending teleports

            if (args.Length > 0)
            {
                List<BasePlayer> targets = FindOnlinePlayers(string.Join(" ", args));

                // No players found
                if (targets.Count == 0)
                {
                    PrintToChat(player, "<color=#d00>ERROR</color> No players found.");
                    return;
                }

                // Multiple players found
                if (targets.Count > 1)
                {
                    PrintToChat(player, $"<color=#d00>ERROR</color> Found multiple players: {string.Join(", ", targets.ConvertAll(t => t.displayName).ToArray())}");
                    return;
                }

                // Cannot teleport to yourself
                if (targets[0] == player)
                {
                    PrintToChat(player, "<color=#d00>ERROR</color> You cannot teleport to yourself.");
                    return;
                }

                TeleportRequest(player, targets[0]);
                return;
            }
        }

        [ChatCommand("tpa")]
        private void OnChatCommandTpa(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            AcceptTeleportRequest(player);
        }

        [ChatCommand("tpd")]
        private void OnChatCommandTpd(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            DenyTeleportRequest(player);
        }

        [ChatCommand("tpc")]
        private void OnChatCommandTpc(BasePlayer player, string command, string[] args)
        {
            if (player == null)
                return;

            CancelTeleportRequest(player);
        }

        private void Init()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                OnPlayerInit(player);
        }

        private void OnPlayerInit(BasePlayer player)
        {
            if (homes.ContainsKey(player.userID))
                homes.Remove(player.userID);

            LoadPlayer(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null || homes.ContainsKey(player.userID))
                return;

            homes.Remove(player.userID);
        }
        #endregion

        #region GUI
        void CreateHomeGUI(BasePlayer player, int index = 0)
        {
            DestroyHomeGUI(player);

            PlayerInfo info = playerInfo[player.userID];

            // Main container
            var container = UI.CreateElementContainer("gl_teleport_home", "0 0 0 0.99", "0.35 0.05", "0.65 0.95", true);

            // Index buttons
            if (index > 0)
                UI.CreateButton(ref container, "gl_teleport_home", "0.12 0.38 0.57 1", "Prev", 12, "0 0", "0.09 0.04", $"teleportgl home open {index - 1}");

            if (homes[player.userID].Count - (index * 15) > 15)
                UI.CreateButton(ref container, "gl_teleport_home", "0.12 0.38 0.57 1", "Next", 12, "0.91 0", "0.997 0.04", $"teleportgl home open {index + 1}");

            int i = 0;
            foreach (KeyValuePair<string, Vector3> home in homes[player.userID].Skip(index * 15).Take(15))
            {
                Vector2 dim = new Vector2(1f, 0.05f);
                Vector2 ori = new Vector2(0.0f, 1f);
                float offset = (0.013f + dim.y) * (i + 1);
                Vector2 off = new Vector2(0f, offset);
                Vector2 min = ori - off;
                Vector2 max = min + dim;

                UI.CreatePanel(ref container, "gl_teleport_home", "1 1 1 0.02", $"{min.x} {min.y}", $"{max.x} {max.y}");
                UI.CreateLabel(ref container, "gl_teleport_home", $"Home {home.Key}", 12, $"{min.x + 0.02} {min.y}", $"{max.x} {max.y}", TextAnchor.MiddleLeft, "1 1 1 1", 0);

                UI.CreateButton(ref container, "gl_teleport_home", "0.12 0.38 0.57 1", $"Teleport", 12, $"{min.x + 0.8} {min.y + 0.01}", $"{max.x - 0.02} {max.y - 0.01}", $"teleportgl home teleport {home.Key}");
                UI.CreateButton(ref container, "gl_teleport_home", "0.8 0.2 0.2 1", $"Delete", 12, $"{min.x + 0.598} {min.y + 0.01}", $"{max.x - 0.22} {max.y - 0.01}", $"teleportgl home remove {home.Key}");

                i++;
            }

            // Add button
            UI.CreateButton(ref container, "gl_teleport_home", "0.12 0.38 0.57 1", $"Add home {i + 1}", 12, $"{(index > 0 ? "0.1" : "0")} 0", "0.49 0.04", $"teleportgl home add {i + 1}");

            // Close button
            UI.CreateButton(ref container, "gl_teleport_home", "0.8 0.2 0.2 1", "Close", 12, "0.51 0", $"{(homes[player.userID].Count - (index * 15) > 15 ? "0.9" : "0.997")} 0.04", "teleportgl home close");

            CuiHelper.AddUi(player, container);
        }


        void DestroyHomeGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_teleport_home");
        }
        #endregion
    }
}

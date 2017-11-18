using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Teleport", "Game Limits", "1.0.0")]
    [Description("The teleport module")]

    public class GameLimitsTeleport : RustPlugin
    {
        // private Dictionary<ulong, TeleportRequest> teleportRequests = new Dictionary<ulong, TeleportRequest>();
        private Dictionary<ulong, ulong> teleportRequest = new Dictionary<ulong, ulong>();

        #region Functions
        private bool RequestTeleport(BasePlayer player, ulong to)
        {
            if (teleportRequest.ContainsKey(player.userID) || teleportRequest.ContainsValue(player.userID) ||
                teleportRequest.ContainsKey(to) || teleportRequest.ContainsValue(to))
                return false;

            return true;
        }

        private void CancelTeleport(BasePlayer player)
        {

        }

        private void AcceptTeleport(BasePlayer player)
        {

        }

        private void DenyTeleport(BasePlayer player)
        {

        }

        private string CanTeleport(BasePlayer player)
        {
            if (!player.IsAlive())
                return "dead";
            if (player.IsWounded())
                return "wounded";
            if (!player.CanBuild())
                return "building blocked";
            if (!player.IsSwimming())
                return "swimming";
            if (player.inventory.crafting.queue.Count > 0)
                return "crafting";
            return null;
        }
        #endregion

        #region Classes
        private class TeleportRequest
        {

        }
        #endregion

        #region Server Events
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
            }
        }
        #endregion

    }
}

using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Spawn", "Game Limits", "2.0.0")]
    [Description("The spawn plugin")]

    public class Spawn : RustPlugin
    {
        #region Oxide Hooks
        private void OnPlayerRespawned(BasePlayer player)
        {
            if (player == null)
                return;

            // Give some basic tools to the player
            player.inventory.GiveItem(ItemManager.CreateByName("arrow.wooden", 32));
            player.inventory.GiveItem(ItemManager.CreateByName("bow.hunting", 1), player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("stone.pickaxe", 1), player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("stonehatchet", 1), player.inventory.containerBelt);

            // Only give clothing to the player when there is not active helicopter to prevent the helicopter getting stuck at the nakedbeach(es)
            if (GameObject.FindObjectsOfType<BaseHelicopter>().Length == 0)
            {
                player.inventory.GiveItem(ItemManager.CreateByName("burlap.trousers", 1, 1127409880), player.inventory.containerWear);
                player.inventory.GiveItem(ItemManager.CreateByName("burlap.shirt", 1, 882451685), player.inventory.containerWear);
                player.inventory.GiveItem(ItemManager.CreateByName("burlap.shoes", 1, 790679533), player.inventory.containerWear);
            }
            else
                player.ChatMessage("<color=#C6C3BF>The helicopter currently in the game, so you wont be given any clothes now.</color>");
        }
        #endregion
    }
}

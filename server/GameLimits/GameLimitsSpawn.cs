using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Spawn", "Game Limits", "1.0.0")]
    [Description("The spawn module")]

    public class GameLimitsSpawn : RustPlugin
    {
        private void OnPlayerRespawned(BasePlayer player)
        {
            player.inventory.GiveItem(ItemManager.CreateByName("arrow.wooden", 32));
            player.inventory.GiveItem(ItemManager.CreateByName("bow.hunting", 1), player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("stone.pickaxe", 1), player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("stonehatchet", 1), player.inventory.containerBelt);
            player.inventory.GiveItem(ItemManager.CreateByName("burlap.trousers", 1, 1127409880), player.inventory.containerWear);
            player.inventory.GiveItem(ItemManager.CreateByName("burlap.shirt", 1, 882451685), player.inventory.containerWear);
            player.inventory.GiveItem(ItemManager.CreateByName("burlap.shoes", 1, 790679533), player.inventory.containerWear);
        }
    }
}

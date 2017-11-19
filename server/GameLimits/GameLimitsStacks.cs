using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Stacks", "Game Limits", "1.0.0")]
    [Description("The stacks module")]

    public class GameLimitsStacks : RustPlugin
    {
        /// <summary>
        /// Dictionary information to override the stack information
        /// </summary>
        private static readonly Dictionary<string, int> stacks = new Dictionary<string, int>()
        {
            // Resources / raw
            { "wood", 4000 },
            { "metal.ore", 4000 },
            { "stones", 4000 },
            { "sulfur.ore", 4000 },

            // Resources / not-raw
            { "metal.fragments", 4000 },
            { "sulfur", 4000 },

            // Resources / other
            { "charcoal", 4000 },
            { "gunpowder", 4000 },
            { "bone.fragments", 4000 },
            { "cloth", 4000 },
            { "lowgradefuel", 1000 },
            { "leather", 4000 },
            { "metal.refined", 250 },

            // Utils
            { "ladder.wooden.wall", 10 },

            // Rockets / Explosives
            { "ammo.rocket.basic", 10 },
            { "ammo.rocket.fire", 10 },
            { "ammo.rocket.hv", 10 },
            { "ammo.rocket.smoke", 10 },
            { "grenade.beancan", 10 },
            { "grenade.f1", 10 },
            { "trap.landmine", 10 },

            // Meds
            { "largemedkit", 5 },
            { "syringe.medical", 10 },
        };

        private void OnServerInitialized()
        {
            List<ItemDefinition> items = ItemManager.itemList;
            string result = "";

            foreach (ItemDefinition item in items)
            {
                // Do not stack unstackable items
                if (item.condition.enabled && item.condition.max > 0)
                    continue;

                // Set the stacked item
                if (stacks.ContainsKey(item.shortname))
                {
                    item.stackable = stacks[item.shortname];
                    result += $"[{item.shortname}:{item.stackable}] ";
                }
            }

            Puts(result);
        }
    }
}

using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("KillFeed", "Game Limits", "2.0.0")]
    [Description("The kill feed plugin")]

    public class KillFeed : RustPlugin
    {
        private void OnEntityDeath(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || info == null)
                return;

            // NPC - Animals
            if (info.Initiator is BaseNpc || entity is BaseNpc)
                return;

            // Helicopter
            if (info.Initiator is BaseHelicopter)
                return;

            // Player kills
            if (entity is BasePlayer && info.Initiator is BasePlayer)
            {
                BasePlayer victim = entity.ToPlayer();
                BasePlayer attacker = info.Initiator.ToPlayer();

                Helper.Chat.Broadcast($"<color=#32AADD>{attacker.displayName}</color> killed <color=#32AADD>{victim.displayName}</color>");
            }
        }
    }
}

using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static Oxide.Plugins.Core;
using Rust;

namespace Oxide.Plugins
{
    [Info("RaidBlock", "Game Limits", "2.0.0")]
    [Description("The raid block plugin")]

    public class RaidBlock : RustPlugin
    {
        private static List<DamageType> whitelistDamageTypes = new List<DamageType>()
        {
            DamageType.Bullet,
            DamageType.Blunt,
            DamageType.Stab,
            DamageType.Slash,
            DamageType.Explosion,
            DamageType.Heat,
        };

        private static List<string> whitelistPrefabs = new List<string>()
        {
            "door",
            "window.bars",
            "floor.ladder.hatch",
            "floor.frame",
            "wall.frame",
            "shutter",
            "external",
        };

        #region Oxide Hooks
        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            // Should be a player
            if (hitInfo == null || hitInfo.WeaponPrefab == null || hitInfo.Initiator == null || hitInfo.Initiator.transform == null || hitInfo.Initiator.transform.position == null)
                return;

            // Only allow whitelisted damage types
            if (!whitelistDamageTypes.Contains(hitInfo.damageTypes.GetMajorityDamageType()))
                return;

            // Only allow valid raid enties
            if (!IsValidRaidEntity(entity))
                return;

            DetectRaid(entity, hitInfo.Initiator, hitInfo.WeaponPrefab.ShortPrefabName, hitInfo.HitPositionWorld);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            // Should be a player
            if (hitInfo == null || hitInfo.WeaponPrefab == null || hitInfo.Initiator == null || hitInfo.Initiator.transform == null || hitInfo.Initiator.transform.position == null)
                return;

            // Only allow whitelisted damage types
            if (!whitelistDamageTypes.Contains(hitInfo.damageTypes.GetMajorityDamageType()))
                return;

            // Only allow valid raid enties
            if (!IsValidRaidEntity(entity))
                return;

            DetectRaid(entity, hitInfo.Initiator, hitInfo.WeaponPrefab.ShortPrefabName, hitInfo.HitPositionWorld);
        }
        #endregion

        #region Functions
        public bool IsValidRaidEntity(BaseCombatEntity entity)
        {
            if (entity is BuildingBlock)
            {
                if (((BuildingBlock)entity).grade == BuildingGrade.Enum.Twigs)
                    return false;

                return true;
            }

            if (whitelistPrefabs.Contains(entity.ShortPrefabName))
                return true;
            
            return false;
        }

        public void DetectRaid(BaseEntity targetEntity, BaseEntity sourceEntity, string weapon, Vector3 hitPosition)
        {

        }
        #endregion
    }
}

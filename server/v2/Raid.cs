using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static Oxide.Plugins.Core;
using Rust;
using System.Linq;

namespace Oxide.Plugins
{
    [Info("Raid", "Game Limits", "2.0.0")]
    [Description("The raid plugin")]

    public class RaidBlock : RustPlugin
    {
        private static List<RaidZone> raidZones = new List<RaidZone>();
        private static Dictionary<BasePlayer, RaidZone> playerZones = new Dictionary<BasePlayer, RaidZone>();
        private static Dictionary<ulong, int> alertCooldowns = new Dictionary<ulong, int>();

        private static List<DamageType> whitelistDamageTypes = new List<DamageType>()
        {
            // DamageType.Bullet,
            // DamageType.Blunt,
            // DamageType.Stab,
            // DamageType.Slash,
            DamageType.Explosion,
            // DamageType.Heat,
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
        private static List<string> whitelistWeapons = new List<string>()
        {
            "explosive.timed.deployed",
            "explosive.satchel.deployed",
            "grenade.beancan.deployed",
            "rocket_hv",
            "rocket_basic",
        };

        #region Oxide Hooks
        private void Init()
        {
            UpdateRaidZones();
        }

        private void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo hitInfo)
        {
            DetectRaid(entity, hitInfo);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            DetectRaid(entity, hitInfo);
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

        public void DetectRaid(BaseCombatEntity entity, HitInfo hitInfo)
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
            
            // Entity Data
            BaseEntity target = entity;
            BaseEntity source = hitInfo.Initiator;
            string weaponPrefab = hitInfo.WeaponPrefab.ShortPrefabName;

            // Only allow valid weapons
            //if (!whitelistWeapons.Contains(weaponPrefab))
            //    return;

            CreateRaidZone(target.transform.position);
            AlertZone(target);

            // Debug data
            //Helper.Chat.Broadcast($"Raid detection ({weaponPrefab}) at {target.transform.position.x}, {target.transform.position.y}, {target.transform.position.z}");
        }

        public void CreateRaidZone(Vector3 position, float distance = 50f, int timer = 60 * 5)
        {
            bool found = false;

            int timestamp = Helper.Timestamp();

            foreach (RaidZone zone in raidZones)
            {
                Vector3 difference = zone.position - position;
                if (difference.magnitude <= distance)
                {
                    zone.expires = Helper.Timestamp() + timer;
                    found = true;
                }

                foreach (BasePlayer player in BasePlayer.activePlayerList)
                {
                    if (playerZones.ContainsKey(player) && playerZones[player] == zone)
                    {
                        Notifications.AddTimedNotification(player, "raid_block", "Raid Block", zone.expires - timestamp, ".7 0 0 1");
                    }
                }
            }

            if (found)
                return;

            RaidZone newZone = new RaidZone()
            {
                position = position,
                distance = distance,
                expires = Helper.Timestamp() + timer,
            };

            raidZones.Add(newZone);
            Puts($"New raid started!");
        }

        public void UpdateRaidZones()
        {
            int timestamp = Helper.Timestamp();

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                // Remove player from invalid zones
                if (playerZones.ContainsKey(player))
                {
                    Vector3 difference = playerZones[player].position - player.transform.position;
                    if (difference.magnitude > playerZones[player].distance || playerZones[player].expires < timestamp)
                    {
                        playerZones.Remove(player);
                        Notifications.RemoveTimedNotification(player, "raid_block");
                    }
                }

                // If the player is not linked to a zone, find a new one
                if (!playerZones.ContainsKey(player))
                {
                    foreach (RaidZone zone in raidZones)
                    {
                        Vector3 difference = zone.position - player.transform.position;
                        if (zone.expires > timestamp && difference.magnitude < zone.distance)
                        {
                            playerZones.Add(player, zone);
                            Notifications.AddTimedNotification(player, "raid_block", "Raid Block", zone.expires - timestamp, ".7 0 0 1");
                            break;
                        }
                    }
                }
            }

            // Update raidzone
            foreach (RaidZone zone in raidZones)
            {
                if (zone.expires > timestamp)
                {
                    foreach (BasePlayer player in BasePlayer.activePlayerList)
                    {
                        if (Admin.IsAdmin(player))
                        {
                            player.SendConsoleCommand("ddraw.sphere", 1f, Color.magenta, zone.position, zone.distance);
                            player.SendConsoleCommand("ddraw.text", 1f, Color.green, zone.position, $"{Helper.TimeFormat.Short(zone.expires - timestamp)}");
                        }
                    }
                }
            }

            // Remove expired raidzones
            foreach (RaidZone zone in raidZones.Where(k => k.expires <= timestamp).ToList())
                raidZones.Remove(zone);

            // Remove expired alert cooldowns
            foreach (var cooldown in alertCooldowns.Where(k => k.Value <= timestamp).ToList())
                alertCooldowns.Remove(cooldown.Key);

            timer.In(1f, () => UpdateRaidZones());
        }

        public static RaidZone IsRaidBlocked(BasePlayer player)
        {
            return playerZones.ContainsKey(player) ? playerZones[player] : null;
        }

        public static RaidZone IsRaidBlocked(Vector3 position)
        {
            foreach (RaidZone zone in raidZones)
                if (((Vector3)zone.position - position).magnitude < zone.distance)
                    return zone;

            return null;
        }

        private void AlertZone(BaseEntity target)
        {
            // Find the building which is being targetted
            BuildingPrivlidge buildingPriv = Helper.ModifiedRust.GetBuildingPrivilege(target.WorldSpaceBounds());

            // Check if the target is not null (aka has a cupboard)
            if (buildingPriv == null)
                return;

            // Fetch the current timestamp of the alert
            int timestamp = Helper.Timestamp();

            // Find all authorized players on that cupboard
            foreach (var player in buildingPriv.authorizedPlayers)
            {
                // Prevent spamming notifications when player is on the cooldown list
                if (alertCooldowns.ContainsKey(player.userid) && alertCooldowns[player.userid] > timestamp)
                    return;

                // Remove the player from the cooldown list if it is still there
                if (alertCooldowns.ContainsKey(player.userid))
                    alertCooldowns.Remove(player.userid);

                // Adds the player to the cooldown dictionary of 15 minutes
                alertCooldowns.Add(player.userid, timestamp + (/*60 **/ 15));

                // Send a rcon command for the webserver to handle!
                Puts($"[alert] {player.userid}");
            }
        }
        #endregion

        #region Classes
        public class RaidZone
        {
            public Vector3 position;
            public float distance;
            public int expires;
        }
        #endregion
    }
}

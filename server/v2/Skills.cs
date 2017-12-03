using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace Oxide.Plugins
{
    [Info("Skills", "Game Limits", "2.0.0")]
    [Description("The skills plugin")]

    public class Skills : RustPlugin
    {
        private static Dictionary<ulong, PlayerSkill> skills = new Dictionary<ulong, PlayerSkill>();

        #region Oxide Hooks
        private void Init()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                CreateUI(player);
                LoadPlayer(player);
            }
        }

        private void Unload()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                DestroyUI(player);
                SavePlayer(player);
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            CreateUI(player);
            LoadPlayer(player);
        }

        private void OnPlayerSleepEnded(BasePlayer player)
        {
            CreateUI(player);
        }

        private void OnPlayerSleep(BasePlayer player)
        {
            DestroyUI(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            UnloadPlayer(player);
        }

        private void OnServerSave()
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                SavePlayer(player);
        }

        private void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || !(entity is BasePlayer))
                return;

            // Cast the entity to a BasePlayer
            BasePlayer player = (BasePlayer)entity;

            if (player == null || !skills.ContainsKey(player.userID) || player.IsSleeping())
                return;

            // The XP Penalty that will be applied to all skills
            long xpPenalty = 200;

            // Loop through all skills
            foreach (Skill skill in SkillList.all)
            {
                long xp = skills[player.userID].GetXp(skill.code);

                if (xp < xpPenalty)
                    return;

                // Decrease the xp and level
                skills[player.userID].SetXp(skill.code, xp - xpPenalty);
                skills[player.userID].SetLevel(skill.code, GetXpLevel(xp - xpPenalty));

                // Update the GUI
                UpdateUI(player, skill);
            }
        }

        private void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null || !(entity is BaseEntity) || item == null || dispenser == null)
                return;

            // Cast the entity to a BasePlayer
            BasePlayer player = (BasePlayer)entity;

            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                LevelHandler(player, item, SkillList.woodcutting);

            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                LevelHandler(player, item, SkillList.mining);

            if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
                LevelHandler(player, item, SkillList.skinning);
        }

        private void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            if (item == null || player == null)
                return;

            LevelHandler(player, item, SkillList.acquiring);
        }

        private void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            if (item == null || player == null)
                return;

            LevelHandler(player, item, SkillList.acquiring);
        }
        #endregion

        #region Functions
        private void LoadPlayer(BasePlayer player)
        {
            if (player == null)
                return;

            PlayerData.PData pdata = PlayerData.Get(player);

            if (pdata == null)
            {
                Puts($"Loading for [{player.UserIDString}] has been delayed, waiting for the PlayerData plugin");
                timer.Once(1f, () => LoadPlayer(player));
                return;
            }

            // Remove the old homes first
            UnloadPlayer(player);

            Database.Query(Database.Build("SELECT * FROM skills WHERE user_id = @0;", pdata.id), records =>
            {
                PlayerSkill pskill = new PlayerSkill();

                string info = $"Player skills for [{pdata.id}:{player.UserIDString}]:\n";

                foreach (var record in records)
                {
                    string skill = Convert.ToString(record["skill"]);
                    int xp = Convert.ToInt32(record["xp"]);
                    int level = Convert.ToInt32(record["level"]);

                    pskill.SetLevel(skill, level);
                    pskill.SetXp(skill, xp);

                    info += $"{skill}: Lv. {level} / Xp. {xp}\n";
                }

                skills.Add(player.userID, pskill);

                Puts(info.TrimEnd('\n'));

                CreateUI(player);
            });
        }

        private void SavePlayer(BasePlayer player, Action callback = null)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null || !skills.ContainsKey(player.userID))
                return;

            foreach (var skill in SkillList.all)
            {
                Database.Query(Database.Build("SELECT id FROM skills WHERE user_id=@0 AND skill=@1 LIMIT 1;", pdata.id, skill.code), records =>
                {
                    if (records.Count == 0)
                        Database.Insert(Database.Build("INSERT INTO skills (user_id, skill, level, xp) VALUES (@0, @1, @2, @3);",
                            pdata.id,
                            skill.code,
                            skills[player.userID].GetLevel(skill.code),
                            skills[player.userID].GetXp(skill.code)));
                    else
                        Database.NonQuery(Database.Build("UPDATE skills SET level=@0, xp=@1 WHERE user_id=@2 AND skill=@3 LIMIT 1;",
                            skills[player.userID].GetLevel(skill.code),
                            skills[player.userID].GetXp(skill.code),
                            pdata.id,
                            skill.code));

                    Puts("Skill saved: " + skill.name);
                });
            }
        }

        private void UnloadPlayer(BasePlayer player)
        {
            if (player == null || !skills.ContainsKey(player.userID))
                return;

            ulong id = player.userID;

            SavePlayer(player);

            skills.Remove(id);
        }

        private int GetExperiencePercent(BasePlayer player, Skill skill)
        {
            if (player == null || !skills.ContainsKey(player.userID))
                return 0;

            long level = skills[player.userID].GetLevel(skill.code);
            long startXp = GetLevelXp(level);
            long nextXp = GetLevelXp(level + 1) - startXp;
            long xp = skills[player.userID].GetXp(skill.code) - startXp;
            int xpProc = Convert.ToInt32((xp / (double)nextXp) * 100);
            if (xpProc > 99)
                xpProc = 99;
            else if (xpProc == 0)
                xpProc = 1;
            return xpProc;
        }

        private long GetLevelXp(long level) => 110 * level * level - 100 * level;

        private long GetXpLevel(long points)
        {
            var a = 110;
            var b = 100;
            var c = -points;
            var x1 = (-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
            return (int)-x1;
        }

        private void LevelHandler(BasePlayer player, Item item, Skill skill)
        {
            if (player == null || !skills.ContainsKey(player.userID))
                return;

            PlayerSkill pskill = skills[player.userID];

            int xpProc = GetExperiencePercent(player, skill);
            long level = pskill.GetLevel(skill.code);
            long xp = pskill.GetXp(skill.code);

            item.amount = Mathf.CeilToInt((float)(item.amount * (1 + 1 * 0.1 * (level - 1))));

            xp += 5;

            if (xp > GetLevelXp(level + 1))
            {
                level = GetXpLevel(xp);

                player.ChatMessage($"<color=\"#DD32AA\">{skill.name}</color> level up! { ((((1 + 1 * 0.1 * (level - 1)) * 100) - 100).ToString("0.##"))}% bonus");
            }

            pskill.SetLevel(skill.code, level);
            pskill.SetXp(skill.code, xp);

            var newXp = GetExperiencePercent(player, skill);
            if (newXp != xpProc)
                UpdateUI(player, skill);
        }
        #endregion

        #region Classes
        private class Skill
        {
            public short index;
            public string code;
            public string name;
            public string color;
        }

        private class SkillList
        {
            public static Skill mining = new Skill()
            {
                index = 1,
                code = "M",
                name = "Mining",
                color = "0.1 0.5 0.8 0.7"
            };

            public static Skill woodcutting = new Skill()
            {
                index = 2,
                code = "W",
                name = "Woodcutting",
                color = "0.8 0.4 0 0.7"
            };

            public static Skill skinning = new Skill()
            {
                index = 3,
                code = "S",
                name = "Skinning",
                color = "0.8 0.1 0 0.7"
            };

            public static Skill acquiring = new Skill()
            {
                index = 4,
                code = "A",
                name = "Acquiring",
                color = "0.2 0.72 0.5 0.7"
            };

            public static Skill[] all = { mining, woodcutting, skinning, acquiring };
        }

        private class PlayerSkill
        {
            public Dictionary<string, long> xp = new Dictionary<string, long>();
            public Dictionary<string, long> level = new Dictionary<string, long>();

            public long GetXp(string code)
            {
                if (!xp.ContainsKey(code))
                    return 10;

                return xp[code];
            }

            public void SetXp(string code, long value)
            {
                xp[code] = value;
            }

            public long GetLevel(string code)
            {
                if (!xp.ContainsKey(code))
                    return 1;

                return level[code];
            }

            public void SetLevel(string code, long value)
            {
                level[code] = value;
            }
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player)
        {
            if (player == null)
                return;

            DestroyUI(player);

            // Main container
            CuiElementContainer container = Helper.UI.Container("ui_skills", "0 0 0 0", "0.725 0.02", "0.83 0.1350", false, "Hud");

            // If the player information has not been loaded, let the player know we are still loading his information
            if (!skills.ContainsKey(player.userID))
                Helper.UI.Label(ref container, "ui_skills", "1 1 1 1", "Loading your profile\n\nIf it does not load, contact us at Discord!", 11, "0 0", "1 1");

            Helper.UI.Add(player, container);

            // Render each skill on the screen
            if (skills.ContainsKey(player.userID))
                foreach (Skill skill in SkillList.all)
                    UpdateUI(player, skill);
        }

        private void DestroyUI(BasePlayer player)
        {
            if (player == null)
                return;

            Helper.UI.Destroy(player, "ui_skills");
        }

        private void UpdateUI(BasePlayer player, Skill skill)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null || !skills.ContainsKey(player.userID))
                return;

            int percent = GetExperiencePercent(player, skill);
            int level = (int)skills[player.userID].GetLevel(skill.code);

            string panel = $"ui_skills_{skill.code}";

            var val = 1 / (float)SkillList.all.Length;
            var min = 1 - (val * skill.index) + .017;
            var max = 2 - (1 - (val * (1 - skill.index))) - .017;

            Helper.UI.Destroy(player, panel);

            CuiElementContainer container = Helper.UI.Container(panel, "0.8 0.8 0.8 0.03", "0 " + min.ToString("0.####"), "1 " + max.ToString("0.####"), false, "ui_skills");

            // XP Bar
            Helper.UI.Panel(ref container, panel, "0.2 0.2 0.2 0.3", "0.235 0.12", "0.97 0.85", false, $"{panel}_bar");

            // XP Bar Progression
            Helper.UI.Panel(ref container, $"{panel}_bar", skill.color, "0 0", (percent / 100.0) + " 0.95");

            // XP Bar Text
            Helper.UI.Label(ref container, $"{panel}_bar", "0.74 0.76 0.78 1", skill.name, 11, "0.05 0", "1 1");

            // XP Bar Percent
            Helper.UI.Label(ref container, $"{panel}_bar", "0.74 0.76 0.78 1", $"{percent}%", 10, "0.025 0", "1 1", TextAnchor.MiddleRight);

            // Level
            Helper.UI.Label(ref container, panel, "0.74 0.76 0.78 1", $"Lv. {level}", 11, "0.025 0", "1.5 1", TextAnchor.MiddleLeft);

            Helper.UI.Add(player, container);
        }
        #endregion
    }
}


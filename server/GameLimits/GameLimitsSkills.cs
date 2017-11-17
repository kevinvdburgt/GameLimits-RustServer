using static Oxide.Plugins.GameLimits;
using System.Net;
using UnityEngine;
using System.Collections.Generic;
using Oxide.Game.Rust.Cui;
using System;

namespace Oxide.Plugins
{
    [Info("Skills", "Game Limits", "1.0.0")]
    [Description("The skills module")]

    public class GameLimitsSkills : RustPlugin
    {
        private Dictionary<ulong, PlayerSkill> playerSkills = new Dictionary<ulong, PlayerSkill>();

        #region Server Events
        void OnEntityDeath(BaseCombatEntity entity, HitInfo hitInfo)
        {
            if (entity == null || !(entity is BasePlayer))
                return;

            var player = (BasePlayer)entity;
            long penalty = 200;

            foreach (Skill skill in Skills.all)
            {
                long xp = playerSkills[player.userID].GetXp(skill.code);

                if (xp < penalty)
                    return;

                playerSkills[player.userID].SetXp(skill.code, xp - penalty);
                playerSkills[player.userID].SetLevel(skill.code, GetXpLevel(xp - penalty));

                PrintToChat(player, $"<color=\"#DD32AA\">{skill.name}</color> -{penalty} XP Penalty for dying.");

                UpdateGUI(player, skill);
            }
                
        }

        void OnDispenserGather(ResourceDispenser dispenser, BaseEntity entity, Item item)
        {
            if (entity == null || !(entity is BaseEntity) || item == null || dispenser == null)
                return;

            var player = (BasePlayer)entity;

            if (dispenser.gatherType == ResourceDispenser.GatherType.Tree)
                LevelHandler(player, item, Skills.woodcutting);

            if (dispenser.gatherType == ResourceDispenser.GatherType.Ore)
                LevelHandler(player, item, Skills.mining);

            if (dispenser.gatherType == ResourceDispenser.GatherType.Flesh)
                LevelHandler(player, item, Skills.skinning);
        }

        void OnCollectiblePickup(Item item, BasePlayer player, CollectibleEntity entity)
        {
            if (item == null || player == null)
                return;

            LevelHandler(player, item, Skills.acquiring);
        }

        void OnCropGather(PlantEntity plant, Item item, BasePlayer player)
        {
            if (item == null || player == null)
                return;

            LevelHandler(player, item, Skills.acquiring);
        }

        void Init()
        {
            foreach (var player in BasePlayer.activePlayerList)
            {
                LoadPlayer(player);
                CreateGUI(player);
            }
        }

        void OnPlayerInit(BasePlayer player)
        {
            if (player == null)
                return;

            CreateGUI(player);
            LoadPlayer(player);
        }

        void OnPlayerSleepEnded(BasePlayer player)
        {
            if (player == null)
                return;

            CreateGUI(player);
        }

        void OnPlayerSleep(BasePlayer player)
        {
            if (player == null)
                return;

            DestroyGUI(player);
        }

        void OnEntityTakeDamage(BaseCombatEntity entity, HitInfo info)
        {
            if (entity == null || !(entity is BasePlayer))
                return;

            NextTick(() => {
                if (entity != null && entity.health <= 0f)
                    DestroyGUI((BasePlayer)entity);
            });
        }

        void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            if (player == null || playerSkills.ContainsKey(player.userID))
                return;

            playerSkills.Remove(player.userID);
        }
        #endregion

        #region Classes
        class Skill
        {
            public short index;
            public string code;
            public string name;
            public string color;
        }

        class Skills
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

        class PlayerSkill
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

        #region Functions
        void LoadPlayer(BasePlayer player)
        {
            if (player == null || playerSkills.ContainsKey(player.userID))
                return;

            if (!playerInfo.ContainsKey(player.userID))
            {
                timer.Once(0.5f, () => LoadPlayer(player));
                return;
            }

            PlayerInfo info = playerInfo[player.userID];

            MQuery(MBuild("SELECT * FROM skills WHERE user_id=@0", info.id), records =>
            {
                PlayerSkill pskill = new PlayerSkill();

                foreach (var record in records)
                {
                    string skill = Convert.ToString(record["skill"]);
                    int xp = Convert.ToInt32(record["xp"]);
                    int level = Convert.ToInt32(record["level"]);

                    pskill.SetXp(skill, xp);
                    pskill.SetLevel(skill, level);
                }

                playerSkills.Add(player.userID, pskill);
                CreateGUI(player);
            });
        }

        void SavePlayer(BasePlayer player)
        {
            if (player == null || !playerSkills.ContainsKey(player.userID))
                return;

            foreach (var skill in Skills.all)
                SavePlayerSkill(player, skill);
        }

        void SavePlayerSkill(BasePlayer player, Skill skill)
        {
            if (player == null || !playerSkills.ContainsKey(player.userID))
                return;

            PlayerSkill pskill = playerSkills[player.userID];
            PlayerInfo info = playerInfo[player.userID];

            MQuery(MBuild("SELECT id FROM skills WHERE user_id=@0 AND skill=@1 LIMIT 1;", info.id, skill.code), records =>
            {
                if (records.Count == 0)
                    MInsert(MBuild("INSERT INTO skills (user_id, skill, level, xp) VALUES (@0, @1, @2, @3);",
                        info.id,
                        skill.code,
                        pskill.GetLevel(skill.code),
                        pskill.GetXp(skill.code)));
                else
                    MNonQuery(MBuild("UPDATE skills SET level=@0, xp=@1 WHERE user_id=@2 AND skill=@3 LIMIT 1;",
                        pskill.GetLevel(skill.code),
                        pskill.GetXp(skill.code),
                        info.id,
                        skill.code));
            });
        }
        #endregion

        #region Helper Functions
        private int GetExperiencePercent(BasePlayer player, Skill skill)
        {
            if (player == null || !playerSkills.ContainsKey(player.userID))
                return 0;

            var level = playerSkills[player.userID].GetLevel(skill.code);
            var startXp = GetLevelXp(level);
            var nextXp = GetLevelXp(level + 1) - startXp;
            var xp = playerSkills[player.userID].GetXp(skill.code) - startXp;
            var xpProc = Convert.ToInt32((xp / (double)nextXp) * 100);
            if (xpProc >= 100)
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
            if (!playerSkills.ContainsKey(player.userID))
                return;

            PlayerSkill pskill = playerSkills[player.userID];

            var xpProc = GetExperiencePercent(player, skill);
            var level = pskill.GetLevel(skill.code);
            var xp = pskill.GetXp(skill.code);
            
            item.amount = Mathf.CeilToInt((float)(item.amount * (1 + 1 * 0.1 * (level - 1))));

            xp += 5;

            if (xp > GetLevelXp(level + 1))
            {
                level = GetXpLevel(xp);

                PrintToChat(player, $"<color=\"#DD32AA\">{skill.name}</color> level up!\nLevel {level} ({xp}/{GetLevelXp(level + 1)} XP).\n<color=\"#DD32AA\">{ (((1 + 1 * 0.1 * (level - 1)) * 100).ToString("0.##"))}%</color> bonus");
            }

            pskill.SetLevel(skill.code, level);
            pskill.SetXp(skill.code, xp);

            var newXp = GetExperiencePercent(player, skill);
            if (newXp != xpProc)
                UpdateGUI(player, skill);

            SavePlayerSkill(player, skill);
        }
        #endregion

        #region GUI
        void CreateGUI(BasePlayer player)
        {
            DestroyGUI(player);

            // Main container
            var container = UI.CreateElementContainer("gl_skills", "0 0 0 0", "0.725 0.02", "0.83 0.1350", false, "Overlay");

            // If the player information has not been loaded, let the player know we are still loading his information
            if (!playerSkills.ContainsKey(player.userID))
                UI.CreateLabel(ref container, "gl_skills", "Loading your skills\n\nIf it does not load, contact us at Discord!", 11, "0 0", "1 1", TextAnchor.MiddleCenter, "1 1 1 1");

            CuiHelper.AddUi(player, container);

            if (playerSkills.ContainsKey(player.userID))
                foreach (Skill skill in Skills.all)
                    UpdateGUI(player, skill);
        }

        void DestroyGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_skills");
        }

        void UpdateGUI(BasePlayer player, Skill skill)
        {
            if (!playerSkills.ContainsKey(player.userID))
                return;

            var percent = GetExperiencePercent(player, skill);
            var level = playerSkills[player.userID].GetLevel(skill.code);

            var panel = "gl_skills_" + skill.code;
            UI.Destroy(player, panel);

            var val = 1 / (float)Skills.all.Length;
            var min = 1 - (val * skill.index) + .017;
            var max = 2 - (1 - (val * (1 - skill.index))) - .017;

            var container = UI.CreateElementContainer(panel, "0.8 0.8 0.8 0.03", "0 " + min.ToString("0.####"), "1 " + max.ToString("0.####"), false, "gl_skills");
            
            // XP Bar
            UI.CreatePanel(ref container, panel, "0.2 0.2 0.2 0.3", "0.235 0.12", "0.97 0.85", false, panel + "_bar");

            // XP Bar Progression
            UI.CreatePanel(ref container, panel + "_bar", skill.color, "0 0", (percent / 100.0) + " 0.95", false);

            // XP Bar Text
            UI.CreateLabel(ref container, panel + "_bar", $"{skill.name}", 11, "0.05 0", "1 1", TextAnchor.MiddleCenter, "0.74 0.76 0.78 1");

            // XP Bar Percent
            UI.CreateLabel(ref container, panel + "_bar", $"{percent}%", 10, "0.025 0", "1 1", TextAnchor.MiddleRight, "0.74 0.76 0.78 1");

            // Level
            UI.CreateLabel(ref container, panel, $"Lv. {level}", 11, "0.025 0", "1.5 1", TextAnchor.MiddleLeft, "0.74 0.76 0.78 1");

            CuiHelper.AddUi(player, container);
        }
        #endregion
    }
}

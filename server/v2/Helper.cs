using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Globalization;
using System.Linq;
using Oxide.Core;

namespace Oxide.Plugins
{
    [Info("Helper", "Game Limits", "2.0.0")]
    [Description("The helper plugin")]

    public class Helper : RustPlugin
    {
        static Helper plug;

        void Init()
        {
            plug = this;
        }

        #region Classes
        public static class UI
        {
            public static void Add(BasePlayer player, string json)
            {
                if (player == null)
                    return;

                CuiHelper.AddUi(player, json);
            }

            public static void Add(BasePlayer player, List<CuiElement> list)
            {
                if (player == null)
                    return;

                CuiHelper.AddUi(player, list);
            }

            public static void Destroy(BasePlayer player, string elem)
            {
                if (player == null)
                    return;

                CuiHelper.DestroyUi(player, elem);
            }

            public static CuiElementContainer Container(string panel, string color, string aMin, string aMax, bool cursor = false, string parent = "Overlay")
            {
                return new CuiElementContainer()
                {
                    {
                        new CuiPanel
                        {
                            Image =
                            {
                                Color = color,
                            },
                            RectTransform =
                            {
                                AnchorMin = aMin,
                                AnchorMax = aMax,
                            },
                            CursorEnabled = cursor,
                        },
                        new CuiElement().Parent = parent,
                        panel
                    }
                };
            }

            public static void Button(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, string command, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0.0f)
            {
                container.Add(new CuiButton
                {
                    Button =
                    {
                        Color = color,
                        Command = command,
                        FadeIn = fadein,
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax,
                    },
                    Text =
                    {
                        Text = text,
                        FontSize = size,
                        Align = align,
                    },
                }, panel, CuiHelper.GetGuid());
            }

            public static void Label(ref CuiElementContainer container, string panel, string color, string text, int size, string aMin, string aMax, TextAnchor align = TextAnchor.MiddleCenter, float fadein = 0.0f, string guid = null)
            {
                container.Add(new CuiLabel
                {
                    Text =
                    {
                        Text = text,
                        FontSize = size,
                        Align = align,
                        Color = color,
                        FadeIn = fadein,
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax,
                    },
                }, panel, guid == null ? CuiHelper.GetGuid() : guid);
            }

            public static void Panel(ref CuiElementContainer container, string panel, string color, string aMin, string aMax, bool cursor = false, string guid = null)
            {
                container.Add(new CuiPanel
                {
                    Image =
                    {
                        Color = color,
                    },
                    RectTransform =
                    {
                        AnchorMin = aMin,
                        AnchorMax = aMax,
                    },
                    CursorEnabled = cursor,
                }, panel, guid == null ? CuiHelper.GetGuid() : guid);
            }

            public static void Image(ref CuiElementContainer container, string panel, string url, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Url = url,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax,
                        },
                    },
                });
            }

            public static void ImageRaw(ref CuiElementContainer container, string panel, string png, string aMin, string aMax)
            {
                container.Add(new CuiElement
                {
                    Name = CuiHelper.GetGuid(),
                    Parent = panel,
                    Components =
                    {
                        new CuiRawImageComponent
                        {
                            Png = png,
                        },
                        new CuiRectTransformComponent
                        {
                            AnchorMin = aMin,
                            AnchorMax = aMax,
                        },
                    },
                });
            }
        }

        public static class TimeFormat
        {
            public static string Short(int seconds)
            {
                return Short(TimeSpan.FromSeconds(seconds));
            }

            public static string Short(TimeSpan ts)
            {
                if (ts.Days > 0)
                    return string.Format("{0}D {1}H", ts.Days, ts.Hours);

                if (ts.Hours > 0)
                    return string.Format("{0}H {1}M", ts.Hours, ts.Minutes);

                if (ts.Minutes > 0)
                    return string.Format("{0}M {1}S", ts.Minutes, ts.Seconds);

                return string.Format("{0}S", ts.TotalSeconds);
            }

            public static string Long(int seconds)
            {
                return Long(TimeSpan.FromSeconds(seconds));
            }

            public static string Long(TimeSpan ts)
            {
                if (ts.Days > 0)
                    return string.Format("{0} Days, {1} Hours, {2} Minutes and {3} Seconds ", ts.Days, ts.Hours, ts.Minutes, ts.Seconds);

                if (ts.Hours > 0)
                    return string.Format("{0} Hours, {1} Minutes and {2} Seconds ", ts.Hours, ts.Minutes, ts.Seconds);

                if (ts.Minutes > 0)
                    return string.Format("{0} Minutes and {1} Seconds ", ts.Minutes, ts.Seconds);

                return string.Format("{0} Seconds ", ts.Seconds);
            }
        }

        public static class Chat
        {
            public static void Broadcast(string message)
            {
                foreach (BasePlayer player in BasePlayer.activePlayerList)
                    if (player != null)
                        player.ChatMessage(message);
            }
        }

        public class InventoryData
        {
            public ItemData[] container = new ItemData[0];

            public InventoryData() { }

            public InventoryData(ItemContainer container)
            {
                this.container = GetItems(container).ToArray();
            }

            private IEnumerable<ItemData> GetItems(ItemContainer container)
            {
                return container.itemList.Select(item => new ItemData
                {
                    itemid = item.info.itemid,
                    amount = item.amount,
                    ammo = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.contents ?? 0,
                    ammotype = (item.GetHeldEntity() as BaseProjectile)?.primaryMagazine.ammoType.shortname ?? null,
                    position = item.position,
                    skin = item.skin,
                    condition = item.condition,
                    instanceData = item.instanceData ?? null,
                    blueprintTarget = item.blueprintTarget,
                    contents = item.contents?.itemList.Select(subitem => new ItemData
                    {
                        itemid = subitem.info.itemid,
                        amount = subitem.amount,
                        condition = subitem.condition
                    }).ToArray()
                });
            }

            public void RestoreItems(ref ItemContainer itemContainer)
            {
                for (int i = 0; i < container.Length; i++)
                {
                    Item item = CreateItem(container[i]);
                    item.MoveToContainer(itemContainer, container[i].position, true);
                }
            }

            private Item CreateItem(ItemData itemData)
            {
                Item item = ItemManager.CreateByItemID(itemData.itemid, itemData.amount, itemData.skin);
                item.condition = itemData.condition;
                if (itemData.instanceData != null)
                    item.instanceData = itemData.instanceData;
                item.blueprintTarget = itemData.blueprintTarget;
                var weapon = item.GetHeldEntity() as BaseProjectile;
                if (weapon != null)
                {
                    if (!string.IsNullOrEmpty(itemData.ammotype))
                        weapon.primaryMagazine.ammoType = ItemManager.FindItemDefinition(itemData.ammotype);
                    weapon.primaryMagazine.contents = itemData.ammo;
                }
                if (itemData.contents != null)
                {
                    foreach (var contentData in itemData.contents)
                    {
                        var newContent = ItemManager.CreateByItemID(contentData.itemid, contentData.amount);
                        if (newContent != null)
                        {
                            newContent.condition = contentData.condition;
                            newContent.MoveToContainer(item.contents);
                        }
                    }
                }
                return item;
            }

            public class ItemData
            {
                public int itemid;
                public ulong skin;
                public int amount;
                public float condition;
                public int ammo;
                public string ammotype;
                public int position;
                public int blueprintTarget;
                public ProtoBuf.Item.InstanceData instanceData;
                public ItemData[] contents;
            }
        }

        public static class DataStore
        {
            public static void Load<T>(ref T data, string filename)
            {
                data = Oxide.Core.Interface.Oxide.DataFileSystem.ReadObject<T>(filename);
            }

            public static void Save<T>(T data, string filename)
            {
                Oxide.Core.Interface.Oxide.DataFileSystem.WriteObject(filename, data);
            }
        }

        public class AnchorPosition
        {
            public class Anchor
            {
                public Vector2 vmin = new Vector2();
                public Vector2 vmax = new Vector2();
                public string smin = "0 0";
                public string smax = "1 1";
            }

            public static Anchor List(int index, float height = 0.05f, float width = 1f, float spacer = 0.012f, float offsetLeft = 0f)
            {
                Vector2 dimension = new Vector2(width - offsetLeft, height);
                Vector2 origin = new Vector2(offsetLeft, 1f);
                Vector2 offset = new Vector2(0f, (spacer + dimension.y) * (index + 1));
                Vector2 min = origin - offset;
                Vector2 max = min + dimension;

                string smin = $"{min.x} {min.y}";
                string smax = $"{max.x} {max.y}";

                return new Anchor()
                {
                    vmin = min,
                    vmax = max,
                    smin = smin,
                    smax = smax,
                };
            }
        }

        public static class ToolCupboard
        {
            public static List<BuildingPrivlidge> FindCupboards(Vector3 position, float radius = 0f)
            {
                List<BuildingPrivlidge> toolCupboardList = new List<BuildingPrivlidge>();

                /*foreach (BuildingPrivlidge tc in UnityEngine.Object.FindObjectsOfType<BuildingPrivlidge>())
                {
                    plug.Puts($"[FTC] - Building ID: {tc.GetBuilding().ID}");

                    OBB obb = tc.GetBuilding().GetDominatingBuildingPrivilege().WorldSpaceBounds();

                    plug.Puts($"[FTC]   OBB: {obb.ToBounds().}");

                    toolCupboardList.Add(tc);

                    //toolCupboard.GetParentEntity();

                    //plug.Puts($"TC MATCH 1 [{(toolCupboard.GetParentEntity().WorldSpaceBounds().Contains(position) ? "y" : "n")}] [{(toolCupboard.GetParentEntity().bounds.Contains(position) ? "y" : "n")}]");
                    //plug.Puts($"TC MATCH 2 [{(toolCupboard.GetEntity().WorldSpaceBounds().Contains(position) ? "y" : "n")}] [{(toolCupboard.GetEntity().bounds.Contains(position) ? "y" : "n")}]");

                    //foreach (var obj in toolCupboard.authorizedPlayers)
                    //    plug.Puts($" - {obj.username} [{obj.userid}]");

                    //if (toolCupboard.bounds.Contains(position))
                    //    toolCupboardList.Add(toolCupboard);
                }

                plug.Puts($"[FTC] Total: {toolCupboardList.Count}");*/
                        
                return toolCupboardList;
            }
        }

        public static class ModifiedRust
        {
            // Code stolen from Assembly-CSharp.dll -> BasePlayer.cs
            public static BuildingPrivlidge GetBuildingPrivilege(OBB obb)
            {
                BuildingBlock buildingBlock1 = (BuildingBlock)null;
                BuildingPrivlidge buildingPrivlidge = (BuildingPrivlidge)null;
                System.Collections.Generic.List<BuildingBlock> list = Facepunch.Pool.GetList<BuildingBlock>();
                Vis.Entities<BuildingBlock>(obb.position, 16f + obb.extents.magnitude, list, 2097152, QueryTriggerInteraction.Collide);
                for (int index = 0; index < list.Count; ++index)
                {
                    BuildingBlock buildingBlock2 = list[index];
                    if (buildingBlock2.IsOlderThan((BaseEntity)buildingBlock1) && (double)obb.Distance(buildingBlock2.WorldSpaceBounds()) <= 16.0)
                    {
                        BuildingManager.Building building = buildingBlock2.GetBuilding();
                        if (building != null)
                        {
                            BuildingPrivlidge buildingPrivilege = building.GetDominatingBuildingPrivilege();
                            if (!((UnityEngine.Object)buildingPrivilege == (UnityEngine.Object)null))
                            {
                                buildingBlock1 = buildingBlock2;
                                buildingPrivlidge = buildingPrivilege;
                            }
                        }
                    }
                }
                Facepunch.Pool.FreeList<BuildingBlock>(ref list);
                return buildingPrivlidge;
            }
        }
        #endregion

        #region Functions
        public static bool ExecuteCommand(BasePlayer player, string command)
        {
            string[] args = command.Split(' ');

            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || args.Length == 0 || pdata == null)
                return false;

            switch (args[0])
            {
                // Add subscriptions
                // syntax: subscription <name> <duration in seconds>
                case "subscription":
                    if (args.Length != 3)
                        return false;

                    pdata.AddSubscription(Convert.ToString(args[1]), Convert.ToInt32(args[2]));
                    break;

                // Give item
                // syntax: give <itemname> [amount] [inventory]
                case "give":
                    if (args.Length == 1)
                        return false;

                    // Parameters
                    int amount = args.Length == 3 ? Convert.ToInt32(args[2]) : 1;
                    ItemContainer inventory = player.inventory.containerMain;
                    if (args.Length == 4 && Convert.ToString(args[3]) == "belt")
                        inventory = player.inventory.containerBelt;
                    else if (args.Length == 4 && Convert.ToString(args[3]) == "wear")
                        inventory = player.inventory.containerWear;

                    ItemDefinition definition = ItemManager.FindItemDefinition(Convert.ToString(args[1]));
                    if (definition == null)
                        return false;

                    Item item = ItemManager.CreateByItemID(definition.itemid, amount);

                    if (inventory.itemList.Count >= 24)
                        item.Drop(player.transform.position, new Vector3(0, 1f, 0));
                    else
                        player.inventory.GiveItem(ItemManager.CreateByItemID(definition.itemid, amount), inventory);
                    break;

                // Call the helicopter to the caller
                // syntax: helicopter
                case "helicopter":
                    BaseHelicopter helicopter = (BaseHelicopter)GameManager.server.CreateEntity("assets/prefabs/npc/patrol helicopter/patrolhelicopter.prefab", new Vector3(), new Quaternion(), true);
                    if (helicopter == null)
                        return false;

                    var ai = helicopter?.GetComponent<PatrolHelicopterAI>() ?? null;
                    if (ai == null)
                        return false;

                    ai.SetInitialDestination(player.transform.position + new Vector3(0, 25f, 0));

                    helicopter.Spawn();
                    break;
            }

            return true;
        }

        public static int Timestamp()
        {
            return (int)DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0)).TotalSeconds;
        }

        public static bool HasMinimumVipRank(PlayerData.PData pdata, string rank)
        {
            if (rank == "vip" && (pdata.HasSubscription("vip") || pdata.HasSubscription("vip_pro") || pdata.HasSubscription("vip_elite")))
                return true;

            if (rank == "vip_pro" && (pdata.HasSubscription("vip_pro") || pdata.HasSubscription("vip_elite")))
                return true;

            if (rank == "vip_elite" && (pdata.HasSubscription("vip_elite")))
                return true;

            return false;
        }

        public static List<BasePlayer> SearchOnlinePlayers(string query)
        {
            List<BasePlayer> players = new List<BasePlayer>();

            if (string.IsNullOrEmpty(query))
                return players;
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player.UserIDString.Equals(query))
                    players.Add(player);
                else if (!string.IsNullOrEmpty(player.displayName) && player.displayName.Contains(query, CompareOptions.IgnoreCase))
                    players.Add(player);
                else if (player.net?.connection != null && player.net.connection.ipaddress.Equals(query))
                    players.Add(player);

            return players;
        }

        public static BasePlayer FindPlayerById(ulong id)
        {
            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player != null && player.userID == id)
                    return player;

            return null;
        }
        #endregion
    }
}

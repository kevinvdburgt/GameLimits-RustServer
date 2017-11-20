using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using Oxide.Core;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Remove", "Game Limits", "1.0.0")]
    [Description("The Remove module")]

    public class GameLimitsRemove : RustPlugin
    {
        static int colliderRemovable = LayerMask.GetMask("Construction", "Deployed", "Default");

        #region Server Events
        private void Init()
        {
            //foreach (BasePlayer p in BasePlayer.activePlayerList)
            //    CreateGUI(p);
        }

        [ChatCommand("r")]
        private void OnChatCommandR(BasePlayer player, string command, string[] args)
        {
            OnChatCommandRemove(player, command, args);
        }

        [ChatCommand("remove")]
        private void OnChatCommandRemove(BasePlayer player, string command, string[] args)
        {
            ToggleRemove(player);
        }
        #endregion

        #region GUI
        private static void CreateGUI(BasePlayer player)
        {
            DestoryGUI(player);

            var container = UI.CreateElementContainer("gl_remove", "0 0 0 0.95", "0.35 0.96", "0.65 1", false, "Under");

            CuiHelper.AddUi(player, container);

            UpdateGUI(player, "Remover Tool (9s..)");
        }

        private static void UpdateGUI(BasePlayer player, string message)
        {
            UI.Destroy(player, "gl_remove_text");

            var container = UI.CreateElementContainer("gl_remove_text", "0 0 0 0", "0 0", "1 1", false, "gl_remove");

            UI.CreateLabel(ref container, "gl_remove_text", message, 15, "0 0", "1 1", TextAnchor.MiddleCenter, "1 1 1 1");

            CuiHelper.AddUi(player, container);
        }

        private static void DestoryGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_remove");
        }
        #endregion

        #region Classes
        class RemoverTool : MonoBehaviour
        {
            public BasePlayer player;
            public int timer;
            public BaseEntity ent;
            RaycastHit RayHit;
            float lastUpdate;
            float lastRemove;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            void UpdateTool()
            {
                timer--;

                if (timer <= 0)
                {
                    Destroy();
                    return;
                }

                UpdateGUI(player, $"Remover Tool, click on a entity to remove! <size=12>({timer})</size>");
            }

            void FixedUpdate()
            {
                if (player == null || player.IsSleeping() || !player.IsConnected)
                {
                    Destroy();
                    return;
                }

                float currentTime = UnityEngine.Time.realtimeSinceStartup;
                if (currentTime - lastUpdate >= 0.5f)
                {
                    bool flag1 = Physics.Raycast(player.eyes.HeadRay(), out RayHit, 3f, colliderRemovable);
                    ent = flag1 ? RayHit.GetEntity() : null;
                    lastUpdate = currentTime;
                }

                if (player.serverInput.IsDown(BUTTON.FIRE_PRIMARY))
                {
                    if (currentTime - lastRemove >= 0.5f)
                    {
                        Remove(player);
                        lastRemove = currentTime;
                    }
                }
            }

            public void Start()
            {
                timer = 30;
                CancelInvoke("UpdateTool");
                InvokeRepeating("UpdateTool", 0f, 1f);
                CreateGUI(player);
            }

            public void Destroy()
            {
                DestoryGUI(player);
                CancelInvoke("UpdateTool");
                GameObject.Destroy(this);
            }
        }
        #endregion

        #region Functions
        private void ToggleRemove(BasePlayer player)
        {
            RemoverTool tool = player.GetComponent<RemoverTool>();

            // Destroy old tool
            if (tool != null)
            {
                tool.Destroy();
                return;
            }

            // Create new tool
            tool = player.gameObject.AddComponent<RemoverTool>();
            tool.Start();
        }

        private static void Remove(BasePlayer player)
        {
            RaycastHit hit;
            BaseEntity entity = Physics.Raycast(player.eyes.HeadRay(), out hit, 3.0f, colliderRemovable) ?
                hit.GetEntity() : null;

            if (entity == null)
            {
                player.ChatMessage("<color=#d00>ERROR</color> No valid entity, or entyity is too far.");
                return;
            }

            if (!player.CanBuild())
            {
                player.ChatMessage("<color=#d00>ERROR</color> No building priviliges.");
                return;
            }

            if (entity.OwnerID != player.userID && !HasFriend(entity.OwnerID, player.userID))
            {
                player.ChatMessage($"<color=#d00>ERROR</color> You are not the owner of this entity.");
                return;
            }
            
            if (entity is StorageContainer && (entity as StorageContainer).inventory.itemList.Count > 0)
                DropUtil.DropItems((entity as StorageContainer).inventory, entity.transform.position, 1f);

            entity.Kill(BaseNetworkable.DestroyMode.Gib);
        }
        #endregion
    }
}

using System.Net;
using UnityEngine;
using static Oxide.Plugins.Core;

namespace Oxide.Plugins
{
    [Info("RemoveTool", "Game Limits", "2.0.0")]
    [Description("The removetool plugin")]

    public class RemoveTool : RustPlugin
    {
        static int colliderRemovable = LayerMask.GetMask("Construction", "Deployed", "Default");

        #region Oxide Hooks
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

        #region Functions
        private void ToggleRemove(BasePlayer player)
        {
            RemoverTool tool = player.GetComponent<RemoverTool>();

            // Destroy old tool
            if (tool != null)
            {
                tool.Stop();
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
                player.ChatMessage("<color=#d00>Error</color> no valid entity, or entyity is too far.");
                return;
            }

            if (!player.CanBuild())
            {
                player.ChatMessage("<color=#d00>Error</color> no building priviliges.");
                return;
            }

            if (entity.OwnerID != player.userID && !Friends.HasFriend(entity.OwnerID, player.userID))
            {
                player.ChatMessage($"<color=#d00>Error</color> you are not the owner of this entity.");
                return;
            }

            if (Raid.IsRaidBlocked(player) != null)
            {
                player.ChatMessage($"<color=#d00>Error</color> you are in a raidblocked zone.");
                return;
            }

            if (entity is StorageContainer && (entity as StorageContainer).inventory.itemList.Count > 0)
                DropUtil.DropItems((entity as StorageContainer).inventory, entity.transform.position);

            entity.Kill(BaseNetworkable.DestroyMode.Gib);
        }
        #endregion

        #region Classes
        class RemoverTool : MonoBehaviour
        {
            public BasePlayer player;
            public BaseEntity ent;
            RaycastHit RayHit;
            float lastUpdate;
            float lastRemove;

            void Awake()
            {
                player = GetComponent<BasePlayer>();
            }

            public void Stop()
            {
                Notifications.RemoveTimedNotification(player, "removetool");
                Destroy();
            }

            public void Start()
            {
                Notifications.AddTimedNotification(player, "removetool", "Remove Tool", 30, "0.3 0.3 0.3 1");
                Invoke("Stop", 30f);
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

            public void Destroy()
            {
                CancelInvoke("Stop");
                GameObject.Destroy(this);
            }
        }
        #endregion
    }
}

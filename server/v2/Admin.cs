using Oxide.Game.Rust.Cui;
using System.Collections.Generic;
using System.Net;
using UnityEngine;
using static Oxide.Plugins.Core;

namespace Oxide.Plugins
{
    [Info("Admin", "Game Limits", "2.0.0")]
    [Description("The admin plugin")]

    public class Admin : RustPlugin
    {
        private static List<BasePlayer> adminEnabled = new List<BasePlayer>();

        #region Oxide Hooks
        [ChatCommand("admin")]
        private void OnChatCommandAdmin(BasePlayer player, string command, string[] args)
        {
            PlayerData.PData pdata = PlayerData.Get(player);

            if (player == null || pdata == null || !pdata.admin)
                return;

            if (adminEnabled.Contains(player))
                StopAdmin(player);
            else
                StartAdmin(player);
        }

        private void Init()
        {
            Update();
        }

        private void Unload()
        {
            foreach (BasePlayer player in adminEnabled)
                StopAdmin(player);
        }
        #endregion

        #region Functions
        private void Update()
        {
            foreach (BasePlayer player in adminEnabled)
            {
                UpdateUI(player);
            }

            timer.In(1f, () => Update());
        }

        private void StartAdmin(BasePlayer player)
        {
            if (adminEnabled.Contains(player))
                return;

            adminEnabled.Add(player);

            CreateUI(player);
        }

        private void StopAdmin(BasePlayer player)
        {
            if (!adminEnabled.Contains(player))
                return;

            adminEnabled.Remove(player);

            DestroyUI(player);
        }

        public static bool IsAdmin(BasePlayer player)
        {
            return adminEnabled.Contains(player);
        }
        #endregion

        #region UI
        private void CreateUI(BasePlayer player)
        {
            DestroyUI(player);

            // Create the main container
            CuiElementContainer container = Helper.UI.Container("ui_admin", "0 0 0 .99", "0 0.97", "1 1");
            
            // Display the container
            Helper.UI.Add(player, container);

            UpdateUI(player);
        }

        private void DestroyUI(BasePlayer player)
        {
            Helper.UI.Destroy(player, "ui_admin");
        }

        private void UpdateUI(BasePlayer player)
        {
            // Remove the container
            Helper.UI.Destroy(player, "ui_admin_info");

            // Create the home items themself
            CuiElementContainer container = Helper.UI.Container("ui_admin_info", "0 0 0 0", "0 0", "1 1", false, "ui_admin");

            RaycastHit hit;
            string entityName = null;
            string entityOwner = null;
            if (Physics.Raycast(player.eyes.HeadRay(), out hit, 10f, LayerMask.GetMask("Construction", "Deployed", "Default")))
            {
                if (hit.GetEntity())
                {
                    entityName = hit.GetEntity()?.ShortPrefabName;
                    entityOwner = BasePlayer.FindByID(hit.GetEntity().OwnerID)?.displayName;
                }
            }

            // Info panel
            string info = $"Players: <color=#0d0>{BasePlayer.activePlayerList.Count}</color> | " +
                $"FPS: <color=#0d0>{(int)Performance.current.frameRateAverage}</color> | " +
                $"Entity: {(entityName == null ? "<color=#d00>-</color>" : $"<color=#0d0>{entityName}</color>")} <color=#ddd>{entityOwner}</color> | ";
            Helper.UI.Label(ref container, "ui_admin_info", "1 1 1 1", info, 11, "0.005 0", "0.99 0.95", UnityEngine.TextAnchor.MiddleLeft);
            
            // Add the container
            Helper.UI.Add(player, container);
        }
        #endregion
    }
}

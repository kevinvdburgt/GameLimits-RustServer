// Requires: Helper
// Requires: Database
// Requires: PlayerData
// Requires: Notifications
// Requires: Chat
// Requires: Tips
// Requires: Stacks
// Requires: Spawn
// Requires: Shop
// Requires: Teleport
// Requires: Skills
// Requires: RemoveTool
// Requires: Playtime
// Requires: Kits
// Requires: Friends
// Requires: Cars
// Requires: Info
// Requires: CombatBlock
// Requires: Raid
// Requires: KillFeed
// Requires: Admin

using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Core", "Game Limits", "2.0.0")]
    [Description("The core module of the Game Limits plugins")]

    public class Core : RustPlugin
    {
        static Core plug;

        #region Oxide Hooks
        private void Init()
        {
            plug = this;
        }

        private void OnServerInitialized()
        {
            Puts("Game Limits Core loading..");

            PrintToChat("Game Limits plugins are reloading..");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
            {
                Plugins.PlayerData.Load(player);
            }
        }

        private void OnPlayerInit(BasePlayer player)
        {
            Plugins.PlayerData.Load(player);
        }

        private void OnPlayerDisconnected(BasePlayer player, string reason)
        {
            Plugins.PlayerData.Unload(player);
        }
        #endregion

        #region Classes
        public static class Data
        {
            public static string currentWipe = "7 december";
            public static string nextWipe = "22 december";
        }
        #endregion
    }
}
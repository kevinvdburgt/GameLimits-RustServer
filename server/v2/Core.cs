// Requires: Helper
// Requires: Database
// Requires: PlayerData
// Requires: Notifications
// Requires: Chat
// Requires: Tips
// Requires: Stacks
// Requires: Spawn
// Requires: Shop

using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Core", "Game Limits", "2.0.0")]
    [Description("The core module of the Game Limits plugins")]

    public class Core : RustPlugin
    {
        static Core plug;

        #region Dictionaries
        public static Dictionary<ulong, HashSet<ulong>> playerFriends = new Dictionary<ulong, HashSet<ulong>>();
        #endregion

        #region Oxide Hooks
        private void Init()
        {
            plug = this;

            Puts("Game Limits Core loading..");

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                Plugins.PlayerData.Load(player);
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

        #region Functions
        #endregion
    }
}
using static Oxide.Plugins.GameLimits;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Net;
using Oxide.Game.Rust.Cui;

namespace Oxide.Plugins
{
    [Info("Killfeed", "Game Limits", "1.0.0")]
    [Description("The killfeed module")]

    public class GameLimitsKillfeed : RustPlugin
    {
        private List<KillFeedItem> killFeed = new List<KillFeedItem>();

        #region Server Events
        private void Init()
        {
            killFeed.Add(new KillFeedItem()
            {
                killer = "./xehbit.sh",
                killed = "black flame",
            });

            killFeed.Add(new KillFeedItem()
            {
                killer = "Kunta-Kinte",
                weapon = "C4",
                killed = "black flame",
            });

            killFeed.Add(new KillFeedItem()
            {
                killer = "[TBW]",
                killed = "black flame",
            });

            killFeed.Add(new KillFeedItem()
            {
                killer = "[TBW]",
                killed = "black flame",
            });

            killFeed.Add(new KillFeedItem()
            {
                killer = "[TBW]",
                killed = "black flame",
            });

            killFeed.Add(new KillFeedItem()
            {
                killer = "[TBW]",
                killed = "black flame",
            });

            foreach (BasePlayer p in BasePlayer.activePlayerList)
                CreateGUI(p);
        }
        #endregion

        #region Functions
        private void CreateEntry()
        {

        }
        #endregion

        #region GUI
        private void CreateGUI(BasePlayer player)
        {
            DestroyGUI(player);

            var container = UI.CreateElementContainer("gl_killfeed", "0 0 0 0", "0 0.92", "0.2 1", false, "Under");

            for (int i = 0; i < killFeed.Count; i++)
            {
                float min = (1f / killFeed.Count * i);
                float max = (1f / killFeed.Count * i) + 0.3333f;

                UI.CreateLabel(ref container, "gl_killfeed", killFeed[i].killer, 11, "0.02 " + min.ToString("0.####"), "0.98 " + max.ToString("0.####"), TextAnchor.MiddleLeft, "1 1 1 1");
                UI.CreateLabel(ref container, "gl_killfeed", killFeed[i].killed, 11, "0.02 " + min.ToString("0.####"), "0.98 " + max.ToString("0.####"), TextAnchor.MiddleRight, "1 1 1 1");

                if (killFeed[i].weapon != null)
                    UI.CreateLabel(ref container, "gl_killfeed", killFeed[i].weapon, 11, "0.02 " + min.ToString("0.####"), "0.98 " + max.ToString("0.####"), TextAnchor.MiddleCenter, "1 1 1 1");
            }

            CuiHelper.AddUi(player, container);
        }

        private void DestroyGUI(BasePlayer player)
        {
            UI.Destroy(player, "gl_killfeed");
        }
        #endregion

        #region Classes
        private class KillFeedItem
        {
            public string killer = null;
            public string weapon = null;
            public string killed = null;
        }
        #endregion
    }
}

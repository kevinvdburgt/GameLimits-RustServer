using static Oxide.Plugins.Core;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;
using System.Collections.Generic;

namespace Oxide.Plugins
{
    [Info("Tips", "Game Limits", "2.0.0")]
    [Description("The tips plugin")]

    public class Tips : RustPlugin
    {
        private static readonly string[] tips =
        {
            "Type /kit or /k for the kits.",
            "Type /info or /i for the welcome screen.",
            "You can use /kit starter for some food and a bandage.",
            "Wonder where people get those weapons? Check the Kits and VIP Packages in the Shop.",
            "You can set homes with /home or /h.",
            "Every hour you are rewarded 2 Reward Points or RP, which you can spend in the Shop.",
            "Did you know you can store stuff in your car's trunk?",
            "Cars spawn every few hours at monuments.",
            "To teleport, use /tpr (name) to request a teleport and /tpa to accept one.",
            "You can add friends! Friends have access to your locked doors, chests and TC's and won't be shot by autoturrets.",
            "This server wipes every two weeks.",
            "Have you seen our website yet? Visit https://rust.gamelimits.com!",
            "Did you we have a Discord server? Go to https://rust.gamelimits.com/discord",
            "Did you know we have a Steam Group? Go to https://rust.gamelimits.com/steam",
            "You can access your friendlist with /friend or /f.",

            "Did you know you can be alerted when you got raided? Add our Telegram bot (@GameLimitsBot) to your chat app.",
            "Did you know you can be alerted when you got raided? Add our Telegram bot (@GameLimitsBot) to your chat app.",
            "Did you know you can be alerted when you got raided? Add our Telegram bot (@GameLimitsBot) to your chat app.",
        };

        private static List<int> lastTips = new List<int>();

        #region Oxide Hooks
        private void OnServerInitialized()
        {
            BroadcastTip();
        }
        #endregion

        #region Functions
        private void BroadcastTip()
        {
            int id;

            // Find a tip id that hasnt been broadcasted recently
            do
            {
                id = Random.Range(0, tips.Length);
            } while (lastTips.Contains(id));

            // Add the tip id to the recently used tips
            lastTips.Add(id);

            if (lastTips.Count > 4)
                lastTips.RemoveAt(0);

            string message = $"<color=#5a0>[TIP]</color> <color=#C6C3BF>{tips[id]}</color>";

            Helper.Chat.Broadcast(message);

            Puts($"{tips[id]}");

            timer.In(3600f, () => BroadcastTip());
        }
        #endregion
    }
}


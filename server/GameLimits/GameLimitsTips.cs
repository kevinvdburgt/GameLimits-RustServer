using static Oxide.Plugins.GameLimits;
using System.Net;
using Oxide.Game.Rust.Cui;
using UnityEngine;

namespace Oxide.Plugins
{
    [Info("Tips", "Game Limits", "1.0.0")]
    [Description("The tips module")]

    public class GameLimitsTips : RustPlugin
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
            //"To teleport, use /tpr (name) to request a teleport and /tpa to accept one.",
            "You can add friends! Friends have access to your locked doors, chests and TC's and won't be shot by autoturrets.",
            "This server wipes every two weeks.",
            "Have you seen our website yet? Visit https://rust.gamelimits.com!",
            "Did you we have a Discord server? Go to https://rust.gamelimits.com/discord",
            "Did you know we have a Steam Group? Go to https://rust.gamelimits.com/rust",
            "You can access your friendlist with /friend or /f."
        };

        private void OnServerInitialized()
        {
            BroadcastTip();
        }

        private void BroadcastTip()
        {
            string tip = tips[Random.Range(0, tips.Length)];
            string message = $"<color=#AA0055>[TIP]</color> <color=#C6C3BF>{tip}</color>";

            Puts(tip);

            foreach (BasePlayer player in BasePlayer.activePlayerList)
                if (player != null)
                    player.ChatMessage(message);

            timer.In(3600f, () => BroadcastTip());
        }
    }
}

using System;
using CalamityMod.Items.Armor.Wulfrum;
using CalamityMod.Projectiles.Ranged;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMPFixes.Fixes
{
    /// <summary>
    /// Gores are not loaded on dedicated servers (including Host & Play), so these methods throw there when they look up their gores.
    /// Everything else they do (sounds, dust) already does nothing on the server, so they are skipped there entirely.
    /// </summary>
    internal static class ServerGoreFixes
    {
        public static void ApplyBuzzkillFix()
        {
            MonoModHooks.Add(FixLoader.GetDeclaredMethod(typeof(BuzzkillSaw), nameof(BuzzkillSaw.OnKill)), BuzzkillSaw_OnKill);
        }

        public static void ApplyWulfrumSetFix()
        {
            MonoModHooks.Add(FixLoader.GetDeclaredMethod(typeof(WulfrumArmorPlayer), nameof(WulfrumArmorPlayer.SetBonusEndEffect)), WulfrumArmorPlayer_SetBonusEndEffect);
        }

        private static void BuzzkillSaw_OnKill(Action<BuzzkillSaw, int> orig, BuzzkillSaw self, int timeLeft)
        {
            if (Main.dedServ)
                return;

            orig(self, timeLeft);
        }

        private static void WulfrumArmorPlayer_SetBonusEndEffect(Action<WulfrumArmorPlayer, bool> orig, WulfrumArmorPlayer self, bool violent)
        {
            if (Main.dedServ)
                return;

            orig(self, violent);
        }
    }
}

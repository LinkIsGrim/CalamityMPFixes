using System;
using CalamityMod.NPCs;
using CalamityMod.NPCs.Deconstructors;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMPFixes.Fixes
{
    internal static class BurrowerFixes
    {
        public static void ApplyLootFix()
        {
            MonoModHooks.Add(FixLoader.GetDeclaredMethod(typeof(Burrower), nameof(Burrower.HitEffect)), Burrower_HitEffect);
        }

        public static void ApplyHitboxFix()
        {
            MonoModHooks.Add(FixLoader.GetDeclaredMethod(typeof(BurrowerHitbox), nameof(BurrowerHitbox.AI)), BurrowerHitbox_AI);
        }

        // Burrower's HitEffect only spawns death gores, and gores are not loaded on dedicated servers (including Host & Play).
        // Looking them up there throws inside the killing strike, before checkDead runs, so the server never drops any loot.
        private static void Burrower_HitEffect(Action<Burrower, NPC.HitInfo> orig, Burrower self, NPC.HitInfo hit)
        {
            if (Main.dedServ)
                return;

            orig(self, hit);
        }

        // Once the head is gone, the base worm hitbox AI deactivates the hitbox, but Calamity's override keeps going and reads the
        // head slot's global NPC data, which throws. This does the base AI's head check first and stops there if the head is gone.
        private static void BurrowerHitbox_AI(Action<BurrowerHitbox> orig, BurrowerHitbox self)
        {
            NPC npc = self.NPC;
            int headIndex = (int)npc.ai[0];
            if (!Main.npc.IndexInRange(headIndex) || !Main.npc[headIndex].active || Main.npc[headIndex].ModNPC is not BaseWormNPC)
            {
                npc.active = false;
                return;
            }

            orig(self);
        }
    }
}

using System;
using CalamityMod;
using CalamityMod.Items.Placeables.Furniture;
using CalamityMod.Projectiles.Typeless;
using Microsoft.Xna.Framework;
using Terraria;
using Terraria.ID;
using Terraria.ModLoader;

namespace CalamityMPFixes.Fixes
{
    /// <summary>
    /// Replaces the Wulfrum Lure signal's AI. The original picked its target with Main.LocalPlayer, which on a server is an inactive dummy
    /// player, so the server never spawned waves. Clients spawned local-only Wulfrums instead, which the server never knew about.
    /// Waves now spawn only on the server (or in singleplayer), and the arrival sparks are sent to clients with a packet.
    /// </summary>
    /// <remarks>
    /// Based on WulfrumLureSignal.AI from the Calamity Mod, © Azafure, LLC: https://github.com/CalamityTeam/CalamityModPublic
    /// </remarks>
    internal static class WulfrumLureFix
    {
        public static void Apply()
        {
            MonoModHooks.Add(FixLoader.GetDeclaredMethod(typeof(WulfrumLureSignal), nameof(WulfrumLureSignal.AI)), WulfrumLureSignal_AI);
        }

        private static void WulfrumLureSignal_AI(Action<WulfrumLureSignal> orig, WulfrumLureSignal self)
        {
            Projectile projectile = self.Projectile;
            ref float time = ref projectile.ai[0];

            time++;

            // Only the server (or singleplayer) may spawn NPCs. Clients spawning them would create local-only enemies the server never knows about.
            if (time % WulfrumLureItem.SpawnIntervals == 0 && Main.netMode != NetmodeID.MultiplayerClient)
                SpawnWave(projectile);

            if (time % 2 == 0 && CalamityUtils.IntoMorseCode("perimeter breached", time / WulfrumLureItem.SignalTime))
            {
                float dustCount = MathHelper.TwoPi * 300 / 8f;
                for (int i = 0; i < dustCount; i++)
                {
                    float angle = MathHelper.TwoPi * i / dustCount;
                    Dust dust = Dust.NewDustPerfect(projectile.Center, DustID.Vortex);
                    dust.position = projectile.Center + angle.ToRotationVector2() * 300;
                    dust.scale = 0.7f;
                    dust.noGravity = true;
                    dust.velocity = projectile.velocity;
                }
            }
        }

        private static void SpawnWave(Projectile projectile)
        {
            // The original overwrote MaxEnemiesPerWave with 5 before every wave, so 5 is the value it always used (1 to 4 enemies).
            int enemiesToSpawn = Main.rand.Next(1, 5);

            Player player = Main.player[Player.FindClosest(projectile.position, projectile.width, projectile.height)];
            if (!player.active || (player.Center - projectile.Center).Length() > 3500)
                return;

            var spawnPool = WulfrumLureSignal.LureSpawnPool;
            for (int i = 0; i < enemiesToSpawn; i++)
            {
                int tries = 0;
                Vector2 spawnPosition;
                do
                {
                    Vector2 displacey = Main.rand.NextVector2Unit();
                    if (displacey.Y > 0)
                        displacey.Y *= -1;
                    spawnPosition = player.Center + displacey * Main.rand.NextFloat(600f, 1015f) * new Vector2(1.5f, 1f);
                    if (spawnPosition.Y > player.Center.Y)
                        spawnPosition.Y = player.Center.Y;
                    if (tries > 500)
                        break;
                    tries++;
                }
                while (WorldGen.SolidTile(CalamityUtils.ParanoidTileRetrieval((int)spawnPosition.X / 16, (int)spawnPosition.Y / 16)));

                if (tries >= 500)
                    continue;

                int npcToSpawn = spawnPool[Main.rand.Next(spawnPool.Count)];
                int index = NPC.NewNPC(projectile.GetSource_FromAI(), (int)spawnPosition.X, (int)spawnPosition.Y, npcToSpawn, Target: player.whoAmI);

                if (Main.netMode == NetmodeID.Server)
                {
                    if (index < Main.maxNPCs)
                        NetMessage.SendData(MessageID.SyncNPC, number: index);
                    SendArrivalSparks(spawnPosition);
                }
                else
                    SpawnArrivalSparks(spawnPosition);
            }
        }

        private static void SendArrivalSparks(Vector2 position)
        {
            ModPacket packet = ModContent.GetInstance<CalamityMPFixes>().GetPacket();
            packet.Write((byte)CalamityMPFixes.MessageType.WulfrumLureSpawnSparks);
            packet.WriteVector2(position);
            packet.Send();
        }

        public static void SpawnArrivalSparks(Vector2 position)
        {
            for (int i = 0; i < 16; i++)
            {
                Dust zapDust = Dust.NewDustPerfect(position + Main.rand.NextVector2Circular(1f, 1f) * 20f, DustID.Electric, Main.rand.NextVector2Circular(1f, 1f) * Main.rand.NextFloat(1f, 2.3f) - Vector2.UnitY * 6f);
                zapDust.noGravity = true;
            }
        }
    }
}

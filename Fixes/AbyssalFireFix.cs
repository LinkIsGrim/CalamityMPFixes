using System;
using System.Reflection;
using CalamityMod.Projectiles.Ranged;
using Microsoft.Xna.Framework;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using Terraria;
using Terraria.ModLoader;

namespace CalamityMPFixes.Fixes
{
    /// <summary>
    /// Voidragon's laser (AbyssalFire) finds its gun through <c>Main.projectile[(int)ai[0]]</c>, but ai[0] is the gun's index on the owner's
    /// client, which is a different slot on the server and other clients. There it finds the wrong projectile and throws a
    /// NullReferenceException in AI and PreDraw. This resolves the gun through the owner's projectile identity instead.
    /// </summary>
    /// <remarks>
    /// The VoidragonHoldout getter is tiny, so the JIT can inline it into its callers and a detour on it would be bypassed.
    /// Instead, every call to it inside AbyssalFire is rewritten to call <see cref="ResolveHoldout"/>.
    /// </remarks>
    internal static class AbyssalFireFix
    {
        private delegate bool orig_PreDraw(AbyssalFire self, ref Color lightColor);
        private delegate bool hook_PreDraw(orig_PreDraw orig, AbyssalFire self, ref Color lightColor);

        public static void Apply()
        {
            MethodInfo getter = typeof(AbyssalFire).GetProperty(nameof(AbyssalFire.VoidragonHoldout))?.GetMethod
                ?? throw new MissingMethodException(typeof(AbyssalFire).FullName, "get_" + nameof(AbyssalFire.VoidragonHoldout));
            MethodInfo resolver = typeof(AbyssalFireFix).GetMethod(nameof(ResolveHoldout), BindingFlags.NonPublic | BindingFlags.Static);

            int patchedMethods = 0;
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            foreach (MethodInfo method in typeof(AbyssalFire).GetMethods(flags))
            {
                if (method == getter || !CallsMethod(method, getter))
                    continue;

                MonoModHooks.Modify(method, il =>
                {
                    var cursor = new ILCursor(il);
                    while (cursor.TryGotoNext(MoveType.Before, i => i.MatchCallOrCallvirt(getter)))
                    {
                        cursor.Remove();
                        cursor.Emit(OpCodes.Call, resolver);
                    }
                });
                patchedMethods++;
            }

            if (patchedMethods == 0)
                throw new InvalidOperationException($"No calls to {getter.Name} found in {typeof(AbyssalFire).FullName}.");

            // PreDraw reads the gun's beam timer before any null check, so skip drawing while the gun can't be found.
            MonoModHooks.Add(FixLoader.GetDeclaredMethod(typeof(AbyssalFire), nameof(AbyssalFire.PreDraw)), new hook_PreDraw(AbyssalFire_PreDraw));
        }

        private static Projectile ResolveHoldout(AbyssalFire self)
        {
            Projectile projectile = self.Projectile;
            int holdoutIndex = Projectile.GetByUUID(projectile.owner, projectile.ai[0]);
            if (holdoutIndex < 0 || Main.projectile[holdoutIndex].type != ModContent.ProjectileType<VoidragonHoldout>())
                return null;

            return Main.projectile[holdoutIndex];
        }

        private static bool AbyssalFire_PreDraw(orig_PreDraw orig, AbyssalFire self, ref Color lightColor)
        {
            if (ResolveHoldout(self) is null)
                return false;

            return orig(self, ref lightColor);
        }

        // Scans the method's IL for a call or callvirt to target. A false positive only costs an IL hook that changes nothing.
        private static bool CallsMethod(MethodInfo method, MethodInfo target)
        {
            byte[] il = method.GetMethodBody()?.GetILAsByteArray();
            if (il is null)
                return false;

            const byte call = 0x28;
            const byte callvirt = 0x6F;
            int token = target.MetadataToken;
            for (int i = 0; i + 4 < il.Length; i++)
            {
                if ((il[i] == call || il[i] == callvirt) && BitConverter.ToInt32(il, i + 1) == token)
                    return true;
            }
            return false;
        }
    }
}

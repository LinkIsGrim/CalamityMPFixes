using System;
using System.Reflection;
using Terraria.ModLoader;

namespace CalamityMPFixes.Fixes
{
    /// <summary>
    /// Applies each fix as a runtime hook on Calamity's code. Every fix is applied independently, so if a Calamity update renames or removes
    /// one of the patched methods, only that fix is skipped (with a log warning) and the rest still load.
    /// </summary>
    public class FixLoader : ModSystem
    {
        // Newest Calamity version these fixes were checked against. A newer version may already include them or have changed the patched code,
        // so every hook is skipped there. Bump this after checking a new Calamity release.
        public static readonly Version MaxTestedCalamityVersion = new(2, 2, 4);

        public override void Load()
        {
            Version calamityVersion = ModLoader.GetMod("CalamityMod").Version;
            if (calamityVersion > MaxTestedCalamityVersion)
            {
                Mod.Logger.Warn($"Calamity {calamityVersion} is newer than the last tested version ({MaxTestedCalamityVersion}). No fixes were applied.");
                return;
            }

            TryApply("Burrower drops no loot in multiplayer", BurrowerFixes.ApplyLootFix);
            TryApply("BurrowerHitbox reads a dead head's data", BurrowerFixes.ApplyHitboxFix);
            TryApply("Buzzkill saw gores on the server", ServerGoreFixes.ApplyBuzzkillFix);
            TryApply("Wulfrum set end gores on the server", ServerGoreFixes.ApplyWulfrumSetFix);
            TryApply("Wulfrum Lure waves in multiplayer", WulfrumLureFix.Apply);
            TryApply("Voidragon laser (AbyssalFire) can't find its gun in multiplayer", AbyssalFireFix.Apply);
        }

        private void TryApply(string name, Action apply)
        {
            try
            {
                apply();
                Mod.Logger.Info($"Applied fix: {name}");
            }
            catch (Exception e)
            {
                Mod.Logger.Warn($"Could not apply fix '{name}', skipping it: {e.Message}");
            }
        }

        /// <summary>
        /// Finds a method declared directly on <paramref name="type"/>, throwing if it is missing so <see cref="TryApply"/> skips the fix.
        /// </summary>
        internal static MethodInfo GetDeclaredMethod(Type type, string name)
        {
            const BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly;
            return type.GetMethod(name, flags) ?? throw new MissingMethodException(type.FullName, name);
        }
    }
}

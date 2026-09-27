# Calamity Multiplayer Fixes

A tModLoader mod with unofficial fixes for [Calamity Mod](https://github.com/CalamityTeam/CalamityModPublic) bugs that only happen in multiplayer, including Host & Play (which runs a dedicated server process in the background). It patches Calamity at runtime with MonoMod hooks and contains none of Calamity's files.

Not affiliated with the Calamity Mod team. The fixes have also been submitted upstream, and this mod is meant to be removed once an official Calamity update includes them.

## Fixes

| Fix | Hook | Upstream PR |
|---|---|---|
| Burrower drops no loot in multiplayer: its death gores throw on the server before loot drops | Skip `Burrower.HitEffect` on the server | [#123](https://github.com/CalamityTeam/CalamityModPublic/pull/123) |
| `BurrowerHitbox.AI` reads a dead head's global NPC data and throws | Deactivate the hitbox before `BurrowerHitbox.AI` runs if the head is gone | [#123](https://github.com/CalamityTeam/CalamityModPublic/pull/123) |
| Buzzkill saw gores throw on the server | Skip `BuzzkillSaw.OnKill` on the server | [#124](https://github.com/CalamityTeam/CalamityModPublic/pull/124) |
| Wulfrum set end gores throw on the server | Skip `WulfrumArmorPlayer.SetBonusEndEffect` on the server | [#124](https://github.com/CalamityTeam/CalamityModPublic/pull/124) |
| Wulfrum Lure waves spawn nothing on the server, and local-only robots on clients | Replace `WulfrumLureSignal.AI`, with a packet for the arrival sparks | [#125](https://github.com/CalamityTeam/CalamityModPublic/pull/125) |
| Voidragon's laser (`AbyssalFire`) looks up its gun by the owner's local projectile index, finds the wrong projectile on the server and other clients, and throws | Rewrite calls to `AbyssalFire.VoidragonHoldout` to resolve the gun with `Projectile.GetByUUID`, and skip `PreDraw` while it can't be found | Not yet submitted |

Every player and the host need the mod (`side = Both`): the loot fixes run on the server, and the spark packet needs every client.

## Version safety

`FixLoader.MaxTestedCalamityVersion` is the newest Calamity version the fixes were checked against (currently 2.2.4). On a newer Calamity version, no fixes are applied and a warning is logged. After checking a new Calamity release, either bump that version and publish an update or retire the mod.

Each fix is applied independently. If a patched method is missing (renamed or removed), only that fix is skipped and a warning is logged. Look for `Applied fix:` and `Could not apply fix` lines from `CalamityMPFixes` in `client.log` or `server.log`.

## Building

1. Install tModLoader from Steam.
2. Clone or symlink this repo into tModLoader's `ModSources` folder as `ModSources/CalamityMPFixes`. On Linux that's `~/.local/share/Terraria/tModLoader/ModSources`; on Windows, `Documents\My Games\Terraria\tModLoader\ModSources`. The `.csproj` imports `..\tModLoader.targets`, which only exists there.
3. Make sure Calamity Mod is installed and enabled.
4. In tModLoader, go to Workshop → Develop Mods and select Build for this mod. The in-game build resolves Calamity from `modReferences` in `build.txt`.

For IDE or command-line builds, the `.csproj` references `ModSources/ModAssemblies/CalamityMod.dll` if it exists. If your IDE can't find Calamity's types, point that `HintPath` at a copy of Calamity's assembly.

## Testing checklist

On a Host & Play world with this mod and Calamity enabled:

- [ ] `server.log` shows six `Applied fix:` lines and no `Could not apply fix` lines.
- [ ] Killing a Burrower (including with electric debuffs) drops Mysterious Circuitry and Dubious Plating.
- [ ] No `KeyNotFoundException` from `Burrower.HitEffect` or `BurrowerHitbox.AI` in `server.log`.
- [ ] Activating a Wulfrum Lure spawns Wulfrum robots every 4 seconds that every player sees, with electric sparks where they appear, and they drop loot.
- [ ] Firing the Voidragon's laser in multiplayer logs no `NullReferenceException` from `AbyssalFire` in `server.log`, and other players see the laser attached to the gun.
- [ ] Singleplayer still works: Burrower gores show, lure waves and sparks show.

## Credits and license

The Wulfrum Lure fix (`Fixes/WulfrumLureFix.cs`) is based on `WulfrumLureSignal.AI` from the Calamity Mod, © Azafure, LLC: https://github.com/CalamityTeam/CalamityModPublic

Everything else is under the MIT License. `Fixes/WulfrumLureFix.cs` is excluded from it and remains subject to the [Calamity Mod's license](https://github.com/CalamityTeam/CalamityModPublic/blob/1.4.4/LICENSE.md). See [LICENSE](LICENSE).

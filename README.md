# Anarchy Mode

Shadows of Doubt mod that turns a new city into an armed anarchy simulation.

## Build

Requirements:

- .NET SDK 6 or newer.
- Shadows of Doubt with BepInEx 6 IL2CPP installed.
- DensityMod, SOD.Common and Guns for runtime testing.

Set `SOD_GAME_DIR` to the folder that contains `BepInEx\core` and `BepInEx\interop`, then build:

```powershell
$env:SOD_GAME_DIR = "D:\Steam\steamapps\common\Shadows of Doubt"
dotnet build -c Release
```

If `SOD_GAME_DIR` is not set, the project uses `D:\Steam\steamapps\common\Shadows of Doubt`.

## Package

Build and create the Thunderstore zip:

```powershell
.\build-release.ps1
```

The resulting `AnarchyMode-0.1.1.zip` contains `manifest.json`, `icon.png`, `README.md` and `BepInEx/plugins/AnarchyMode.dll`.

## Project layout

- `AnarchyBehaviour.cs` - city setup, combat loop, kills and player actions.
- `WarDirector.cs` - NPC target selection and aggression.
- `DeathTracker.cs` - death polling, kill credit and scoreboard.
- `Patches.cs` - Harmony patches.
- `Hud.cs` - HUD, kill feed, scoreboard and buttons.
- `SpectatorMode.cs` - free camera and respawn.
- `WallHack.cs` - NPC markers.
- `WeaponSpawner.cs` - street weapons and NPC arming.
- `ModConfig.cs` - BepInEx configuration.
- `package/` - Thunderstore package files.

## Links

- Thunderstore: https://thunderstore.io/c/shadows-of-doubt/p/AvSGames/AnarchyMode/
- Author: AvSGames


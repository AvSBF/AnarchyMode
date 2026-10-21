# Anarchy Mode

Anarchy Mode turns a newly generated Shadows of Doubt city into an armed survival simulation. NPCs fight each other and hunt the player, the kill feed tracks the carnage, and death is not the end.

## Features

- Scatters weapons around the city and arms NPCs.
- NPCs search for nearby targets and attack other citizens or the player.
- Locked doors are forced open during combat.
- Kill feed with killer and victim names.
- Scoreboard with the top killers.
- HUD with alive/dead counters.
- Free camera after death and respawn controls.
- Full heal, free camera, wallhack markers and a weapon menu.
- Detective cases, side jobs and normal routines are suppressed.

## Requirements

- Shadows of Doubt with IL2CPP.
- BepInEx 6 IL2CPP pack.
- DensityMod, SOD.Common and Guns. Thunderstore Mod Manager installs them automatically.

## Installation

Install the mod with Thunderstore Mod Manager or r2modman and launch the game through the mod manager. Manual installation requires BepInEx and all dependencies to be installed first.

## Important

Start a new save. Existing saves and their detective cases are not supported and may break.

## Controls

| Key | Action |
| --- | --- |
| H | Full heal |
| V | Toggle free camera |
| B | Open weapon menu |
| F7 | Toggle wallhack markers |
| F5 | Respawn on the surface after death |
| F | Respawn at the spectator camera position after death |

All keys can be changed in `BepInEx/config/com.avs.anarchymode.cfg`.

## Configuration

The config file exposes city size, weapon counts, NPC arm chance, aggression radius, attack limits, door unlock delay, HUD options, kill feed duration and key bindings. Default city size is 2x2 blocks and requires DensityMod.

## Known issues

- Designed and tested only on a fresh city.
- Large NPC counts can reduce performance.
- Game updates may break IL2CPP method signatures until the mod is updated.

## Links

- Source: https://github.com/AvSBF/AnarchyMode
- Author: AvSGames

using System;
using UnityEngine;

namespace AnarchyMode;

internal sealed class WeaponSpawner
{
    private readonly System.Collections.Generic.List<NewGameLocation> _outside = new();
    private readonly System.Collections.Generic.List<InteractablePreset> _presets = new();
    private readonly System.Collections.Generic.List<Citizen> _citizens = new();
    private readonly System.Collections.Generic.List<Interactable> _streetWeapons = new();

    private System.Random _rng;
    private int _streetSpawned;
    private int _streetTarget;
    private int _streetAttempts;
    private int _presetCursor;
    private int _armCursor;
    private int _armTarget;
    private int _armed;

    internal bool StreetSpawnDone { get; private set; }
    internal bool ArmingDone { get; private set; }
    internal int SpawnedWeaponCount => _streetSpawned;
    internal int ArmedNpcCount => _armed;
    internal int PresetCount => _presets.Count;

    internal InteractablePreset GetPreset(int index)
    {
        if (index < 0 || index >= _presets.Count)
        {
            return null;
        }

        return _presets[index];
    }

    internal string PresetName(int index)
    {
        var preset = GetPreset(index);
        if (preset == null)
        {
            return "No weapons";
        }

        var name = preset.name;
        return string.IsNullOrEmpty(name) ? "Weapon" : name;
    }

    internal bool GivePresetToPlayer(int index, Player player)
    {
        var preset = GetPreset(index);
        if (preset == null || player == null || player.isDead)
        {
            return false;
        }

        try
        {
            preset.SpawnIntoInventory();
            return true;
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("GivePresetToPlayer: " + ex);
            return false;
        }
    }

    internal void Begin(string seed)
    {
        _outside.Clear();
        _presets.Clear();
        _citizens.Clear();
        _streetWeapons.Clear();

        StreetSpawnDone = false;
        ArmingDone = false;
        _streetSpawned = 0;
        _streetAttempts = 0;
        _presetCursor = 0;
        _streetTarget = Math.Max(0, ModConfig.StreetWeaponCount.Value);
        _armCursor = 0;
        _armed = 0;
        _armTarget = ModConfig.ArmNpcs.Value ? Math.Max(0, ModConfig.MaxNpcsToArm.Value) : 0;
        _rng = new System.Random(StableHash(seed + "|weapons"));

        try
        {
            var city = CityData.Instance;
            if (city == null)
            {
                StreetSpawnDone = true;
                ArmingDone = true;
                return;
            }

            if (city.gameLocationDirectory != null)
            {
                foreach (var loc in city.gameLocationDirectory)
                {
                    if (loc != null && loc.isOutside)
                    {
                        _outside.Add(loc);
                    }
                }

                if (_outside.Count == 0)
                {
                    foreach (var loc in city.gameLocationDirectory)
                    {
                        if (loc != null)
                        {
                            _outside.Add(loc);
                        }
                    }
                }
            }

            var toolbox = Toolbox.Instance;
            if (toolbox?.allWeapons != null)
            {
                foreach (var preset in toolbox.allWeapons)
                {
                    AddPreset(preset);
                }
            }

            try
            {
                foreach (var preset in Resources.FindObjectsOfTypeAll<InteractablePreset>())
                {
                    AddPreset(preset);
                }
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning("Weapon preset scan: " + ex.Message);
            }

            if (city.citizenDirectory != null)
            {
                foreach (var citizen in city.citizenDirectory)
                {
                    if (citizen != null && !citizen.isPlayer)
                    {
                        _citizens.Add(citizen);
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("WeaponSpawner.Begin: " + ex);
        }

        if (_outdoorCount == 0 || _presets.Count == 0)
        {
            StreetSpawnDone = true;
        }

        if (!ModConfig.ArmNpcs.Value || _citizens.Count == 0 || _presets.Count == 0)
        {
            ArmingDone = true;
        }

        Plugin.Logger.LogInfo(
            $"WeaponSpawner: outside={_outside.Count}, presets={_presets.Count}, citizens={_citizens.Count}");
    }

    private void AddPreset(InteractablePreset preset)
    {
        if (preset == null || preset.weapon == null || preset.weapon.disabled)
        {
            return;
        }

        if (!_presets.Contains(preset))
        {
            _presets.Add(preset);
        }
    }

    private int _outdoorCount => _outside.Count;

    internal void SpawnStreetWeapons(int budget)
    {
        if (StreetSpawnDone)
        {
            return;
        }

        if (_outside.Count == 0 || _presets.Count == 0)
        {
            StreetSpawnDone = true;
            return;
        }

        try
        {
            for (var i = 0; i < budget && _streetSpawned < _streetTarget; i++)
            {
                _streetAttempts++;
                if (_streetAttempts > _streetTarget * 5)
                {
                    StreetSpawnDone = true;
                    break;
                }

                var loc = _outside[_rng.Next(_outside.Count)];
                var preset = _presets[_presetCursor++ % _presets.Count];
                if (loc.anchorNode == null)
                {
                    continue;
                }

                var item = loc.PlaceObject(preset, null, null, null, out var furniture, passVariable: false,
                    forceSecuritySettings: true, ignoreLimits: true);

                if (item == null)
                {
                    continue;
                }

                _streetWeapons.Add(item);
                _streetSpawned++;
                _ = furniture;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("SpawnStreetWeapons: " + ex.Message);
        }

        if (_streetSpawned >= _streetTarget || _streetAttempts > _streetTarget * 5)
        {
            StreetSpawnDone = true;
        }
    }

    internal void ArmNpcs(int budget)
    {
        if (ArmingDone)
        {
            return;
        }

        if (_citizens.Count == 0 || _presets.Count == 0)
        {
            ArmingDone = true;
            return;
        }

        var processed = 0;
        while (processed < budget && _armCursor < _citizens.Count && _armed < _armTarget)
        {
            var human = _citizens[_armCursor++];
            processed++;

            try
            {
                if (human == null || human.isDead || human.isPlayer)
                {
                    continue;
                }

                if (ModConfig.ArmChance.Value < 1f && _rng.NextDouble() > ModConfig.ArmChance.Value)
                {
                    continue;
                }

                var loc = human.currentGameLocation;
                if (loc == null)
                {
                    loc = human.home;
                }

                if (loc == null)
                {
                    continue;
                }

                var node = human.currentNode ?? loc.anchorNode;
                if (node == null)
                {
                    continue;
                }

                var preset = _presets[_rng.Next(_presets.Count)];

                var item = loc.PlaceObject(preset, human, null, null, out var furniture, passVariable: false,
                    placeClosestTo: node, forceSecuritySettings: true, ignoreLimits: true);

                if (item == null)
                {
                    continue;
                }

                var given = false;
                try
                {
                    given = human.TryGiveItem(item, null, true, false);
                }
                catch
                {
                }

                if (!given && human.ai != null)
                {
                    try
                    {
                        human.ai.SetCurrentWeapon(item);
                        human.ai.RecalculateWeaponStats();
                        given = true;
                    }
                    catch
                    {
                    }
                }

                if (given)
                {
                    try
                    {
                        if (human.ai != null)
                        {
                            if (human.ai.currentWeapon == null)
                            {
                                human.ai.SetCurrentWeapon(item);
                            }

                            human.ai.UpdateCurrentWeapon();
                            human.ai.RecalculateWeaponStats();
                        }
                    }
                    catch
                    {
                    }

                    _armed++;
                }

                _ = furniture;
            }
            catch (Exception ex)
            {
                if (_armCursor < 5)
                {
                    Plugin.Logger.LogWarning("ArmNpcs: " + ex.Message);
                }
            }
        }

        if (_armCursor >= _citizens.Count || _armed >= _armTarget)
        {
            ArmingDone = true;
        }
    }

    internal void Finish()
    {
        StreetSpawnDone = true;
        ArmingDone = true;
    }

    internal Interactable TakeNearestWeapon(Vector3 position, float maxDistance)
    {
        Interactable best = null;
        var bestDistance = maxDistance;
        var bestIndex = -1;

        for (var i = 0; i < _streetWeapons.Count; i++)
        {
            var item = _streetWeapons[i];
            if (item == null)
            {
                continue;
            }

            var distance = Vector3.Distance(position, item.GetWorldPosition());
            if (distance < bestDistance)
            {
                bestDistance = distance;
                best = item;
                bestIndex = i;
            }
        }

        if (bestIndex >= 0)
        {
            _streetWeapons.RemoveAt(bestIndex);
        }

        return best;
    }

    private static int StableHash(string text)
    {
        unchecked
        {
            var hash = (uint)2166136261;
            if (text == null)
            {
                return (int)hash;
            }

            for (var i = 0; i < text.Length; i++)
            {
                hash ^= text[i];
                hash *= 16777619;
            }

            return (int)(hash & 0x7fffffff);
        }
    }
}

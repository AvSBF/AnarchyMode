using System;
using System.Collections.Generic;
using UnityEngine;

namespace AnarchyMode;

internal sealed class WarDirector
{
    private sealed class DoorPlan
    {
        internal NewDoor Door;
        internal float At;
    }

    private readonly System.Collections.Generic.List<Human> _humans = new();
    private readonly System.Collections.Generic.Dictionary<int, float> _bravery = new();
    private readonly System.Collections.Generic.Dictionary<int, float> _nextAttack = new();
    private readonly System.Collections.Generic.Dictionary<int, DoorPlan> _doorPlans = new();
    private readonly System.Collections.Generic.HashSet<int> _humanIds = new();

    private System.Random _rng;
    private string _seed;
    private float _timer;
    private int _scanCounter;
    private int _attackErrors;
    private WeaponSpawner _weapons;

    internal void Initialize(string seed)
    {
        _seed = seed ?? "anarchy";
        _rng = new System.Random(StableHash(_seed + "|war"));
        _bravery.Clear();
        _nextAttack.Clear();
        _doorPlans.Clear();
        _humans.Clear();
        _humanIds.Clear();
        _timer = 1f;
        _scanCounter = 0;
        _attackErrors = 0;
    }

    internal void AttachWeapons(WeaponSpawner weapons)
    {
        _weapons = weapons;
    }

    internal void Tick(float dt)
    {
        _timer -= dt;
        if (_timer > 0f)
        {
            return;
        }

        _timer = Math.Max(0.2f, ModConfig.AggressionInterval.Value);

        var city = CityData.Instance;
        var player = Player.Instance;
        if (city == null || player == null)
        {
            return;
        }

        CollectHumans(city, player);
        if (_humans.Count == 0)
        {
            return;
        }

        var now = Time.time;
        var started = 0;
        var maxAttackers = Math.Max(1, ModConfig.MaxAttackersPerTick.Value);

        for (var i = 0; i < _humans.Count; i++)
        {
            if (started >= maxAttackers)
            {
                break;
            }

            var attacker = _humans[i];
            if (attacker == null || attacker.isDead)
            {
                continue;
            }

            var ai = attacker.ai;
            if (ai == null)
            {
                continue;
            }

            if (ai.attackActive && ai.attackTarget != null && !ai.attackTarget.isDead)
            {
                continue;
            }

            if (ai.persuit && ai.persuitTarget != null && !ai.persuitTarget.isDead)
            {
                continue;
            }

            var next = NextAttackAt(attacker);
            if (now < next)
            {
                continue;
            }

            var target = ChooseTarget(attacker, player);
            if (target == null)
            {
                continue;
            }

            TryArm(attacker, ai);

            if (ai.currentWeapon == null)
            {
                continue;
            }

            try
            {
                ai.SetPersue(target, false, 2, true, Math.Max(ModConfig.AttackRadius.Value, 500f));
            }
            catch (Exception ex)
            {
                if (_attackErrors++ < 5)
                {
                    Plugin.Logger.LogWarning("SetPersue: " + ex);
                }
            }

            try
            {
                ai.StartAttack(target);
                _nextAttack[attacker.humanID] = now + Cooldown(attacker);
                started++;
            }
            catch (Exception ex)
            {
                if (_attackErrors++ < 5)
                {
                    Plugin.Logger.LogWarning("StartAttack: " + ex);
                }

                _nextAttack[attacker.humanID] = now + 3f;
            }
        }

        UpdateDoors(now);
    }

    private void CollectHumans(CityData city, Player player)
    {
        _humans.Clear();
        _humanIds.Clear();
        _scanCounter++;

        try
        {
            if (city.citizenDictionary != null)
            {
                foreach (var pair in city.citizenDictionary)
                {
                    AddHuman(pair.Value, player);
                    if (_humans.Count >= 5000)
                    {
                        break;
                    }
                }
            }

            if (city.visibleActors != null)
            {
                foreach (var actor in city.visibleActors)
                {
                    AddHuman(actor, player);
                    if (_humans.Count >= 5000)
                    {
                        break;
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("CollectHumans: " + ex.Message);
        }
    }

    private void AddHuman(Actor actor, Player player)
    {
        if (actor == null)
        {
            return;
        }

        var human = actor.TryCast<Human>();
        if (human == null)
        {
            return;
        }

        try
        {
            if (human.isPlayer || human.isDead || human.transform == null)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        if (_humanIds.Add(human.humanID))
        {
            _humans.Add(human);
        }
    }
    private Actor ChooseTarget(Human attacker, Player player)
    {
        var position = attacker.transform.position;
        var bravery = Bravery(attacker);
        var playerRadius = Math.Max(ModConfig.AttackRadius.Value, 120f);

        var playerDistance = Vector3.Distance(position, player.transform.position);
        if (!player.isDead && playerDistance <= playerRadius && _rng.NextDouble() < 0.18 + bravery * 0.25)
        {
            return player;
        }

        Actor best = null;
        var bestScore = float.MaxValue;

        for (var i = 0; i < _humans.Count; i++)
        {
            var candidate = _humans[i];
            if (candidate == null || candidate == attacker || candidate.isDead)
            {
                continue;
            }

            float distance;
            try
            {
                distance = Vector3.Distance(position, candidate.transform.position);
            }
            catch
            {
                continue;
            }

            var score = distance;
            if (SameBuilding(attacker, candidate))
            {
                score -= 25f;
            }

            if (score < bestScore)
            {
                bestScore = score;
                best = candidate;
            }
        }

        if (best == null && playerDistance <= playerRadius && !player.isDead)
        {
            best = player;
        }

        return best;
    }
    private static bool SameBuilding(Human a, Human b)
    {
        try
        {
            var homeA = a.home;
            var homeB = b.home;
            if (homeA == null || homeB == null)
            {
                return false;
            }

            return homeA.building == homeB.building;
        }
        catch
        {
            return false;
        }
    }

    private void TryArm(Human human, NewAIController ai)
    {
        if (_weapons == null || ai.currentWeapon != null)
        {
            return;
        }

        try
        {
            var weapon = _weapons.TakeNearestWeapon(human.transform.position, 30f);
            if (weapon == null)
            {
                return;
            }

            ai.SetCurrentWeapon(weapon);
            ai.UpdateCurrentWeapon();
            ai.RecalculateWeaponStats();
            Plugin.Logger.LogInfo($"Armed {Name(human)} with {weapon.GetName()}");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("TryArm: " + ex.Message);
        }
    }

    private void UpdateDoors(float now)
    {
        for (var i = 0; i < _humans.Count; i++)
        {
            var human = _humans[i];
            if (human?.ai == null)
            {
                continue;
            }

            var door = human.ai.doorCheckDoor;
            if (door == null)
            {
                continue;
            }

            var id = door.GetInstanceID();
            if (!door.isLocked)
            {
                _doorPlans.Remove(id);
                continue;
            }

            if (!_doorPlans.TryGetValue(id, out var plan))
            {
                _doorPlans[id] = new DoorPlan
                {
                    Door = door,
                    At = now + ModConfig.DoorUnlockDelay.Value + (float)_rng.NextDouble()
                };
                continue;
            }

            if (now < plan.At)
            {
                continue;
            }

            try
            {
                door.SetLocked(false, human, false);
                door.Barge(human);
                _doorPlans.Remove(id);
            }
            catch (Exception ex)
            {
                Plugin.Logger.LogWarning("Door: " + ex.Message);
                _doorPlans.Remove(id);
            }
        }
    }

    private float Bravery(Human human)
    {
        if (_bravery.TryGetValue(human.humanID, out var value))
        {
            return value;
        }

        var min = ModConfig.BraveryMin.Value;
        var max = ModConfig.BraveryMax.Value;
        var hash = StableHash(_seed + "|bravery|" + human.humanID);
        value = min + (max - min) * ((hash & 0xffff) / 65535f);
        _bravery[human.humanID] = value;
        return value;
    }

    private float NextAttackAt(Human human)
    {
        if (_nextAttack.TryGetValue(human.humanID, out var value))
        {
            return value;
        }

        var delay = 2f + (float)_rng.NextDouble() * 12f;
        value = Time.time + delay;
        _nextAttack[human.humanID] = value;
        return value;
    }

    private float Cooldown(Human human)
    {
        var bravery = Bravery(human);
        return 4f + (1f - bravery) * 12f + (float)_rng.NextDouble() * 4f;
    }

    internal static string Name(Human human)
    {
        if (human == null)
        {
            return "Unknown";
        }

        try
        {
            var name = human.GetCitizenName();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            name = human.citizenName;
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }

            name = (human.firstName + " " + human.surName).Trim();
            if (!string.IsNullOrEmpty(name))
            {
                return name;
            }
        }
        catch
        {
        }

        return "Citizen #" + human.humanID;
    }

    internal static int StableHash(string text)
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

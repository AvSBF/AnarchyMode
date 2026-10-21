using System;
using System.Collections.Generic;

namespace AnarchyMode;

internal sealed class DeathTracker
{
    internal sealed class ScoreEntry
    {
        internal int Id;
        internal string Name;
        internal int Kills;
        internal bool IsPlayer;
    }

    private readonly System.Collections.Generic.HashSet<int> _knownDead = new();
    private readonly System.Collections.Generic.HashSet<int> _credited = new();
    private readonly System.Collections.Generic.Dictionary<int, ScoreEntry> _scores = new();
    private readonly System.Collections.Generic.Dictionary<int, Human> _pending = new();
    private float _timer;

    internal int Alive { get; private set; }

    internal void Reset()
    {
        _knownDead.Clear();
        _credited.Clear();
        _scores.Clear();
        _pending.Clear();
        _timer = 0f;
        Alive = 0;
    }

    internal void Scan(CityData city, Hud hud, bool firstScan)
    {
        _timer -= UnityEngine.Time.deltaTime;
        if (_timer > 0f)
        {
            return;
        }

        _timer = 1f;

        if (city?.citizenDictionary == null)
        {
            return;
        }

        var total = city.citizenDirectory?.Count ?? 0;
        var newlyDead = new System.Collections.Generic.List<Human>();

        try
        {
            foreach (var pair in city.citizenDictionary)
            {
                var human = pair.Value;
                if (human == null || human.isPlayer)
                {
                    continue;
                }

                if (!human.isDead)
                {
                    continue;
                }

                if (_knownDead.Add(human.humanID))
                {
                    if (!firstScan)
                    {
                        newlyDead.Add(human);
                        _pending[human.humanID] = human;
                    }
                }
                else if (!_credited.Contains(human.humanID))
                {
                    _pending[human.humanID] = human;
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("DeathTracker: " + ex.Message);
        }

        Alive = Math.Max(0, total - _knownDead.Count);
        hud.SetCounters(Alive, _knownDead.Count);

        foreach (var pair in new List<Human>(_pending.Values))
        {
            if (TryCreditKill(pair))
            {
                _pending.Remove(pair.humanID);
            }
        }

        for (var i = 0; i < newlyDead.Count; i++)
        {
            var victim = newlyDead[i];
            var killer = GetKiller(victim);
            if (killer != null && killer.humanID == victim.humanID)
            {
                killer = null;
            }

            var line = killer != null
                ? (killer.isPlayer ? "YOU" : WarDirector.Name(killer)) + " killed " + WarDirector.Name(victim)
                : WarDirector.Name(victim) + " is killed";
            hud.AddKill(line);
        }

        if (newlyDead.Count > 0)
        {
            Plugin.Logger.LogInfo($"{newlyDead.Count} new death(s)");
        }
    }

    internal List<ScoreEntry> GetTopScores(int count)
    {
        var list = new List<ScoreEntry>(_scores.Values);
        list.Sort((a, b) =>
        {
            var byKills = b.Kills.CompareTo(a.Kills);
            return byKills != 0 ? byKills : string.Compare(a.Name, b.Name, StringComparison.Ordinal);
        });

        if (list.Count > count)
        {
            list.RemoveRange(count, list.Count - count);
        }

        return list;
    }

    private bool TryCreditKill(Human victim)
    {
        if (victim == null)
        {
            return true;
        }

        if (_credited.Contains(victim.humanID))
        {
            return true;
        }

        var killer = GetKiller(victim);
        if (killer == null || killer.humanID == victim.humanID)
        {
            return false;
        }

        _credited.Add(victim.humanID);

        if (!_scores.TryGetValue(killer.humanID, out var score))
        {
            score = new ScoreEntry
            {
                Id = killer.humanID,
                Name = killer.isPlayer ? "YOU" : WarDirector.Name(killer),
                IsPlayer = killer.isPlayer
            };
            _scores[killer.humanID] = score;
        }

        score.Kills++;
        return true;
    }

    private static Human GetKiller(Human victim)
    {
        try
        {
            var death = victim?.death;
            return death?.GetKiller();
        }
        catch
        {
            return null;
        }
    }
}
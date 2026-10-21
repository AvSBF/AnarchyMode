using System;
using BepInEx.Logging;
using Il2CppInterop.Runtime.InteropTypes;
using UnityEngine;

namespace AnarchyMode;

public class AnarchyBehaviour : MonoBehaviour
{
    internal static AnarchyBehaviour Instance { get; private set; }

    internal Hud Hud;
    internal SpectatorMode Spectator;

    internal bool Enabled => ModConfig.Enabled.Value;

    private WarDirector _war;
    private DeathTracker _deaths;
    private WeaponSpawner _weapons;

    private bool _cityReady;
    private bool _initialized;
    private bool _generationComplete;
    private string _seed;
    private int _initStage;
    private bool _autoStartTried;
    private float _autoStartTimer;
    private bool _citySizeApplied;
    private int _weaponIndex;
    private bool _wallHack;
    private bool _weaponMenuOpen;
    private bool _killingNpc;
    private bool _scoreboardDismissed;
    private bool _investigationWarned;
    private float _scoreboardTimer;
    private readonly System.Collections.Generic.Dictionary<int, Human> _lastAttacker = new();
    private readonly System.Collections.Generic.Dictionary<int, float> _lastAttackTime = new();

    public AnarchyBehaviour(IntPtr ptr) : base(ptr)
    {
    }

    private void Awake()
    {
        Instance = this;
        _war = new WarDirector();
        _deaths = new DeathTracker();
        _weapons = new WeaponSpawner();
        _war.AttachWeapons(_weapons);
        Plugin.Logger.LogInfo("AnarchyBehaviour ready");
    }

    private void Update()
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            if (ModConfig.AutoStartTestCity.Value)
            {
                TryAutoStartTestCity();
            }

            if (CityData.Instance == null)
            {
                ApplyCitySize();
            }

            var city = CityData.Instance;
            if (city == null)
            {
                _generationComplete = false;
                Hud?.SetVisible(false);
                return;
            }

            if (Player.Instance == null)
            {
                return;
            }

            if (_cityReady && !string.IsNullOrEmpty(city.seed) && city.seed != _seed)
            {
                _cityReady = false;
                _initialized = false;
                _generationComplete = false;
            }

            if (!_cityReady)
            {
                if (!_generationComplete || !IsCityUsable(city))
                {
                    return;
                }

                BeginCity(city);
            }

            if (!_initialized)
            {
                StepInitialization();
                return;
            }

            var player = Player.Instance;
            HandleControls();
            _war.Tick(Time.deltaTime);
            _deaths.Scan(city, Hud, false);
            UpdateScoreboard(player);
            Spectator.Update(Time.deltaTime);
            Hud.Tick(Time.deltaTime);

            if (player != null && player.isDead && !Spectator.Active)
            {
                Spectator.Enter(player);
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("Anarchy Update: " + ex);
        }
    }

    private void EnsureHud()
    {
        if (Hud == null || !Hud.IsAlive)
        {
            Hud = new Hud();
            Spectator = new SpectatorMode(Hud);
        }
        else if (Spectator == null)
        {
            Spectator = new SpectatorMode(Hud);
        }
    }

    internal void ApplyCitySize()
    {
        if (_citySizeApplied)
        {
            return;
        }

        try
        {
            var restart = RestartSafeController.Instance;
            if (restart == null)
            {
                return;
            }

            var width = Math.Max(1, ModConfig.CityWidth.Value);
            var height = Math.Max(1, ModConfig.CityHeight.Value);
            restart.cityX = width + 2;
            restart.cityY = height + 2;
            _citySizeApplied = true;
            Plugin.Logger.LogInfo($"City size set to {width}x{height} blocks");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("ApplyCitySize: " + ex.Message);
        }
    }

    private void TryAutoStartTestCity()
    {
        if (_autoStartTried)
        {
            return;
        }

        _autoStartTimer += Time.unscaledDeltaTime;
        if (_autoStartTimer < 8f)
        {
            return;
        }

        _autoStartTried = true;

        try
        {
            var constructor = CityConstructor.Instance;
            Plugin.Logger.LogInfo(
                $"Auto-start check: constructor={constructor != null}, city={CityData.Instance != null}, player={Player.Instance != null}");

            if (constructor != null && CityData.Instance == null)
            {
                Plugin.Logger.LogInfo("Auto-starting test city");
                constructor.GenerateNewCity();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("Auto-start test city: " + ex);
        }
    }

    private static bool IsCityUsable(CityData city)
    {
        if (Player.Instance == null)
        {
            return false;
        }

        return city.citizenDictionary != null
               && city.citizenDictionary.Count > 0
               && city.gameLocationDirectory != null
               && city.gameLocationDirectory.Count > 0;
    }

    private void BeginCity(CityData city)
    {
        _seed = string.IsNullOrEmpty(city.seed) ? city.cityName : city.seed;
        _cityReady = true;
        _initialized = false;
        _initStage = 0;

        _deaths.Reset();
        _war.Initialize(_seed);
        _weapons.Begin(_seed);
        EnsureHud();
        Spectator.Cancel();

        _weaponIndex = 0;
        _wallHack = false;
        _weaponMenuOpen = false;
        _scoreboardDismissed = false;
        _investigationWarned = false;
        _scoreboardTimer = 0f;
        _lastAttacker.Clear();
        _lastAttackTime.Clear();
        RefreshWeaponLabel();
        Hud.SetHealVisible(true);
        Hud.SetWeaponMenuVisible(false);
        Hud.HideScoreboard();

        DisableMissions();
        SuppressInvestigations();

        Hud.ClearFeed();
        Hud.SetVisible(true);
        Hud.SetHudVisible(ModConfig.ShowHud.Value);
        Hud.SetStatus("ANARCHY BEGINS", 6f);
        Hud.SetRespawnVisible(false);
        Plugin.Logger.LogInfo($"Anarchy city started: {_seed}, citizens={city.citizenDictionary.Count}");
    }

    private void StepInitialization()
    {
        try
        {
            if (_initStage == 0)
            {
                _weapons.SpawnStreetWeapons(12);
                if (!_weapons.StreetSpawnDone)
                {
                    return;
                }

                _initStage = 1;
                return;
            }

            if (_initStage == 1)
            {
                if (ModConfig.ArmNpcs.Value)
                {
                    _weapons.ArmNpcs(24);
                }

                if (!ModConfig.ArmNpcs.Value || _weapons.ArmingDone)
                {
                    _initStage = 2;
                }

                return;
            }

            _weapons.Finish();
            RefreshWeaponLabel();
            _deaths.Scan(CityData.Instance, Hud, true);
            _initialized = true;
            Hud.SetStatus("ANARCHY BEGINS", 6f);
            Plugin.Logger.LogInfo(
                $"Anarchy init done: street weapons={_weapons.SpawnedWeaponCount}, armed NPCs={_weapons.ArmedNpcCount}");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("Anarchy init: " + ex);
            _initialized = true;
        }
    }

    private static void DisableMissions()
    {
        try
        {
            var murders = MurderController.Instance;
            if (murders != null)
            {
                murders.SetProcGenKillerLoop(false);
                murders.activeMurders?.Clear();
                murders.inactiveMurders?.Clear();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("Disable murders: " + ex.Message);
        }

        try
        {
            var jobs = SideJobController.Instance;
            if (jobs != null)
            {
                jobs.enableJobs = false;
                jobs.allJobsDictionary?.Clear();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("Disable side jobs: " + ex.Message);
        }

        try
        {
            var panel = CasePanelController.Instance;
            if (panel != null)
            {
                if (panel.activeCases != null)
                {
                    foreach (var activeCase in panel.activeCases)
                    {
                        if (activeCase == null)
                        {
                            continue;
                        }

                        activeCase.ClearAllObjectives();
                        activeCase.isActive = false;
                    }

                    panel.activeCases.Clear();
                }

                panel.activeCase = null;
                panel.UpdateCaseControls();
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("Disable detective cases: " + ex.Message);
        }
    }

    private void SuppressInvestigations()
    {
        if (!Enabled)
        {
            return;
        }

        try
        {
            var panel = CasePanelController.Instance;
            if (panel != null)
            {
                var cases = panel.activeCases;
                if (cases != null)
                {
                    for (var i = 0; i < cases.Count; i++)
                    {
                        var activeCase = cases[i];
                        if (activeCase == null)
                        {
                            continue;
                        }

                        try
                        {
                            activeCase.ClearAllObjectives();
                            activeCase.isActive = false;
                        }
                        catch
                        {
                        }
                    }

                    if (cases.Count > 0)
                    {
                        cases.Clear();
                    }
                }

                panel.activeCase = null;
                panel.archivedCases?.Clear();
                panel.UpdateCaseControls();
            }
        }
        catch (Exception ex)
        {
            WarnInvestigation("Disable case panel: " + ex.Message);
        }

        try
        {
            var murders = MurderController.Instance;
            if (murders != null)
            {
                murders.activeMurders?.Clear();
                murders.inactiveMurders?.Clear();
            }
        }
        catch (Exception ex)
        {
            WarnInvestigation("Disable murder cases: " + ex.Message);
        }

        try
        {
            var gameplay = GameplayController.Instance;
            if (gameplay != null)
            {
                var scenes = gameplay.crimeScenes;
                if (scenes != null && scenes.Count > 0)
                {
                    var snapshot = new System.Collections.Generic.List<NewGameLocation>();
                    foreach (var scene in scenes)
                    {
                        if (scene != null)
                        {
                            snapshot.Add(scene);
                        }
                    }

                    for (var i = 0; i < snapshot.Count; i++)
                    {
                        try
                        {
                            snapshot[i].SetAsCrimeScene(false);
                        }
                        catch
                        {
                        }
                    }

                    scenes.Clear();
                }

                gameplay.crimeSceneCleanups?.Clear();
            }
        }
        catch (Exception ex)
        {
            WarnInvestigation("Clear crime scenes: " + ex.Message);
        }
    }

    private void WarnInvestigation(string message)
    {
        if (_investigationWarned)
        {
            return;
        }

        _investigationWarned = true;
        Plugin.Logger.LogWarning(message);
    }

    internal void OnPlayerZeroHealth(Player player)
    {
        if (!Enabled || player == null)
        {
            return;
        }

        EnsureHud();
        Spectator.Enter(player);
    }

    internal void OnCityGenerationComplete()
    {
        _generationComplete = true;
        Plugin.Logger.LogInfo("City generation complete; waiting for player");
    }

    internal void RespawnPlayer()
    {
        Spectator.Respawn();
    }

    private void HandleControls()
    {
        var player = Player.Instance;
        if (player == null)
        {
            return;
        }

        if (Input.GetKeyDown(ParseKey(ModConfig.HealKey.Value, KeyCode.H)))
        {
            HealPlayer();
        }

        if (Input.GetKeyDown(ParseKey(ModConfig.FreeCamKey.Value, KeyCode.V)))
        {
            Spectator.ToggleFreeCam(player);
        }

        if (Input.GetKeyDown(ParseKey(ModConfig.WallHackKey.Value, KeyCode.F7)))
        {
            _wallHack = !_wallHack;
            Hud.SetStatus(_wallHack ? "WALLHACK ON" : "WALLHACK OFF", 2f);
        }

        if (Input.GetKeyDown(ParseKey(ModConfig.WeaponMenuKey.Value, KeyCode.B)))
        {
            _weaponMenuOpen = !_weaponMenuOpen;
            Hud.SetWeaponMenuVisible(_weaponMenuOpen);
            Hud.SetStatus(_weaponMenuOpen ? "WEAPON MENU" : "WEAPON MENU CLOSED", 1.5f);
        }
    }

    internal void HealPlayer()
    {
        var player = Player.Instance;
        if (!Enabled || player == null || player.isDead)
        {
            return;
        }

        try
        {
            player.ResetHealthToMaximum();
            player.ResetNerveToMaximum();

            try
            {
                player.ResetNegativeStatuses(1f);
            }
            catch
            {
            }

            try
            {
                player.nourishment = 1f;
                player.hydration = 1f;
                player.alertness = 1f;
                player.energy = 1f;
                player.excitement = 0f;
                player.hygiene = 1f;
                player.bladder = 0f;
                player.breath = 1f;
                player.heat = 0.5f;
                player.drunk = 0f;
                player.sick = 0f;
                player.headache = 0f;
                player.wet = 0f;
                player.brokenLeg = 0f;
                player.bruised = 0f;
                player.blackEye = 0f;
                player.blackedOut = 0f;
                player.numb = 0f;
                player.poisoned = 0f;
                player.bleeding = 0f;
                player.starchAddiction = 0f;
                player.blinded = 0f;
            }
            catch
            {
            }

            try
            {
                var status = StatusController.Instance;
                var active = status?.activeStatuses;
                if (active != null)
                {
                    var snapshot = new Il2CppSystem.Collections.Generic.List<StatusPreset>();
                    foreach (var preset in active)
                    {
                        snapshot.Add(preset);
                    }

                    foreach (var preset in snapshot)
                    {
                        status.RemoveAllCounts(preset);
                    }
                }
            }
            catch
            {
            }

            Hud.SetStatus("FULLY HEALED", 3f);
            Plugin.Logger.LogInfo("Player fully healed");
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("HealPlayer: " + ex);
        }
    }

    internal void CycleWeapon(int delta)
    {
        var count = _weapons?.PresetCount ?? 0;
        if (count <= 0)
        {
            Hud.SetStatus("NO WEAPONS AVAILABLE", 2f);
            return;
        }

        _weaponIndex = (_weaponIndex + delta) % count;
        if (_weaponIndex < 0)
        {
            _weaponIndex += count;
        }

        RefreshWeaponLabel();
    }

    internal void GiveSelectedWeapon()
    {
        var player = Player.Instance;
        if (_weapons == null || player == null)
        {
            return;
        }

        if (_weapons.GivePresetToPlayer(_weaponIndex, player))
        {
            Hud.SetStatus("ADDED: " + _weapons.PresetName(_weaponIndex), 3f);
        }
        else
        {
            Hud.SetStatus("COULD NOT ADD WEAPON", 2f);
        }
    }

    private void RefreshWeaponLabel()
    {
        Hud?.SetWeaponLabel(_weapons?.PresetName(_weaponIndex));
    }

    internal void DismissScoreboard()
    {
        _scoreboardDismissed = true;
        Hud?.HideScoreboard();
    }

    private void UpdateScoreboard(Player player)
    {
        _scoreboardTimer -= Time.deltaTime;
        if (_scoreboardTimer > 0f)
        {
            return;
        }

        _scoreboardTimer = 1f;
        SuppressInvestigations();

        if (player == null)
        {
            return;
        }

        var over = player.isDead || Spectator.Active || _deaths.Alive <= 1;
        if (!over)
        {
            _scoreboardDismissed = false;
            Hud.HideScoreboard();
            return;
        }

        if (_scoreboardDismissed)
        {
            return;
        }

        Hud.SetScoreboard(_deaths.GetTopScores(50));
    }

    private void OnGUI()
    {
        if (!_wallHack || !_initialized)
        {
            return;
        }

        var camera = Spectator?.ActiveCamera ?? Camera.main;
        if (camera != null)
        {
            WallHack.Draw(camera);
        }
    }

    private static KeyCode ParseKey(string value, KeyCode fallback)
    {
        return Enum.TryParse(value, true, out KeyCode key) ? key : fallback;
    }

    internal void RecordDamage(Human victim, Actor fromWho)
    {
        if (!Enabled || victim == null || victim.isPlayer)
        {
            return;
        }

        try
        {
            Human attacker = null;
            if (fromWho != null)
            {
                attacker = fromWho.TryCast<Human>();
            }

            if (attacker == null || attacker.humanID == victim.humanID)
            {
                return;
            }

            _lastAttacker[victim.humanID] = attacker;
            _lastAttackTime[victim.humanID] = Time.time;
        }
        catch
        {
        }
    }

    internal void KillKnockedOut(Human human)
    {
        if (!Enabled || human == null || _killingNpc)
        {
            return;
        }

        try
        {
            if (human.isPlayer || human.isDead)
            {
                return;
            }
        }
        catch
        {
            return;
        }

        _killingNpc = true;
        try
        {
            Human killer = null;
            try
            {
                if (_lastAttacker.TryGetValue(human.humanID, out var attacker)
                    && attacker != null
                    && attacker.humanID != human.humanID
                    && Time.time - _lastAttackTime[human.humanID] <= 30f)
                {
                    killer = attacker;
                }
            }
            catch
            {
            }

            var murders = MurderController.Instance;
            if (murders == null || murders.murderPreset == null)
            {
                Plugin.Logger.LogWarning("KillKnockedOut missing MurderController preset for " + human.humanID);
                return;
            }

            var weapon = GetMurderWeapon(human, killer);
            if (weapon == null)
            {
                Plugin.Logger.LogWarning("KillKnockedOut missing weapon for " + human.humanID);
                return;
            }

            var actualKiller = killer ?? human;
            var murder = new MurderController.Murder(human, human, murders.murderPreset, new MurderMO());
            murder.victim = human;
            murder.victimID = human.humanID;
            murder.murderer = actualKiller;
            murder.murdererID = actualKiller.humanID;
            murder.weapon = weapon;
            murder.weaponStr = string.IsNullOrEmpty(weapon.name) ? "Blunt Object" : weapon.name;
            murder.weaponID = weapon.id;
            murder.weaponPreset = weapon.preset;
            try
            {
                if (murder.weaponPreset != null && murder.weaponPreset.weapon == null)
                {
                    murder.weaponPreset.weapon = Resources.Load<MurderWeaponPreset>("data/weapons/bluntobjects/Dumbell");
                }
            }
            catch
            {
            }
            try
            {
                var location = human.currentGameLocation;
                if (location != null)
                {
                    murder.location = location.thisAsAddress;
                }
            }
            catch
            {
            }

            try
            {
                human.currentHealth = 0f;
                human.SetHealth(0f);
            }
            catch
            {
            }

            if (!human.isDead)
            {
                human.Murder(actualKiller, true, murder, weapon, 0f);
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("KillKnockedOut murder: " + ex);
        }
        finally
        {
            _killingNpc = false;
        }

        try
        {
            if (!human.isDead)
            {
                Plugin.Logger.LogWarning("KillKnockedOut failed for " + human.humanID);
            }
        }
        catch
        {
        }
    }

    private static Interactable GetMurderWeapon(Human victim, Human killer)
    {
        var item = FindInventoryWeapon(victim);
        if (item != null)
        {
            return item;
        }

        item = FindInventoryWeapon(killer);
        if (item != null)
        {
            return item;
        }

        try
        {
            var preset = new InteractablePreset();
            preset.weapon = Resources.Load<MurderWeaponPreset>("data/weapons/bluntobjects/Dumbell");
            return new Interactable(preset);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("KillKnockedOut weapon: " + ex.Message);
            return null;
        }
    }

    private static Interactable FindInventoryWeapon(Human human)
    {
        if (human == null)
        {
            return null;
        }

        try
        {
            var inventory = human.inventory;
            if (inventory == null)
            {
                return null;
            }

            for (var i = 0; i < inventory.Count; i++)
            {
                var item = inventory[i];
                if (item == null)
                {
                    continue;
                }

                try
                {
                    if (item.preset != null && item.preset.weapon != null)
                    {
                        return item;
                    }
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return null;
    }}


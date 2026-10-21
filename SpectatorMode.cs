using System;
using UnityEngine;
using Object = UnityEngine.Object;

namespace AnarchyMode;

internal sealed class SpectatorMode
{
    private readonly Hud _hud;
    private readonly System.Random _rng = new();

    private GameObject _cameraObject;
    private Camera _camera;
    private Camera _mainCamera;
    private Player _player;
    private float _yaw;
    private float _pitch;
    private bool _hadMainCamera;

    internal bool Active { get; private set; }
    internal bool FreeCam { get; private set; }
    internal Camera ActiveCamera => _camera != null ? _camera : Camera.main;

    internal SpectatorMode(Hud hud)
    {
        _hud = hud;
    }

    internal void Enter(Player player)
    {
        if (Active || player == null)
        {
            return;
        }

        if (FreeCam)
        {
            ExitFreeCam();
        }

        Active = true;
        _player = player;

        try
        {
            player.isDead = false;
            player.ResetHealthToMaximum();
            player.ResetNerveToMaximum();
            player.EnablePlayerMovement(false);
            player.EnablePlayerMouseLook(false);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("Spectator player setup: " + ex.Message);
        }

        CreateCamera(player.transform.position + Vector3.up * 2.5f, player.transform.eulerAngles.y);

        _hud.SetRespawnVisible(true);
        _hud.SetHealVisible(false);
        _hud.SetStatus("YOU ARE DEAD - F5: SURFACE | F: PLACE HERE | RMB: LOOK", 999f);
        Plugin.Logger.LogInfo("Spectator mode entered");
    }

    internal void ToggleFreeCam(Player player)
    {
        if (Active)
        {
            return;
        }

        if (FreeCam)
        {
            ExitFreeCam();
            return;
        }

        if (player == null || player.isDead)
        {
            return;
        }

        FreeCam = true;
        _player = player;

        try
        {
            player.EnablePlayerMovement(false);
            player.EnablePlayerMouseLook(false);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("FreeCam player setup: " + ex.Message);
        }

        CreateCamera(player.transform.position + Vector3.up * 1.7f, player.transform.eulerAngles.y);

        _hud.SetRespawnVisible(false);
        _hud.SetHealVisible(true);
        _hud.SetStatus("FREECAM - V: EXIT | WASD: MOVE | SPACE/CTRL: UP/DOWN | RMB: LOOK", 999f);
        Plugin.Logger.LogInfo("FreeCam entered");
    }

    internal void Update(float dt)
    {
        if ((!Active && !FreeCam) || _cameraObject == null)
        {
            return;
        }

        var realDt = Time.unscaledDeltaTime;

        if (Active)
        {
            if (Input.GetKeyDown(ParseKey(ModConfig.RespawnKey.Value, KeyCode.F5)))
            {
                Respawn(null);
                return;
            }

            if (Input.GetKeyDown(ParseKey(ModConfig.RespawnHereKey.Value, KeyCode.F)))
            {
                Respawn(_cameraObject.transform.position);
                return;
            }

            if (_player != null)
            {
                try
                {
                    if (_player.currentHealth < 1f || _player.isDead)
                    {
                        _player.isDead = false;
                        _player.ResetHealthToMaximum();
                    }
                }
                catch
                {
                }
            }
        }

        if (Input.GetMouseButton(1))
        {
            _yaw += Input.GetAxis("Mouse X") * 3.2f;
            _pitch -= Input.GetAxis("Mouse Y") * 3.2f;
            _pitch = Mathf.Clamp(_pitch, -85f, 85f);
        }

        _cameraObject.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        var forward = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward;
        var right = Quaternion.Euler(0f, _yaw, 0f) * Vector3.right;

        var move = forward * Input.GetAxisRaw("Vertical") + right * Input.GetAxisRaw("Horizontal");
        if (Input.GetKey(KeyCode.Space))
        {
            move += Vector3.up;
        }
        if (Input.GetKey(KeyCode.LeftControl))
        {
            move += Vector3.down;
        }

        if (move.sqrMagnitude > 0.001f)
        {
            var speed = Input.GetKey(KeyCode.LeftShift) ? 45f : 13f;
            _cameraObject.transform.position += move.normalized * (speed * realDt);
        }
    }

    internal void Respawn()
    {
        Respawn(null);
    }

    private void Respawn(Vector3? atPosition)
    {
        if (!Active || _player == null)
        {
            return;
        }

        try
        {
            var position = atPosition.HasValue ? SnapToGround(atPosition.Value) : FindSpawnPosition();
            _player.isDead = false;
            _player.ResetHealthToMaximum();
            _player.ResetNerveToMaximum();
            _player.SetPosition(position, Quaternion.Euler(0f, (float)_rng.NextDouble() * 360f, 0f));
            _player.EnablePlayerMovement(true);
            _player.EnablePlayerMouseLook(true);
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogError("Respawn: " + ex);
        }

        DestroyCamera();
        _player = null;
        Active = false;
        FreeCam = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        _hud.SetRespawnVisible(false);
        _hud.SetHealVisible(true);
        _hud.SetStatus("RESPAWNED", 3f);
        Plugin.Logger.LogInfo("Player respawned");
    }

    private void ExitFreeCam()
    {
        if (!FreeCam)
        {
            return;
        }

        if (_player != null)
        {
            try
            {
                _player.EnablePlayerMovement(true);
                _player.EnablePlayerMouseLook(true);
            }
            catch
            {
            }
        }

        DestroyCamera();
        _player = null;
        FreeCam = false;
        _hud.SetRespawnVisible(false);
        _hud.SetHealVisible(true);
        _hud.SetStatus("", 0f);
        Plugin.Logger.LogInfo("FreeCam exited");
    }

    internal void Cancel()
    {
        if (FreeCam && _player != null)
        {
            try
            {
                _player.EnablePlayerMovement(true);
                _player.EnablePlayerMouseLook(true);
            }
            catch
            {
            }
        }

        DestroyCamera();
        _player = null;
        Active = false;
        FreeCam = false;
        _hud.SetRespawnVisible(false);
        _hud.SetHealVisible(true);
    }

    private void CreateCamera(Vector3 position, float yaw)
    {
        _mainCamera = Camera.main;
        if (_mainCamera == null)
        {
            try
            {
                _mainCamera = CameraController.Instance?.cam;
            }
            catch
            {
            }
        }

        if (_mainCamera != null)
        {
            _hadMainCamera = true;
            _mainCamera.enabled = false;
        }

        _cameraObject = new GameObject("AnarchyFreeCam");
        Object.DontDestroyOnLoad(_cameraObject);

        _camera = _cameraObject.AddComponent<Camera>();
        _camera.clearFlags = CameraClearFlags.SolidColor;
        _camera.backgroundColor = new Color(0.02f, 0.02f, 0.03f, 1f);
        _camera.depth = 200f;
        _camera.nearClipPlane = 0.1f;
        _camera.farClipPlane = 1000f;

        _cameraObject.transform.position = position;
        _yaw = yaw;
        _pitch = 0f;
        _cameraObject.transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    private void DestroyCamera()
    {
        if (_cameraObject != null)
        {
            Object.Destroy(_cameraObject);
        }

        _cameraObject = null;
        _camera = null;

        if (_hadMainCamera && _mainCamera != null)
        {
            _mainCamera.enabled = true;
        }

        _hadMainCamera = false;
        _mainCamera = null;
    }

    private Vector3 FindSpawnPosition()
    {
        try
        {
            var city = CityData.Instance;
            if (city?.gameLocationDirectory != null)
            {
                var candidates = new System.Collections.Generic.List<NewGameLocation>();
                foreach (var loc in city.gameLocationDirectory)
                {
                    if (loc != null && loc.isOutside && loc.anchorNode != null)
                    {
                        candidates.Add(loc);
                    }
                }

                if (candidates.Count > 0)
                {
                    var loc = candidates[_rng.Next(candidates.Count)];
                    return SnapToGround(loc.anchorNode.position + Vector3.up * 0.2f);
                }
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("FindSpawnPosition: " + ex.Message);
        }

        return _player != null ? _player.transform.position + Vector3.up * 0.5f : Vector3.zero;
    }

    private static KeyCode ParseKey(string value, KeyCode fallback)
    {
        return Enum.TryParse(value, true, out KeyCode key) ? key : fallback;
    }

    private static Vector3 SnapToGround(Vector3 position)
    {
        try
        {
            var origin = position + Vector3.up * 120f;
            if (Physics.Raycast(origin, Vector3.down, out var hit, 300f, ~0, QueryTriggerInteraction.Ignore))
            {
                return hit.point + Vector3.up * 0.2f;
            }
        }
        catch (Exception ex)
        {
            Plugin.Logger.LogWarning("SnapToGround: " + ex.Message);
        }

        return position;
    }
}
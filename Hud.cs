using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AnarchyMode;

internal sealed class Hud
{
    private sealed class FeedLine
    {
        internal string Text;
        internal float Life;
    }

    private readonly Font _font;
    private readonly List<FeedLine> _feed = new();

    private GameObject _root;
    private Text _counters;
    private Text _feedText;
    private Text _status;
    private Text _hint;
    private GameObject _respawnButton;
    private GameObject _healButton;
    private GameObject _weaponPanel;
    private Text _weaponLabel;
    private bool _weaponMenuOpen;
    private bool _hudVisible = true;
    private GameObject _scoreboardPanel;
    private Text _scoreboardTitle;
    private Text _scoreboardLeft;
    private Text _scoreboardRight;
    private float _statusTimer;

    internal Hud()
    {
        _font = LoadFont();
        Build();
    }

    internal bool IsAlive
    {
        get
        {
            try
            {
                return _root != null;
            }
            catch
            {
                return false;
            }
        }
    }

    private static Font LoadFont()
    {
        Font font = null;
        try
        {
            font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        catch
        {
        }

        if (font == null)
        {
            try
            {
                font = Font.CreateDynamicFontFromOSFont(new[] { "Segoe UI", "Arial" }, 16);
            }
            catch
            {
            }
        }

        return font;
    }

    private void Build()
    {
        _root = new GameObject("AnarchyModeCanvas");
        UnityEngine.Object.DontDestroyOnLoad(_root);

        var canvas = _root.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 2000;

        var scaler = _root.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        _root.AddComponent<GraphicRaycaster>();

        _counters = NewText("Counters", 24, Color.white, TextAnchor.UpperLeft);
        var crt = _counters.rectTransform;
        crt.anchorMin = new Vector2(0f, 1f);
        crt.anchorMax = new Vector2(0f, 1f);
        crt.pivot = new Vector2(0f, 1f);
        crt.anchoredPosition = new Vector2(22f, -20f);
        crt.sizeDelta = new Vector2(700f, 90f);
        _counters.text = "ANARCHY MODE\nALIVE 0    DEAD 0";

        _status = NewText("Status", 24, new Color(1f, 0.85f, 0.4f, 1f), TextAnchor.UpperCenter);
        var srt = _status.rectTransform;
        srt.anchorMin = new Vector2(0.5f, 1f);
        srt.anchorMax = new Vector2(0.5f, 1f);
        srt.pivot = new Vector2(0.5f, 1f);
        srt.anchoredPosition = new Vector2(0f, -28f);
        srt.sizeDelta = new Vector2(1100f, 48f);
        _status.text = "";

        _feedText = NewText("KillFeed", 19, Color.white, TextAnchor.LowerLeft);
        var frt = _feedText.rectTransform;
        frt.anchorMin = new Vector2(0f, 0f);
        frt.anchorMax = new Vector2(0f, 0f);
        frt.pivot = new Vector2(0f, 0f);
        frt.anchoredPosition = new Vector2(22f, 22f);
        frt.sizeDelta = new Vector2(900f, 300f);
        _feedText.text = "";

        _hint = NewText("ControlsHint", 17, new Color(0.82f, 0.86f, 0.95f, 1f), TextAnchor.LowerCenter);
        var hrt = _hint.rectTransform;
        hrt.anchorMin = new Vector2(0.5f, 0f);
        hrt.anchorMax = new Vector2(0.5f, 0f);
        hrt.pivot = new Vector2(0.5f, 0f);
        hrt.anchoredPosition = new Vector2(0f, 18f);
        hrt.sizeDelta = new Vector2(1300f, 30f);
        _hint.text = "V: FREECAM    H: FULL HEAL    B: WEAPONS    F7: WALLHACK    F5: SURFACE    F: PLACE HERE";

        BuildHealButton();
        BuildWeaponSelector();
        BuildRespawnButton();
        BuildScoreboard();
        _root.SetActive(false);
    }

    private void BuildRespawnButton()
    {
        _respawnButton = new GameObject("RespawnButton");
        _respawnButton.transform.SetParent(_root.transform, false);

        var image = _respawnButton.AddComponent<Image>();
        image.color = new Color(0.45f, 0.06f, 0.06f, 0.95f);
        image.raycastTarget = true;

        var button = _respawnButton.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener((UnityAction)(() => AnarchyBehaviour.Instance?.RespawnPlayer()));

        var rt = image.rectTransform;
        rt.anchorMin = new Vector2(0.5f, 0f);
        rt.anchorMax = new Vector2(0.5f, 0f);
        rt.pivot = new Vector2(0.5f, 0f);
        rt.anchoredPosition = new Vector2(0f, 110f);
        rt.sizeDelta = new Vector2(320f, 62f);

        var labelGo = new GameObject("Label");
        labelGo.transform.SetParent(_respawnButton.transform, false);
        var label = labelGo.AddComponent<Text>();
        label.font = _font;
        label.fontSize = 26;
        label.alignment = TextAnchor.MiddleCenter;
        label.color = Color.white;
        label.raycastTarget = false;
        label.text = "RESPAWN";
        var lrt = label.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        _respawnButton.SetActive(false);
    }

    private void BuildHealButton()
    {
        _healButton = NewButton("HealButton", "HEAL", new Color(0.08f, 0.42f, 0.16f, 0.95f),
            _root.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-22f, 22f), new Vector2(190f, 54f),
            () => AnarchyBehaviour.Instance?.HealPlayer());
    }

    private void BuildWeaponSelector()
    {
        _weaponPanel = new GameObject("WeaponSelector");
        _weaponPanel.transform.SetParent(_root.transform, false);
        var background = _weaponPanel.AddComponent<Image>();
        background.color = new Color(0.03f, 0.03f, 0.04f, 0.88f);
        background.raycastTarget = false;

        var prt = background.rectTransform;
        prt.anchorMin = new Vector2(1f, 0.5f);
        prt.anchorMax = new Vector2(1f, 0.5f);
        prt.pivot = new Vector2(1f, 0.5f);
        prt.anchoredPosition = new Vector2(-22f, 0f);
        prt.sizeDelta = new Vector2(420f, 150f);

        var title = NewText("WeaponTitle", 22, new Color(1f, 0.85f, 0.4f, 1f), TextAnchor.MiddleCenter);
        title.transform.SetParent(_weaponPanel.transform, false);
        var trt2 = title.rectTransform;
        trt2.anchorMin = new Vector2(0f, 1f);
        trt2.anchorMax = new Vector2(1f, 1f);
        trt2.pivot = new Vector2(0.5f, 1f);
        trt2.anchoredPosition = new Vector2(0f, -8f);
        trt2.sizeDelta = new Vector2(-16f, 32f);
        title.text = "WEAPON SELECT";

        _weaponLabel = NewText("WeaponName", 19, Color.white, TextAnchor.MiddleCenter);
        _weaponLabel.transform.SetParent(_weaponPanel.transform, false);
        var wlrt = _weaponLabel.rectTransform;
        wlrt.anchorMin = new Vector2(0f, 1f);
        wlrt.anchorMax = new Vector2(1f, 1f);
        wlrt.pivot = new Vector2(0.5f, 1f);
        wlrt.anchoredPosition = new Vector2(0f, -46f);
        wlrt.sizeDelta = new Vector2(-16f, 34f);
        _weaponLabel.text = "WEAPON: ...";

        NewButton("PrevWeapon", "<", new Color(0.18f, 0.18f, 0.22f, 0.95f),
            _weaponPanel.transform, new Vector2(0f, 0f), new Vector2(0f, 0f), new Vector2(10f, 10f), new Vector2(42f, 40f),
            () => AnarchyBehaviour.Instance?.CycleWeapon(-1));

        NewButton("GiveWeapon", "ADD TO INVENTORY", new Color(0.18f, 0.3f, 0.5f, 0.95f),
            _weaponPanel.transform, new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0f, 10f), new Vector2(230f, 40f),
            () => AnarchyBehaviour.Instance?.GiveSelectedWeapon());

        NewButton("NextWeapon", ">", new Color(0.18f, 0.18f, 0.22f, 0.95f),
            _weaponPanel.transform, new Vector2(1f, 0f), new Vector2(1f, 0f), new Vector2(-10f, 10f), new Vector2(42f, 40f),
            () => AnarchyBehaviour.Instance?.CycleWeapon(1));

        _weaponPanel.SetActive(false);
    }

    private void BuildScoreboard()
    {
        _scoreboardPanel = new GameObject("Scoreboard");
        _scoreboardPanel.transform.SetParent(_root.transform, false);
        var background = _scoreboardPanel.AddComponent<Image>();
        background.color = new Color(0.015f, 0.015f, 0.025f, 0.94f);
        background.raycastTarget = false;

        var prt = background.rectTransform;
        prt.anchorMin = new Vector2(0.5f, 0.5f);
        prt.anchorMax = new Vector2(0.5f, 0.5f);
        prt.pivot = new Vector2(0.5f, 0.5f);
        prt.anchoredPosition = Vector2.zero;
        prt.sizeDelta = new Vector2(980f, 820f);

        _scoreboardTitle = NewText("ScoreboardTitle", 30, new Color(1f, 0.85f, 0.35f, 1f), TextAnchor.UpperCenter);
        _scoreboardTitle.transform.SetParent(_scoreboardPanel.transform, false);
        var trt = _scoreboardTitle.rectTransform;
        trt.anchorMin = new Vector2(0f, 1f);
        trt.anchorMax = new Vector2(1f, 1f);
        trt.pivot = new Vector2(0.5f, 1f);
        trt.anchoredPosition = new Vector2(0f, -12f);
        trt.sizeDelta = new Vector2(-20f, 42f);
        _scoreboardTitle.text = "TOP 50 - KILLS";

        NewButton("CloseScoreboard", "X", new Color(0.5f, 0.08f, 0.08f, 0.95f),
            _scoreboardPanel.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(-12f, -10f), new Vector2(42f, 34f),
            () => AnarchyBehaviour.Instance?.DismissScoreboard());

        _scoreboardLeft = ScoreColumn("ScoreboardLeft", new Vector2(0f, 0f), new Vector2(0.5f, 1f), new Vector2(18f, 16f), new Vector2(-9f, -58f));
        _scoreboardRight = ScoreColumn("ScoreboardRight", new Vector2(0.5f, 0f), new Vector2(1f, 1f), new Vector2(9f, 16f), new Vector2(-18f, -58f));

        _scoreboardPanel.SetActive(false);
    }

    private Text ScoreColumn(string name, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        var text = NewText(name, 18, Color.white, TextAnchor.UpperLeft);
        text.transform.SetParent(_scoreboardPanel.transform, false);
        text.lineSpacing = 1.05f;
        var rt = text.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.offsetMin = offsetMin;
        rt.offsetMax = offsetMax;
        return text;
    }

    private GameObject NewButton(string name, string labelText, Color color, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 position, Vector2 size, Action onClick)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var image = go.AddComponent<Image>();
        image.color = color;
        image.raycastTarget = true;

        var button = go.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener((UnityAction)(() => onClick()));

        var rt = image.rectTransform;
        rt.anchorMin = anchorMin;
        rt.anchorMax = anchorMax;
        rt.pivot = anchorMin;
        rt.anchoredPosition = position;
        rt.sizeDelta = size;

        var label = new GameObject("Label");
        label.transform.SetParent(go.transform, false);
        var text = label.AddComponent<Text>();
        text.font = _font;
        text.fontSize = 18;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;
        text.raycastTarget = false;
        text.text = labelText;
        var lrt = text.rectTransform;
        lrt.anchorMin = Vector2.zero;
        lrt.anchorMax = Vector2.one;
        lrt.offsetMin = Vector2.zero;
        lrt.offsetMax = Vector2.zero;

        return go;
    }

    internal void SetHealVisible(bool visible)
    {
        if (!IsAlive || _healButton == null)
        {
            return;
        }

        _healButton.SetActive(visible);
    }

    internal void SetWeaponLabel(string text)
    {
        if (IsAlive && _weaponLabel != null)
        {
            _weaponLabel.text = "WEAPON: " + (string.IsNullOrEmpty(text) ? "..." : text);
        }
    }

    internal void SetWeaponMenuVisible(bool visible)
    {
        _weaponMenuOpen = visible;
        if (!IsAlive || _weaponPanel == null)
        {
            return;
        }

        _weaponPanel.SetActive(visible && _hudVisible);
    }

    internal void SetScoreboard(List<DeathTracker.ScoreEntry> entries)
    {
        if (!IsAlive || _scoreboardPanel == null)
        {
            return;
        }

        if (entries == null || entries.Count == 0)
        {
            _scoreboardLeft.text = "NO KILLS RECORDED";
            _scoreboardRight.text = "";
            _scoreboardPanel.SetActive(true);
            return;
        }

        var left = new List<string>();
        var right = new List<string>();
        for (var i = 0; i < entries.Count; i++)
        {
            var entry = entries[i];
            var line = $"{i + 1}. {entry.Name} - {entry.Kills}";
            if (i < 25)
            {
                left.Add(line);
            }
            else
            {
                right.Add(line);
            }
        }

        _scoreboardLeft.text = string.Join("\n", left);
        _scoreboardRight.text = string.Join("\n", right);
        _scoreboardPanel.SetActive(true);
    }

    internal void HideScoreboard()
    {
        if (IsAlive && _scoreboardPanel != null)
        {
            _scoreboardPanel.SetActive(false);
        }
    }
    private Text NewText(string name, int size, Color color, TextAnchor align)
    {
        var go = new GameObject(name);
        go.transform.SetParent(_root.transform, false);

        var text = go.AddComponent<Text>();
        text.font = _font;
        text.fontSize = size;
        text.color = color;
        text.alignment = align;
        text.horizontalOverflow = HorizontalWrapMode.Overflow;
        text.verticalOverflow = VerticalWrapMode.Overflow;
        text.raycastTarget = false;

        var outline = go.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
        outline.effectDistance = new Vector2(1f, -1f);

        return text;
    }

    internal void SetCounters(int alive, int dead)
    {
        if (!IsAlive || _counters == null)
        {
            return;
        }

        _counters.text = $"ANARCHY MODE\nALIVE {alive}    DEAD {dead}";
    }

    internal void AddKill(string line)
    {
        if (string.IsNullOrEmpty(line) || !IsAlive)
        {
            return;
        }

        _feed.Add(new FeedLine { Text = line, Life = ModConfig.KillFeedSeconds.Value });
        while (_feed.Count > 8)
        {
            _feed.RemoveAt(0);
        }

        RebuildFeed();
    }

    internal void SetStatus(string text, float seconds = 4f)
    {
        if (!IsAlive || _status == null)
        {
            return;
        }

        _status.text = text ?? "";
        _statusTimer = seconds;
    }

    internal void SetRespawnVisible(bool visible)
    {
        if (!IsAlive || _respawnButton == null)
        {
            return;
        }

        _respawnButton.SetActive(visible);
    }

    internal void SetVisible(bool visible)
    {
        if (!IsAlive)
        {
            return;
        }

        try
        {
            if (_root.activeSelf != visible)
            {
                _root.SetActive(visible);
            }
        }
        catch
        {
        }
    }

    internal void SetHudVisible(bool visible)
    {
        if (!IsAlive)
        {
            return;
        }

        _hudVisible = visible;

        if (_counters != null)
        {
            _counters.gameObject.SetActive(visible);
        }
        if (_feedText != null)
        {
            _feedText.gameObject.SetActive(visible);
        }
        if (_status != null)
        {
            _status.gameObject.SetActive(visible);
        }
        if (_hint != null)
        {
            _hint.gameObject.SetActive(visible);
        }
        if (_healButton != null)
        {
            _healButton.SetActive(visible);
        }
        if (_weaponPanel != null)
        {
            _weaponPanel.SetActive(visible && _weaponMenuOpen);
        }
        if (_scoreboardPanel != null && !visible)
        {
            _scoreboardPanel.SetActive(false);
        }
    }

    internal void ClearFeed()
    {
        _feed.Clear();
        if (!IsAlive)
        {
            return;
        }

        if (_feedText != null)
        {
            _feedText.text = "";
        }
        if (_status != null)
        {
            _status.text = "";
        }
        _statusTimer = 0f;
        HideScoreboard();
    }

    internal void Tick(float dt)
    {
        if (!IsAlive)
        {
            return;
        }

        if (_statusTimer > 0f)
        {
            _statusTimer -= dt;
            if (_statusTimer <= 0f)
            {
                _status.text = "";
            }
        }

        if (_feed.Count == 0)
        {
            return;
        }

        var changed = false;
        for (var i = _feed.Count - 1; i >= 0; i--)
        {
            _feed[i].Life -= dt;
            if (_feed[i].Life <= 0f)
            {
                _feed.RemoveAt(i);
                changed = true;
            }
        }

        if (changed)
        {
            RebuildFeed();
        }
    }

    private void RebuildFeed()
    {
        if (_feed.Count == 0)
        {
            _feedText.text = "";
            return;
        }

        var lines = new string[_feed.Count];
        for (var i = 0; i < _feed.Count; i++)
        {
            lines[i] = _feed[i].Text;
        }

        _feedText.text = string.Join("\n", lines);
    }
}

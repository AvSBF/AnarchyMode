using UnityEngine;

namespace AnarchyMode;

internal static class WallHack
{
    private static Texture2D _white;
    private static GUIStyle _label;

    internal static void Draw(Camera camera)
    {
        if (camera == null || Event.current == null || Event.current.type != EventType.Repaint)
        {
            return;
        }

        var city = CityData.Instance;
        if (city?.citizenDictionary == null)
        {
            return;
        }

        EnsureStyles();

        foreach (var pair in city.citizenDictionary)
        {
            var human = pair.Value;
            if (human == null || human.isPlayer || human.isDead)
            {
                continue;
            }

            Vector3 screen;
            float distance;
            try
            {
                var world = human.transform.position + Vector3.up * 1.35f;
                screen = camera.WorldToScreenPoint(world);
                distance = Vector3.Distance(camera.transform.position, world);
            }
            catch
            {
                continue;
            }

            if (screen.z <= 0f || screen.x < -80f || screen.x > Screen.width + 80f || screen.y < -80f || screen.y > Screen.height + 80f)
            {
                continue;
            }

            var height = Mathf.Clamp(1300f / Mathf.Max(1f, distance), 18f, 150f);
            var width = height * 0.42f;
            var x = screen.x - width * 0.5f;
            var y = Screen.height - screen.y - height * 0.5f;

            var fill = new Rect(x, y, width, height);
            GUI.color = new Color(1f, 0.1f, 0.1f, 0.18f);
            GUI.DrawTexture(fill, _white);

            GUI.color = new Color(1f, 0.2f, 0.2f, 0.95f);
            DrawBorder(fill);

            if (distance <= 160f)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(x - 40f, y - 20f, width + 80f, 20f),
                    WarDirector.Name(human) + "  " + Mathf.RoundToInt(distance) + "m", _label);
            }
        }

        GUI.color = Color.white;
    }

    private static void DrawBorder(Rect rect)
    {
        GUI.DrawTexture(new Rect(rect.x, rect.y, rect.width, 1f), _white);
        GUI.DrawTexture(new Rect(rect.x, rect.yMax - 1f, rect.width, 1f), _white);
        GUI.DrawTexture(new Rect(rect.x, rect.y, 1f, rect.height), _white);
        GUI.DrawTexture(new Rect(rect.xMax - 1f, rect.y, 1f, rect.height), _white);
    }

    private static void EnsureStyles()
    {
        if (_white == null)
        {
            _white = new Texture2D(1, 1, TextureFormat.RGBA32, false);
            _white.SetPixel(0, 0, Color.white);
            _white.Apply();
        }

        if (_label == null)
        {
            _label = new GUIStyle(GUI.skin.label)
            {
                fontSize = 14,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = false
            };
            _label.normal.textColor = new Color(1f, 0.55f, 0.55f, 1f);
        }
    }
}
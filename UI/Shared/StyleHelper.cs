using System.Reflection;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Visual style helpers that pull colors and assets directly from the game.
    public static class StyleHelper
    {
        // ── Colors loaded from CharacterSelectHandler at runtime ──────────
        // Fallback values if the handler isn't found.
        private static Color _blue      = new Color(0.22f, 0.50f, 0.84f, 1f);
        private static Color _darkBlue  = new Color(0.13f, 0.28f, 0.50f, 1f);
        private static Color _orange    = new Color(0.97f, 0.55f, 0.13f, 1f);
        private static bool  _colorsLoaded;

        public static Color Blue      { get { EnsureColors(); return _blue; } }
        public static Color DarkBlue  { get { EnsureColors(); return _darkBlue; } }
        public static Color Orange    { get { EnsureColors(); return _orange; } }
        public static Color DarkPanel => new Color(0.08f, 0.10f, 0.15f, 0.92f);
        public static Color White     => new Color(0.95f, 0.95f, 0.95f, 1f);

        private static void EnsureColors()
        {
            if (!_colorsLoaded) LoadGameColors();
        }

        public static void LoadGameColors()
        {
            if (_colorsLoaded) return;

            // Try CharacterSelectHandler first (local lobby)
            var csh = Object.FindObjectOfType<CharacterSelectHandler>(true);
            if (csh != null)
            {
                _blue     = GetColorField(csh, "blue")     ?? _blue;
                _darkBlue = GetColorField(csh, "darkBlue") ?? _darkBlue;
                _orange   = GetColorField(csh, "orange")   ?? _orange;
                _colorsLoaded = true;
                Plugin.Log.LogInfo("[StyleHelper] Loaded colors from CharacterSelectHandler.");
                return;
            }

            // Try online variant
            var csho = Object.FindObjectOfType<CharacterSelectHandler_online>(true);
            if (csho != null)
            {
                _blue     = GetColorField(csho, "blue")     ?? _blue;
                _darkBlue = GetColorField(csho, "darkBlue") ?? _darkBlue;
                _orange   = GetColorField(csho, "orange")   ?? _orange;
                _colorsLoaded = true;
                Plugin.Log.LogInfo("[StyleHelper] Loaded colors from CharacterSelectHandler_online.");
                return;
            }

            Plugin.Log.LogWarning("[StyleHelper] CharacterSelectHandler not found — using fallback colors.");
            _colorsLoaded = true;
        }

        private static Color? GetColorField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(Color))
                return (Color)field.GetValue(obj);
            return null;
        }

        // ── Platform materials ────────────────────────────────────────────
        // Returns the actual shader material used by each platform type.
        // The material uses parameters: _Scale, _BevelRadius, _RHeight, _RWidth.
        private static readonly Material?[] _platformMaterials = new Material[6];
        private static bool _materialsScanned;

        public static Material? GetPlatformMaterial(int type)
        {
            if (!_materialsScanned) ScanPlatformMaterials();
            return _platformMaterials[Mathf.Clamp(type, 0, 5)];
        }

        public static void ScanPlatformMaterials()
        {
            if (_materialsScanned) return;
            _materialsScanned = true;

            var platforms = Object.FindObjectsOfType<StickyRoundedRectangle>(true);
            foreach (var p in platforms)
            {
                int t = (int)p.platformType;
                if (t < 0 || t >= 6) continue;
                if (_platformMaterials[t] != null) continue;

                var sr = p.GetComponent<SpriteRenderer>();
                if (sr != null && sr.material != null)
                    _platformMaterials[t] = sr.material;
            }

            int found = 0;
            foreach (var m in _platformMaterials) if (m != null) found++;
            Plugin.Log.LogInfo($"[StyleHelper] Scanned platform materials: {found}/6 found.");
        }

        public static void InvalidateMaterialCache()
        {
            _materialsScanned = false;
            for (int i = 0; i < 6; i++) _platformMaterials[i] = null;
        }

        // ── Platform display colors (fallback when material not found) ────
        public static readonly Color[] PlatformColors = {
            new Color(0.25f, 0.62f, 0.18f, 1f),  // grass
            new Color(0.82f, 0.88f, 0.98f, 1f),  // snow
            new Color(0.55f, 0.82f, 0.95f, 1f),  // ice
            new Color(0.08f, 0.08f, 0.18f, 1f),  // space
            new Color(0.52f, 0.55f, 0.60f, 1f),  // robot
            new Color(0.22f, 0.75f, 0.32f, 1f),  // slime
        };

        public static readonly string[] PlatformNames = { "Grass", "Snow", "Ice", "Space", "Robot", "Slime" };
        public static readonly string[] ThemeNames    = { "Grass", "Snow", "Space" };
        public static readonly Color[]  ThemeColors   = {
            new Color(0.25f, 0.62f, 0.18f, 1f),
            new Color(0.82f, 0.88f, 0.98f, 1f),
            new Color(0.08f, 0.08f, 0.18f, 1f),
        };

        // ── Fonts ─────────────────────────────────────────────────────────
        // Grab the font that CharacterSelectHandler's startText uses.
        private static TMP_FontAsset? _gameFont;
        private static bool _fontLoaded;

        public static TMP_FontAsset? GetGameFont()
        {
            if (_fontLoaded) return _gameFont;
            _fontLoaded = true;

            // Prefer the font on the lobby start button text
            var csh = Object.FindObjectOfType<CharacterSelectHandler>(true);
            if (csh != null)
            {
                var startText = GetField<TextMeshProUGUI>(csh, "startText");
                if (startText != null && startText.font != null)
                {
                    _gameFont = startText.font;
                    Plugin.Log.LogInfo("[StyleHelper] Loaded game font from startText.");
                    return _gameFont;
                }
            }

            // Fallback: any TMP text in scene
            var anyText = Object.FindObjectOfType<TextMeshProUGUI>(true);
            if (anyText != null && anyText.font != null)
            {
                _gameFont = anyText.font;
                Plugin.Log.LogInfo("[StyleHelper] Loaded game font from scene TMP.");
            }
            return _gameFont;
        }

        // ── Animation curve (matches ReadyButton hover style) ─────────────
        private static AnimationCurve? _hoverCurve;

        public static AnimationCurve GetHoverCurve()
        {
            if (_hoverCurve != null) return _hoverCurve;

            // Try to steal ReadyButton's HoverScaleAnim
            var rb = Object.FindObjectOfType<ReadyButton>(true);
            if (rb != null)
            {
                var curve = GetField<AnimationCurve>(rb, "HoverScaleAnim");
                if (curve != null)
                {
                    _hoverCurve = curve;
                    Plugin.Log.LogInfo("[StyleHelper] Loaded hover curve from ReadyButton.");
                    return _hoverCurve;
                }
            }

            // Fallback: ease-in-out
            _hoverCurve = AnimationCurve.EaseInOut(0f, 1f, 0.15f, 1.08f);
            return _hoverCurve;
        }

        // ── Sprite factories ──────────────────────────────────────────────
        public static Sprite MakeSolidSprite(Color color)
        {
            var tex = new Texture2D(4, 4, TextureFormat.RGBA32, false);
            for (int y = 0; y < 4; y++)
                for (int x = 0; x < 4; x++)
                    tex.SetPixel(x, y, color);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
        }

        public static Sprite MakeRoundedSprite()
        {
            const int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;
            const float r = 7f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float cx = Mathf.Min(x, size - 1 - x);
                    float cy = Mathf.Min(y, size - 1 - y);
                    bool corner = cx < r && cy < r;
                    float d = corner ? Vector2.Distance(new Vector2(cx, cy), new Vector2(r, r)) : 0f;
                    tex.SetPixel(x, y, new Color(1, 1, 1, d > r ? 0f : 1f));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size / 2f, 1,
                SpriteMeshType.FullRect, new Vector4(r, r, r, r));
        }

        // ── Button styling ────────────────────────────────────────────────
        // Normal = Blue, Hover = lighter Blue, Pressed = Orange (like the game)
        public static void StyleButton(Button btn, Color baseColor)
        {
            // Use blue as base regardless of passed color (caller can override via img.color)
            var c = btn.colors;
            c.normalColor      = _blue;
            c.highlightedColor = _blue + new Color(0.10f, 0.10f, 0.12f, 0f);  // чуть светлее
            c.pressedColor     = _orange;                                        // оранжевый при нажатии
            c.selectedColor    = _orange;
            c.disabledColor    = new Color(0.25f, 0.28f, 0.35f, 0.6f);
            c.colorMultiplier  = 1f;
            c.fadeDuration     = 0.08f;
            btn.colors = c;
            btn.transition = Selectable.Transition.ColorTint;

            if (btn.GetComponent<Image>() is Image img)
            {
                img.color  = baseColor; // сохраняем переданный цвет для тематических кнопок
                img.sprite = MakeRoundedSprite();
                img.type   = Image.Type.Sliced;
            }
        }

        // Вариант для стандартных синих кнопок без тематического цвета
        public static void StyleButtonBlue(Button btn)
        {
            StyleButton(btn, _blue);
        }

        // Attach this to any button to get the blue→orange press effect on the Image directly.
        public static void AddPressColorSwap(Button btn)
        {
            var swapper = btn.gameObject.AddComponent<PressColorSwapper>();
            swapper.NormalColor  = _blue;
            swapper.PressedColor = _orange;
        }

        // ── Text styling ──────────────────────────────────────────────────
        public static void StyleText(TextMeshProUGUI tmp, float fontSize = 18f, bool bold = false)
        {
            var font = GetGameFont();
            if (font != null) tmp.font = font;
            tmp.fontSize  = fontSize;
            tmp.color     = new Color(0.95f, 0.95f, 0.95f, 1f);
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.alignment = TextAlignmentOptions.Center;
        }

        // ── Reflection helper ─────────────────────────────────────────────
        private static T? GetField<T>(object obj, string name) where T : class
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return f?.GetValue(obj) as T;
        }
    }
}

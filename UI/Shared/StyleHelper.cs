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
        private static Color _blue      = new Color(0.125f, 0.600f, 0.933f, 1f);
        private static Color _darkBlue  = new Color(0.10f,  0.25f,  0.45f,  1f);
        private static Color _orange    = new Color(0.933f, 0.643f, 0.125f, 1f);
        private static bool  _colorsLoaded;

        public static Color Blue      { get { EnsureColors(); return _blue; } }
        public static Color DarkBlue  { get { EnsureColors(); return _darkBlue; } }
        public static Color Orange    { get { EnsureColors(); return _orange; } }

        // Surface / background tones (dark game aesthetic)
        public static Color DarkPanel     => new Color(0.07f, 0.09f, 0.14f, 0.97f);
        public static Color DarkSurface   => new Color(0.09f, 0.11f, 0.17f, 1f);
        public static Color DarkElevated  => new Color(0.12f, 0.15f, 0.22f, 1f);
        public static Color DarkBorder    => new Color(0.20f, 0.25f, 0.38f, 0.7f);
        public static Color TextPrimary   => new Color(0.95f, 0.95f, 0.95f, 1f);
        public static Color TextSecondary => new Color(0.60f, 0.65f, 0.75f, 1f);
        public static Color TextMuted     => new Color(0.40f, 0.43f, 0.52f, 1f);
        public static Color White         => new Color(0.95f, 0.95f, 0.95f, 1f);
        public static Color DangerColor   => new Color(0.80f, 0.18f, 0.18f, 1f);
        public static Color SuccessColor  => new Color(0.20f, 0.65f, 0.30f, 1f);

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
                _blue     = Opaque(GetColorField(csh, "blue")     ?? _blue);
                _darkBlue = Opaque(GetColorField(csh, "darkBlue") ?? _darkBlue);
                _orange   = Opaque(GetColorField(csh, "orange")   ?? _orange);
                _colorsLoaded = true;
                Plugin.Log.LogInfo($"[StyleHelper] Loaded colors from CharacterSelectHandler. blue={_blue} orange={_orange}");
                return;
            }

            // Try online variant
            var csho = Object.FindObjectOfType<CharacterSelectHandler_online>(true);
            if (csho != null)
            {
                _blue     = Opaque(GetColorField(csho, "blue")     ?? _blue);
                _darkBlue = Opaque(GetColorField(csho, "darkBlue") ?? _darkBlue);
                _orange   = Opaque(GetColorField(csho, "orange")   ?? _orange);
                _colorsLoaded = true;
                Plugin.Log.LogInfo($"[StyleHelper] Loaded colors from CharacterSelectHandler_online. blue={_blue} orange={_orange}");
                return;
            }

            Plugin.Log.LogWarning("[StyleHelper] CharacterSelectHandler not found — using fallback colors.");
            _colorsLoaded = true;
        }

        private static Color Opaque(Color c) => new Color(c.r, c.g, c.b, 1f);

        private static Color? GetColorField(object obj, string fieldName)
        {
            var field = obj.GetType().GetField(fieldName,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null && field.FieldType == typeof(Color))
                return (Color)field.GetValue(obj);
            return null;
        }

        // ── Platform sprites & materials ──────────────────────────────────
        private static readonly Material?[] _platformMaterials = new Material[6];
        private static readonly Sprite?[]   _platformSprites   = new Sprite[6];
        private static bool _platformsScanned;

        public static Material? GetPlatformMaterial(int type)
        {
            if (!_platformsScanned) ScanPlatformAssets();
            return _platformMaterials[Mathf.Clamp(type, 0, 5)];
        }

        public static Sprite? GetPlatformSprite(int type)
        {
            if (!_platformsScanned) ScanPlatformAssets();
            return _platformSprites[Mathf.Clamp(type, 0, 5)];
        }

        // Legacy name kept for callers that still use it
        public static void ScanPlatformMaterials() => ScanPlatformAssets();

        public static void ScanPlatformAssets()
        {
            if (_platformsScanned) return;
            _platformsScanned = true;

            var platforms = Object.FindObjectsOfType<StickyRoundedRectangle>(true);
            foreach (var p in platforms)
            {
                int t = (int)p.platformType;
                if (t < 0 || t >= 6) continue;

                var sr = p.GetComponent<SpriteRenderer>();
                if (sr == null) continue;

                if (_platformMaterials[t] == null && sr.material != null)
                    _platformMaterials[t] = sr.material;

                if (_platformSprites[t] == null && sr.sprite != null)
                    _platformSprites[t] = sr.sprite;
            }

            int found = 0;
            for (int i = 0; i < 6; i++) if (_platformSprites[i] != null) found++;
            Plugin.Log.LogInfo($"[StyleHelper] Scanned platform assets: {found}/6 sprites found.");
        }

        public static void InvalidateMaterialCache()
        {
            _platformsScanned = false;
            for (int i = 0; i < 6; i++) { _platformMaterials[i] = null; _platformSprites[i] = null; }
        }

        // ── Game UI button sprite (from CharacterSelectBox.joinColor Image) ──
        private static Sprite? _gameButtonSprite;

        public static Sprite? GetGameButtonSprite()
        {
            if (_gameButtonSprite != null) return _gameButtonSprite;

            // Scan all sliced Images in the scene — game's rounded button sprite
            var images = Object.FindObjectsOfType<Image>(true);
            foreach (var img in images)
            {
                if (img.sprite != null && img.type == Image.Type.Sliced)
                {
                    _gameButtonSprite = img.sprite;
                    Plugin.Log.LogInfo($"[StyleHelper] Found game button sprite: {img.sprite.name} on {img.gameObject.name}");
                    return _gameButtonSprite;
                }
            }
            return null;
        }

        public static void InvalidateButtonSpriteCache() => _gameButtonSprite = null;

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
        private static TMP_FontAsset? _gameFont;
        private static bool _fontLoaded;

        public static TMP_FontAsset? GetGameFont()
        {
            if (_fontLoaded && _gameFont != null) return _gameFont;
            _fontLoaded = true;

            // Best source: LocalizedText.localizationTable — this is what ReadyButton uses
            try
            {
                var table = GetStaticField<LocalizationTable>(typeof(LocalizedText), "localizationTable");
                if (table != null)
                {
                    var settings = GetStaticField<Settings>(typeof(Settings), null);
                    // Settings.Get() is a static method
                    var getMethod = typeof(Settings).GetMethod("Get",
                        System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public);
                    var settingsInst = getMethod?.Invoke(null, null);
                    if (settingsInst != null)
                    {
                        var getFontMethod = table.GetType().GetMethod("GetFont",
                            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);
                        if (getFontMethod != null)
                        {
                            var langField = settingsInst.GetType().GetField("Language",
                                System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                            var lang = langField?.GetValue(settingsInst) ?? 0;
                            var font = getFontMethod.Invoke(table, new object[] { lang, false }) as TMP_FontAsset;
                            if (font != null)
                            {
                                _gameFont = font;
                                Plugin.Log.LogInfo("[StyleHelper] Loaded game font from LocalizationTable.");
                                return _gameFont;
                            }
                        }
                    }
                }
            }
            catch { /* fallback below */ }

            // Fallback: grab font from ReadyButton's text
            var rb = Object.FindObjectOfType<ReadyButton>(true);
            if (rb != null)
            {
                var txt = GetField<TextMeshProUGUI>(rb, "text");
                if (txt?.font != null)
                {
                    _gameFont = txt.font;
                    Plugin.Log.LogInfo("[StyleHelper] Loaded game font from ReadyButton.text.");
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

            _hoverCurve = AnimationCurve.EaseInOut(0f, 1f, 0.15f, 1.08f);
            return _hoverCurve;
        }

        // ── ReadyButton sprite steal ───────────────────────────────────────
        // Tries to clone the ReadyButton's Image.sprite from the current scene.
        // Returns null if the ReadyButton is not present (e.g. before lobby loads).
        private static Sprite? _readyButtonSprite;
        private static bool    _readyButtonSpriteSearched;

        public static Sprite? TryGetReadyButtonSprite()
        {
            if (_readyButtonSpriteSearched) return _readyButtonSprite;
            _readyButtonSpriteSearched = true;

            var rb = Object.FindObjectOfType<ReadyButton>(true);
            if (rb != null)
            {
                var img = rb.GetComponent<Image>();
                if (img != null && img.sprite != null)
                {
                    _readyButtonSprite = img.sprite;
                    Plugin.Log.LogInfo("[StyleHelper] Stole sprite from ReadyButton.");
                    return _readyButtonSprite;
                }
                // Also check child images
                foreach (var childImg in rb.GetComponentsInChildren<Image>(true))
                {
                    if (childImg.sprite != null)
                    {
                        _readyButtonSprite = childImg.sprite;
                        Plugin.Log.LogInfo("[StyleHelper] Stole sprite from ReadyButton child image.");
                        return _readyButtonSprite;
                    }
                }
            }

            Plugin.Log.LogWarning("[StyleHelper] ReadyButton sprite not found — will use MakeRoundedSprite() fallback.");
            return null;
        }

        public static void InvalidateReadyButtonSpriteCache()
        {
            _readyButtonSprite = null;
            _readyButtonSpriteSearched = false;
        }

        // Returns the ReadyButton sprite if available, else our own rounded sprite.
        public static Sprite GetButtonSprite()
        {
            return TryGetReadyButtonSprite() ?? MakeRoundedSprite();
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

        // High-quality 64px rounded rect sprite with anti-aliased edges.
        // Border = 12px — works well with Image.Type.Sliced at any size.
        public static Sprite MakeRoundedSprite()
        {
            const int size   = 64;
            const float r    = 12f;  // corner radius in pixels
            const float aa   = 1.2f; // anti-alias width in pixels

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    // Distance from each corner's rounded region
                    float cx = Mathf.Min(px, size - 1 - px);
                    float cy = Mathf.Min(py, size - 1 - py);

                    float alpha;
                    if (cx >= r || cy >= r)
                    {
                        // Straight edge — fully opaque
                        alpha = 1f;
                    }
                    else
                    {
                        // Corner — distance from the corner arc centre
                        float dx = r - cx;
                        float dy = r - cy;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = 1f - Mathf.Clamp01((dist - r + aa) / aa);
                    }

                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size / 2f, 1,
                SpriteMeshType.FullRect,
                new Vector4(r + 2, r + 2, r + 2, r + 2));
        }

        // Thinner rounded sprite for small UI chips / badges (8px corner radius).
        public static Sprite MakeRoundedSpriteSmall()
        {
            const int size   = 32;
            const float r    = 6f;
            const float aa   = 1.0f;

            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            for (int py = 0; py < size; py++)
            {
                for (int px = 0; px < size; px++)
                {
                    float cx = Mathf.Min(px, size - 1 - px);
                    float cy = Mathf.Min(py, size - 1 - py);

                    float alpha;
                    if (cx >= r || cy >= r)
                    {
                        alpha = 1f;
                    }
                    else
                    {
                        float dx = r - cx;
                        float dy = r - cy;
                        float dist = Mathf.Sqrt(dx * dx + dy * dy);
                        alpha = 1f - Mathf.Clamp01((dist - r + aa) / aa);
                    }

                    tex.SetPixel(px, py, new Color(1f, 1f, 1f, alpha));
                }
            }

            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size),
                new Vector2(0.5f, 0.5f), size / 2f, 1,
                SpriteMeshType.FullRect,
                new Vector4(r + 1, r + 1, r + 1, r + 1));
        }

        // ── Button styling ────────────────────────────────────────────────
        public static void StyleButton(Button btn, Color baseColor)
        {
            var c = btn.colors;
            c.normalColor      = baseColor;
            c.highlightedColor = new Color(
                Mathf.Min(baseColor.r + 0.15f, 1f),
                Mathf.Min(baseColor.g + 0.15f, 1f),
                Mathf.Min(baseColor.b + 0.18f, 1f),
                baseColor.a);
            c.pressedColor     = _orange;
            c.selectedColor    = _orange;
            c.disabledColor    = new Color(0.22f, 0.25f, 0.32f, 0.5f);
            c.colorMultiplier  = 1f;
            c.fadeDuration     = 0.08f;
            btn.colors = c;
            btn.transition = Selectable.Transition.ColorTint;

            if (btn.GetComponent<Image>() is Image img)
            {
                img.color  = baseColor;
                img.sprite = GetButtonSprite();
                img.type   = Image.Type.Sliced;
            }
        }

        // Sets font, size, bold+uppercase style and a muted color — for section labels.
        public static void StyleTextAllCaps(TextMeshProUGUI tmp, float size)
        {
            var font = GetGameFont();
            if (font != null) tmp.font = font;
            tmp.fontSize  = size;
            tmp.color     = TextMuted;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Left;
        }

        public static void StyleButtonBlue(Button btn)
        {
            StyleButton(btn, _blue);
        }

        public static void AddPressColorSwap(Button btn)
        {
            var swapper = btn.gameObject.AddComponent<PressColorSwapper>();
            swapper.NormalColor  = btn.GetComponent<Image>()?.color ?? _blue;
            swapper.PressedColor = _orange;
        }

        // ── Text styling ──────────────────────────────────────────────────
        public static void StyleText(TextMeshProUGUI tmp, float fontSize = 18f, bool bold = false)
        {
            var font = GetGameFont();
            if (font != null) tmp.font = font;
            tmp.fontSize  = fontSize;
            tmp.color     = TextPrimary;
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

        private static T? GetStaticField<T>(System.Type type, string? name) where T : class
        {
            if (name == null) return null;
            var f = type.GetField(name, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            return f?.GetValue(null) as T;
        }
    }
}

using BoplMapEditor.Core;
using BoplMapEditor.Data;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Native map editor screen — lives inside the game's own Canvas.
    // Layout:
    //   Top bar (60px)     — Back | MapName | Save | Undo | Redo | Grid
    //   Scene viewport     — sky gradient + water strip (SceneBounds proportions)
    //   Bottom palette     — horizontal scroll strip with every island found in game
    public class NativeMapEditorScreen : MonoBehaviour
    {
        // SceneBounds: YMin=-26, YMax=40, waterHeight=-11.3
        // water % from bottom = (−11.3 − (−26)) / 66 ≈ 0.223
        const float WATER_FRAC = 0.223f;
        const float TOP_H      = 60f;
        const float PALETTE_H  = 120f;
        const float ITEM_W     = 96f;
        const float ITEM_H     = 96f;

        static readonly Color SkyTop    = new Color(0.38f, 0.62f, 0.90f, 1f);
        static readonly Color SkyMid    = new Color(0.55f, 0.78f, 0.97f, 1f);
        static readonly Color WaterDeep = new Color(0.05f, 0.22f, 0.58f, 1f);
        static readonly Color WaterSurf = new Color(0.14f, 0.45f, 0.82f, 1f);
        static readonly Color TopBarBg  = new Color(0.10f, 0.15f, 0.26f, 0.97f);
        static readonly Color PalBg     = new Color(0.07f, 0.11f, 0.20f, 0.97f);
        static readonly Color ItemNorm  = new Color(0.15f, 0.22f, 0.38f, 1f);
        static readonly Color ItemSel   = new Color(0.90f, 0.90f, 1.00f, 1f);
        static readonly Color White     = Color.white;

        // ── References ────────────────────────────────────────────────────
        MapEditorController _ctrl    = null!;
        MapBrowserScreen    _browser = null!;
        Color _blue, _darkBlue, _orange;

        // Top bar
        TextMeshProUGUI _mapNameLabel = null!;
        TextMeshProUGUI _gridLabel    = null!;

        // Palette
        RectTransform   _paletteContent = null!;
        int             _selectedSlot   = 0;
        readonly List<PaletteItem> _items = new List<PaletteItem>();

        struct PaletteItem
        {
            public int   PresetIndex;
            public int   MaterialType;
            public Image ThumbBg;
            public Image ThumbShape;
        }

        public void SetBrowser(MapBrowserScreen browser) => _browser = browser;

        // ── Factory ───────────────────────────────────────────────────────

        public static NativeMapEditorScreen Create(
            Transform canvasRoot,
            MapEditorController ctrl,
            MapBrowserScreen browser,
            Color blue, Color darkBlue, Color orange)
        {
            var go = new GameObject("NativeMapEditorScreen");
            go.transform.SetParent(canvasRoot, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var s        = go.AddComponent<NativeMapEditorScreen>();
            s._ctrl      = ctrl;
            s._browser   = browser;
            s._blue      = blue;
            s._darkBlue  = darkBlue;
            s._orange    = orange;
            s.BuildUI(rt);
            go.SetActive(false);
            return s;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void Open(MapData map)
        {
            _ctrl.Open(map);
            if (_mapNameLabel != null)
                _mapNameLabel.text = map.Name.ToUpper();
            StyleHelper.ScanPlatformAssets();
            RefreshPalette();
            gameObject.SetActive(true);
        }

        public void Close()
        {
            _ctrl.Close();
            gameObject.SetActive(false);
            _browser.gameObject.SetActive(true);
            _browser.Refresh();
        }

        // ── Build UI ──────────────────────────────────────────────────────

        void BuildUI(RectTransform root)
        {
            BuildTopBar(root);
            BuildViewport(root);
            BuildPalette(root);
        }

        // ── Top bar ───────────────────────────────────────────────────────

        void BuildTopBar(RectTransform root)
        {
            var go = new GameObject("TopBar");
            go.transform.SetParent(root, false);
            go.AddComponent<Image>().color = TopBarBg;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f); rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, -TOP_H); rt.offsetMax = Vector2.zero;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding               = new RectOffset(12, 12, 0, 0);
            hlg.spacing               = 8;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;

            var backBtn = TopBtn(go.transform, "< Back", new Color(0.60f, 0.15f, 0.15f, 1f), 80f);
            backBtn.onClick.AddListener(Close);

            Div(go.transform);

            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            _mapNameLabel = nameGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(_mapNameLabel, 20f, true);
            _mapNameLabel.text      = "Untitled";
            _mapNameLabel.alignment = TextAlignmentOptions.Left;
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            Div(go.transform);

            var save = TopBtn(go.transform, "Save", _orange, 70f);
            save.onClick.AddListener(() => {
                MapSerializer.SaveMap(_ctrl.CurrentMap, _ctrl.CurrentMap.Name);
            });

            var undo = TopBtn(go.transform, "Undo", _blue, 65f);
            undo.onClick.AddListener(() => _ctrl.History.Undo());

            var redo = TopBtn(go.transform, "Redo", _blue, 65f);
            redo.onClick.AddListener(() => _ctrl.History.Redo());

            var grid = TopBtn(go.transform, "Grid ON", new Color(0.20f, 0.42f, 0.22f, 1f), 84f);
            _gridLabel = grid.GetComponentInChildren<TextMeshProUGUI>();
            grid.onClick.AddListener(() => {
                _ctrl.SnapToGrid = !_ctrl.SnapToGrid;
                if (_gridLabel != null)
                    _gridLabel.text = _ctrl.SnapToGrid ? "Grid ON" : "Grid OFF";
            });
        }

        // ── Viewport ──────────────────────────────────────────────────────

        void BuildViewport(RectTransform root)
        {
            var go = new GameObject("Viewport");
            go.transform.SetParent(root, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, PALETTE_H);
            rt.offsetMax = new Vector2(0f, -TOP_H);

            go.AddComponent<Image>().color = Color.clear;
            go.AddComponent<Mask>().showMaskGraphic = false;

            // Sky
            Add(go.transform, "Sky", SkyMid,
                new Vector2(0f, WATER_FRAC), Vector2.one);
            // Sky top tint
            var skytop = Add(go.transform, "SkyTop",
                new Color(SkyTop.r, SkyTop.g, SkyTop.b, 0.5f),
                new Vector2(0f, WATER_FRAC + 0.38f), Vector2.one);

            // Water body
            Add(go.transform, "Water", WaterDeep,
                Vector2.zero, new Vector2(1f, WATER_FRAC));
            // Water surface shimmer line
            Add(go.transform, "WaterSurf", WaterSurf,
                new Vector2(0f, WATER_FRAC - 0.016f),
                new Vector2(1f, WATER_FRAC + 0.010f));

            // Animated wave strips
            go.AddComponent<WaterShimmer>().Init(WATER_FRAC);

            // Placeholder hint
            var hintGo = new GameObject("Hint");
            hintGo.transform.SetParent(go.transform, false);
            var hintTmp = hintGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(hintTmp, 15f, false);
            hintTmp.text      = "Drag islands here to build your map";
            hintTmp.color     = new Color(1f, 1f, 1f, 0.18f);
            hintTmp.alignment = TextAlignmentOptions.Center;
            hintTmp.raycastTarget = false;
            var hrt = hintGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0.15f, WATER_FRAC + 0.08f);
            hrt.anchorMax = new Vector2(0.85f, WATER_FRAC + 0.22f);
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;
        }

        // ── Palette (horizontal scroll) ───────────────────────────────────

        void BuildPalette(RectTransform root)
        {
            // Container
            var palGo = new GameObject("Palette");
            palGo.transform.SetParent(root, false);
            palGo.AddComponent<Image>().color = PalBg;
            var palRt = palGo.GetComponent<RectTransform>();
            palRt.anchorMin = Vector2.zero; palRt.anchorMax = new Vector2(1f, 0f);
            palRt.offsetMin = Vector2.zero; palRt.offsetMax = new Vector2(0f, PALETTE_H);

            // Separator line on top
            var sep = new GameObject("Sep");
            sep.transform.SetParent(palGo.transform, false);
            sep.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.35f);
            var srt = sep.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f); srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(0f, -1f); srt.offsetMax = Vector2.zero;

            // ScrollRect
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(palGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero; scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0f, 2f); scrollRt.offsetMax = new Vector2(0f, -1f);

            scrollGo.AddComponent<Image>().color = Color.clear;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.vertical   = false;
            scroll.horizontal = true;
            scroll.scrollSensitivity = 30f;
            scroll.movementType = ScrollRect.MovementType.Elastic;

            // Viewport
            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 0f);
            contentRt.anchorMax = new Vector2(0f, 1f);
            contentRt.pivot     = new Vector2(0f, 0.5f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;

            var hlg = contentGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding               = new RectOffset(8, 8, 4, 4);
            hlg.spacing               = 6;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            contentGo.AddComponent<ContentSizeFitter>().horizontalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRt;
            _paletteContent = contentRt;
        }

        // Populates items — called on Open() after ScanPlatformAssets()
        void RefreshPalette()
        {
            foreach (Transform c in _paletteContent) Destroy(c.gameObject);
            _items.Clear();
            _selectedSlot = 0;

            // Build one item per (preset × material type)
            // Group by material type first so same-material islands are together
            string[] presetNames = { "WIDE", "MED", "SMALL", "ROUND", "NORM", "LONG" };

            int slot = 0;
            for (int mat = 0; mat < 6; mat++)
            {
                for (int pi = 0; pi < MapEditorController.IslandPresets.Length; pi++)
                {
                    int captureSlot = slot;
                    int captureMat  = mat;
                    int capturePI   = pi;

                    var preset = MapEditorController.IslandPresets[pi];
                    var item   = BuildPaletteItem(_paletteContent, preset, presetNames[pi], mat,
                                                  captureSlot == _selectedSlot);
                    item.ThumbBg.GetComponent<Button>().onClick.AddListener(
                        () => SelectSlot(captureSlot, capturePI, captureMat));

                    _items.Add(item);
                    slot++;
                }
            }

            ApplySlotHighlight();
        }

        PaletteItem BuildPaletteItem(RectTransform parent, IslandPreset preset,
                                     string presetName, int matType, bool selected)
        {
            var go = new GameObject($"Island_{matType}_{presetName}");
            go.transform.SetParent(parent, false);

            // Background card
            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = StyleHelper.MakeRoundedSprite();
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = selected ? ItemSel : ItemNorm;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth  = ITEM_W;
            le.minHeight = ITEM_H;
            le.flexibleHeight = 1;

            go.AddComponent<Button>(); // click handled by RefreshPalette

            // Island shape thumbnail
            // Scale: normalize so longest dimension fills ~70% of card width
            float maxDim  = Mathf.Max(preset.hw * 2f, preset.hh * 2f);
            float scale   = (ITEM_W * 0.70f) / maxDim;
            float thumbW  = Mathf.Clamp(preset.hw * 2f * scale, 6f, ITEM_W - 8f);
            float thumbH  = Mathf.Clamp(preset.hh * 2f * scale, 4f, ITEM_H - 22f);

            var shapeGo = new GameObject("Shape");
            shapeGo.transform.SetParent(go.transform, false);
            var shapeImg = shapeGo.AddComponent<Image>();
            shapeImg.raycastTarget = false;

            // Use real game sprite if available, otherwise solid color
            var gameSprite = StyleHelper.GetPlatformSprite(matType);
            if (gameSprite != null)
            {
                shapeImg.sprite = gameSprite;
                shapeImg.type   = Image.Type.Sliced;
                shapeImg.color  = Color.white; // let the sprite show naturally
            }
            else
            {
                shapeImg.sprite = StyleHelper.MakeRoundedSprite();
                shapeImg.type   = Image.Type.Sliced;
                shapeImg.color  = StyleHelper.PlatformColors[matType];
            }

            var srt = shapeGo.GetComponent<RectTransform>();
            srt.anchorMin        = new Vector2(0.5f, 0.5f);
            srt.anchorMax        = new Vector2(0.5f, 0.5f);
            srt.pivot            = new Vector2(0.5f, 0.5f);
            srt.sizeDelta        = new Vector2(thumbW, thumbH);
            srt.anchoredPosition = new Vector2(0f, 6f);

            // Material color dot (small circle in top-right corner)
            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(go.transform, false);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color        = StyleHelper.PlatformColors[matType];
            dotImg.sprite       = StyleHelper.MakeRoundedSpriteSmall();
            dotImg.type         = Image.Type.Sliced;
            dotImg.raycastTarget = false;
            var drt = dotGo.GetComponent<RectTransform>();
            drt.anchorMin = new Vector2(1f, 1f); drt.anchorMax = new Vector2(1f, 1f);
            drt.pivot     = new Vector2(1f, 1f);
            drt.sizeDelta = new Vector2(12f, 12f);
            drt.anchoredPosition = new Vector2(-4f, -4f);

            // Label at bottom: preset name
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(lbl, 9f, true);
            lbl.text      = presetName;
            lbl.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = selected ? new Color(0.1f, 0.1f, 0.2f, 1f) : White;
            lbl.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f); lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot     = new Vector2(0.5f, 0f);
            lrt.offsetMin = new Vector2(0f, 4f); lrt.offsetMax = new Vector2(0f, 16f);

            return new PaletteItem
            {
                PresetIndex  = (int)(System.Array.IndexOf(MapEditorController.IslandPresets,
                               preset)),
                MaterialType = matType,
                ThumbBg      = bgImg,
                ThumbShape   = shapeImg,
            };
        }

        void SelectSlot(int slot, int presetIdx, int matType)
        {
            _selectedSlot            = slot;
            _ctrl.SelectedPreset     = presetIdx;
            _ctrl.PlacePlatformType  = matType;
            ApplySlotHighlight();
        }

        void ApplySlotHighlight()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                bool sel = i == _selectedSlot;
                var item = _items[i];
                item.ThumbBg.color = sel ? ItemSel : ItemNorm;

                // Update label color
                var lbl = item.ThumbBg.transform.Find("Label")
                               ?.GetComponent<TextMeshProUGUI>();
                if (lbl != null)
                    lbl.color = sel ? new Color(0.1f, 0.1f, 0.2f, 1f) : White;

                // If using fallback color (no real sprite), re-tint shape
                var shape = item.ThumbShape;
                if (shape != null && shape.sprite == null)
                    shape.color = StyleHelper.PlatformColors[item.MaterialType];
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        Button TopBtn(Transform parent, string text, Color color, float minW)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var c = btn.colors;
            c.normalColor = color;
            c.highlightedColor = new Color(
                Mathf.Min(color.r+0.12f,1f),
                Mathf.Min(color.g+0.12f,1f),
                Mathf.Min(color.b+0.12f,1f),1f);
            c.pressedColor = _darkBlue;
            c.fadeDuration = 0.07f;
            btn.colors = c;

            go.AddComponent<LayoutElement>().minWidth = minW;
            go.GetComponent<LayoutElement>().flexibleHeight = 1;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(tmp, 13f, true);
            tmp.text = text;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6,0); lrt.offsetMax = new Vector2(-6,0);
            return btn;
        }

        void Div(Transform parent)
        {
            var go = new GameObject("Div");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(1f,1f,1f,0.07f);
            go.AddComponent<LayoutElement>().minWidth = 1f;
        }

        // Full-rect panel helper
        static Image Add(Transform parent, string name, Color color,
                         Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return img;
        }

        static void ApplyFont(TextMeshProUGUI tmp, float size, bool bold)
        {
            try
            {
                var font = LocalizedText.localizationTable
                    ?.GetFont(Settings.Get().Language, useFontWithStroke: false);
                if (font != null) tmp.font = font;
            }
            catch { }
            tmp.fontSize  = size;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color     = White;
            tmp.enableWordWrapping = false;
        }
    }

    // ── Animated water shimmer ────────────────────────────────────────────

    public class WaterShimmer : MonoBehaviour
    {
        float   _wf;
        Image[] _stripes = new Image[5];
        float   _t;

        public void Init(float waterFrac)
        {
            _wf = waterFrac;
            for (int i = 0; i < _stripes.Length; i++)
            {
                var go = new GameObject("S" + i);
                go.transform.SetParent(transform, false);
                var img = go.AddComponent<Image>();
                img.color = new Color(0.50f, 0.76f, 0.97f, 0f);
                img.raycastTarget = false;
                _stripes[i] = img;
            }
        }

        void Update()
        {
            _t += Time.deltaTime;
            for (int i = 0; i < _stripes.Length; i++)
            {
                float phase = _t * 0.55f + i * 0.65f;
                float y     = _wf - 0.004f + Mathf.Sin(phase * 1.2f) * 0.009f;
                float alpha = 0.08f + Mathf.Sin(phase * 2.0f + i) * 0.05f;

                var rt = _stripes[i].GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, y - 0.003f);
                rt.anchorMax = new Vector2(1f, y + 0.003f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                var c = _stripes[i].color;
                c.a = Mathf.Clamp(alpha, 0f, 1f);
                _stripes[i].color = c;
            }
        }
    }
}

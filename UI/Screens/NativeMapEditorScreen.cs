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
    //   Top bar (72px)    — Back | MapName | Save | Undo | Redo | Grid
    //   Scene viewport    — sky gradient + clouds + water strip (SceneBounds proportions)
    //   Bottom palette    — horizontal scroll strip (130px) with island cards
    public class NativeMapEditorScreen : MonoBehaviour
    {
        // SceneBounds: YMin=-26, YMax=40, waterHeight=-11.3
        // water % from bottom = (−11.3 − (−26)) / 66 ≈ 0.223
        const float WATER_FRAC = 0.223f;
        const float TOP_H      = 120f;
        const float PALETTE_H  = 160f;
        const float ITEM_W     = 130f;

        static readonly Color SkyBottom  = new Color(0.42f, 0.68f, 0.95f, 1f);
        static readonly Color SkyTop     = new Color(0.22f, 0.45f, 0.78f, 1f);
        static readonly Color WaterDeep  = new Color(0.04f, 0.18f, 0.52f, 1f);
        static readonly Color WaterSurf  = new Color(0.12f, 0.38f, 0.72f, 1f);
        static readonly Color TopBarBg   = new Color(0.08f, 0.12f, 0.22f, 1f);
        static readonly Color PalBg      = new Color(0.06f, 0.09f, 0.18f, 1f);
        static readonly Color ItemNorm   = new Color(0.13f, 0.20f, 0.35f, 1f);
        static readonly Color ItemSel    = new Color(0.88f, 0.88f, 1.00f, 1f);
        static readonly Color OrangeAcc  = new Color(1.00f, 0.55f, 0.10f, 1f);
        static readonly Color White      = Color.white;

        // ── References ────────────────────────────────────────────────────
        MapEditorController _ctrl    = null!;
        MapBrowserScreen    _browser = null!;
        Color _blue, _darkBlue, _orange;
        bool _inDedicatedScene;

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

        // Injected into CharacterSelect canvas (browser → editor flow)
        public static NativeMapEditorScreen Create(
            Transform canvasRoot,
            MapEditorController ctrl,
            MapBrowserScreen browser,
            Color blue, Color darkBlue, Color orange)
        {
            var s = Spawn(canvasRoot, ctrl, blue, darkBlue, orange);
            s._browser = browser;
            s.gameObject.SetActive(false);
            return s;
        }

        // Spawned inside the dedicated Level1 editor scene
        public static NativeMapEditorScreen CreateInScene(
            Transform canvasRoot,
            MapEditorController ctrl,
            Color blue, Color darkBlue, Color orange)
        {
            var s = Spawn(canvasRoot, ctrl, blue, darkBlue, orange);
            s._inDedicatedScene = true;
            s.gameObject.SetActive(true);
            return s;
        }

        static NativeMapEditorScreen Spawn(
            Transform parent, MapEditorController ctrl,
            Color blue, Color darkBlue, Color orange)
        {
            var go = new GameObject("NativeMapEditorScreen");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var rootBg = go.AddComponent<Image>();
            rootBg.color = Color.clear; rootBg.raycastTarget = true;

            var s = go.AddComponent<NativeMapEditorScreen>();
            s._ctrl = ctrl; s._blue = blue; s._darkBlue = darkBlue; s._orange = orange;
            s.BuildUI(rt);
            return s;
        }

        // ── Public API ────────────────────────────────────────────────────

        // Called from browser screen (CharacterSelect canvas flow)
        public void Open(MapData map)
        {
            // Use dedicated scene instead of injected canvas approach
            EditorSceneManager.Open(map);
        }

        // Called from EditorBootstrap after dedicated scene loaded
        public void OpenWithMap(MapData map)
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
            if (_inDedicatedScene)
                EditorSceneManager.Close(); // loads CharacterSelect
            else
            {
                gameObject.SetActive(false);
                if (_browser != null)
                {
                    _browser.gameObject.SetActive(true);
                    _browser.Refresh();
                }
            }
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
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, -TOP_H);
            rt.offsetMax = Vector2.zero;

            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.padding               = new RectOffset(10, 10, 10, 10);
            hlg.spacing               = 6;
            hlg.childAlignment        = TextAnchor.MiddleLeft;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;

            // ← BACK
            var backBtn = TopBtn(go.transform, "\u2190 BACK", new Color(0.70f, 0.15f, 0.15f, 1f), 100f, 52f);
            backBtn.onClick.AddListener(Close);

            // Map name label (fills remaining space)
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(go.transform, false);
            _mapNameLabel = nameGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(_mapNameLabel, 36f, true);
            _mapNameLabel.text      = "UNTITLED";
            _mapNameLabel.alignment = TextAlignmentOptions.Left;
            var nameLe = nameGo.AddComponent<LayoutElement>();
            nameLe.flexibleWidth = 1;

            // SAVE
            var save = TopBtn(go.transform, "SAVE", OrangeAcc, 90f, 52f);
            save.onClick.AddListener(() =>
            {
                MapSerializer.SaveMap(_ctrl.CurrentMap, _ctrl.CurrentMap.Name);
            });

            // UNDO / REDO
            var undo = TopBtn(go.transform, "UNDO", _blue, 80f, 52f);
            undo.onClick.AddListener(() => _ctrl.History.Undo());

            var redo = TopBtn(go.transform, "REDO", _blue, 80f, 52f);
            redo.onClick.AddListener(() => _ctrl.History.Redo());

            // GRID ON/OFF
            var grid = TopBtn(go.transform, "GRID ON", new Color(0.18f, 0.45f, 0.20f, 1f), 100f, 52f);
            _gridLabel = grid.GetComponentInChildren<TextMeshProUGUI>();
            grid.onClick.AddListener(() =>
            {
                _ctrl.SnapToGrid = !_ctrl.SnapToGrid;
                if (_gridLabel != null)
                    _gridLabel.text = _ctrl.SnapToGrid ? "GRID ON" : "GRID OFF";
            });

            // 2px orange accent line along the bottom edge of the top bar
            var accent = new GameObject("BottomAccent");
            accent.transform.SetParent(go.transform, false);
            var accentImg = accent.AddComponent<Image>();
            accentImg.color        = OrangeAcc;
            accentImg.raycastTarget = false;
            var art = accent.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f);
            art.anchorMax = new Vector2(1f, 0f);
            art.pivot     = new Vector2(0.5f, 0f);
            art.offsetMin = new Vector2(0f, 0f);
            art.offsetMax = new Vector2(0f, 2f);
        }

        // ── Viewport ──────────────────────────────────────────────────────

        void BuildViewport(RectTransform root)
        {
            var go = new GameObject("Viewport");
            go.transform.SetParent(root, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(0f, PALETTE_H);
            rt.offsetMax = new Vector2(0f, -TOP_H);

            // Transparent — Level1 cameras (depth=1) render below this
            // ScreenSpaceOverlay canvas and show through transparent pixels
            var img = go.AddComponent<Image>();
            img.color        = Color.clear;
            img.raycastTarget = false;
        }

        // ── Palette (horizontal scroll) ───────────────────────────────────

        void BuildPalette(RectTransform root)
        {
            var palGo = new GameObject("Palette");
            palGo.transform.SetParent(root, false);
            palGo.AddComponent<Image>().color = PalBg;
            var palRt = palGo.GetComponent<RectTransform>();
            palRt.anchorMin = Vector2.zero;
            palRt.anchorMax = new Vector2(1f, 0f);
            palRt.offsetMin = Vector2.zero;
            palRt.offsetMax = new Vector2(0f, PALETTE_H);

            // 2px orange accent line at top of palette
            var topAccent = new GameObject("TopAccent");
            topAccent.transform.SetParent(palGo.transform, false);
            var taImg = topAccent.AddComponent<Image>();
            taImg.color        = OrangeAcc;
            taImg.raycastTarget = false;
            var tart = topAccent.GetComponent<RectTransform>();
            tart.anchorMin = new Vector2(0f, 1f);
            tart.anchorMax = Vector2.one;
            tart.offsetMin = new Vector2(0f, -2f);
            tart.offsetMax = Vector2.zero;

            // "ISLANDS" label strip (20px tall, left-aligned, muted white)
            var labelStrip = new GameObject("LabelStrip");
            labelStrip.transform.SetParent(palGo.transform, false);
            var labelStripRt = labelStrip.AddComponent<RectTransform>();
            labelStripRt.anchorMin = new Vector2(0f, 1f);
            labelStripRt.anchorMax = Vector2.one;
            labelStripRt.offsetMin = new Vector2(0f, -22f);
            labelStripRt.offsetMax = Vector2.zero;

            var islandsLbl = labelStrip.AddComponent<TextMeshProUGUI>();
            ApplyFont(islandsLbl, 11f, false);
            islandsLbl.text      = "ISLANDS";
            islandsLbl.color     = new Color(0.5f, 0.6f, 0.7f, 1f);
            islandsLbl.alignment = TextAlignmentOptions.MidlineLeft;
            var islandsLblRt = islandsLbl.GetComponent<RectTransform>();
            islandsLblRt.anchorMin = Vector2.zero;
            islandsLblRt.anchorMax = Vector2.one;
            islandsLblRt.offsetMin = new Vector2(12f, 0f);
            islandsLblRt.offsetMax = Vector2.zero;

            // ScrollRect (fills remaining height below label strip)
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(palGo.transform, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = new Vector2(1f, 1f);
            scrollRt.offsetMin = new Vector2(0f, 2f);
            scrollRt.offsetMax = new Vector2(0f, -22f);

            scrollGo.AddComponent<Image>().color = Color.clear;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.vertical          = false;
            scroll.horizontal        = true;
            scroll.scrollSensitivity = 30f;
            scroll.movementType      = ScrollRect.MovementType.Elastic;

            // Viewport
            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.white; // must be >0 for Mask stencil
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

            scroll.content  = contentRt;
            _paletteContent = contentRt;
        }

        void RefreshPalette()
        {
            foreach (Transform c in _paletteContent) Destroy(c.gameObject);
            _items.Clear();
            _selectedSlot = 0;

            var scanned = StyleHelper.ScannedPlatforms;

            // Show only the real islands from the loaded scene — no tinting/fallbacks
            // Grass map → Level1 sprites, Snow map → Level22 sprites, Space → Level35
            for (int i = 0; i < scanned.Count; i++)
            {
                int idx   = i;
                var entry = scanned[i];
                var item  = BuildIslandCard(_paletteContent, entry, i == 0);
                item.ThumbBg.GetComponent<Button>().onClick.AddListener(() =>
                {
                    _selectedSlot          = idx;
                    _ctrl.PlacePlatformType = entry.MaterialType;
                    ApplySlotHighlight();
                });
                _items.Add(item);
            }

            ApplySlotHighlight();
        }

        PaletteItem BuildIslandCard(RectTransform parent, StyleHelper.PlatformEntry entry, bool selected)
        {
            // Fixed card size — sprite fits inside preserving proportions
            const float cardW  = 120f;
            const float thumbW = 96f;
            const float thumbH = 56f;
            float hw    = Mathf.Max(entry.HalfW, 0.1f);
            float hh    = Mathf.Max(entry.HalfH, 0.1f);
            float scale = Mathf.Min(thumbW / (hw * 2f), thumbH / (hh * 2f));
            float isoW  = Mathf.Clamp(hw * 2f * scale, 8f, thumbW);
            float isoH  = Mathf.Clamp(hh * 2f * scale, 4f, thumbH);

            var go = new GameObject("Island");
            go.transform.SetParent(parent, false);

            // Transparent card background — island floats in the palette
            var bgImg = go.AddComponent<Image>();
            bgImg.color = selected
                ? new Color(1f, 1f, 1f, 0.18f)
                : new Color(1f, 1f, 1f, 0.06f);
            bgImg.sprite = StyleHelper.MakeRoundedSprite();
            bgImg.type   = Image.Type.Sliced;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth       = cardW;
            le.flexibleHeight = 1;
            go.AddComponent<Button>();

            // Island sprite — exact proportions, centered in card
            var islandGo = new GameObject("Island");
            islandGo.transform.SetParent(go.transform, false);
            var islandImg = islandGo.AddComponent<Image>();
            islandImg.raycastTarget = false;

            if (entry.Sprite != null)
            {
                islandImg.sprite = entry.Sprite;
                islandImg.type   = Image.Type.Sliced;
                islandImg.color  = Color.white; // show real sprite naturally
            }
            else
            {
                islandImg.sprite = StyleHelper.MakeRoundedSprite();
                islandImg.type   = Image.Type.Sliced;
                islandImg.color  = entry.Color;
            }

            var irt = islandGo.GetComponent<RectTransform>();
            irt.anchorMin        = new Vector2(0.5f, 0.5f);
            irt.anchorMax        = new Vector2(0.5f, 0.5f);
            irt.pivot            = new Vector2(0.5f, 0.5f);
            irt.sizeDelta        = new Vector2(isoW, isoH);
            irt.anchoredPosition = new Vector2(0f, 6f);

            return new PaletteItem { MaterialType = entry.MaterialType, ThumbBg = bgImg, ThumbShape = islandImg };
        }

        PaletteItem BuildSpriteItem(RectTransform parent, Sprite? sprite, Color color,
                                    string label, bool selected)
        {
            var go = new GameObject("PaletteItem_" + label);
            go.transform.SetParent(parent, false);

            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = StyleHelper.MakeRoundedSprite();
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = selected ? ItemSel : ItemNorm;

            go.AddComponent<LayoutElement>().minWidth  = ITEM_W;
            go.GetComponent<LayoutElement>().flexibleHeight = 1;
            go.AddComponent<Button>();

            // Sprite preview — fills most of the card
            var shapeGo = new GameObject("Sprite");
            shapeGo.transform.SetParent(go.transform, false);
            var shapeImg = shapeGo.AddComponent<Image>();
            shapeImg.raycastTarget = false;
            if (sprite != null)
            {
                shapeImg.sprite = sprite;
                shapeImg.type   = Image.Type.Sliced;
                shapeImg.color  = Color.white;
            }
            else
            {
                shapeImg.sprite = StyleHelper.MakeRoundedSprite();
                shapeImg.type   = Image.Type.Sliced;
                shapeImg.color  = color;
            }
            var srt = shapeGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0.1f, 0.25f);
            srt.anchorMax = new Vector2(0.9f, 0.85f);
            srt.offsetMin = srt.offsetMax = Vector2.zero;

            // Label at bottom
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(lbl, 13f, true);
            lbl.text      = label.ToUpper();
            lbl.alignment = TextAlignmentOptions.Center;
            lbl.color     = selected ? new Color(0.1f, 0.1f, 0.2f) : White;
            lbl.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0.25f);
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            return new PaletteItem { MaterialType = 0, ThumbBg = bgImg, ThumbShape = shapeImg };
        }

        PaletteItem BuildScannedItem(RectTransform parent, StyleHelper.PlatformEntry entry, bool selected)
        {
            var go = new GameObject("ScannedIsland");
            go.transform.SetParent(parent, false);

            var bgImg = go.AddComponent<Image>();
            bgImg.sprite = StyleHelper.MakeRoundedSprite();
            bgImg.type   = Image.Type.Sliced;
            bgImg.color  = selected ? ItemSel : ItemNorm;

            go.AddComponent<LayoutElement>().minWidth = ITEM_W;
            go.GetComponent<LayoutElement>().flexibleHeight = 1;
            go.AddComponent<Button>();

            // Actual platform sprite, scaled to fit card
            float maxW = 74f, maxH = 46f;
            float hw = Mathf.Max(entry.HalfW, 0.1f);
            float hh = Mathf.Max(entry.HalfH, 0.1f);
            float s  = Mathf.Min(maxW / (hw * 2f), maxH / (hh * 2f));
            float tw = hw * 2f * s;
            float th = hh * 2f * s;

            var shapeGo = new GameObject("Shape");
            shapeGo.transform.SetParent(go.transform, false);
            var shapeImg = shapeGo.AddComponent<Image>();
            shapeImg.raycastTarget = false;
            if (entry.Sprite != null)
            {
                shapeImg.sprite = entry.Sprite;
                shapeImg.type   = Image.Type.Sliced;
                shapeImg.color  = Color.white;
            }
            else
            {
                shapeImg.sprite = StyleHelper.MakeRoundedSprite();
                shapeImg.type   = Image.Type.Sliced;
                shapeImg.color  = entry.Color;
            }
            var srt = shapeGo.GetComponent<RectTransform>();
            srt.anchorMin = srt.anchorMax = srt.pivot = new Vector2(0.5f, 0.5f);
            srt.sizeDelta = new Vector2(Mathf.Max(tw, 8f), Mathf.Max(th, 4f));
            srt.anchoredPosition = new Vector2(0f, 8f);

            // Material color dot
            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(go.transform, false);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color        = StyleHelper.PlatformColors[Mathf.Clamp(entry.MaterialType, 0, 4)];
            dotImg.sprite       = StyleHelper.MakeRoundedSpriteSmall();
            dotImg.type         = Image.Type.Sliced;
            dotImg.raycastTarget = false;
            var drt = dotGo.GetComponent<RectTransform>();
            drt.anchorMin = drt.anchorMax = drt.pivot = new Vector2(1f, 1f);
            drt.sizeDelta = new Vector2(14f, 14f);
            drt.anchoredPosition = new Vector2(-4f, -4f);

            return new PaletteItem { MaterialType = entry.MaterialType, ThumbBg = bgImg, ThumbShape = shapeImg };
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
            le.minWidth       = ITEM_W;
            le.flexibleHeight = 1;

            go.AddComponent<Button>();

            // Island shape thumbnail
            // Scale so longest dimension fills proportionally (max 70px wide, 44px tall)
            float rawW = preset.hw * 2f;
            float rawH = preset.hh * 2f;
            float scaleW = 70f / rawW;
            float scaleH = 44f / rawH;
            float scale  = Mathf.Min(scaleW, scaleH);
            float thumbW = Mathf.Clamp(rawW * scale, 4f, 70f);
            float thumbH = Mathf.Clamp(rawH * scale, 4f, 44f);

            var shapeGo = new GameObject("Shape");
            shapeGo.transform.SetParent(go.transform, false);
            var shapeImg = shapeGo.AddComponent<Image>();
            shapeImg.raycastTarget = false;

            var gameSprite = StyleHelper.GetPlatformSprite(matType);
            if (gameSprite != null)
            {
                shapeImg.sprite = gameSprite;
                shapeImg.type   = Image.Type.Sliced;
                shapeImg.color  = Color.white;
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
            srt.anchoredPosition = new Vector2(0f, 8f);

            // Material color dot: 14px circle, top-right of card
            var dotGo = new GameObject("Dot");
            dotGo.transform.SetParent(go.transform, false);
            var dotImg = dotGo.AddComponent<Image>();
            dotImg.color         = StyleHelper.PlatformColors[matType];
            dotImg.sprite        = StyleHelper.MakeRoundedSpriteSmall();
            dotImg.type          = Image.Type.Sliced;
            dotImg.raycastTarget = false;
            var drt = dotGo.GetComponent<RectTransform>();
            drt.anchorMin        = new Vector2(1f, 1f);
            drt.anchorMax        = new Vector2(1f, 1f);
            drt.pivot            = new Vector2(1f, 1f);
            drt.sizeDelta        = new Vector2(14f, 14f);
            drt.anchoredPosition = new Vector2(-4f, -4f);

            // Preset name label: 12px bold, bottom of card, uppercase
            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var lbl = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(lbl, 12f, true);
            lbl.text          = presetName;
            lbl.fontStyle     = FontStyles.Bold | FontStyles.UpperCase;
            lbl.alignment     = TextAlignmentOptions.Center;
            lbl.color         = selected ? new Color(0.1f, 0.1f, 0.2f, 1f) : White;
            lbl.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = new Vector2(0f, 0f);
            lrt.anchorMax = new Vector2(1f, 0f);
            lrt.pivot     = new Vector2(0.5f, 0f);
            lrt.offsetMin = new Vector2(2f, 4f);
            lrt.offsetMax = new Vector2(-2f, 20f);

            return new PaletteItem
            {
                PresetIndex  = System.Array.IndexOf(MapEditorController.IslandPresets, preset),
                MaterialType = matType,
                ThumbBg      = bgImg,
                ThumbShape   = shapeImg,
            };
        }

        void SelectSlot(int slot, int presetIdx, int matType)
        {
            _selectedSlot           = slot;
            _ctrl.SelectedPreset    = presetIdx;
            _ctrl.PlacePlatformType = matType;
            ApplySlotHighlight();
        }

        void ApplySlotHighlight()
        {
            for (int i = 0; i < _items.Count; i++)
            {
                bool sel  = i == _selectedSlot;
                var  item = _items[i];
                item.ThumbBg.color = sel ? ItemSel : ItemNorm;

                var lbl = item.ThumbBg.transform.Find("Label")
                               ?.GetComponent<TextMeshProUGUI>();
                if (lbl != null)
                    lbl.color = sel ? new Color(0.1f, 0.1f, 0.2f, 1f) : White;

                var shape = item.ThumbShape;
                if (shape != null && shape.sprite == null)
                    shape.color = StyleHelper.PlatformColors[item.MaterialType];
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        Button TopBtn(Transform parent, string text, Color color, float minW, float minH)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);
            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var c   = btn.colors;
            c.normalColor = color;
            c.highlightedColor = new Color(
                Mathf.Min(color.r + 0.12f, 1f),
                Mathf.Min(color.g + 0.12f, 1f),
                Mathf.Min(color.b + 0.12f, 1f), 1f);
            c.pressedColor = _darkBlue;
            c.fadeDuration = 0.07f;
            btn.colors = c;

            var le        = go.AddComponent<LayoutElement>();
            le.minWidth   = minW;
            le.minHeight  = minH;
            le.preferredWidth  = minW;
            le.preferredHeight = minH;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyFont(tmp, 28f, true);
            tmp.text          = text;
            tmp.fontStyle     = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment     = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4f, 0f);
            lrt.offsetMax = new Vector2(-4f, 0f);
            return btn;
        }

        // Full-rect panel helper
        static Image Add(Transform parent, string name, Color color,
                         Vector2 anchorMin, Vector2 anchorMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color         = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
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
            tmp.fontSize           = size;
            tmp.fontStyle          = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color              = Color.white;
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
                img.color         = new Color(0.50f, 0.76f, 0.97f, 0f);
                img.raycastTarget = false;
                _stripes[i]       = img;
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

                var col = _stripes[i].color;
                col.a           = Mathf.Clamp(alpha, 0f, 1f);
                _stripes[i].color = col;
            }
        }
    }
}

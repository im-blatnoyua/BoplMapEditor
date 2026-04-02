using System.Collections.Generic;
using BoplMapEditor.Data;
using BoplMapEditor.Core;
using BoplMapEditor.Sync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Full-screen map editor window built with UGUI.
    // Super Mario Maker 2-inspired layout:
    //   - Top bar (52px): undo/redo | title | map name field | save | close
    //   - Left panel (88px): tool + block type + theme buttons
    //   - Canvas (center): solid dark background + grid + platform widgets
    //   - Right sidebar (200px): platform properties (absolute positioning)
    public class MapEditorWindow : MonoBehaviour
    {
        private MapEditorController     _ctrl        = null!;
        private EditorCanvasController  _canvasCtrl  = null!;
        private Canvas                  _canvas      = null!;
        private SlideAnimator           _slideAnimator = null!;

        // Sidebar fields
        private TMP_InputField      _mapNameField       = null!;
        private TextMeshProUGUI     _platformCountLabel = null!;
        private TextMeshProUGUI     _selectedInfoLabel  = null!;
        private TMP_InputField      _propX    = null!, _propY  = null!;
        private TMP_InputField      _propHW   = null!, _propHH = null!;
        private TMP_InputField      _propRadius = null!, _propRotation = null!;
        private readonly List<Button> _typeButtons   = new List<Button>();
        private readonly List<Button> _themeButtons  = new List<Button>();
        private readonly List<Button> _toolButtons   = new List<Button>();
        private readonly List<Button> _presetButtons = new List<Button>();

        // Undo/Redo/Snap toolbar buttons
        private Button _undoBtn = null!;
        private Button _redoBtn = null!;
        private Button _snapBtn = null!;

        // Viewport image — made transparent when a background scene is loaded
        private Image _viewportImg = null!;

        // Tab fields — kept for API compatibility, wired to dummy objects
        private GameObject       _platformsTab   = null!;
        private GameObject       _environmentTab = null!;
        private EnvironmentPanel _envPanel       = null!;
        private MovementPanel    _movPanel       = null!;
        private Button           _tabPlatforms   = null!;
        private Button           _tabEnvironment = null!;

        // In-editor load browser (modal panel)
        private GameObject    _browserPanel   = null!;
        private RectTransform _browserContent = null!;

        // Layout constants
        private const float TOP_BAR_H    = 60f;
        private const float LEFT_PANEL_W = 100f;
        private const float RIGHT_PANEL_W = 210f;

        // ── Factory ───────────────────────────────────────────────────────

        public static MapEditorWindow Create(MapEditorController ctrl)
        {
            var canvas = UIBuilder.CreateCanvas("MapEditorCanvas", sortOrder: 200);
            var window = canvas.gameObject.AddComponent<MapEditorWindow>();
            window._canvas = canvas;
            window._ctrl   = ctrl;
            window.BuildUI();

            window._slideAnimator = canvas.gameObject.AddComponent<SlideAnimator>();
            window._slideAnimator.Target     = canvas.GetComponent<RectTransform>();
            window._slideAnimator.OffscreenY = 900f;

            window.gameObject.SetActive(false);
            return window;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void Open(MapData? map = null)
        {
            StyleHelper.ScanPlatformMaterials();
            StyleHelper.LoadGameColors();

            _ctrl.Open(map);
            _mapNameField.text = _ctrl.CurrentMap.Name;
            _canvasCtrl.Refresh();
            RefreshSidebar();
            if (_envPanel != null) _envPanel.SetData(_ctrl.CurrentMap.Environment);
            gameObject.SetActive(true);
            _slideAnimator.AnimateIn();
        }

        public void Close()
        {
            Util.BackgroundSceneLoader.Unload();
            _slideAnimator.AnimateOut(() => {
                _ctrl.Close();
                gameObject.SetActive(false);
            });
        }

        // ── Build UI ──────────────────────────────────────────────────────

        private void BuildUI()
        {
            var root = _canvas.GetComponent<RectTransform>();

            // Solid dark background — game does NOT show through (SMM2 style: new page feel)
            var bg = UIBuilder.FlatPanel(root, "Background",
                new Color(0.08f, 0.11f, 0.18f, 1.0f),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── Top bar ───────────────────────────────────────────────────
            var topBar = UIBuilder.FlatPanel(bg, "TopBar",
                new Color(0.07f, 0.10f, 0.20f, 1.0f),
                new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -TOP_BAR_H), Vector2.zero);
            BuildTopBar(topBar);

            // Thin bottom-border on top bar
            {
                var border = new GameObject("TopBarBorder");
                border.transform.SetParent(bg, false);
                var img = border.AddComponent<Image>();
                img.color = StyleHelper.DarkBorder;
                var rt = border.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(0f, -TOP_BAR_H - 1f);
                rt.offsetMax = new Vector2(0f, -TOP_BAR_H);
            }

            // ── Main area (below top bar) ──────────────────────────────────
            var main = new GameObject("Main").AddComponent<RectTransform>();
            main.SetParent(bg, false);
            main.anchorMin = Vector2.zero;
            main.anchorMax = Vector2.one;
            main.offsetMin = new Vector2(0f, 0f);
            main.offsetMax = new Vector2(0f, -TOP_BAR_H - 1f);

            // ── Left panel (88px wide, dark navy) ──────────────────────────
            var leftPanelGo = new GameObject("LeftPanel");
            leftPanelGo.transform.SetParent(main, false);
            var leftPanelImg = leftPanelGo.AddComponent<Image>();
            leftPanelImg.color = new Color(0.07f, 0.10f, 0.20f, 1.0f);
            var leftPanelRt = leftPanelGo.GetComponent<RectTransform>();
            leftPanelRt.anchorMin = new Vector2(0f, 0f);
            leftPanelRt.anchorMax = new Vector2(0f, 1f);
            leftPanelRt.offsetMin = new Vector2(0f, 0f);
            leftPanelRt.offsetMax = new Vector2(LEFT_PANEL_W, 0f);
            BuildLeftPanel(leftPanelRt);

            // Thin right-border on left panel
            {
                var border = new GameObject("LeftPanelBorder");
                border.transform.SetParent(main, false);
                var img = border.AddComponent<Image>();
                img.color = StyleHelper.DarkBorder;
                var rt = border.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 0f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.offsetMin = new Vector2(LEFT_PANEL_W, 0f);
                rt.offsetMax = new Vector2(LEFT_PANEL_W + 1f, 0f);
            }

            // ── Right sidebar (200px wide, dark bg) ────────────────────────
            var sidebarGo = new GameObject("Sidebar");
            sidebarGo.transform.SetParent(main, false);
            var sidebarImg = sidebarGo.AddComponent<Image>();
            sidebarImg.color = new Color(0.06f, 0.09f, 0.16f, 0.97f);
            var sidebarRt = sidebarGo.GetComponent<RectTransform>();
            sidebarRt.anchorMin = new Vector2(1f, 0f);
            sidebarRt.anchorMax = Vector2.one;
            sidebarRt.offsetMin = new Vector2(-RIGHT_PANEL_W, 0f);
            sidebarRt.offsetMax = Vector2.zero;
            BuildSidebar(sidebarRt);

            // Thin left-border on sidebar
            {
                var border = new GameObject("SidebarBorder");
                border.transform.SetParent(main, false);
                var img = border.AddComponent<Image>();
                img.color = StyleHelper.DarkBorder;
                var rt = border.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(1f, 0f);
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(-RIGHT_PANEL_W - 1f, 0f);
                rt.offsetMax = new Vector2(-RIGHT_PANEL_W, 0f);
            }

            // ── Canvas viewport (center, solid dark bg, grid) ──────────────
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(main, false);
            _viewportImg = viewportGo.AddComponent<Image>();
            _viewportImg.color = new Color(0.08f, 0.11f, 0.18f, 1.0f);
            _viewportImg.raycastTarget = true;
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = new Vector2(LEFT_PANEL_W + 1f, 0f);
            viewportRt.offsetMax = new Vector2(-RIGHT_PANEL_W - 1f, 0f);

            // Grid renderer (sits as a component on viewport parent, draws into viewport)
            var gridRenderer = viewportGo.AddComponent<GridRenderer>();
            gridRenderer.Init(viewportRt);

            // Input receiver — canvas events land on this (fills viewport)
            var canvasCtrlGo = new GameObject("CanvasController");
            canvasCtrlGo.transform.SetParent(viewportGo.transform, false);
            var ccRt = canvasCtrlGo.AddComponent<RectTransform>();
            ccRt.anchorMin = Vector2.zero;
            ccRt.anchorMax = Vector2.one;
            ccRt.offsetMin = Vector2.zero;
            ccRt.offsetMax = Vector2.zero;
            var ccImg = canvasCtrlGo.AddComponent<Image>();
            ccImg.color = Color.clear;
            ccImg.raycastTarget = true;

            // Content panel — platform widgets parented here
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(canvasCtrlGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = Vector2.zero;

            _canvasCtrl = canvasCtrlGo.AddComponent<EditorCanvasController>();
            _canvasCtrl.Init(_ctrl, ccRt, contentRt);

            // ── Dummy objects for tab fields (API compatibility) ───────────
            _platformsTab = new GameObject("Tab_Platforms_Dummy");
            _platformsTab.transform.SetParent(bg, false);
            _environmentTab = new GameObject("Tab_Environment_Dummy");
            _environmentTab.transform.SetParent(bg, false);

            // ── In-editor load browser (modal) ────────────────────────────
            BuildBrowserPanel(bg);
        }

        // ── Top Bar ───────────────────────────────────────────────────────

        private void BuildTopBar(RectTransform topBar)
        {
            var layout = topBar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 8);
            layout.spacing = 6;
            layout.childAlignment       = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth  = false;

            // Scene background buttons (top bar, before undo/redo)
            {
                var sceneGrassBtn = AddTopBarButton(topBar, "GRASS", new Color(0.20f, 0.65f, 0.15f, 1f), minWidth: 64, minHeight: 44);
                sceneGrassBtn.onClick.AddListener(() => Util.BackgroundSceneLoader.Load(0));
                var sceneSnowBtn = AddTopBarButton(topBar, "SNOW", new Color(0.45f, 0.75f, 0.95f, 1f), minWidth: 60, minHeight: 44);
                sceneSnowBtn.onClick.AddListener(() => Util.BackgroundSceneLoader.Load(1));
                var sceneSpaceBtn = AddTopBarButton(topBar, "SPACE", new Color(0.25f, 0.15f, 0.65f, 1f), minWidth: 64, minHeight: 44);
                sceneSpaceBtn.onClick.AddListener(() => Util.BackgroundSceneLoader.Load(2));
            }
            AddTopBarSep(topBar);

            // Undo button
            _undoBtn = AddTopBarButton(topBar, "UNDO", StyleHelper.DarkBlue, minWidth: 60, minHeight: 44);
            _undoBtn.onClick.AddListener(OnUndo);

            // Redo button
            _redoBtn = AddTopBarButton(topBar, "REDO", StyleHelper.DarkBlue, minWidth: 60, minHeight: 44);
            _redoBtn.onClick.AddListener(OnRedo);

            AddTopBarSep(topBar);

            // Title label
            AddTopBarLabel(topBar, "MAP EDITOR", 18f, bold: true, minWidth: 140);

            // Map name input field
            _mapNameField = AddTopBarInputField(topBar);
            _mapNameField.onEndEdit.AddListener(n => _ctrl.CurrentMap.Name = n);

            AddTopBarSep(topBar);

            // Save button (blue oval)
            var saveBtn = AddTopBarButton(topBar, "SAVE", StyleHelper.Blue, minWidth: 80, minHeight: 44);
            saveBtn.onClick.AddListener(OnSave);

            // Snap button (green, toggleable)
            _snapBtn = AddTopBarButton(topBar, "SNAP", new Color(0.25f, 0.60f, 0.30f, 1f), minWidth: 72, minHeight: 44);
            _snapBtn.onClick.AddListener(() => {
                _ctrl.SnapToGrid = !_ctrl.SnapToGrid;
                UpdateSnapHighlight(_snapBtn);
            });

            // Flexible spacer
            {
                var spacer = new GameObject("Spacer");
                spacer.transform.SetParent(topBar, false);
                spacer.AddComponent<LayoutElement>().flexibleWidth = 1f;
            }

            // Close button (red oval)
            var closeBtn = AddTopBarButton(topBar, "EXIT", new Color(0.85f, 0.14f, 0.14f, 1f), minWidth: 72, minHeight: 44);
            closeBtn.onClick.AddListener(Close);

            // Dummy tab buttons (API compatibility — never shown)
            var dummyGo = new GameObject("TabBtns_Dummy");
            dummyGo.transform.SetParent(topBar, false);
            dummyGo.AddComponent<LayoutElement>().minWidth = 0;
            _tabPlatforms   = dummyGo.AddComponent<Button>();
            _tabEnvironment = dummyGo.AddComponent<Button>();

            UpdateSnapHighlight(_snapBtn);
        }

        private Button AddTopBarButton(RectTransform parent, string text, Color color,
            float minWidth = 60, float minHeight = 36)
        {
            var go = new GameObject("TBtn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.GetButtonSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth    = minWidth;
            le.minHeight   = minHeight;
            le.flexibleWidth = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 15f, bold: true);
            tmp.text = text;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 1);
            lrt.offsetMax = new Vector2(-4, -1);

            return btn;
        }

        private TMP_InputField AddTopBarInputField(RectTransform parent)
        {
            var go = new GameObject("MapNameInput");
            go.transform.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color  = StyleHelper.DarkElevated;
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type   = Image.Type.Sliced;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth   = 120f;
            le.minHeight  = 36f;
            le.flexibleWidth = 1f;

            var field = go.AddComponent<TMP_InputField>();

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 12f);
            phTmp.text  = "Map name...";
            phTmp.color = StyleHelper.TextMuted;
            phTmp.alignment = TextAlignmentOptions.Left;
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(8, 2); phRt.offsetMax = new Vector2(-8, -2);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(textTmp, 12f);
            textTmp.color = StyleHelper.TextPrimary;
            textTmp.alignment = TextAlignmentOptions.Left;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 2); textRt.offsetMax = new Vector2(-8, -2);

            field.textViewport  = textRt;
            field.textComponent = textTmp;
            field.placeholder   = phTmp;
            field.caretColor    = StyleHelper.White;
            return field;
        }

        private TextMeshProUGUI AddTopBarLabel(RectTransform parent, string text,
            float fontSize = 14f, bool bold = false, float minWidth = 60)
        {
            var go = new GameObject("TLbl_" + text);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize, bold);
            tmp.text  = text;
            tmp.color = bold ? StyleHelper.TextPrimary : StyleHelper.TextSecondary;
            tmp.alignment = TextAlignmentOptions.Left;
            if (bold) tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            go.AddComponent<LayoutElement>().minWidth = minWidth;
            return tmp;
        }

        private void AddTopBarSep(RectTransform parent)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = StyleHelper.DarkBorder;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth  = 2;
            le.minHeight = 32;
            le.flexibleHeight = 0;
        }

        // ── Left Panel (absolute positioning) ─────────────────────────────

        private void BuildLeftPanel(RectTransform panel)
        {
            float cx = 8f;    // x position of buttons from left edge
            float bw = 72f;   // button width

            // ── TOOLS section ─────────────────────────────────────────────
            MakeLeftLabel(panel, "TOOLS", cx, bw, -24f, 16f);

            string[] toolNames  = { "SELECT", "PLACE", "DELETE" };
            Color[]  toolColors = {
                StyleHelper.Blue,
                StyleHelper.Orange,
                new Color(0.85f, 0.20f, 0.20f, 1f)
            };

            float[] toolY = { -56f, -106f, -156f };
            for (int i = 0; i < toolNames.Length; i++)
            {
                int idx = i;
                var btn = MakeLeftButton(panel, toolNames[i], toolColors[i],
                    new Vector2(cx, toolY[i]), new Vector2(bw, 44f));
                btn.onClick.AddListener(() => SetTool(idx));
                _toolButtons.Add(btn);
            }

            // Separator after tools
            MakeLeftDivider(panel, -184f, bw, cx);

            // ── ISLANDS section ───────────────────────────────────────────
            MakeLeftLabel(panel, "ISLANDS", cx, bw, -200f, 14f);

            float islandStartY = -222f;
            float islandStep   = 42f;
            for (int i = 0; i < MapEditorController.IslandPresets.Length; i++)
            {
                int idx = i;
                float by = islandStartY - i * islandStep;
                Color baseColor = StyleHelper.PlatformColors[Mathf.Clamp(_ctrl.PlacePlatformType, 0, StyleHelper.PlatformColors.Length - 1)];
                var btn = MakeLeftButton(panel, MapEditorController.IslandPresets[i].name,
                    baseColor,
                    new Vector2(cx, by), new Vector2(bw, 38f));
                btn.onClick.AddListener(() => {
                    _ctrl.SelectedPreset = idx;
                    _ctrl.ActiveTool = EditorTool.Place;
                    UpdateToolHighlights();
                    UpdatePresetHighlights();
                });
                _presetButtons.Add(btn);
            }

            // Material selector row (small colored squares, 14x14 each)
            float matRowY = islandStartY - MapEditorController.IslandPresets.Length * islandStep - 8f;
            MakeLeftLabel(panel, "MATERIAL", cx, bw, matRowY, 12f);
            float matSqY   = matRowY - 16f;
            float matSqSize = 14f;
            float matSqGap  = 2f;
            float matRowX   = cx;
            for (int i = 0; i < StyleHelper.PlatformColors.Length; i++)
            {
                int idx = i;
                var sqGo = new GameObject("MatSq_" + i);
                sqGo.transform.SetParent(panel, false);
                var img = sqGo.AddComponent<Image>();
                img.color  = StyleHelper.PlatformColors[i];
                img.sprite = StyleHelper.GetButtonSprite();
                img.type   = Image.Type.Sliced;
                var btn = sqGo.AddComponent<Button>();
                StyleHelper.StyleButton(btn, StyleHelper.PlatformColors[i]);
                StyleHelper.AddPressColorSwap(btn);
                var rt = sqGo.GetComponent<RectTransform>();
                rt.anchorMin = new Vector2(0f, 1f);
                rt.anchorMax = new Vector2(0f, 1f);
                rt.pivot     = new Vector2(0f, 1f);
                rt.sizeDelta = new Vector2(matSqSize, matSqSize);
                rt.anchoredPosition = new Vector2(matRowX + i * (matSqSize + matSqGap), matSqY);
                btn.onClick.AddListener(() => {
                    SetPlacePlatformType(idx);
                    UpdatePresetHighlights();
                });
                _typeButtons.Add(btn);
            }

            // Separator after material selector
            float afterMaterialY = matSqY - matSqSize - 8f;
            MakeLeftDivider(panel, afterMaterialY, bw, cx);

            // ── THEME section ─────────────────────────────────────────────
            float themeLabelY = afterMaterialY - 18f;
            MakeLeftLabel(panel, "THEME", cx, bw, themeLabelY, 14f);

            float themeStartY = themeLabelY - 34f;
            float themeStep   = 44f;
            string[] themeShort = { "GRASS", "SNOW", "SPACE" };
            for (int i = 0; i < StyleHelper.ThemeNames.Length; i++)
            {
                int idx = i;
                float ty = themeStartY - i * themeStep;
                var btn = MakeLeftButton(panel, themeShort[i],
                    StyleHelper.ThemeColors[i],
                    new Vector2(cx, ty), new Vector2(bw, 36f));
                btn.onClick.AddListener(() => SetTheme(idx));
                _themeButtons.Add(btn);
            }

            UpdateToolHighlights();
            UpdateTypeHighlights();
            UpdateThemeHighlights();
            UpdatePresetHighlights();
        }

        private Button MakeLeftButton(RectTransform parent, string text, Color color,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject("LBtn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.GetButtonSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 12f, bold: true);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(2, 1);
            lrt.offsetMax = new Vector2(-2, -1);

            return btn;
        }

        private TextMeshProUGUI MakeLeftLabel(RectTransform parent, string text,
            float x, float w, float y, float h)
        {
            var go = new GameObject("LLbl_" + text);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleTextAllCaps(tmp, 11f);
            tmp.text  = text;
            tmp.color = StyleHelper.TextMuted;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return tmp;
        }

        private void MakeLeftDivider(RectTransform parent, float y, float w, float x)
        {
            var go = new GameObject("LDiv");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = StyleHelper.DarkBorder;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, 1f);
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ── Sidebar (absolute positioning, no VerticalLayoutGroup) ────────

        private void BuildSidebar(RectTransform sidebar)
        {
            float padX  = 8f;
            float innerW = RIGHT_PANEL_W - padX * 2f;
            float x0    = padX;

            // Platform count label
            _platformCountLabel = MakeAbsSideLabel(sidebar, "PLATFORMS: 0",
                x0, innerW, -16f, 18f, bold: true);
            _platformCountLabel.color = StyleHelper.TextPrimary;

            // Divider
            MakeAbsDivider(sidebar, -40f, innerW, x0);

            // "SELECTED" section label
            MakeAbsSideLabel(sidebar, "SELECTED", x0, innerW, -56f, 16f, bold: true, muted: true);

            // Selected info label
            _selectedInfoLabel = MakeAbsSideLabel(sidebar, "None selected",
                x0, innerW, -74f, 18f);
            _selectedInfoLabel.color = StyleHelper.TextMuted;
            _selectedInfoLabel.fontSize = 13f;

            // X / Y row (y=-100)
            float lblW   = 20f;
            float inputW = (innerW - lblW * 2f - 8f) * 0.5f;
            {
                var xl = MakeAbsSideLabel(sidebar, "X", x0, lblW, -100f, 28f);
                xl.color = new Color(0.9f, 0.4f, 0.4f, 1f);
                xl.fontStyle = FontStyles.Bold;
            }
            _propX = MakeAbsInputField(sidebar, "0.00", x0 + lblW + 2f, inputW, -100f, 28f);
            {
                var yl = MakeAbsSideLabel(sidebar, "Y", x0 + lblW + 2f + inputW + 4f, lblW, -100f, 28f);
                yl.color = new Color(0.4f, 0.9f, 0.5f, 1f);
                yl.fontStyle = FontStyles.Bold;
            }
            _propY = MakeAbsInputField(sidebar, "0.00",
                x0 + lblW * 2f + 2f + inputW + 6f, inputW, -100f, 28f);

            // W / H row (y=-136)
            {
                var wl = MakeAbsSideLabel(sidebar, "W", x0, lblW, -136f, 28f);
                wl.color = new Color(0.45f, 0.70f, 1.00f, 1f);
                wl.fontStyle = FontStyles.Bold;
            }
            _propHW = MakeAbsInputField(sidebar, "8.00", x0 + lblW + 2f, inputW, -136f, 28f);
            {
                var hl = MakeAbsSideLabel(sidebar, "H", x0 + lblW + 2f + inputW + 4f, lblW, -136f, 28f);
                hl.color = new Color(0.45f, 0.70f, 1.00f, 1f);
                hl.fontStyle = FontStyles.Bold;
            }
            _propHH = MakeAbsInputField(sidebar, "1.50",
                x0 + lblW * 2f + 2f + inputW + 6f, inputW, -136f, 28f);

            // Rad / Rot row (y=-172)
            MakeAbsSideLabel(sidebar, "Rad", x0, lblW, -172f, 28f);
            _propRadius = MakeAbsInputField(sidebar, "1.00", x0 + lblW + 2f, inputW, -172f, 28f);
            MakeAbsSideLabel(sidebar, "Rot", x0 + lblW + 2f + inputW + 4f, lblW, -172f, 28f);
            _propRotation = MakeAbsInputField(sidebar, "0.00",
                x0 + lblW * 2f + 2f + inputW + 6f, inputW, -172f, 28f);

            WirePropertyField(_propX,        v => UpdateSelectedPlatform(x: v));
            WirePropertyField(_propY,        v => UpdateSelectedPlatform(y: v));
            WirePropertyField(_propHW,       v => UpdateSelectedPlatform(hw: v));
            WirePropertyField(_propHH,       v => UpdateSelectedPlatform(hh: v));
            WirePropertyField(_propRadius,   v => UpdateSelectedPlatform(radius: v));
            WirePropertyField(_propRotation, v => UpdateSelectedPlatform(rotation: v));

            // DELETE button (y=-208)
            var delBtn = MakeAbsButton(sidebar, "DELETE",
                StyleHelper.DangerColor,
                new Vector2(x0, -208f), new Vector2(innerW, 32f));
            delBtn.onClick.AddListener(() => {
                _ctrl.DeleteSelected();
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // Divider (y=-248)
            MakeAbsDivider(sidebar, -248f, innerW, x0);

            // PUSH TO LOBBY (y=-264)
            var lobbyBtn = MakeAbsButton(sidebar, "▶  Push to Lobby",
                StyleHelper.Orange,
                new Vector2(x0, -264f), new Vector2(innerW, 32f));
            lobbyBtn.onClick.AddListener(OnPushToLobby);

            // Divider (y=-304)
            MakeAbsDivider(sidebar, -304f, innerW, x0);

            // LOAD button (y=-320)
            var loadBtn = MakeAbsButton(sidebar, "Load Map",
                StyleHelper.DarkBlue,
                new Vector2(x0, -320f), new Vector2(innerW, 32f));
            loadBtn.onClick.AddListener(OnLoad);

            // NEW MAP button (y=-360)
            var newBtn = MakeAbsButton(sidebar, "+ New Map",
                StyleHelper.DarkBlue,
                new Vector2(x0, -360f), new Vector2(innerW, 32f));
            newBtn.onClick.AddListener(() => {
                _ctrl.NewMap();
                _mapNameField.text = _ctrl.CurrentMap.Name;
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // _movPanel intentionally kept null — movement panel omitted in this redesign.
            // RefreshSidebar guards all _movPanel calls.
        }

        // ── Sidebar absolute-position helpers ─────────────────────────────

        private TextMeshProUGUI MakeAbsSideLabel(RectTransform parent, string text,
            float x, float w, float y, float h, bool bold = false, bool muted = false)
        {
            var go = new GameObject("Label_" + text);
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, bold ? 11f : 12f, bold);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Left;
            if (muted) tmp.color = StyleHelper.TextMuted;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);
            return tmp;
        }

        private TMP_InputField MakeAbsInputField(RectTransform parent, string placeholder,
            float x, float w, float y, float h)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color  = StyleHelper.DarkElevated;
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type   = Image.Type.Sliced;

            var field = go.AddComponent<TMP_InputField>();
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, h);
            rt.anchoredPosition = new Vector2(x, y);

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 11f);
            phTmp.text  = placeholder;
            phTmp.color = StyleHelper.TextMuted;
            phTmp.alignment = TextAlignmentOptions.Left;
            var phRt = phGo.GetComponent<RectTransform>();
            phRt.anchorMin = Vector2.zero; phRt.anchorMax = Vector2.one;
            phRt.offsetMin = new Vector2(6, 2); phRt.offsetMax = new Vector2(-6, -2);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(textTmp, 11f);
            textTmp.color = StyleHelper.TextPrimary;
            textTmp.alignment = TextAlignmentOptions.Left;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero; textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(6, 2); textRt.offsetMax = new Vector2(-6, -2);

            field.textViewport  = textRt;
            field.textComponent = textTmp;
            field.placeholder   = phTmp;
            field.caretColor    = StyleHelper.White;
            return field;
        }

        private Button MakeAbsButton(RectTransform parent, string text, Color color,
            Vector2 pos, Vector2 size)
        {
            var go = new GameObject("AbsBtn_" + text);
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.GetButtonSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = size;
            rt.anchoredPosition = pos;

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 12f, bold: true);
            tmp.text = text;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 0); lrt.offsetMax = new Vector2(-6, 0);

            return btn;
        }

        private void MakeAbsDivider(RectTransform parent, float y, float w, float x)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = StyleHelper.DarkBorder;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 1f);
            rt.anchorMax = new Vector2(0f, 1f);
            rt.pivot     = new Vector2(0f, 1f);
            rt.sizeDelta = new Vector2(w, 1f);
            rt.anchoredPosition = new Vector2(x, y);
        }

        // ── In-editor load browser (modal) ─────────────────────────────────

        private void BuildBrowserPanel(RectTransform parent)
        {
            _browserPanel = new GameObject("BrowserPanel");
            _browserPanel.transform.SetParent(parent, false);

            var overlay = _browserPanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.68f);
            var ort = _browserPanel.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero;
            ort.offsetMax = Vector2.zero;

            var box = UIBuilder.Panel(ort, "BrowserBox",
                new Color(0.09f, 0.11f, 0.18f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-210f, -260f), new Vector2(210f, 260f));

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 10;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(box, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 16f, bold: true);
            titleTmp.text = "LOAD MAP";
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleGo.AddComponent<LayoutElement>().minHeight = 24;

            UIBuilder.AddRule(box, StyleHelper.DarkBorder);

            var scroll = UIBuilder.MakeScrollView(box,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -80f));
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight      = 320;
            _browserContent = scroll.content;

            var cancelBtn = UIBuilder.MakeButton(box, "Cancel", StyleHelper.DarkBlue,
                new Vector2(0f, 34f), Vector2.zero);
            cancelBtn.gameObject.AddComponent<LayoutElement>().minHeight = 34;
            cancelBtn.onClick.AddListener(() => _browserPanel.SetActive(false));

            _browserPanel.SetActive(false);
        }

        // ── Actions ───────────────────────────────────────────────────────

        private void SetTool(int idx)
        {
            _ctrl.ActiveTool = (EditorTool)idx;
            UpdateToolHighlights();
        }

        private void SetPlacePlatformType(int idx)
        {
            _ctrl.PlacePlatformType = idx;
            _ctrl.ActiveTool = EditorTool.Place;
            UpdateToolHighlights();
            UpdateTypeHighlights();
        }

        private void SetTheme(int idx)
        {
            _ctrl.CurrentMap.LevelTheme = idx;
            UpdateThemeHighlights();
        }

        private void OnSave()
        {
            string name = _mapNameField.text.Trim();
            if (string.IsNullOrEmpty(name)) name = "Untitled";
            _ctrl.CurrentMap.Name = name;
            _ctrl.SaveCurrentMap(name);
            _platformCountLabel.text = "SAVED! (" + _ctrl.CurrentMap.Platforms.Count + ")";
            Plugin.Log.LogInfo("[MapEditorWindow] Saved map '" + name + "'");
        }

        private void OnLoad()
        {
            PopulateBrowser();
            _browserPanel.SetActive(true);
        }

        private void OnPushToLobby()
        {
            _ctrl.PushToLobby();
            Plugin.Log.LogInfo("[MapEditorWindow] Pushed map to lobby.");
        }

        private void PopulateBrowser()
        {
            foreach (Transform child in _browserContent)
                Destroy(child.gameObject);

            string[] maps = MapSerializer.ListMaps();
            foreach (var name in maps)
            {
                string captureName = name;

                var rowGo = new GameObject("BRow_" + name);
                rowGo.transform.SetParent(_browserContent, false);
                var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
                hlg.spacing = 5;
                hlg.childForceExpandWidth  = false;
                hlg.childForceExpandHeight = true;
                rowGo.AddComponent<LayoutElement>().minHeight = 36;
                rowGo.AddComponent<Image>().color = new Color(1f, 1f, 1f, 0.03f);
                var row = rowGo.GetComponent<RectTransform>();

                var lbl = new GameObject("Label");
                lbl.transform.SetParent(row, false);
                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(tmp, 13f);
                tmp.text      = captureName;
                tmp.color     = StyleHelper.TextPrimary;
                tmp.alignment = TextAlignmentOptions.Left;
                lbl.AddComponent<LayoutElement>().flexibleWidth = 1;

                var loadBtn = UIBuilder.MakeButton(row, "Load", StyleHelper.Blue,
                    Vector2.zero, Vector2.zero);
                loadBtn.gameObject.AddComponent<LayoutElement>().minWidth = 64;
                loadBtn.onClick.AddListener(() => {
                    _ctrl.LoadFromFile(captureName);
                    _mapNameField.text = _ctrl.CurrentMap.Name;
                    _canvasCtrl.Refresh();
                    RefreshSidebar();
                    _browserPanel.SetActive(false);
                });

                var delBtn = UIBuilder.MakeButton(row, "Del", StyleHelper.DangerColor,
                    Vector2.zero, Vector2.zero);
                delBtn.gameObject.AddComponent<LayoutElement>().minWidth = 48;
                delBtn.onClick.AddListener(() => {
                    MapSerializer.DeleteMap(captureName);
                    PopulateBrowser();
                });
            }
        }

        // ── Sidebar property sync ─────────────────────────────────────────

        public void RefreshSidebar()
        {
            _platformCountLabel.text = "PLATFORMS: " + _ctrl.CurrentMap.Platforms.Count;
            int  sel    = _ctrl.SelectedPlatformIndex;
            bool hasSel = sel >= 0 && sel < _ctrl.CurrentMap.Platforms.Count;

            if (hasSel)
            {
                var p = _ctrl.CurrentMap.Platforms[sel];
                _selectedInfoLabel.text  = "#" + (sel + 1) + "  " +
                    StyleHelper.PlatformNames[Mathf.Clamp(p.Type, 0, 5)];
                _selectedInfoLabel.color = StyleHelper.TextSecondary;
                if (_movPanel != null)
                {
                    _movPanel.SetData(p);
                    _canvasCtrl.RefreshMovementPreview();
                }
                _propX.SetTextWithoutNotify(p.X.ToString("F2"));
                _propY.SetTextWithoutNotify(p.Y.ToString("F2"));
                _propHW.SetTextWithoutNotify(p.HalfW.ToString("F2"));
                _propHH.SetTextWithoutNotify(p.HalfH.ToString("F2"));
                _propRadius.SetTextWithoutNotify(p.Radius.ToString("F2"));
                _propRotation.SetTextWithoutNotify(p.Rotation.ToString("F2"));
            }
            else
            {
                _selectedInfoLabel.text  = "None selected";
                _selectedInfoLabel.color = StyleHelper.TextMuted;
            }

            UpdateToolHighlights();
            UpdateTypeHighlights();
            UpdateThemeHighlights();
            UpdatePresetHighlights();
        }

        private void UpdateSelectedPlatform(float? x = null, float? y = null,
            float? hw = null, float? hh = null, float? radius = null, float? rotation = null)
        {
            int sel = _ctrl.SelectedPlatformIndex;
            if (sel < 0 || sel >= _ctrl.CurrentMap.Platforms.Count) return;
            var p = _ctrl.CurrentMap.Platforms[sel];
            if (x.HasValue)        p.X        = x.Value;
            if (y.HasValue)        p.Y        = y.Value;
            if (hw.HasValue)       p.HalfW    = Mathf.Max(0.5f, hw.Value);
            if (hh.HasValue)       p.HalfH    = Mathf.Max(0.5f, hh.Value);
            if (radius.HasValue)   p.Radius   = radius.Value;
            if (rotation.HasValue) p.Rotation = rotation.Value;
            _ctrl.CurrentMap.Platforms[sel] = p;
            _canvasCtrl.RefreshPositions();
        }

        private void WirePropertyField(TMP_InputField field, System.Action<float> setter)
        {
            field.onEndEdit.AddListener(val => {
                float f;
                if (float.TryParse(val, out f)) setter(f);
            });
        }

        private void UpdateToolHighlights()
        {
            Color[] tc = {
                StyleHelper.Blue,
                StyleHelper.Orange,
                new Color(0.72f, 0.20f, 0.20f, 1f)
            };
            for (int i = 0; i < _toolButtons.Count; i++)
            {
                bool active = i == (int)_ctrl.ActiveTool;
                var  img    = _toolButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = active ? tc[i] : tc[i] * 0.45f;
            }
        }

        private void UpdateTypeHighlights()
        {
            for (int i = 0; i < _typeButtons.Count; i++)
            {
                bool active = i == _ctrl.PlacePlatformType;
                var  img    = _typeButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = active
                        ? StyleHelper.PlatformColors[i]
                        : StyleHelper.PlatformColors[i] * 0.45f;
            }
        }

        private void UpdateThemeHighlights()
        {
            for (int i = 0; i < _themeButtons.Count; i++)
            {
                bool active = i == _ctrl.CurrentMap.LevelTheme;
                var  img    = _themeButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = active
                        ? StyleHelper.ThemeColors[i]
                        : StyleHelper.ThemeColors[i] * 0.45f;
            }
        }

        private void UpdatePresetHighlights()
        {
            for (int i = 0; i < _presetButtons.Count; i++)
            {
                bool active = i == _ctrl.SelectedPreset;
                var  img    = _presetButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    int typeIdx = Mathf.Clamp(_ctrl.PlacePlatformType, 0, StyleHelper.PlatformColors.Length - 1);
                    Color baseColor = StyleHelper.PlatformColors[typeIdx];
                    img.color = active ? baseColor : baseColor * 0.45f;
                }
            }
        }

        private void UpdateSnapHighlight(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            var snapOnColor  = new Color(0.25f, 0.60f, 0.30f, 1f);
            var snapOffColor = snapOnColor * 0.45f;
            img.color = _ctrl.SnapToGrid ? snapOnColor : snapOffColor;
        }

        private void OnUndo()
        {
            _ctrl.History.Undo();
            _canvasCtrl.Refresh();
            RefreshSidebar();
        }

        private void OnRedo()
        {
            _ctrl.History.Redo();
            _canvasCtrl.Refresh();
            RefreshSidebar();
        }

        void Update()
        {
            if (_ctrl == null) return;

            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (ctrl)
            {
                if (Input.GetKeyDown(KeyCode.Z)) OnUndo();
                if (Input.GetKeyDown(KeyCode.Y)) OnRedo();
            }

            if (Input.GetKeyDown(KeyCode.G))
            {
                _ctrl.SnapToGrid = !_ctrl.SnapToGrid;
                if (_snapBtn != null) UpdateSnapHighlight(_snapBtn);
            }

            // Make viewport transparent when a background scene is loaded
            if (_viewportImg != null)
            {
                _viewportImg.color = Util.BackgroundSceneLoader.IsLoaded
                    ? Color.clear
                    : new Color(0.08f, 0.11f, 0.18f, 1.0f);
            }
        }
    }

    // Extension helper
    internal static class Extensions
    {
        public static T Let<T>(this T obj, System.Action<T> action) { action(obj); return obj; }
    }
}

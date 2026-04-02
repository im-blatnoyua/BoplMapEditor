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
    // Dark game aesthetic: toolbar strip at top, sidebar on right, canvas viewport fills the rest.
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
        private readonly List<Button> _typeButtons  = new();
        private readonly List<Button> _themeButtons = new();
        private readonly List<Button> _toolButtons  = new();

        // Sidebar tabs
        private GameObject      _platformsTab   = null!;
        private GameObject      _environmentTab = null!;
        private EnvironmentPanel _envPanel      = null!;
        private MovementPanel    _movPanel      = null!;
        private Button           _tabPlatforms  = null!;
        private Button           _tabEnvironment = null!;

        // In-editor load browser (modal panel)
        private GameObject      _browserPanel   = null!;
        private RectTransform   _browserContent = null!;

        // Layout constants (at 1920×1080 reference)
        private const float TOOLBAR_H  = 52f;
        private const float SIDEBAR_W  = 264f;

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
            window._slideAnimator.OffscreenY = 1200f;

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
            _envPanel.SetData(_ctrl.CurrentMap.Environment);
            _slideAnimator.AnimateIn();
        }

        public void Close()
        {
            _slideAnimator.AnimateOut(() => _ctrl.Close());
        }

        // ── Build UI ──────────────────────────────────────────────────────

        private void BuildUI()
        {
            var root = _canvas.GetComponent<RectTransform>();

            // ── Full-screen background ────────────────────────────────────
            var bg = UIBuilder.FlatPanel(root, "Background", StyleHelper.DarkPanel,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── Toolbar (top strip) ───────────────────────────────────────
            var toolbar = UIBuilder.FlatPanel(bg, "Toolbar",
                new Color(0.06f, 0.08f, 0.14f, 1f),
                new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -TOOLBAR_H), Vector2.zero);
            BuildToolbar(toolbar);

            // Hairline border below toolbar
            var tbBorder = new GameObject("ToolbarBorder");
            tbBorder.transform.SetParent(bg, false);
            var tbbImg = tbBorder.AddComponent<Image>();
            tbbImg.color = StyleHelper.DarkBorder;
            var tbbRt = tbBorder.GetComponent<RectTransform>();
            tbbRt.anchorMin = new Vector2(0f, 1f);
            tbbRt.anchorMax = Vector2.one;
            tbbRt.offsetMin = new Vector2(0f, -TOOLBAR_H - 1f);
            tbbRt.offsetMax = new Vector2(0f, -TOOLBAR_H);

            // ── Main area (below toolbar) ─────────────────────────────────
            var main = new GameObject("Main").AddComponent<RectTransform>();
            main.SetParent(bg, false);
            main.anchorMin = Vector2.zero;
            main.anchorMax = Vector2.one;
            main.offsetMin = new Vector2(0f, 0f);
            main.offsetMax = new Vector2(0f, -TOOLBAR_H - 1f);

            // Sidebar border (left edge of sidebar)
            var sbBorder = new GameObject("SidebarBorder");
            sbBorder.transform.SetParent(main, false);
            var sbBorderImg = sbBorder.AddComponent<Image>();
            sbBorderImg.color = StyleHelper.DarkBorder;
            var sbBorderRt = sbBorder.GetComponent<RectTransform>();
            sbBorderRt.anchorMin = new Vector2(1f, 0f);
            sbBorderRt.anchorMax = Vector2.one;
            sbBorderRt.offsetMin = new Vector2(-SIDEBAR_W - 1f, 0f);
            sbBorderRt.offsetMax = new Vector2(-SIDEBAR_W, 0f);

            // ── Sidebar (right, fixed width) ──────────────────────────────
            var sidebar = UIBuilder.FlatPanel(main, "Sidebar",
                new Color(0.07f, 0.09f, 0.15f, 1f),
                new Vector2(1f, 0f), Vector2.one,
                new Vector2(-SIDEBAR_W, 0f), Vector2.zero);
            BuildSidebar(sidebar);

            // ── Canvas viewport (remainder of screen) ─────────────────────
            var viewport = UIBuilder.FlatPanel(main, "Viewport",
                new Color(0.04f, 0.05f, 0.09f, 1f),
                Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(-SIDEBAR_W - 1f, 0f));

            // Input receiver (clear image so scroll/click events land here)
            var canvasCtrlGo = new GameObject("CanvasController");
            canvasCtrlGo.transform.SetParent(viewport, false);
            var ccRt = canvasCtrlGo.AddComponent<RectTransform>();
            ccRt.anchorMin = Vector2.zero;
            ccRt.anchorMax = Vector2.one;
            ccRt.offsetMin = Vector2.zero;
            ccRt.offsetMax = Vector2.zero;
            canvasCtrlGo.AddComponent<Image>().color = Color.clear;

            // Content panel (platforms rendered here)
            var contentGo  = new GameObject("Content");
            contentGo.transform.SetParent(canvasCtrlGo.transform, false);
            var contentRt  = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = Vector2.zero;

            _canvasCtrl = canvasCtrlGo.AddComponent<EditorCanvasController>();
            _canvasCtrl.Init(_ctrl, ccRt, contentRt);

            // ── Load-map browser (modal) ───────────────────────────────────
            BuildBrowserPanel(bg);
        }

        // ── Toolbar ───────────────────────────────────────────────────────

        private void BuildToolbar(RectTransform toolbar)
        {
            var layout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 10, 7, 7);
            layout.spacing = 6;
            layout.childAlignment      = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth  = false;

            // App identity: accent bar + title
            var accentGo = new GameObject("TitleAccent");
            accentGo.transform.SetParent(toolbar, false);
            var accentImg = accentGo.AddComponent<Image>();
            accentImg.color = StyleHelper.Blue;
            var accentLe = accentGo.AddComponent<LayoutElement>();
            accentLe.minWidth = 3;
            accentLe.flexibleHeight = 0;
            accentLe.minHeight = 28;

            var spacerA = MakeToolbarSpacer(toolbar, 6);

            AddToolbarLabel(toolbar, "MAP EDITOR", 16f, bold: true, minWidth: 130);

            AddToolbarSep(toolbar);

            // ── Tool mode ─────────────────────────────────────────────────
            AddToolbarLabel(toolbar, "Tool", 12f, minWidth: 30);
            string[] toolNames  = { "Select", "Place", "Delete" };
            Color[]  toolColors = {
                StyleHelper.Blue,
                StyleHelper.Orange,
                new Color(0.72f, 0.20f, 0.20f, 1f)
            };
            for (int i = 0; i < toolNames.Length; i++)
            {
                int idx = i;
                var btn = AddToolbarButton(toolbar, toolNames[i], toolColors[i], minWidth: 72);
                btn.onClick.AddListener(() => SetTool(idx));
                _toolButtons.Add(btn);
            }

            AddToolbarSep(toolbar);

            // ── Block type ────────────────────────────────────────────────
            AddToolbarLabel(toolbar, "Block", 12f, minWidth: 36);
            for (int i = 0; i < StyleHelper.PlatformNames.Length; i++)
            {
                int idx = i;
                var btn = AddToolbarButton(toolbar, StyleHelper.PlatformNames[i],
                    StyleHelper.PlatformColors[i], minWidth: 62);
                btn.onClick.AddListener(() => SetPlacePlatformType(idx));
                _typeButtons.Add(btn);
            }

            AddToolbarSep(toolbar);

            // ── Level theme ───────────────────────────────────────────────
            AddToolbarLabel(toolbar, "Theme", 12f, minWidth: 42);
            for (int i = 0; i < StyleHelper.ThemeNames.Length; i++)
            {
                int idx = i;
                var btn = AddToolbarButton(toolbar, StyleHelper.ThemeNames[i],
                    StyleHelper.ThemeColors[i], minWidth: 62);
                btn.onClick.AddListener(() => SetTheme(idx));
                _themeButtons.Add(btn);
            }

            // Flexible spacer pushes Close to the right
            var flex = new GameObject("Flex");
            flex.transform.SetParent(toolbar, false);
            flex.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Close button
            var closeBtn = AddToolbarButton(toolbar, "✕  Close",
                new Color(0.60f, 0.15f, 0.15f, 1f), minWidth: 88);
            closeBtn.onClick.AddListener(Close);

            UpdateToolHighlights();
        }

        private Button AddToolbarButton(RectTransform parent, string text, Color color,
            float minWidth = 80)
        {
            var go = new GameObject($"TBtn_{text}");
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type  = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth    = minWidth;
            le.flexibleWidth = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 12f, bold: true);
            tmp.text = text;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(4, 1);
            lrt.offsetMax = new Vector2(-4, -1);

            return btn;
        }

        private TextMeshProUGUI AddToolbarLabel(RectTransform parent, string text,
            float fontSize = 13f, bool bold = false, float minWidth = 60)
        {
            var go = new GameObject($"TLbl_{text}");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize, bold);
            tmp.text  = text;
            tmp.color = bold ? StyleHelper.TextPrimary : StyleHelper.TextSecondary;
            tmp.alignment = TextAlignmentOptions.Left;
            go.AddComponent<LayoutElement>().minWidth = minWidth;
            return tmp;
        }

        private void AddToolbarSep(RectTransform parent)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = StyleHelper.DarkBorder;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth  = 1;
            le.minHeight = 30;
        }

        private LayoutElement MakeToolbarSpacer(RectTransform parent, float width)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = width;
            return le;
        }

        // ── Sidebar ───────────────────────────────────────────────────────

        private void BuildSidebar(RectTransform sidebar)
        {
            // Outer scroll so sidebar content can be taller than the window
            var scrollView = UIBuilder.MakeScrollView(sidebar,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            var sideContent = scrollView.content;

            var layout = sideContent.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 10, 14);
            layout.spacing = 5;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            // Override the default VLG added by MakeScrollView
            // (MakeScrollView adds its own; remove it first)
            var existingVlg = sideContent.gameObject.GetComponent<VerticalLayoutGroup>();
            // existingVlg was already assigned via layout above — no duplicate needed.

            // ── Section: Tab row ──────────────────────────────────────────
            var tabRow = MakeSideRow(sideContent);
            tabRow.gameObject.GetComponent<LayoutElement>().minHeight = 34;
            _tabPlatforms   = AddRowButton(tabRow, "Platforms",   StyleHelper.Blue);
            _tabEnvironment = AddRowButton(tabRow, "Environment", StyleHelper.DarkBlue);
            _tabPlatforms.onClick.AddListener(()   => ShowTab(true));
            _tabEnvironment.onClick.AddListener(() => ShowTab(false));

            AddSideDivider(sideContent);

            // ── Platforms tab ─────────────────────────────────────────────
            var platformsGo     = new GameObject("Tab_Platforms");
            platformsGo.transform.SetParent(sideContent, false);
            var platformsLayout = platformsGo.AddComponent<VerticalLayoutGroup>();
            platformsLayout.spacing             = 5;
            platformsLayout.childForceExpandWidth  = true;
            platformsLayout.childForceExpandHeight = false;
            _platformsTab = platformsGo;

            var platformsRt = platformsGo.GetComponent<RectTransform>();

            // Map name section
            AddSideLabel(platformsRt, "MAP NAME", bold: true, sectionHeader: true);
            _mapNameField = AddSideInputField(platformsRt, "Map name...");
            _mapNameField.onEndEdit.AddListener(name => _ctrl.CurrentMap.Name = name);

            // Save / Load row
            var saveRow = MakeSideRow(platformsRt);
            var saveBtn = AddRowButton(saveRow, "Save", StyleHelper.Blue);
            saveBtn.onClick.AddListener(OnSave);
            var loadBtn = AddRowButton(saveRow, "Load", StyleHelper.DarkBlue);
            loadBtn.onClick.AddListener(OnLoad);

            AddSideDivider(platformsRt);

            // Push to lobby
            var lobbyBtn = AddSideButton(platformsRt, "▶  Push to Lobby", StyleHelper.Orange);
            lobbyBtn.onClick.AddListener(OnPushToLobby);

            AddSideDivider(platformsRt);

            // Platform count chip
            var countRow = MakeSideRow(platformsRt);
            countRow.gameObject.GetComponent<LayoutElement>().minHeight = 22;
            _platformCountLabel = AddRowLabel(countRow, "Platforms: 0");
            _platformCountLabel.color = StyleHelper.TextSecondary;

            AddSideDivider(platformsRt);

            // ── Selected platform properties ──────────────────────────────
            AddSideLabel(platformsRt, "SELECTED", bold: true, sectionHeader: true);
            _selectedInfoLabel = AddSideLabel(platformsRt, "None selected");
            _selectedInfoLabel.color = StyleHelper.TextMuted;
            _selectedInfoLabel.fontSize = 12f;

            // X / Y
            var xyRow = MakeSideRow(platformsRt);
            AddRowLabel(xyRow, "X");
            _propX = AddRowInput(xyRow, "0.00");
            AddRowLabel(xyRow, "Y");
            _propY = AddRowInput(xyRow, "0.00");

            // W (halfW) / H (halfH)
            var whRow = MakeSideRow(platformsRt);
            AddRowLabel(whRow, "W");
            _propHW = AddRowInput(whRow, "8.00");
            AddRowLabel(whRow, "H");
            _propHH = AddRowInput(whRow, "1.50");

            // Radius / Rotation
            var rrRow = MakeSideRow(platformsRt);
            AddRowLabel(rrRow, "Rad");
            _propRadius = AddRowInput(rrRow, "1.00");
            AddRowLabel(rrRow, "Rot");
            _propRotation = AddRowInput(rrRow, "0.00");

            WirePropertyField(_propX,        v => UpdateSelectedPlatform(x: v));
            WirePropertyField(_propY,        v => UpdateSelectedPlatform(y: v));
            WirePropertyField(_propHW,       v => UpdateSelectedPlatform(hw: v));
            WirePropertyField(_propHH,       v => UpdateSelectedPlatform(hh: v));
            WirePropertyField(_propRadius,   v => UpdateSelectedPlatform(radius: v));
            WirePropertyField(_propRotation, v => UpdateSelectedPlatform(rotation: v));

            // Movement
            AddSideDivider(platformsRt);
            AddSideLabel(platformsRt, "MOVEMENT", bold: true, sectionHeader: true);
            _movPanel = MovementPanel.Create(platformsRt, () => {
                _canvasCtrl.RefreshMovementPreview();
            });

            // Delete platform
            AddSideDivider(platformsRt);
            var delBtn = AddSideButton(platformsRt, "Delete Platform",
                StyleHelper.DangerColor);
            delBtn.onClick.AddListener(() => {
                _ctrl.DeleteSelected();
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // New map
            AddSideDivider(platformsRt);
            var newBtn = AddSideButton(platformsRt, "+ New Map", StyleHelper.DarkBlue);
            newBtn.onClick.AddListener(() => {
                _ctrl.NewMap();
                _mapNameField.text = _ctrl.CurrentMap.Name;
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // ── Environment tab ───────────────────────────────────────────
            var envGo = new GameObject("Tab_Environment");
            envGo.transform.SetParent(sideContent, false);
            var envRt = envGo.AddComponent<RectTransform>();
            envGo.AddComponent<LayoutElement>().flexibleHeight = 1;
            var envScroll = UIBuilder.MakeScrollView(envRt,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _environmentTab = envGo;

            _envPanel = EnvironmentPanel.Create(envScroll.content, () => { });

            ShowTab(true);
        }

        // ── Sidebar helpers ───────────────────────────────────────────────

        private TextMeshProUGUI AddSideLabel(RectTransform parent, string text,
            bool bold = false, bool sectionHeader = false)
        {
            var go  = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            float size = sectionHeader ? 11f : (bold ? 13f : 12f);
            StyleHelper.StyleText(tmp, size, bold);
            tmp.text      = text;
            tmp.alignment = TextAlignmentOptions.Left;
            if (sectionHeader)
            {
                tmp.color = StyleHelper.TextMuted;
                // Letter-spacing makes section headers read cleaner at small sizes
                tmp.characterSpacing = 1.5f;
            }
            go.AddComponent<LayoutElement>().minHeight = sectionHeader ? 18 : (bold ? 20 : 16);
            return tmp;
        }

        private TMP_InputField AddSideInputField(RectTransform parent, string placeholder)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = 34;

            var bg    = go.AddComponent<Image>();
            bg.color  = StyleHelper.DarkElevated;
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type   = Image.Type.Sliced;

            var field = go.AddComponent<TMP_InputField>();

            var phGo  = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 13f);
            phTmp.color     = StyleHelper.TextMuted;
            phTmp.text      = placeholder;
            phTmp.alignment = TextAlignmentOptions.Left;
            SetFullRect(phGo, 10, 3);

            var textGo  = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(textTmp, 13f);
            textTmp.color     = StyleHelper.TextPrimary;
            textTmp.alignment = TextAlignmentOptions.Left;
            SetFullRect(textGo, 10, 3);

            field.textViewport  = textGo.GetComponent<RectTransform>();
            field.textComponent = textTmp;
            field.placeholder   = phTmp;
            field.caretColor    = StyleHelper.White;
            return field;
        }

        private Button AddSideButton(RectTransform parent, string text, Color color)
        {
            var btn = UIBuilder.MakeButton(parent, text, color, new Vector2(240f, 34f), Vector2.zero);
            var le  = btn.gameObject.GetComponent<LayoutElement>() ??
                      btn.gameObject.AddComponent<LayoutElement>();
            le.minHeight = 34;
            return btn;
        }

        private RectTransform MakeSideRow(RectTransform parent)
        {
            var go  = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing             = 5;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;
            go.AddComponent<LayoutElement>().minHeight = 30;
            return go.GetComponent<RectTransform>();
        }

        private Button AddRowButton(RectTransform row, string text, Color color)
        {
            var btn = UIBuilder.MakeButton(row, text, color, Vector2.zero, Vector2.zero);
            btn.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            return btn;
        }

        private TextMeshProUGUI AddRowLabel(RectTransform row, string text)
        {
            var go  = new GameObject($"RL_{text}");
            go.transform.SetParent(row, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 11f);
            tmp.text      = text;
            tmp.color     = StyleHelper.TextSecondary;
            tmp.alignment = TextAlignmentOptions.Right;
            go.AddComponent<LayoutElement>().minWidth = 28;
            return tmp;
        }

        private TMP_InputField AddRowInput(RectTransform row, string value)
        {
            var field = AddSideInputField(row, value);
            field.text = value;
            var le = field.gameObject.GetComponent<LayoutElement>() ??
                     field.gameObject.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight     = 28;
            return field;
        }

        private void AddSideDivider(RectTransform parent)
        {
            var go  = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = StyleHelper.DarkBorder;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 1;
        }

        private void SetFullRect(GameObject go, float padX = 0, float padY = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        // ── Tab switching ─────────────────────────────────────────────────

        private void ShowTab(bool platforms)
        {
            _platformsTab.SetActive(platforms);
            _environmentTab.SetActive(!platforms);

            var pImg = _tabPlatforms.GetComponent<Image>();
            var eImg = _tabEnvironment.GetComponent<Image>();

            if (pImg != null)
                pImg.color = platforms ? StyleHelper.Blue : StyleHelper.Blue * 0.45f;
            if (eImg != null)
                eImg.color = !platforms ? StyleHelper.DarkBlue : StyleHelper.DarkBlue * 0.6f;
        }

        // ── In-editor browser panel ───────────────────────────────────────

        private void BuildBrowserPanel(RectTransform parent)
        {
            _browserPanel = new GameObject("BrowserPanel");
            _browserPanel.transform.SetParent(parent, false);

            // Dim overlay
            var overlay = _browserPanel.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.68f);
            var ort = _browserPanel.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero;
            ort.offsetMax = Vector2.zero;

            // Dialog box (420 × 520)
            var box = UIBuilder.Panel(ort, "BrowserBox",
                new Color(0.09f, 0.11f, 0.18f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-210f, -260f), new Vector2(210f, 260f));

            // Top accent stripe
            var accentGo = new GameObject("TopAccent");
            accentGo.transform.SetParent(box, false);
            var atImg = accentGo.AddComponent<Image>();
            atImg.color = StyleHelper.Blue;
            var atRt = accentGo.GetComponent<RectTransform>();
            atRt.anchorMin = new Vector2(0f, 1f);
            atRt.anchorMax = Vector2.one;
            atRt.offsetMin = new Vector2(0f, -3f);
            atRt.offsetMax = Vector2.zero;

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(16, 16, 16, 16);
            layout.spacing = 10;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            // Title
            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(box, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 16f, bold: true);
            titleTmp.text = "LOAD MAP";
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleGo.AddComponent<LayoutElement>().minHeight = 24;

            UIBuilder.AddRule(box, StyleHelper.DarkBorder);

            // Scroll list
            var scroll   = UIBuilder.MakeScrollView(box,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0f, -80f));
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight      = 320;
            _browserContent = scroll.content;

            // Cancel button
            var cancelBtn = AddSideButton(box, "Cancel", StyleHelper.DarkBlue);
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
            _platformCountLabel.text = $"Saved! ({_ctrl.CurrentMap.Platforms.Count} platforms)";
            Plugin.Log.LogInfo($"[MapEditorWindow] Saved map '{name}'");
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

                var row = MakeSideRow(_browserContent);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 36;

                // Add a subtle alternating background strip
                var rowBg = row.gameObject.AddComponent<Image>();
                rowBg.color = new Color(1f, 1f, 1f, 0.03f);

                var lbl = new GameObject("Label");
                lbl.transform.SetParent(row, false);
                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(tmp, 13f);
                tmp.text      = captureName;
                tmp.color     = StyleHelper.TextPrimary;
                tmp.alignment = TextAlignmentOptions.Left;
                lbl.AddComponent<LayoutElement>().flexibleWidth = 1;

                var loadBtn = AddRowButton(row, "Load", StyleHelper.Blue);
                loadBtn.GetComponent<LayoutElement>().minWidth = 64;
                loadBtn.onClick.AddListener(() => {
                    _ctrl.LoadFromFile(captureName);
                    _mapNameField.text = _ctrl.CurrentMap.Name;
                    _canvasCtrl.Refresh();
                    RefreshSidebar();
                    _browserPanel.SetActive(false);
                });

                var delBtn = AddRowButton(row, "Del", StyleHelper.DangerColor);
                delBtn.GetComponent<LayoutElement>().minWidth = 48;
                delBtn.onClick.AddListener(() => {
                    MapSerializer.DeleteMap(captureName);
                    PopulateBrowser();
                });
            }
        }

        // ── Sidebar property sync ─────────────────────────────────────────

        public void RefreshSidebar()
        {
            _platformCountLabel.text = $"Platforms: {_ctrl.CurrentMap.Platforms.Count}";
            int  sel   = _ctrl.SelectedPlatformIndex;
            bool hasSel = sel >= 0 && sel < _ctrl.CurrentMap.Platforms.Count;

            if (hasSel)
            {
                var p = _ctrl.CurrentMap.Platforms[sel];
                _selectedInfoLabel.text = $"#{sel + 1}  {StyleHelper.PlatformNames[Mathf.Clamp(p.Type, 0, 5)]}";
                _selectedInfoLabel.color = StyleHelper.TextSecondary;
                _movPanel.SetData(p);
                _canvasCtrl.RefreshMovementPreview();
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
                if (float.TryParse(val, out float f)) setter(f);
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
                bool  active = i == (int)_ctrl.ActiveTool;
                var   img    = _toolButtons[i].GetComponent<Image>();
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
                        : StyleHelper.PlatformColors[i] * 0.42f;
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
                        : StyleHelper.ThemeColors[i] * 0.42f;
            }
        }
    }

    // Extension helper
    internal static class Extensions
    {
        public static T Let<T>(this T obj, System.Action<T> action) { action(obj); return obj; }
    }
}

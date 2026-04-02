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
    // Toolbar at top, sidebar on right (absolute-positioned, no VLG), transparent canvas viewport.
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
        private readonly List<Button> _typeButtons  = new List<Button>();
        private readonly List<Button> _themeButtons = new List<Button>();
        private readonly List<Button> _toolButtons  = new List<Button>();

        // Undo/Redo/Snap toolbar buttons
        private Button _undoBtn = null!;
        private Button _redoBtn = null!;
        private Button _snapBtn = null!;

        // Tab fields — kept for API compatibility but wired to dummy objects
        private GameObject      _platformsTab   = null!;
        private GameObject      _environmentTab = null!;
        private EnvironmentPanel _envPanel      = null!;
        private MovementPanel    _movPanel      = null!;
        private Button           _tabPlatforms  = null!;
        private Button           _tabEnvironment = null!;

        // In-editor load browser (modal panel)
        private GameObject      _browserPanel   = null!;
        private RectTransform   _browserContent = null!;

        // Layout constants (at 1280×720 reference)
        private const float TOOLBAR_H  = 54f;
        private const float SIDEBAR_W  = 220f;

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
            _slideAnimator.AnimateOut(() => {
                _ctrl.Close();
                gameObject.SetActive(false);
            });
        }

        // ── Build UI ──────────────────────────────────────────────────────

        private void BuildUI()
        {
            var root = _canvas.GetComponent<RectTransform>();

            // Fully transparent root background — game shows through
            var bg = UIBuilder.FlatPanel(root, "Background", Color.clear,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // ── Toolbar (top strip) ───────────────────────────────────────
            var toolbar = UIBuilder.FlatPanel(bg, "Toolbar",
                new Color(0.05f, 0.08f, 0.15f, 0.92f),
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

            // ── Right sidebar (absolute-positioned, dark background) ──────
            var sidebarGo = new GameObject("Sidebar");
            sidebarGo.transform.SetParent(main, false);
            var sidebarImg = sidebarGo.AddComponent<Image>();
            sidebarImg.color = new Color(0.06f, 0.10f, 0.18f, 0.93f);
            var sidebarRt = sidebarGo.GetComponent<RectTransform>();
            sidebarRt.anchorMin = new Vector2(1f, 0f);
            sidebarRt.anchorMax = Vector2.one;
            sidebarRt.offsetMin = new Vector2(-SIDEBAR_W, 0f);
            sidebarRt.offsetMax = Vector2.zero;
            BuildSidebar(sidebarRt);

            // Sidebar left-edge border
            var sbBorder = new GameObject("SidebarBorder");
            sbBorder.transform.SetParent(main, false);
            var sbBorderImg = sbBorder.AddComponent<Image>();
            sbBorderImg.color = StyleHelper.DarkBorder;
            var sbBorderRt = sbBorder.GetComponent<RectTransform>();
            sbBorderRt.anchorMin = new Vector2(1f, 0f);
            sbBorderRt.anchorMax = Vector2.one;
            sbBorderRt.offsetMin = new Vector2(-SIDEBAR_W - 1f, 0f);
            sbBorderRt.offsetMax = new Vector2(-SIDEBAR_W, 0f);

            // ── Canvas viewport (transparent, left of sidebar) ─────────────
            var viewportGo = new GameObject("Viewport");
            viewportGo.transform.SetParent(main, false);
            var viewportImg = viewportGo.AddComponent<Image>();
            viewportImg.color = Color.clear;
            viewportImg.raycastTarget = true;
            var viewportRt = viewportGo.GetComponent<RectTransform>();
            viewportRt.anchorMin = Vector2.zero;
            viewportRt.anchorMax = Vector2.one;
            viewportRt.offsetMin = Vector2.zero;
            viewportRt.offsetMax = new Vector2(-SIDEBAR_W - 1f, 0f);

            // Input receiver — canvas events land here
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

            // Content panel (platform widgets parented here)
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(canvasCtrlGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = Vector2.zero;

            _canvasCtrl = canvasCtrlGo.AddComponent<EditorCanvasController>();
            _canvasCtrl.Init(_ctrl, ccRt, contentRt);

            // ── Close button — absolute, top-right of root canvas ─────────
            var closeBtnGo = new GameObject("CloseBtn");
            closeBtnGo.transform.SetParent(root, false);
            var closeBtnImg = closeBtnGo.AddComponent<Image>();
            closeBtnImg.color = new Color(0.78f, 0.14f, 0.14f, 1f);
            closeBtnImg.sprite = StyleHelper.GetButtonSprite();
            closeBtnImg.type = Image.Type.Sliced;
            var closeBtnBtn = closeBtnGo.AddComponent<Button>();
            StyleHelper.StyleButton(closeBtnBtn, new Color(0.78f, 0.14f, 0.14f, 1f));
            StyleHelper.AddPressColorSwap(closeBtnBtn);
            var closeBtnRt = closeBtnGo.GetComponent<RectTransform>();
            closeBtnRt.anchorMin = new Vector2(1f, 1f);
            closeBtnRt.anchorMax = new Vector2(1f, 1f);
            closeBtnRt.pivot     = new Vector2(1f, 1f);
            closeBtnRt.sizeDelta = new Vector2(48f, 48f);
            closeBtnRt.anchoredPosition = new Vector2(-6f, -4f);
            var closeLblGo = new GameObject("Label");
            closeLblGo.transform.SetParent(closeBtnGo.transform, false);
            var closeTmp = closeLblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(closeTmp, 18f, bold: true);
            closeTmp.text = "✕";
            closeTmp.raycastTarget = false;
            var closeLblRt = closeLblGo.GetComponent<RectTransform>();
            closeLblRt.anchorMin = Vector2.zero;
            closeLblRt.anchorMax = Vector2.one;
            closeLblRt.offsetMin = Vector2.zero;
            closeLblRt.offsetMax = Vector2.zero;
            closeBtnBtn.onClick.AddListener(Close);

            // ── Dummy objects for tab fields (API compatibility) ───────────
            _platformsTab = new GameObject("Tab_Platforms_Dummy");
            _platformsTab.transform.SetParent(bg, false);
            _environmentTab = new GameObject("Tab_Environment_Dummy");
            _environmentTab.transform.SetParent(bg, false);

            // ── In-editor load browser (modal) ────────────────────────────
            BuildBrowserPanel(bg);
        }

        // ── Toolbar ───────────────────────────────────────────────────────

        private void BuildToolbar(RectTransform toolbar)
        {
            var layout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(12, 62, 7, 7);  // right pad leaves room for close btn
            layout.spacing = 7;
            layout.childAlignment      = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth  = false;

            // Title — bold, game-style all-caps
            AddToolbarLabel(toolbar, "✏ MAP EDITOR", 16f, bold: true, minWidth: 120);

            AddToolbarSep(toolbar);

            // ── Tool mode buttons ─────────────────────────────────────────
            string[] toolNames  = { "Select", "Place", "Delete" };
            Color[]  toolColors = {
                StyleHelper.Blue,
                StyleHelper.Orange,
                new Color(0.72f, 0.20f, 0.20f, 1f)
            };
            for (int i = 0; i < toolNames.Length; i++)
            {
                int idx = i;
                var btn = AddToolbarButton(toolbar, toolNames[i], toolColors[i], minWidth: 60);
                btn.onClick.AddListener(() => SetTool(idx));
                _toolButtons.Add(btn);
            }

            AddToolbarSep(toolbar);

            // ── Block type buttons ────────────────────────────────────────
            for (int i = 0; i < StyleHelper.PlatformNames.Length; i++)
            {
                int idx = i;
                var btn = AddToolbarButton(toolbar, StyleHelper.PlatformNames[i],
                    StyleHelper.PlatformColors[i], minWidth: 48);
                btn.onClick.AddListener(() => SetPlacePlatformType(idx));
                _typeButtons.Add(btn);
            }

            AddToolbarSep(toolbar);

            // ── Level theme buttons ───────────────────────────────────────
            for (int i = 0; i < StyleHelper.ThemeNames.Length; i++)
            {
                int idx = i;
                var btn = AddToolbarButton(toolbar, StyleHelper.ThemeNames[i],
                    StyleHelper.ThemeColors[i], minWidth: 48);
                btn.onClick.AddListener(() => SetTheme(idx));
                _themeButtons.Add(btn);
            }

            AddToolbarSep(toolbar);

            // ── Undo / Redo buttons ────────────────────────────────────────
            _undoBtn = AddToolbarButton(toolbar, "↩ Undo", StyleHelper.DarkBlue, minWidth: 56);
            _undoBtn.onClick.AddListener(OnUndo);
            _redoBtn = AddToolbarButton(toolbar, "↪ Redo", StyleHelper.DarkBlue, minWidth: 56);
            _redoBtn.onClick.AddListener(OnRedo);

            AddToolbarSep(toolbar);

            // ── Snap to grid toggle ────────────────────────────────────────
            _snapBtn = AddToolbarButton(toolbar, "⊞ SNAP", new Color(0.25f, 0.60f, 0.30f, 1f), minWidth: 64);
            _snapBtn.onClick.AddListener(() => {
                _ctrl.SnapToGrid = !_ctrl.SnapToGrid;
                UpdateSnapHighlight(_snapBtn);
            });

            // Dummy tab buttons (API compatibility — never shown in toolbar)
            var dummyGo = new GameObject("TabBtns_Dummy");
            dummyGo.transform.SetParent(toolbar, false);
            dummyGo.AddComponent<LayoutElement>().minWidth = 0;
            _tabPlatforms   = dummyGo.AddComponent<Button>();
            _tabEnvironment = dummyGo.AddComponent<Button>();

            UpdateToolHighlights();
            UpdateSnapHighlight(_snapBtn);
        }

        private Button AddToolbarButton(RectTransform parent, string text, Color color,
            float minWidth = 60)
        {
            var go = new GameObject("TBtn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color = color;
            img.sprite = StyleHelper.GetButtonSprite();
            img.type  = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth   = minWidth;
            le.flexibleWidth = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 12f, bold: true);
            tmp.text      = text;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
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

        private void AddToolbarSep(RectTransform parent)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = StyleHelper.DarkBorder;
            var le = go.AddComponent<LayoutElement>();
            le.minWidth  = 1;
            le.minHeight = 32;
            le.flexibleHeight = 0;
        }

        // ── Sidebar (absolute positioning, no VerticalLayoutGroup) ────────

        private void BuildSidebar(RectTransform sidebar)
        {
            float padX = 16f;
            float innerW = SIDEBAR_W - padX * 2f;
            float x0 = padX;

            // "MAP NAME" section label — all-caps muted game style
            {
                var lbl = MakeAbsSideLabel(sidebar, "MAP NAME", x0, innerW, -20f, 16f, bold: true, muted: true);
                StyleHelper.StyleTextAllCaps(lbl, 12f);
            }

            // Map name input field
            _mapNameField = MakeAbsInputField(sidebar, "Map name...", x0, innerW, -52f, 32f);
            _mapNameField.onEndEdit.AddListener(n => _ctrl.CurrentMap.Name = n);

            // Save / Load row
            float btnW = (innerW - 8f) * 0.5f;
            var saveBtn = MakeAbsButton(sidebar, "Save", StyleHelper.Blue,
                new Vector2(x0, -92f), new Vector2(btnW, 32f));
            saveBtn.onClick.AddListener(OnSave);
            var loadBtn = MakeAbsButton(sidebar, "Load", StyleHelper.DarkBlue,
                new Vector2(x0 + btnW + 8f, -92f), new Vector2(btnW, 32f));
            loadBtn.onClick.AddListener(OnLoad);

            // Push to lobby (full width)
            var lobbyBtn = MakeAbsButton(sidebar, "▶  Push to Lobby", StyleHelper.Orange,
                new Vector2(x0, -136f), new Vector2(innerW, 32f));
            lobbyBtn.onClick.AddListener(OnPushToLobby);

            // Divider
            MakeAbsDivider(sidebar, -176f, innerW, x0);

            // PLATFORMS: N label — colored dot prefix, muted
            _platformCountLabel = MakeAbsSideLabel(sidebar, "● Platforms: 0", x0, innerW, -192f, 16f);
            _platformCountLabel.color = StyleHelper.TextSecondary;
            _platformCountLabel.fontSize = 12f;

            // Divider
            MakeAbsDivider(sidebar, -216f, innerW, x0);

            // SELECTED section label — all-caps muted
            {
                var lbl = MakeAbsSideLabel(sidebar, "● SELECTED", x0, innerW, -232f, 16f, bold: true, muted: true);
                StyleHelper.StyleTextAllCaps(lbl, 11f);
            }

            // Selected info label
            _selectedInfoLabel = MakeAbsSideLabel(sidebar, "None selected", x0, innerW, -252f, 18f);
            _selectedInfoLabel.color = StyleHelper.TextMuted;
            _selectedInfoLabel.fontSize = 12f;

            // X/Y row — X=red-ish, Y=green-ish axis colors
            float lblW = 20f;
            float inputW = (innerW - lblW * 2f - 8f) * 0.5f;
            {
                var xl = MakeAbsSideLabel(sidebar, "X", x0, lblW, -278f, 24f);
                xl.color = new Color(0.95f, 0.45f, 0.45f, 1f);
                xl.fontStyle = FontStyles.Bold;
            }
            _propX = MakeAbsInputField(sidebar, "0.00", x0 + lblW + 2f, inputW, -278f, 24f);
            {
                var yl = MakeAbsSideLabel(sidebar, "Y", x0 + lblW + 2f + inputW + 4f, lblW, -278f, 24f);
                yl.color = new Color(0.45f, 0.90f, 0.55f, 1f);
                yl.fontStyle = FontStyles.Bold;
            }
            _propY = MakeAbsInputField(sidebar, "0.00", x0 + lblW * 2f + 2f + inputW + 4f + 2f, inputW, -278f, 24f);

            // W/H row — W/H=blue-ish
            {
                var wl = MakeAbsSideLabel(sidebar, "W", x0, lblW, -310f, 24f);
                wl.color = new Color(0.45f, 0.70f, 1.00f, 1f);
                wl.fontStyle = FontStyles.Bold;
            }
            _propHW = MakeAbsInputField(sidebar, "8.00", x0 + lblW + 2f, inputW, -310f, 24f);
            {
                var hl = MakeAbsSideLabel(sidebar, "H", x0 + lblW + 2f + inputW + 4f, lblW, -310f, 24f);
                hl.color = new Color(0.45f, 0.70f, 1.00f, 1f);
                hl.fontStyle = FontStyles.Bold;
            }
            _propHH = MakeAbsInputField(sidebar, "1.50", x0 + lblW * 2f + 2f + inputW + 4f + 2f, inputW, -310f, 24f);

            // Rad/Rot row — use same layout formula as X/Y and W/H
            MakeAbsSideLabel(sidebar, "Rad", x0, lblW, -340f, 24f);
            _propRadius = MakeAbsInputField(sidebar, "1.00", x0 + lblW + 2f, inputW, -340f, 24f);
            MakeAbsSideLabel(sidebar, "Rot", x0 + lblW + 2f + inputW + 4f, lblW, -340f, 24f);
            _propRotation = MakeAbsInputField(sidebar, "0.00", x0 + lblW * 2f + 2f + inputW + 4f + 2f, inputW, -340f, 24f);

            WirePropertyField(_propX,        v => UpdateSelectedPlatform(x: v));
            WirePropertyField(_propY,        v => UpdateSelectedPlatform(y: v));
            WirePropertyField(_propHW,       v => UpdateSelectedPlatform(hw: v));
            WirePropertyField(_propHH,       v => UpdateSelectedPlatform(hh: v));
            WirePropertyField(_propRadius,   v => UpdateSelectedPlatform(radius: v));
            WirePropertyField(_propRotation, v => UpdateSelectedPlatform(rotation: v));

            // Divider
            MakeAbsDivider(sidebar, -374f, innerW, x0);

            // Delete Platform button
            var delBtn = MakeAbsButton(sidebar, "Delete Platform", StyleHelper.DangerColor,
                new Vector2(x0, -390f), new Vector2(innerW, 32f));
            delBtn.onClick.AddListener(() => {
                _ctrl.DeleteSelected();
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // Divider
            MakeAbsDivider(sidebar, -430f, innerW, x0);

            // + New Map button
            var newBtn = MakeAbsButton(sidebar, "+ New Map", StyleHelper.DarkBlue,
                new Vector2(x0, -446f), new Vector2(innerW, 32f));
            newBtn.onClick.AddListener(() => {
                _ctrl.NewMap();
                _mapNameField.text = _ctrl.CurrentMap.Name;
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // _movPanel kept null intentionally — movement panel omitted in this redesign.
            // RefreshSidebar guards the _movPanel call.
        }

        // ── Sidebar absolute-position helpers ─────────────────────────────

        // anchoredPosition uses pivot at top-left of sidebar (anchorMin=anchorMax=(0,1))
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
            _platformCountLabel.text = "● Saved! (" + _ctrl.CurrentMap.Platforms.Count + " platforms)";
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
            _platformCountLabel.text = "● Platforms: " + _ctrl.CurrentMap.Platforms.Count;
            int  sel   = _ctrl.SelectedPlatformIndex;
            bool hasSel = sel >= 0 && sel < _ctrl.CurrentMap.Platforms.Count;

            if (hasSel)
            {
                var p = _ctrl.CurrentMap.Platforms[sel];
                _selectedInfoLabel.text  = "#" + (sel + 1) + "  " + StyleHelper.PlatformNames[Mathf.Clamp(p.Type, 0, 5)];
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
                bool  active = i == (int)_ctrl.ActiveTool;
                var   img    = _toolButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = active ? tc[i] : tc[i] * 0.50f;
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
                        : StyleHelper.PlatformColors[i] * 0.50f;
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
                        : StyleHelper.ThemeColors[i] * 0.50f;
            }
        }

        private void UpdateSnapHighlight(Button btn)
        {
            var img = btn.GetComponent<Image>();
            if (img == null) return;
            var snapOnColor  = new Color(0.25f, 0.60f, 0.30f, 1f);
            var snapOffColor = snapOnColor * 0.50f;
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
        }
    }

    // Extension helper
    internal static class Extensions
    {
        public static T Let<T>(this T obj, System.Action<T> action) { action(obj); return obj; }
    }
}

using System.Collections.Generic;
using BoplMapEditor.Data;
using BoplMapEditor.Core;
using BoplMapEditor.Sync;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // The full-screen map editor window built with UGUI.
    // Opened by the lobby button. Matches the game's visual style.
    public class MapEditorWindow : MonoBehaviour
    {
        private MapEditorController _ctrl = null!;
        private EditorCanvasController _canvasCtrl = null!;
        private Canvas _canvas = null!;
        private SlideAnimator _slideAnimator = null!;

        // Sidebar elements
        private TMP_InputField _mapNameField = null!;
        private TextMeshProUGUI _platformCountLabel = null!;
        private TextMeshProUGUI _selectedInfoLabel = null!;
        private TMP_InputField _propX = null!, _propY = null!;
        private TMP_InputField _propHW = null!, _propHH = null!;
        private TMP_InputField _propRadius = null!, _propRotation = null!;
        private readonly List<Button> _typeButtons = new();
        private readonly List<Button> _themeButtons = new();
        private readonly List<Button> _toolButtons = new();

        // Sidebar tabs
        private GameObject _platformsTab = null!;
        private GameObject _environmentTab = null!;
        private EnvironmentPanel _envPanel = null!;
        private MovementPanel _movPanel = null!;
        private Button _tabPlatforms = null!;
        private Button _tabEnvironment = null!;

        // Map browser
        private GameObject _browserPanel = null!;
        private RectTransform _browserContent = null!;

        public static MapEditorWindow Create(MapEditorController ctrl)
        {
            var canvas = UIBuilder.CreateCanvas("MapEditorCanvas", sortOrder: 200);
            var window = canvas.gameObject.AddComponent<MapEditorWindow>();
            window._canvas = canvas;
            window._ctrl = ctrl;
            window.BuildUI();

            // Add slide-in animation (matches AnimateInOutUI)
            window._slideAnimator = canvas.gameObject.AddComponent<SlideAnimator>();
            window._slideAnimator.Target = canvas.GetComponent<RectTransform>();
            window._slideAnimator.OffscreenY = 1200f;

            window.gameObject.SetActive(false);
            return window;
        }

        public void Open(MapData? map = null)
        {
            // Scan game assets now that scene is loaded
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

            // Dark background overlay
            var bg = UIBuilder.Panel(root, "Background", StyleHelper.DarkPanel,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

            // Top toolbar (height 54px)
            var toolbar = UIBuilder.Panel(bg, "Toolbar",
                new Color(0.06f, 0.08f, 0.16f, 1f),
                new Vector2(0, 1), Vector2.one,
                new Vector2(0, -54), Vector2.zero);
            BuildToolbar(toolbar);

            // Main area below toolbar
            var main = new GameObject("Main").AddComponent<RectTransform>();
            main.SetParent(bg, false);
            main.anchorMin = Vector2.zero;
            main.anchorMax = new Vector2(1, 1);
            main.offsetMin = new Vector2(0, 0);
            main.offsetMax = new Vector2(0, -54);

            // Sidebar (260px on right)
            var sidebar = UIBuilder.Panel(main, "Sidebar",
                new Color(0.06f, 0.09f, 0.15f, 1f),
                new Vector2(1, 0), Vector2.one,
                new Vector2(-260, 0), Vector2.zero);
            BuildSidebar(sidebar);

            // Canvas viewport (rest of screen)
            var viewport = UIBuilder.Panel(main, "Viewport",
                new Color(0.05f, 0.06f, 0.10f, 1f),
                Vector2.zero, Vector2.one,
                Vector2.zero, new Vector2(-260, 0));

            // Add raycast target for click events
            var vpGo = viewport.gameObject;
            var canvasCtrlGo = new GameObject("CanvasController");
            canvasCtrlGo.transform.SetParent(viewport, false);
            var ccRt = canvasCtrlGo.AddComponent<RectTransform>();
            ccRt.anchorMin = Vector2.zero;
            ccRt.anchorMax = Vector2.one;
            ccRt.offsetMin = Vector2.zero;
            ccRt.offsetMax = Vector2.zero;
            var ccImg = canvasCtrlGo.AddComponent<Image>();
            ccImg.color = Color.clear;

            // Content panel (platforms live here)
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(canvasCtrlGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0.5f, 0.5f);
            contentRt.anchorMax = new Vector2(0.5f, 0.5f);
            contentRt.sizeDelta = Vector2.zero;

            _canvasCtrl = canvasCtrlGo.AddComponent<EditorCanvasController>();
            _canvasCtrl.Init(_ctrl, ccRt, contentRt);

            // Map browser modal
            BuildBrowserPanel(bg);
        }

        private void BuildToolbar(RectTransform toolbar)
        {
            var layout = toolbar.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(10, 10, 8, 8);
            layout.spacing = 8;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            // Title
            AddLayoutLabel(toolbar, "MAP EDITOR", 20f, bold: true, width: 160);

            AddSeparator(toolbar);

            // Tool buttons
            AddLayoutLabel(toolbar, "Tool:", 14f, width: 40);
            string[] toolNames = { "Select", "Place", "Delete" };
            Color[] toolColors = { StyleHelper.Blue, StyleHelper.Orange, new Color(0.7f, 0.2f, 0.2f, 1f) };
            for (int i = 0; i < toolNames.Length; i++)
            {
                int idx = i;
                var btn = AddLayoutButton(toolbar, toolNames[i], toolColors[i], width: 80);
                btn.onClick.AddListener(() => SetTool(idx));
                _toolButtons.Add(btn);
            }

            AddSeparator(toolbar);

            // Platform type palette
            AddLayoutLabel(toolbar, "Block:", 14f, width: 45);
            for (int i = 0; i < StyleHelper.PlatformNames.Length; i++)
            {
                int idx = i;
                var btn = AddLayoutButton(toolbar, StyleHelper.PlatformNames[i],
                    StyleHelper.PlatformColors[i], width: 72);
                btn.onClick.AddListener(() => SetPlacePlatformType(idx));
                _typeButtons.Add(btn);
            }

            AddSeparator(toolbar);

            // Level theme
            AddLayoutLabel(toolbar, "Theme:", 14f, width: 50);
            for (int i = 0; i < StyleHelper.ThemeNames.Length; i++)
            {
                int idx = i;
                var btn = AddLayoutButton(toolbar, StyleHelper.ThemeNames[i],
                    StyleHelper.ThemeColors[i], width: 70);
                btn.onClick.AddListener(() => SetTheme(idx));
                _themeButtons.Add(btn);
            }

            // Flexible spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(toolbar, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Close button (right side)
            var closeBtn = AddLayoutButton(toolbar, "✕ Close",
                new Color(0.55f, 0.15f, 0.15f, 1f), width: 90);
            closeBtn.onClick.AddListener(Close);

            UpdateToolHighlights();
        }

        private void BuildSidebar(RectTransform sidebar)
        {
            var layout = sidebar.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 8, 12);
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Tab row
            var tabRow = AddRow(sidebar);
            tabRow.gameObject.AddComponent<LayoutElement>().minHeight = 36;
            _tabPlatforms = AddRowButton(tabRow, "Platforms", StyleHelper.Blue);
            _tabEnvironment = AddRowButton(tabRow, "Environment", StyleHelper.DarkBlue);
            _tabPlatforms.onClick.AddListener(() => ShowTab(true));
            _tabEnvironment.onClick.AddListener(() => ShowTab(false));

            AddDivider(sidebar);

            // ── Platforms tab content ─────────────────────────────────────
            var platformsGo = new GameObject("Tab_Platforms");
            platformsGo.transform.SetParent(sidebar, false);
            var platformsLayout = platformsGo.AddComponent<VerticalLayoutGroup>();
            platformsLayout.spacing = 6;
            platformsLayout.childForceExpandWidth = true;
            platformsLayout.childForceExpandHeight = false;
            _platformsTab = platformsGo;

            var platformsRt = platformsGo.GetComponent<RectTransform>();

            // Map name
            AddSideLabel(platformsRt, "MAP NAME", bold: true);
            _mapNameField = AddSideInputField(sidebar, "Map name...");
            _mapNameField.onEndEdit.AddListener(name => _ctrl.CurrentMap.Name = name);

            // Save / Load buttons
            var saveRow = AddRow(sidebar);
            var saveBtn = AddRowButton(saveRow, "Save", StyleHelper.Blue);
            saveBtn.onClick.AddListener(OnSave);
            var loadBtn = AddRowButton(saveRow, "Load", StyleHelper.DarkBlue);
            loadBtn.onClick.AddListener(OnLoad);

            AddDivider(sidebar);

            // Push to lobby
            var lobbyBtn = AddSideButton(sidebar, "▶  Push to Lobby", StyleHelper.Orange);
            lobbyBtn.onClick.AddListener(OnPushToLobby);

            AddDivider(sidebar);

            // Platform count
            _platformCountLabel = AddSideLabel(sidebar, "Platforms: 0");

            AddDivider(sidebar);

            // Selected platform properties
            AddSideLabel(sidebar, "SELECTED PLATFORM", bold: true);
            _selectedInfoLabel = AddSideLabel(sidebar, "None selected");

            var xyRow = AddRow(sidebar);
            AddRowLabel(xyRow, "X");
            _propX = AddRowInput(xyRow, "0.00");
            AddRowLabel(xyRow, "Y");
            _propY = AddRowInput(xyRow, "0.00");

            var whRow = AddRow(sidebar);
            AddRowLabel(whRow, "W");
            _propHW = AddRowInput(whRow, "8.00");
            AddRowLabel(whRow, "H");
            _propHH = AddRowInput(whRow, "1.50");

            var rrRow = AddRow(sidebar);
            AddRowLabel(rrRow, "Rad");
            _propRadius = AddRowInput(rrRow, "1.00");
            AddRowLabel(rrRow, "Rot");
            _propRotation = AddRowInput(rrRow, "0.00");

            // Wire property fields → update selected platform
            WirePropertyField(_propX,        v => UpdateSelectedPlatform(x: v));
            WirePropertyField(_propY,        v => UpdateSelectedPlatform(y: v));
            WirePropertyField(_propHW,       v => UpdateSelectedPlatform(hw: v));
            WirePropertyField(_propHH,       v => UpdateSelectedPlatform(hh: v));
            WirePropertyField(_propRadius,   v => UpdateSelectedPlatform(radius: v));
            WirePropertyField(_propRotation, v => UpdateSelectedPlatform(rotation: v));

            // Movement
            AddDivider(sidebar);
            AddSideLabel(sidebar, "MOVEMENT", bold: true);
            _movPanel = MovementPanel.Create(sidebar, () => {
                _canvasCtrl.RefreshMovementPreview();
            });

            // Delete button
            AddDivider(sidebar);
            var delBtn = AddSideButton(sidebar, "Delete Platform",
                new Color(0.65f, 0.15f, 0.15f, 1f));
            delBtn.onClick.AddListener(() => {
                _ctrl.DeleteSelected();
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // New map at bottom
            AddDivider(platformsRt);
            var newBtn = AddSideButton(platformsRt, "+ New Map", StyleHelper.DarkBlue);
            newBtn.onClick.AddListener(() => {
                _ctrl.NewMap();
                _mapNameField.text = _ctrl.CurrentMap.Name;
                _canvasCtrl.Refresh();
                RefreshSidebar();
            });

            // ── Environment tab content ───────────────────────────────────
            var envGo = new GameObject("Tab_Environment");
            envGo.transform.SetParent(sidebar, false);
            var envScroll = UIBuilder.MakeScrollView(envGo.GetComponent<RectTransform>() ?? envGo.AddComponent<RectTransform>(),
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            _environmentTab = envGo;

            _envPanel = EnvironmentPanel.Create(envScroll.content, () => {
                // Environment changed — nothing extra needed, saved with map
            });

            ShowTab(true); // Start on Platforms tab
        }

        private void ShowTab(bool platforms)
        {
            _platformsTab.SetActive(platforms);
            _environmentTab.SetActive(!platforms);

            var pImg = _tabPlatforms.GetComponent<Image>();
            var eImg = _tabEnvironment.GetComponent<Image>();
            if (pImg != null) pImg.color = platforms ? StyleHelper.Blue : StyleHelper.Blue * 0.5f;
            if (eImg != null) eImg.color = !platforms ? StyleHelper.DarkBlue : StyleHelper.DarkBlue * 0.7f;
        }

        private void BuildBrowserPanel(RectTransform parent)
        {
            // Modal overlay
            _browserPanel = new GameObject("BrowserPanel");
            _browserPanel.transform.SetParent(parent, false);
            var overlay = _browserPanel.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.6f);
            var ort = _browserPanel.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = Vector2.zero;
            ort.offsetMax = Vector2.zero;

            // Dialog box (400 x 500)
            var box = UIBuilder.Panel(ort, "BrowserBox",
                new Color(0.08f, 0.10f, 0.18f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-200, -250), new Vector2(200, 250));

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(12, 12, 12, 12);
            layout.spacing = 8;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            AddSideLabel(box, "LOAD MAP", bold: true);

            var scroll = UIBuilder.MakeScrollView(box,
                Vector2.zero, Vector2.one, Vector2.zero, new Vector2(0, -80));
            var scrollLe = scroll.gameObject.AddComponent<LayoutElement>();
            scrollLe.flexibleHeight = 1;
            scrollLe.minHeight = 300;
            _browserContent = scroll.content;

            var cancelBtn = AddSideButton(box, "Cancel", StyleHelper.DarkBlue);
            cancelBtn.onClick.AddListener(() => _browserPanel.SetActive(false));

            _browserPanel.SetActive(false);
        }

        // ── Sidebar helpers ───────────────────────────────────────────────

        private TextMeshProUGUI AddSideLabel(RectTransform parent, string text, bool bold = false)
        {
            var go = new GameObject("Label");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize: bold ? 14f : 13f, bold: bold);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Left;
            go.AddComponent<LayoutElement>().minHeight = bold ? 22 : 18;
            return tmp;
        }

        private TMP_InputField AddSideInputField(RectTransform parent, string placeholder)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = 32;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.2f, 1f);
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type = Image.Type.Sliced;

            var field = go.AddComponent<TMP_InputField>();

            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 13f);
            phTmp.color = new Color(0.4f, 0.4f, 0.4f);
            phTmp.text = placeholder;
            phTmp.alignment = TextAlignmentOptions.Left;
            SetFullRect(phGo, 8, 4);

            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(textTmp, 13f);
            textTmp.alignment = TextAlignmentOptions.Left;
            SetFullRect(textGo, 8, 4);

            field.textViewport = textGo.GetComponent<RectTransform>();
            field.textComponent = textTmp;
            field.placeholder = phTmp;
            field.caretColor = Color.white;
            return field;
        }

        private Button AddSideButton(RectTransform parent, string text, Color color)
        {
            var btn = UIBuilder.MakeButton(parent, text, color, new Vector2(236, 34), Vector2.zero);
            btn.GetComponent<LayoutElement>()?.Let(le => le.minHeight = 34);
            if (btn.GetComponent<LayoutElement>() == null)
                btn.gameObject.AddComponent<LayoutElement>().minHeight = 34;
            return btn;
        }

        private RectTransform AddRow(RectTransform parent)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
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
            var go = new GameObject($"Label_{text}");
            go.transform.SetParent(row, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 12f);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Right;
            go.AddComponent<LayoutElement>().minWidth = 28;
            return tmp;
        }

        private TMP_InputField AddRowInput(RectTransform row, string value)
        {
            var field = AddSideInputField(row, value);
            field.text = value;
            if (field.gameObject.GetComponent<LayoutElement>() == null)
                field.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            return field;
        }

        private void AddDivider(RectTransform parent)
        {
            var go = new GameObject("Divider");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.30f, 0.45f, 0.6f);
            go.AddComponent<LayoutElement>().minHeight = 1;
        }

        private void SetFullRect(GameObject go, float padX = 0, float padY = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padX, padY);
            rt.offsetMax = new Vector2(-padX, -padY);
        }

        // ── Toolbar helpers ───────────────────────────────────────────────

        private Button AddLayoutButton(RectTransform parent, string text, Color color, float width)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth = width;
            le.flexibleWidth = 0;

            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 13f, bold: true);
            tmp.text = text;
            SetFullRect(labelGo, 4, 2);

            return btn;
        }

        private TextMeshProUGUI AddLayoutLabel(RectTransform parent, string text,
            float fontSize = 14f, bool bold = false, float width = 100)
        {
            var go = new GameObject($"Label_{text}");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize, bold);
            tmp.text = text;
            go.AddComponent<LayoutElement>().minWidth = width;
            return tmp;
        }

        private void AddSeparator(RectTransform parent)
        {
            var go = new GameObject("Sep");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(0.25f, 0.30f, 0.45f, 0.5f);
            var le = go.AddComponent<LayoutElement>();
            le.minWidth = 1;
            le.minHeight = 38;
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
            _ctrl.SaveCurrentMap(name);
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
            // Clear existing entries
            foreach (Transform child in _browserContent)
                Destroy(child.gameObject);

            string[] maps = MapSerializer.ListMaps();
            foreach (var name in maps)
            {
                string captureName = name;
                var row = AddRow(_browserContent);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 36;

                var lbl = new GameObject("Label");
                lbl.transform.SetParent(row, false);
                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(tmp, 14f);
                tmp.text = captureName;
                tmp.alignment = TextAlignmentOptions.Left;
                lbl.AddComponent<LayoutElement>().flexibleWidth = 1;

                var loadBtn = AddRowButton(row, "Load", StyleHelper.Blue);
                loadBtn.GetComponent<LayoutElement>().minWidth = 70;
                loadBtn.onClick.AddListener(() => {
                    _ctrl.LoadFromFile(captureName);
                    _mapNameField.text = _ctrl.CurrentMap.Name;
                    _canvasCtrl.Refresh();
                    RefreshSidebar();
                    _browserPanel.SetActive(false);
                });

                var delBtn = AddRowButton(row, "Del", new Color(0.6f, 0.15f, 0.15f, 1f));
                delBtn.GetComponent<LayoutElement>().minWidth = 50;
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
            int sel = _ctrl.SelectedPlatformIndex;
            bool hasSel = sel >= 0 && sel < _ctrl.CurrentMap.Platforms.Count;

            if (hasSel)
            {
                var p = _ctrl.CurrentMap.Platforms[sel];
                _selectedInfoLabel.text = $"Platform #{sel + 1} · {StyleHelper.PlatformNames[Mathf.Clamp(p.Type, 0, 5)]}";
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
                _selectedInfoLabel.text = "None selected";
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
            if (x.HasValue)        p.X = x.Value;
            if (y.HasValue)        p.Y = y.Value;
            if (hw.HasValue)       p.HalfW = Mathf.Max(0.5f, hw.Value);
            if (hh.HasValue)       p.HalfH = Mathf.Max(0.5f, hh.Value);
            if (radius.HasValue)   p.Radius = radius.Value;
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
            for (int i = 0; i < _toolButtons.Count; i++)
            {
                bool active = i == (int)_ctrl.ActiveTool;
                var img = _toolButtons[i].GetComponent<Image>();
                if (img != null)
                {
                    Color[] tc = { StyleHelper.Blue, StyleHelper.Orange, new Color(0.7f, 0.2f, 0.2f, 1f) };
                    img.color = active ? tc[i] : tc[i] * 0.55f;
                }
            }
        }

        private void UpdateTypeHighlights()
        {
            for (int i = 0; i < _typeButtons.Count; i++)
            {
                bool active = i == _ctrl.PlacePlatformType;
                var img = _typeButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = active ? StyleHelper.PlatformColors[i] : StyleHelper.PlatformColors[i] * 0.5f;
            }
        }

        private void UpdateThemeHighlights()
        {
            for (int i = 0; i < _themeButtons.Count; i++)
            {
                bool active = i == _ctrl.CurrentMap.LevelTheme;
                var img = _themeButtons[i].GetComponent<Image>();
                if (img != null)
                    img.color = active ? StyleHelper.ThemeColors[i] : StyleHelper.ThemeColors[i] * 0.5f;
            }
        }
    }

    // Extension helper
    internal static class Extensions
    {
        public static T Let<T>(this T obj, System.Action<T> action) { action(obj); return obj; }
    }
}

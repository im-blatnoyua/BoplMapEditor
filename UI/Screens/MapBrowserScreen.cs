using System;
using System.Collections.Generic;
using BoplMapEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Full-screen map browser: shown when clicking "Map Editor" in lobby.
    // Dark, clean aesthetic — cards with thumbnail, name, platform count, edit/delete buttons.
    public class MapBrowserScreen : MonoBehaviour
    {
        private Canvas      _canvas      = null!;
        private SlideAnimator _slide     = null!;
        private RectTransform _grid      = null!;
        private MapEditorWindow _editorWindow = null!;

        // New-map dialog refs
        private GameObject    _newMapDialog    = null!;
        private TMP_InputField _newMapNameField = null!;
        private TextMeshProUGUI _newMapError   = null!;

        // Card dimensions — slightly taller to give the thumbnail more room.
        private const float CARD_W   = 210f;
        private const float CARD_H   = 230f;
        private const float CARD_GAP = 18f;

        // Header height (reference resolution 1920×1080)
        private const float HEADER_H = 64f;

        // ── Factory ───────────────────────────────────────────────────────

        public static MapBrowserScreen Create(MapEditorWindow editorWindow)
        {
            var canvas = UIBuilder.CreateCanvas("MapBrowserCanvas", sortOrder: 150);
            var screen = canvas.gameObject.AddComponent<MapBrowserScreen>();
            screen._canvas      = canvas;
            screen._editorWindow = editorWindow;
            screen.BuildUI();
            screen._slide = canvas.gameObject.AddComponent<SlideAnimator>();
            screen._slide.Target    = canvas.GetComponent<RectTransform>();
            screen._slide.OffscreenY = 1100f;
            canvas.gameObject.SetActive(false);
            return screen;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
            _slide.AnimateIn();
        }

        public void Close()
        {
            _slide.AnimateOut(() => gameObject.SetActive(false));
        }

        // ── Build UI ──────────────────────────────────────────────────────

        private void BuildUI()
        {
            var root = _canvas.GetComponent<RectTransform>();

            // Semi-transparent backdrop — game background shows through
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.02f, 0.03f, 0.06f, 0.82f);

            // ── Header bar ────────────────────────────────────────────────
            // Slightly elevated surface with a subtle bottom border.
            var header = UIBuilder.FlatPanel(root, "Header",
                new Color(0.07f, 0.09f, 0.15f, 1f),
                new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -HEADER_H), Vector2.zero);
            BuildHeader(header);

            // Hairline border under header
            var borderGo = new GameObject("HeaderBorder");
            borderGo.transform.SetParent(root, false);
            var borderImg = borderGo.AddComponent<Image>();
            borderImg.color = StyleHelper.DarkBorder;
            var brt = borderGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(0f, -HEADER_H - 1f);
            brt.offsetMax = new Vector2(0f, -HEADER_H);

            // ── Scrollable card grid ──────────────────────────────────────
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(root, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(0f, -HEADER_H - 1f);

            // Transparent image for raycasting
            var scrollBg = scrollGo.AddComponent<Image>();
            scrollBg.color = Color.clear;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;
            _grid = contentRt;
            scroll.content = contentRt;

            // ── New-map dialog (modal) ─────────────────────────────────────
            BuildNewMapDialog(root);
        }

        // ── Header ────────────────────────────────────────────────────────

        private void BuildHeader(RectTransform header)
        {
            var layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(24, 16, 0, 0);
            layout.spacing = 10;
            layout.childAlignment      = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth  = false;

            // ── Left: accent bar + title ──────────────────────────────────
            // Vertical blue accent bar
            var accentGo = new GameObject("Accent");
            accentGo.transform.SetParent(header, false);
            var accentImg = accentGo.AddComponent<Image>();
            accentImg.color = StyleHelper.Blue;
            var accentLe = accentGo.AddComponent<LayoutElement>();
            accentLe.minWidth  = 4;
            accentLe.minHeight = 32;
            accentLe.flexibleHeight = 0;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(header, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 22f, bold: true);
            titleTmp.text  = "MY MAPS";
            titleTmp.color = StyleHelper.TextPrimary;
            titleTmp.alignment = TextAlignmentOptions.Left;
            var titleLe = titleGo.AddComponent<LayoutElement>();
            titleLe.minWidth = 160;

            // ── Spacer ────────────────────────────────────────────────────
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // ── Right-side action buttons ─────────────────────────────────
            // Import current level
            var importBtn = AddHeaderButton(header, "⬇  Import Level",
                new Color(0.18f, 0.48f, 0.28f, 1f), minWidth: 148);
            importBtn.onClick.AddListener(OnImportCurrentLevel);

            // + New Map (orange, prominent)
            var newBtn = AddHeaderButton(header, "+  New Map",
                StyleHelper.Orange, minWidth: 120);
            newBtn.onClick.AddListener(OpenNewMapDialog);

            // Close (red-tinted)
            var closeBtn = AddHeaderButton(header, "✕",
                new Color(0.60f, 0.15f, 0.15f, 1f), minWidth: 44);
            closeBtn.onClick.AddListener(Close);
        }

        private Button AddHeaderButton(RectTransform parent, string text, Color color,
            float minWidth = 100)
        {
            var go = new GameObject($"HBtn_{text}");
            go.transform.SetParent(parent, false);

            var img    = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth   = minWidth;
            le.minHeight  = 38;
            le.flexibleHeight = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 13f, bold: true);
            tmp.text = text;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8, 0);
            lrt.offsetMax = new Vector2(-8, 0);

            return btn;
        }

        // ── New-map dialog ────────────────────────────────────────────────

        private void BuildNewMapDialog(RectTransform root)
        {
            _newMapDialog = new GameObject("NewMapDialog");
            _newMapDialog.transform.SetParent(root, false);

            // Dim overlay
            var overlay = _newMapDialog.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.70f);
            var ort = _newMapDialog.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;

            // ── Dialog box ────────────────────────────────────────────────
            // Centred, fixed 380×220 px (at 1920×1080 reference).
            var box = UIBuilder.Panel(ort, "Box",
                new Color(0.10f, 0.13f, 0.20f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-190f, -115f), new Vector2(190f, 115f));

            // Top accent line on the dialog box
            var accentTop = new GameObject("TopAccent");
            accentTop.transform.SetParent(box, false);
            var atImg = accentTop.AddComponent<Image>();
            atImg.color = StyleHelper.Blue;
            var atRt = accentTop.GetComponent<RectTransform>();
            atRt.anchorMin = new Vector2(0f, 1f);
            atRt.anchorMax = Vector2.one;
            atRt.offsetMin = new Vector2(0f, -3f);
            atRt.offsetMax = Vector2.zero;

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 22, 20);
            layout.spacing = 12;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            // Title
            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(box, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 17f, bold: true);
            titleTmp.text = "NEW MAP";
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleGo.AddComponent<LayoutElement>().minHeight = 26;

            UIBuilder.AddRule(box, StyleHelper.DarkBorder);

            // Input field
            _newMapNameField = UIBuilder.MakeInputField(box, "Enter map name...",
                Vector2.zero, new Vector2(332f, 40f));
            _newMapNameField.gameObject.AddComponent<LayoutElement>().minHeight = 40;
            _newMapNameField.onValueChanged.AddListener(_ => ClearError());

            // Error label
            var errGo  = new GameObject("Error");
            errGo.transform.SetParent(box, false);
            _newMapError = errGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(_newMapError, 12f);
            _newMapError.color = new Color(1f, 0.35f, 0.35f, 1f);
            _newMapError.text  = "";
            _newMapError.alignment = TextAlignmentOptions.Center;
            errGo.AddComponent<LayoutElement>().minHeight = 16;

            // Button row
            var btnRow = new GameObject("Buttons");
            btnRow.transform.SetParent(box, false);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing             = 10;
            hlg.childForceExpandWidth  = true;
            hlg.childForceExpandHeight = true;
            btnRow.AddComponent<LayoutElement>().minHeight = 38;

            var cancelBtn = UIBuilder.MakeButton(btnRow.GetComponent<RectTransform>(),
                "Cancel", StyleHelper.DarkBlue, Vector2.zero, Vector2.zero);
            cancelBtn.onClick.AddListener(CloseNewMapDialog);

            var createBtn = UIBuilder.MakeButton(btnRow.GetComponent<RectTransform>(),
                "Create", StyleHelper.Orange, Vector2.zero, Vector2.zero);
            createBtn.onClick.AddListener(OnCreateMap);

            _newMapDialog.SetActive(false);
        }

        private void OpenNewMapDialog()
        {
            _newMapNameField.text  = "";
            _newMapError.text      = "";
            _newMapDialog.SetActive(true);
            _newMapNameField.Select();
        }

        private void CloseNewMapDialog() => _newMapDialog.SetActive(false);
        private void ClearError()        => _newMapError.text = "";

        private void OnCreateMap()
        {
            string name = _newMapNameField.text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                _newMapError.text = "Please enter a map name.";
                return;
            }

            foreach (var existing in MapSerializer.ListMaps())
            {
                if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                {
                    _newMapError.text = $"A map named \"{name}\" already exists.";
                    return;
                }
            }

            foreach (var def in DefaultMaps.GetDefaults())
            {
                if (string.Equals(def.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    _newMapError.text = $"\"{name}\" is a default map name. Use a different name.";
                    return;
                }
            }

            var newMap = new MapData(name);
            MapSerializer.SaveMap(newMap, name);
            CloseNewMapDialog();
            Close();
            _editorWindow.Open(newMap);
        }

        private void OnImportCurrentLevel()
        {
            string name = $"Import_{DateTime.Now:HHmmss}";
            var map = DefaultMaps.CaptureCurrentLevel(name);
            if (map == null)
            {
                Plugin.Log.LogWarning("[MapBrowserScreen] No level to import — open a level first.");
                return;
            }
            MapSerializer.SaveMap(map, map.Name);
            Refresh();
        }

        // ── Card grid ─────────────────────────────────────────────────────

        public void Refresh()
        {
            foreach (Transform child in _grid) Destroy(child.gameObject);

            var allMaps = new List<(MapData map, bool isDefault, string? fileName)>();

            foreach (var def in DefaultMaps.GetDefaults())
                allMaps.Add((def, true, null));

            foreach (var name in MapSerializer.ListMaps())
            {
                var map = MapSerializer.LoadMap(name);
                if (map != null) allMaps.Add((map, false, name));
            }

            // GridLayoutGroup — column count adapts to screen width
            var glg = _grid.gameObject.GetComponent<GridLayoutGroup>()
                   ?? _grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize  = new Vector2(CARD_W, CARD_H);
            glg.spacing   = new Vector2(CARD_GAP, CARD_GAP);
            glg.padding   = new RectOffset(28, 28, 28, 28);
            glg.constraint      = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = Mathf.Max(1,
                Mathf.FloorToInt((Screen.width - 56f) / (CARD_W + CARD_GAP)));
            glg.startCorner     = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis       = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment  = TextAnchor.UpperLeft;

            foreach (var (map, isDefault, fileName) in allMaps)
                SpawnCard(map, isDefault, fileName);
        }

        private void SpawnCard(MapData map, bool isDefault, string? fileName)
        {
            // ── Card root ─────────────────────────────────────────────────
            var card = new GameObject($"Card_{map.Name}");
            card.transform.SetParent(_grid, false);

            var cardImg    = card.AddComponent<Image>();
            cardImg.color  = StyleHelper.DarkSurface;
            cardImg.sprite = StyleHelper.MakeRoundedSprite();
            cardImg.type   = Image.Type.Sliced;

            // Button for click-to-open + hover scale
            var btn = card.AddComponent<Button>();
            StyleHelper.StyleButton(btn, StyleHelper.DarkSurface);
            var hoverAnim = card.AddComponent<Patches.HoverScaleAnimator>();
            hoverAnim.Curve = StyleHelper.GetHoverCurve();

            // Subtle hover overlay (slightly lighter surface)
            var hoverOverlay = new GameObject("HoverOverlay");
            hoverOverlay.transform.SetParent(card.transform, false);
            var hoImg = hoverOverlay.AddComponent<Image>();
            hoImg.color       = new Color(1f, 1f, 1f, 0f); // animated by PressColorSwapper
            hoImg.sprite      = StyleHelper.MakeRoundedSprite();
            hoImg.type        = Image.Type.Sliced;
            hoImg.raycastTarget = false;
            var hoRt = hoverOverlay.GetComponent<RectTransform>();
            hoRt.anchorMin = Vector2.zero;
            hoRt.anchorMax = Vector2.one;
            hoRt.offsetMin = hoRt.offsetMax = Vector2.zero;

            // ── Thumbnail (top 65% of card) ───────────────────────────────
            var thumbGo  = new GameObject("Thumb");
            thumbGo.transform.SetParent(card.transform, false);
            var thumbImg = thumbGo.AddComponent<RawImage>();
            thumbImg.texture      = ThumbnailGenerator.Generate(map);
            thumbImg.raycastTarget = false;
            var thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0f, 0.34f);
            thumbRt.anchorMax = Vector2.one;
            thumbRt.offsetMin = new Vector2(0f, 0f);
            thumbRt.offsetMax = Vector2.zero;

            // Rounded mask overlay on thumbnail for consistent look
            var thumbMaskGo  = new GameObject("ThumbMask");
            thumbMaskGo.transform.SetParent(thumbGo.transform, false);
            var tmImg  = thumbMaskGo.AddComponent<Image>();
            tmImg.color       = new Color(0f, 0f, 0f, 0.08f);
            tmImg.sprite      = StyleHelper.MakeRoundedSprite();
            tmImg.type        = Image.Type.Sliced;
            tmImg.raycastTarget = false;
            var tmRt = thumbMaskGo.GetComponent<RectTransform>();
            tmRt.anchorMin = Vector2.zero;
            tmRt.anchorMax = Vector2.one;
            tmRt.offsetMin = tmRt.offsetMax = Vector2.zero;

            // ── Thin separator between thumb and info strip ───────────────
            var sepGo  = new GameObject("ThumbSep");
            sepGo.transform.SetParent(card.transform, false);
            var sepImg = sepGo.AddComponent<Image>();
            sepImg.color       = StyleHelper.DarkBorder;
            sepImg.raycastTarget = false;
            var sepRt = sepGo.GetComponent<RectTransform>();
            sepRt.anchorMin = new Vector2(0.05f, 0.34f);
            sepRt.anchorMax = new Vector2(0.95f, 0.34f);
            sepRt.offsetMin = new Vector2(0f, -1f);
            sepRt.offsetMax = Vector2.zero;

            // ── Info strip (bottom 34% of card) ───────────────────────────
            var infoStrip = new GameObject("InfoStrip");
            infoStrip.transform.SetParent(card.transform, false);
            var infoRt = infoStrip.GetComponent<RectTransform>() ?? infoStrip.AddComponent<RectTransform>();
            infoRt.anchorMin = Vector2.zero;
            infoRt.anchorMax = new Vector2(1f, 0.34f);
            infoRt.offsetMin = Vector2.zero;
            infoRt.offsetMax = Vector2.zero;

            // Map name (bold, slightly larger)
            var nameGo  = new GameObject("Name");
            nameGo.transform.SetParent(infoStrip.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(nameTmp, 13f, bold: true);
            nameTmp.text          = map.Name;
            nameTmp.color         = StyleHelper.TextPrimary;
            nameTmp.alignment     = TextAlignmentOptions.Left;
            nameTmp.overflowMode  = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = new Vector2(0f, 0.5f);
            nameRt.anchorMax = Vector2.one;
            nameRt.offsetMin = new Vector2(10f, -2f);
            nameRt.offsetMax = new Vector2(-10f, 0f);

            // Platform count (dimmer, smaller)
            var infoGo  = new GameObject("Info");
            infoGo.transform.SetParent(infoStrip.transform, false);
            var infoTmp = infoGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(infoTmp, 11f);
            infoTmp.text          = $"{map.Platforms.Count} platforms";
            infoTmp.color         = StyleHelper.TextSecondary;
            infoTmp.alignment     = TextAlignmentOptions.Left;
            infoTmp.raycastTarget = false;
            var infoLblRt = infoGo.GetComponent<RectTransform>();
            infoLblRt.anchorMin = Vector2.zero;
            infoLblRt.anchorMax = new Vector2(1f, 0.5f);
            infoLblRt.offsetMin = new Vector2(10f, 4f);
            infoLblRt.offsetMax = new Vector2(-10f, 0f);

            // ── Top-left badge (DEFAULT / user map indicator) ─────────────
            if (isDefault)
            {
                var badgeTmp = UIBuilder.MakeChip(card.transform, "DEFAULT",
                    new Color(StyleHelper.Blue.r, StyleHelper.Blue.g, StyleHelper.Blue.b, 0.85f),
                    fontSize: 9f);
                var badgeRt = badgeTmp.transform.parent.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0f, 1f);
                badgeRt.anchorMax = new Vector2(0f, 1f);
                badgeRt.pivot     = new Vector2(0f, 1f);
                badgeRt.sizeDelta = new Vector2(62f, 20f);
                badgeRt.anchoredPosition = new Vector2(8f, -8f);
            }
            else
            {
                // Small "custom" indicator dot
                var dotGo  = new GameObject("CustomDot");
                dotGo.transform.SetParent(card.transform, false);
                var dotImg = dotGo.AddComponent<Image>();
                dotImg.color       = StyleHelper.Orange;
                dotImg.sprite      = StyleHelper.MakeRoundedSpriteSmall();
                dotImg.type        = Image.Type.Sliced;
                dotImg.raycastTarget = false;
                var dotRt = dotGo.GetComponent<RectTransform>();
                dotRt.anchorMin = new Vector2(0f, 1f);
                dotRt.anchorMax = new Vector2(0f, 1f);
                dotRt.pivot     = new Vector2(0f, 1f);
                dotRt.sizeDelta = new Vector2(8f, 8f);
                dotRt.anchoredPosition = new Vector2(8f, -8f);
            }

            // ── Delete button (top-right corner, user maps only) ──────────
            if (!isDefault && fileName != null)
            {
                var delGo  = new GameObject("DelBtn");
                delGo.transform.SetParent(card.transform, false);

                var delImg    = delGo.AddComponent<Image>();
                delImg.color  = new Color(0.65f, 0.12f, 0.12f, 0.90f);
                delImg.sprite = StyleHelper.MakeRoundedSpriteSmall();
                delImg.type   = Image.Type.Sliced;

                var delBtn = delGo.AddComponent<Button>();
                StyleHelper.StyleButton(delBtn, new Color(0.65f, 0.12f, 0.12f, 0.90f));

                var delRt = delGo.GetComponent<RectTransform>();
                delRt.anchorMin = new Vector2(1f, 1f);
                delRt.anchorMax = new Vector2(1f, 1f);
                delRt.pivot     = new Vector2(1f, 1f);
                delRt.sizeDelta = new Vector2(28f, 28f);
                delRt.anchoredPosition = new Vector2(-6f, -6f);

                var delTxt = new GameObject("X");
                delTxt.transform.SetParent(delGo.transform, false);
                var dt = delTxt.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(dt, 13f, bold: true);
                dt.text = "✕";
                dt.raycastTarget = false;
                var drt = delTxt.GetComponent<RectTransform>();
                drt.anchorMin = Vector2.zero;
                drt.anchorMax = Vector2.one;
                drt.offsetMin = drt.offsetMax = Vector2.zero;

                string captureName = fileName;
                delBtn.onClick.AddListener(() => {
                    MapSerializer.DeleteMap(captureName);
                    Refresh();
                });
            }

            // ── Card click → open editor ──────────────────────────────────
            var captureMap = map;
            btn.onClick.AddListener(() => {
                Close();
                _editorWindow.Open(captureMap.Clone());
            });
        }
    }
}

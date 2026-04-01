using System;
using System.Collections.Generic;
using BoplMapEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Full-screen map browser: shown when clicking "Map Editor" in lobby.
    // Shows thumbnail cards for all maps. Click → open editor.
    public class MapBrowserScreen : MonoBehaviour
    {
        private Canvas _canvas = null!;
        private SlideAnimator _slide = null!;
        private RectTransform _grid = null!;
        private MapEditorWindow _editorWindow = null!;
        private GameObject _newMapDialog = null!;
        private TMP_InputField _newMapNameField = null!;
        private TextMeshProUGUI _newMapError = null!;

        private const float CARD_W = 200f;
        private const float CARD_H = 220f;
        private const float CARD_GAP = 16f;

        public static MapBrowserScreen Create(MapEditorWindow editorWindow)
        {
            var canvas = UIBuilder.CreateCanvas("MapBrowserCanvas", sortOrder: 150);
            var screen = canvas.gameObject.AddComponent<MapBrowserScreen>();
            screen._canvas = canvas;
            screen._editorWindow = editorWindow;
            screen.BuildUI();
            screen._slide = canvas.gameObject.AddComponent<SlideAnimator>();
            screen._slide.Target = canvas.GetComponent<RectTransform>();
            screen._slide.OffscreenY = 1100f;
            canvas.gameObject.SetActive(false);
            return screen;
        }

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

            // Dark background
            var bg = root.gameObject.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.14f, 0.97f);

            // Header bar
            var header = UIBuilder.Panel(root, "Header",
                new Color(0.05f, 0.07f, 0.12f, 1f),
                new Vector2(0, 1), Vector2.one,
                new Vector2(0, -60), Vector2.zero);
            BuildHeader(header);

            // Scrollable card grid
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(root, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = new Vector2(0, 0);
            scrollRt.offsetMax = new Vector2(0, -60);
            scrollGo.AddComponent<Image>().color = Color.clear;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero; vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero; vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = new Vector2(0, 0);
            contentRt.offsetMax = new Vector2(0, 0);
            contentGo.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            _grid = contentRt;
            scroll.content = contentRt;

            // New map dialog (modal)
            BuildNewMapDialog(root);
        }

        private void BuildHeader(RectTransform header)
        {
            var layout = header.gameObject.AddComponent<HorizontalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.spacing = 12;
            layout.childAlignment = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth = false;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(header, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 24f, bold: true);
            titleTmp.text = "MY MAPS";
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleGo.AddComponent<LayoutElement>().minWidth = 200;

            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Import current level button (if in a level scene)
            var importBtn = AddHeaderButton(header, "⬇ Import Level", new Color(0.2f, 0.5f, 0.3f, 1f), 160);
            importBtn.onClick.AddListener(OnImportCurrentLevel);

            // New map button
            var newBtn = AddHeaderButton(header, "+ New Map", StyleHelper.Orange, 130);
            newBtn.onClick.AddListener(OpenNewMapDialog);

            // Close button
            var closeBtn = AddHeaderButton(header, "✕", new Color(0.5f, 0.15f, 0.15f, 1f), 50);
            closeBtn.onClick.AddListener(Close);
        }

        private Button AddHeaderButton(RectTransform parent, string text, Color color, float width)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color; img.sprite = StyleHelper.MakeRoundedSprite(); img.type = Image.Type.Sliced;
            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);
            go.AddComponent<LayoutElement>().minWidth = width;

            var lgo = new GameObject("L"); lgo.transform.SetParent(go.transform, false);
            var tmp = lgo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 15f, bold: true); tmp.text = text;
            var lrt = lgo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 0); lrt.offsetMax = new Vector2(-6, 0);
            return btn;
        }

        // ── New map dialog ────────────────────────────────────────────────

        private void BuildNewMapDialog(RectTransform root)
        {
            _newMapDialog = new GameObject("NewMapDialog");
            _newMapDialog.transform.SetParent(root, false);

            // Dim overlay
            var overlay = _newMapDialog.AddComponent<Image>();
            overlay.color = new Color(0, 0, 0, 0.65f);
            var ort = _newMapDialog.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;

            // Dialog box
            var box = UIBuilder.Panel(ort, "Box",
                new Color(0.08f, 0.10f, 0.18f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180, -120), new Vector2(180, 120));

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(20, 20, 20, 20);
            layout.spacing = 12;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(box, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 18f, bold: true);
            titleTmp.text = "NEW MAP";
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleGo.AddComponent<LayoutElement>().minHeight = 28;

            // Input field
            _newMapNameField = UIBuilder.MakeInputField(box, "Enter map name...",
                Vector2.zero, new Vector2(320, 40));
            _newMapNameField.gameObject.AddComponent<LayoutElement>().minHeight = 40;
            _newMapNameField.onValueChanged.AddListener(_ => ClearError());

            // Error label
            var errGo = new GameObject("Error");
            errGo.transform.SetParent(box, false);
            _newMapError = errGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(_newMapError, 13f);
            _newMapError.color = new Color(1f, 0.35f, 0.35f, 1f);
            _newMapError.text = "";
            _newMapError.alignment = TextAlignmentOptions.Center;
            errGo.AddComponent<LayoutElement>().minHeight = 18;

            // Buttons row
            var btnRow = new GameObject("Buttons");
            btnRow.transform.SetParent(box, false);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 10; hlg.childForceExpandWidth = true; hlg.childForceExpandHeight = true;
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
            _newMapNameField.text = "";
            _newMapError.text = "";
            _newMapDialog.SetActive(true);
            _newMapNameField.Select();
        }

        private void CloseNewMapDialog() => _newMapDialog.SetActive(false);
        private void ClearError() => _newMapError.text = "";

        private void OnCreateMap()
        {
            string name = _newMapNameField.text.Trim();

            if (string.IsNullOrEmpty(name))
            {
                _newMapError.text = "Please enter a map name.";
                return;
            }

            // Check for duplicate
            foreach (var existing in MapSerializer.ListMaps())
            {
                if (string.Equals(existing, name, StringComparison.OrdinalIgnoreCase))
                {
                    _newMapError.text = $"A map named \"{name}\" already exists.";
                    return;
                }
            }
            // Check default map names
            foreach (var def in DefaultMaps.GetDefaults())
            {
                if (string.Equals(def.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    _newMapError.text = $"\"{name}\" is a default map name. Use a different name.";
                    return;
                }
            }

            // Create and open editor
            var newMap = new MapData(name);
            MapSerializer.SaveMap(newMap, name);
            CloseNewMapDialog();
            Close();
            _editorWindow.Open(newMap);
        }

        private void OnImportCurrentLevel()
        {
            string name = $"Import_{System.DateTime.Now:HHmmss}";
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

            // Default maps first
            foreach (var def in DefaultMaps.GetDefaults())
                allMaps.Add((def, true, null));

            // User maps
            foreach (var name in MapSerializer.ListMaps())
            {
                var map = MapSerializer.LoadMap(name);
                if (map != null) allMaps.Add((map, false, name));
            }

            // Lay out cards in a wrapping grid using a GridLayoutGroup
            var glg = _grid.gameObject.GetComponent<GridLayoutGroup>()
                   ?? _grid.gameObject.AddComponent<GridLayoutGroup>();
            glg.cellSize = new Vector2(CARD_W, CARD_H);
            glg.spacing  = new Vector2(CARD_GAP, CARD_GAP);
            glg.padding  = new RectOffset(24, 24, 24, 24);
            glg.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            glg.constraintCount = Mathf.Max(1, Mathf.FloorToInt((Screen.width - 48f) / (CARD_W + CARD_GAP)));
            glg.startCorner = GridLayoutGroup.Corner.UpperLeft;
            glg.startAxis = GridLayoutGroup.Axis.Horizontal;
            glg.childAlignment = TextAnchor.UpperLeft;

            foreach (var (map, isDefault, fileName) in allMaps)
                SpawnCard(map, isDefault, fileName);
        }

        private void SpawnCard(MapData map, bool isDefault, string? fileName)
        {
            var card = new GameObject($"Card_{map.Name}");
            card.transform.SetParent(_grid, false);

            // Card background
            var bg = card.AddComponent<Image>();
            bg.color = new Color(0.10f, 0.13f, 0.20f, 1f);
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type = Image.Type.Sliced;

            // Hover effect
            var btn = card.AddComponent<Button>();
            StyleHelper.StyleButton(btn, new Color(0.10f, 0.13f, 0.20f, 1f));
            var hoverAnim = card.AddComponent<Patches.HoverScaleAnimator>();
            hoverAnim.Curve = StyleHelper.GetHoverCurve();

            // ── Thumbnail ─────────────────────────────────────────────────
            var thumbGo = new GameObject("Thumb");
            thumbGo.transform.SetParent(card.transform, false);
            var thumbImg = thumbGo.AddComponent<RawImage>();
            thumbImg.texture = ThumbnailGenerator.Generate(map);
            thumbImg.raycastTarget = false;
            var thumbRt = thumbGo.GetComponent<RectTransform>();
            thumbRt.anchorMin = new Vector2(0, 0.3f);
            thumbRt.anchorMax = Vector2.one;
            thumbRt.offsetMin = new Vector2(6, 4);
            thumbRt.offsetMax = new Vector2(-6, -6);

            // Rounded overlay on thumbnail
            var thumbOverlay = new GameObject("ThumbOverlay");
            thumbOverlay.transform.SetParent(thumbGo.transform, false);
            var toImg = thumbOverlay.AddComponent<Image>();
            toImg.color = new Color(0, 0, 0, 0.15f);
            toImg.sprite = StyleHelper.MakeRoundedSprite();
            toImg.type = Image.Type.Sliced;
            toImg.raycastTarget = false;
            var toRt = thumbOverlay.GetComponent<RectTransform>();
            toRt.anchorMin = Vector2.zero; toRt.anchorMax = Vector2.one;
            toRt.offsetMin = toRt.offsetMax = Vector2.zero;

            // ── Map name ──────────────────────────────────────────────────
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(card.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(nameTmp, 14f, bold: true);
            nameTmp.text = map.Name;
            nameTmp.alignment = TextAlignmentOptions.Center;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;
            var nameRt = nameGo.GetComponent<RectTransform>();
            nameRt.anchorMin = Vector2.zero;
            nameRt.anchorMax = new Vector2(1, 0.3f);
            nameRt.offsetMin = new Vector2(4, 22);
            nameRt.offsetMax = new Vector2(-4, 0);

            // Platform count
            var infoGo = new GameObject("Info");
            infoGo.transform.SetParent(card.transform, false);
            var infoTmp = infoGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(infoTmp, 11f);
            infoTmp.text = $"{map.Platforms.Count} platforms";
            infoTmp.color = new Color(0.6f, 0.6f, 0.7f, 1f);
            infoTmp.alignment = TextAlignmentOptions.Center;
            infoTmp.raycastTarget = false;
            var infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.anchorMin = Vector2.zero;
            infoRt.anchorMax = new Vector2(1, 0.3f);
            infoRt.offsetMin = new Vector2(4, 6);
            infoRt.offsetMax = new Vector2(-4, 20);

            // Default badge or delete button
            if (isDefault)
            {
                var badge = new GameObject("Badge");
                badge.transform.SetParent(card.transform, false);
                var badgeImg = badge.AddComponent<Image>();
                badgeImg.color = StyleHelper.DarkBlue;
                badgeImg.sprite = StyleHelper.MakeRoundedSprite();
                badgeImg.type = Image.Type.Sliced;
                badgeImg.raycastTarget = false;
                var badgeRt = badge.GetComponent<RectTransform>();
                badgeRt.anchorMin = new Vector2(0, 1);
                badgeRt.anchorMax = new Vector2(0, 1);
                badgeRt.pivot = new Vector2(0, 1);
                badgeRt.sizeDelta = new Vector2(60, 20);
                badgeRt.anchoredPosition = new Vector2(6, -6);

                var badgeTxt = new GameObject("BadgeTxt");
                badgeTxt.transform.SetParent(badge.transform, false);
                var bt = badgeTxt.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(bt, 10f); bt.text = "DEFAULT";
                bt.raycastTarget = false;
                var brt = badgeTxt.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = brt.offsetMax = Vector2.zero;
            }
            else if (fileName != null)
            {
                // Delete button (top-right corner)
                var delGo = new GameObject("DelBtn");
                delGo.transform.SetParent(card.transform, false);
                var delImg = delGo.AddComponent<Image>();
                delImg.color = new Color(0.55f, 0.1f, 0.1f, 0.85f);
                delImg.sprite = StyleHelper.MakeRoundedSprite();
                delImg.type = Image.Type.Sliced;
                var delBtn = delGo.AddComponent<Button>();
                StyleHelper.StyleButton(delBtn, new Color(0.55f, 0.1f, 0.1f, 0.85f));
                var delRt = delGo.GetComponent<RectTransform>();
                delRt.anchorMin = new Vector2(1, 1);
                delRt.anchorMax = new Vector2(1, 1);
                delRt.pivot = new Vector2(1, 1);
                delRt.sizeDelta = new Vector2(26, 26);
                delRt.anchoredPosition = new Vector2(-5, -5);

                var delTxt = new GameObject("X");
                delTxt.transform.SetParent(delGo.transform, false);
                var dt = delTxt.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(dt, 14f, bold: true); dt.text = "✕";
                var drt = delTxt.GetComponent<RectTransform>();
                drt.anchorMin = Vector2.zero; drt.anchorMax = Vector2.one;
                drt.offsetMin = drt.offsetMax = Vector2.zero;

                string captureName = fileName;
                delBtn.onClick.AddListener(() => {
                    MapSerializer.DeleteMap(captureName);
                    Refresh();
                });
            }

            // Click card → open editor
            var captureMap = map;
            btn.onClick.AddListener(() => {
                Close();
                _editorWindow.Open(captureMap.Clone());
            });
        }
    }
}

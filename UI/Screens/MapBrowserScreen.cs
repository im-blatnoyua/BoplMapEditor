using System;
using System.Collections.Generic;
using BoplMapEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Full-screen map browser: shown when clicking "Map Editor" in lobby.
    // Clean list layout: one row per map with name, platform count, Edit and Delete buttons.
    public class MapBrowserScreen : MonoBehaviour
    {
        private Canvas          _canvas       = null!;
        private SlideAnimator   _slide        = null!;
        private RectTransform   _listContent  = null!;
        private MapEditorWindow _editorWindow = null!;

        // New-map dialog refs
        private GameObject      _newMapDialog    = null!;
        private TMP_InputField  _newMapNameField = null!;
        private TextMeshProUGUI _newMapError     = null!;

        private const float HEADER_H = 56f;

        // ── Factory ───────────────────────────────────────────────────────

        public static MapBrowserScreen Create(MapEditorWindow editorWindow)
        {
            var canvas = UIBuilder.CreateCanvas("MapBrowserCanvas", sortOrder: 150);
            var screen = canvas.gameObject.AddComponent<MapBrowserScreen>();
            screen._canvas       = canvas;
            screen._editorWindow = editorWindow;
            screen.BuildUI();
            screen._slide = canvas.gameObject.AddComponent<SlideAnimator>();
            screen._slide.Target    = canvas.GetComponent<RectTransform>();
            screen._slide.OffscreenY = 900f;
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

            // Semi-transparent backdrop
            var backdrop = root.gameObject.AddComponent<Image>();
            backdrop.color = new Color(0.02f, 0.03f, 0.07f, 0.88f);

            // ── Header bar ────────────────────────────────────────────────
            var header = UIBuilder.FlatPanel(root, "Header",
                new Color(0.07f, 0.09f, 0.16f, 0.92f),
                new Vector2(0f, 1f), Vector2.one,
                new Vector2(0f, -HEADER_H), Vector2.zero);
            BuildHeader(header);

            // Hairline border under header
            var borderGo = new GameObject("HeaderBorder");
            borderGo.transform.SetParent(root, false);
            borderGo.AddComponent<Image>().color = StyleHelper.DarkBorder;
            var brt = borderGo.GetComponent<RectTransform>();
            brt.anchorMin = new Vector2(0f, 1f);
            brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(0f, -HEADER_H - 1f);
            brt.offsetMax = new Vector2(0f, -HEADER_H);

            // ── Scrollable map list ───────────────────────────────────────
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(root, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(0f, -HEADER_H - 1f);

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

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(24, 24, 12, 12);
            vlg.spacing = 4f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            contentGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _listContent = contentRt;
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
            layout.childAlignment         = TextAnchor.MiddleLeft;
            layout.childForceExpandHeight = true;
            layout.childForceExpandWidth  = false;

            // Title
            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(header, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 22f, bold: true);
            titleTmp.text      = "MY MAPS";
            titleTmp.color     = StyleHelper.TextPrimary;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleGo.AddComponent<LayoutElement>().minWidth = 160;

            // Spacer
            var spacer = new GameObject("Spacer");
            spacer.transform.SetParent(header, false);
            spacer.AddComponent<LayoutElement>().flexibleWidth = 1;

            // + New Map (orange)
            var newBtn = AddHeaderButton(header, "+ New Map", StyleHelper.Orange, minWidth: 110);
            newBtn.onClick.AddListener(OpenNewMapDialog);

            // ✕ Close (red)
            var closeBtn = AddHeaderButton(header, "✕ Close",
                new Color(0.60f, 0.15f, 0.15f, 1f), minWidth: 80);
            closeBtn.onClick.AddListener(Close);
        }

        private Button AddHeaderButton(RectTransform parent, string text, Color color,
            float minWidth = 100)
        {
            var go = new GameObject("HBtn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
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

            var overlay = _newMapDialog.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.70f);
            var ort = _newMapDialog.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero;
            ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;

            // Dialog box: 360 × 200
            var box = UIBuilder.Panel(ort, "Box",
                new Color(0.10f, 0.13f, 0.20f, 1f),
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                new Vector2(-180f, -100f), new Vector2(180f, 100f));

            var layout = box.gameObject.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 12;
            layout.childForceExpandWidth  = true;
            layout.childForceExpandHeight = false;

            // Title
            var titleGo  = new GameObject("Title");
            titleGo.transform.SetParent(box, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(titleTmp, 17f, bold: true);
            titleTmp.text      = "NEW MAP";
            titleTmp.alignment = TextAlignmentOptions.Center;
            titleGo.AddComponent<LayoutElement>().minHeight = 26;

            UIBuilder.AddRule(box, StyleHelper.DarkBorder);

            // Input field
            _newMapNameField = UIBuilder.MakeInputField(box, "Enter map name...",
                Vector2.zero, new Vector2(312f, 40f));
            _newMapNameField.gameObject.AddComponent<LayoutElement>().minHeight = 40;
            _newMapNameField.onValueChanged.AddListener(_ => ClearError());

            // Error label
            var errGo  = new GameObject("Error");
            errGo.transform.SetParent(box, false);
            _newMapError = errGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(_newMapError, 12f);
            _newMapError.color     = new Color(1f, 0.35f, 0.35f, 1f);
            _newMapError.text      = "";
            _newMapError.alignment = TextAlignmentOptions.Center;
            errGo.AddComponent<LayoutElement>().minHeight = 16;

            // Button row
            var btnRow = new GameObject("Buttons");
            btnRow.transform.SetParent(box, false);
            var hlg = btnRow.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing               = 10;
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
            _newMapNameField.text = "";
            _newMapError.text     = "";
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
                    _newMapError.text = "A map named \"" + name + "\" already exists.";
                    return;
                }
            }

            foreach (var def in DefaultMaps.GetDefaults())
            {
                if (string.Equals(def.Name, name, StringComparison.OrdinalIgnoreCase))
                {
                    _newMapError.text = "\"" + name + "\" is a default map name. Use a different name.";
                    return;
                }
            }

            var newMap = new MapData(name);
            MapSerializer.SaveMap(newMap, name);
            CloseNewMapDialog();
            Close();
            _editorWindow.Open(newMap);
        }

        // ── Map list ──────────────────────────────────────────────────────

        public void Refresh()
        {
            foreach (Transform child in _listContent) Destroy(child.gameObject);

            var allMaps = new List<MapEntry>();

            foreach (var def in DefaultMaps.GetDefaults())
                allMaps.Add(new MapEntry(def, true, null));

            foreach (var fname in MapSerializer.ListMaps())
            {
                var map = MapSerializer.LoadMap(fname);
                if (map != null) allMaps.Add(new MapEntry(map, false, fname));
            }

            bool alternate = false;
            foreach (var entry in allMaps)
            {
                SpawnRow(entry.Map, entry.IsDefault, entry.FileName, alternate);
                alternate = !alternate;
            }
        }

        private void SpawnRow(MapData map, bool isDefault, string? fileName, bool altBg)
        {
            var rowGo = new GameObject("Row_" + map.Name);
            rowGo.transform.SetParent(_listContent, false);

            // Row background
            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color = isDefault
                ? new Color(0.10f, 0.13f, 0.20f, altBg ? 0.80f : 0.60f)
                : new Color(0.08f, 0.10f, 0.16f, altBg ? 0.80f : 0.60f);
            rowImg.sprite = StyleHelper.MakeRoundedSprite();
            rowImg.type   = Image.Type.Sliced;

            var rowHlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowHlg.padding = new RectOffset(12, 8, 4, 4);
            rowHlg.spacing = 8;
            rowHlg.childAlignment         = TextAnchor.MiddleLeft;
            rowHlg.childForceExpandWidth  = false;
            rowHlg.childForceExpandHeight = true;
            rowGo.AddComponent<LayoutElement>().minHeight = 48f;

            // Map name (bold, left)
            var nameGo  = new GameObject("Name");
            nameGo.transform.SetParent(rowGo.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(nameTmp, 14f, bold: true);
            nameTmp.text          = map.Name;
            nameTmp.color         = StyleHelper.TextPrimary;
            nameTmp.alignment     = TextAlignmentOptions.Left;
            nameTmp.overflowMode  = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            // DEFAULT badge
            if (isDefault)
            {
                var badgeGo  = new GameObject("Badge");
                badgeGo.transform.SetParent(rowGo.transform, false);
                var badgeImg = badgeGo.AddComponent<Image>();
                badgeImg.color  = new Color(StyleHelper.Blue.r, StyleHelper.Blue.g, StyleHelper.Blue.b, 0.70f);
                badgeImg.sprite = StyleHelper.MakeRoundedSpriteSmall();
                badgeImg.type   = Image.Type.Sliced;
                var badgeLe = badgeGo.AddComponent<LayoutElement>();
                badgeLe.minWidth  = 62f;
                badgeLe.minHeight = 22f;
                badgeLe.flexibleHeight = 0;

                var badgeTxtGo  = new GameObject("T");
                badgeTxtGo.transform.SetParent(badgeGo.transform, false);
                var badgeTmp = badgeTxtGo.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(badgeTmp, 9f, bold: true);
                badgeTmp.text          = "DEFAULT";
                badgeTmp.raycastTarget = false;
                var brt = badgeTxtGo.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = new Vector2(4, 2); brt.offsetMax = new Vector2(-4, -2);
            }

            // Platform count (muted, right-ish)
            var countGo  = new GameObject("Count");
            countGo.transform.SetParent(rowGo.transform, false);
            var countTmp = countGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(countTmp, 11f);
            countTmp.text          = map.Platforms.Count + " platforms";
            countTmp.color         = StyleHelper.TextSecondary;
            countTmp.alignment     = TextAlignmentOptions.Right;
            countTmp.raycastTarget = false;
            countGo.AddComponent<LayoutElement>().minWidth = 90f;

            // [Edit] button (blue)
            var editBtn = MakeRowButton(rowGo.transform, "Edit", StyleHelper.Blue, 60f);
            var captureMap = map;
            editBtn.onClick.AddListener(() => {
                Close();
                _editorWindow.Open(captureMap.Clone());
            });

            // [Del] button (red) — user maps only
            if (!isDefault && fileName != null)
            {
                string captureName = fileName;
                var delBtn = MakeRowButton(rowGo.transform, "Del",
                    new Color(0.65f, 0.12f, 0.12f, 0.90f), 40f);
                delBtn.onClick.AddListener(() => {
                    MapSerializer.DeleteMap(captureName);
                    Refresh();
                });
            }
        }

        private Button MakeRowButton(Transform parent, string text, Color color, float w)
        {
            var go = new GameObject("RBtn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var le = go.AddComponent<LayoutElement>();
            le.minWidth   = w;
            le.minHeight  = 32f;
            le.flexibleHeight = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 12f, bold: true);
            tmp.text = text;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            return btn;
        }

        // ── Internal data class ───────────────────────────────────────────

        private class MapEntry
        {
            public readonly MapData Map;
            public readonly bool    IsDefault;
            public readonly string? FileName;

            public MapEntry(MapData map, bool isDefault, string? fileName)
            {
                Map       = map;
                IsDefault = isDefault;
                FileName  = fileName;
            }
        }
    }
}

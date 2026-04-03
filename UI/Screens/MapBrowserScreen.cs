using System;
using System.Collections.Generic;
using BoplMapEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Full-screen map browser injected into the game's own canvas.
    // No separate overlay — lives as a child of the CharacterSelect canvas.
    public class MapBrowserScreen : MonoBehaviour
    {
        private RectTransform         _root        = null!;
        private RectTransform         _listContent = null!;
        private NativeMapEditorScreen _editorScreen = null!;

        // Game colors passed in from CharacterSelectHandler
        private Color _blue;
        private Color _darkBlue;
        private Color _orange;

        // Sky blue — main background of this screen
        static readonly Color SkyBlue     = new Color(0.44f, 0.72f, 0.94f, 1f);
        static readonly Color SkyBlueDark = new Color(0.28f, 0.55f, 0.80f, 1f);
        static readonly Color SkyPanel    = new Color(0.34f, 0.62f, 0.86f, 0.96f);
        static readonly Color White       = Color.white;

        const float HEADER_H = 80f;

        // ── Factory ───────────────────────────────────────────────────────

        public static MapBrowserScreen Create(
            Transform canvasRoot,
            NativeMapEditorScreen editorScreen,
            Color blue, Color darkBlue, Color orange)
        {
            var go = new GameObject("MapBrowserScreen");
            go.transform.SetParent(canvasRoot, false);

            // Stretch to fill the entire canvas
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var screen = go.AddComponent<MapBrowserScreen>();
            screen._root        = rt;
            screen._editorScreen = editorScreen;
            screen._blue        = blue;
            screen._darkBlue    = darkBlue;
            screen._orange      = orange;
            screen.BuildUI();

            go.SetActive(false);
            return screen;
        }

        // ── Public API ────────────────────────────────────────────────────

        public void Open()
        {
            gameObject.SetActive(true);
            Refresh();
        }

        public void Close()
        {
            gameObject.SetActive(false);
        }

        // ── Build UI ──────────────────────────────────────────────────────

        void BuildUI()
        {
            // Sky blue full-screen backdrop
            var bg = _root.gameObject.AddComponent<Image>();
            bg.color = SkyBlue;

            // Subtle gradient overlay — darker at top, fades out
            var gradGo = new GameObject("Gradient");
            gradGo.transform.SetParent(_root, false);
            var gradImg = gradGo.AddComponent<Image>();
            gradImg.color = new Color(0f, 0.12f, 0.28f, 0.22f);
            var grt = gradGo.GetComponent<RectTransform>();
            grt.anchorMin = new Vector2(0f, 0.6f);
            grt.anchorMax = Vector2.one;
            grt.offsetMin = grt.offsetMax = Vector2.zero;

            // ── Header ────────────────────────────────────────────────────
            var headerGo = new GameObject("Header");
            headerGo.transform.SetParent(_root, false);
            var headerImg = headerGo.AddComponent<Image>();
            headerImg.color = SkyBlueDark;
            var hrt = headerGo.GetComponent<RectTransform>();
            hrt.anchorMin = new Vector2(0f, 1f);
            hrt.anchorMax = Vector2.one;
            hrt.offsetMin = new Vector2(0f, -HEADER_H);
            hrt.offsetMax = Vector2.zero;

            var hlg = headerGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding               = new RectOffset(32, 20, 0, 0);
            hlg.spacing               = 16;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.childForceExpandHeight = true;
            hlg.childForceExpandWidth  = false;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(headerGo.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(titleTmp, 32f, bold: true);
            titleTmp.text      = "MY MAPS";
            titleTmp.color     = White;
            titleTmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            titleTmp.alignment = TextAlignmentOptions.Left;
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            // + New Map button
            var newBtn = MakeHeaderButton(headerGo.transform, "+ New Map", _orange, 140f, 50f);
            newBtn.onClick.AddListener(OnNewMap);

            // Close button
            var closeBtn = MakeHeaderButton(headerGo.transform, "X Close",
                new Color(0.75f, 0.18f, 0.18f, 1f), 100f, 50f);
            closeBtn.onClick.AddListener(Close);

            // Thin separator line under header
            var sepGo = new GameObject("Sep");
            sepGo.transform.SetParent(_root, false);
            sepGo.AddComponent<Image>().color = new Color(0.18f, 0.40f, 0.65f, 0.70f);
            var srt = sepGo.GetComponent<RectTransform>();
            srt.anchorMin = new Vector2(0f, 1f);
            srt.anchorMax = Vector2.one;
            srt.offsetMin = new Vector2(0f, -HEADER_H - 2f);
            srt.offsetMax = new Vector2(0f, -HEADER_H);

            // ── Scrollable content area ───────────────────────────────────
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(_root, false);
            var scrollRt = scrollGo.AddComponent<RectTransform>();
            scrollRt.anchorMin = Vector2.zero;
            scrollRt.anchorMax = Vector2.one;
            scrollRt.offsetMin = Vector2.zero;
            scrollRt.offsetMax = new Vector2(0f, -HEADER_H - 2f);
            scrollGo.AddComponent<Image>().color = Color.clear;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding              = new RectOffset(32, 32, 20, 20);
            vlg.spacing              = 8f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _listContent = contentRt;
            scroll.content = contentRt;
        }

        // ── Header button helper ──────────────────────────────────────────

        Button MakeHeaderButton(Transform parent, string text, Color color, float minW, float h)
        {
            var go = new GameObject("HBtn_" + text);
            go.transform.SetParent(parent, false);

            // Rounded white-ish panel matching game button style
            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = color;
            cols.highlightedColor = new Color(
                Mathf.Min(color.r + 0.12f, 1f),
                Mathf.Min(color.g + 0.12f, 1f),
                Mathf.Min(color.b + 0.12f, 1f), 1f);
            cols.pressedColor  = _darkBlue;
            cols.fadeDuration  = 0.07f;
            btn.colors = cols;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth   = minW;
            le.minHeight  = h;
            le.flexibleHeight = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(tmp, 15f, bold: true);
            tmp.text = text;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(12, 0);
            lrt.offsetMax = new Vector2(-12, 0);

            return btn;
        }

        // ── Refresh list ──────────────────────────────────────────────────

        public void Refresh()
        {
            foreach (Transform child in _listContent)
                Destroy(child.gameObject);

            var allMaps = new List<(MapData map, bool isDefault, string? file)>();

            foreach (var def in DefaultMaps.GetDefaults())
                allMaps.Add((def, true, null));

            foreach (var fname in MapSerializer.ListMaps())
            {
                var map = MapSerializer.LoadMap(fname);
                if (map != null) allMaps.Add((map, false, fname));
            }

            if (allMaps.Count == 0)
            {
                SpawnEmptyHint();
                return;
            }

            bool alt = false;
            foreach (var (map, isDefault, file) in allMaps)
            {
                SpawnRow(map, isDefault, file, alt);
                alt = !alt;
            }
        }

        void SpawnEmptyHint()
        {
            var go = new GameObject("Hint");
            go.transform.SetParent(_listContent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(tmp, 20f, bold: false);
            tmp.text      = "No maps yet. Press + New Map to create one!";
            tmp.color     = new Color(1f, 1f, 1f, 0.55f);
            tmp.alignment = TextAlignmentOptions.Center;
            go.AddComponent<LayoutElement>().minHeight = 80f;
        }

        void SpawnRow(MapData map, bool isDefault, string? fileName, bool altBg)
        {
            var rowGo = new GameObject("Row_" + map.Name);
            rowGo.transform.SetParent(_listContent, false);

            // Row card — white-ish translucent panel
            var rowImg = rowGo.AddComponent<Image>();
            rowImg.color  = altBg
                ? new Color(1f, 1f, 1f, 0.20f)
                : new Color(1f, 1f, 1f, 0.12f);
            rowImg.sprite = StyleHelper.MakeRoundedSprite();
            rowImg.type   = Image.Type.Sliced;

            var rowHlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            rowHlg.padding               = new RectOffset(18, 12, 0, 0);
            rowHlg.spacing               = 12;
            rowHlg.childAlignment         = TextAnchor.MiddleLeft;
            rowHlg.childForceExpandWidth  = false;
            rowHlg.childForceExpandHeight = true;
            rowGo.AddComponent<LayoutElement>().minHeight = 60f;

            // Map name
            var nameGo  = new GameObject("Name");
            nameGo.transform.SetParent(rowGo.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(nameTmp, 20f, bold: true);
            nameTmp.text         = map.Name;
            nameTmp.color        = White;
            nameTmp.alignment    = TextAlignmentOptions.Left;
            nameTmp.overflowMode = TextOverflowModes.Ellipsis;
            nameTmp.raycastTarget = false;
            nameGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            // DEFAULT badge
            if (isDefault)
            {
                var badgeGo = new GameObject("Badge");
                badgeGo.transform.SetParent(rowGo.transform, false);
                var badgeImg = badgeGo.AddComponent<Image>();
                badgeImg.color  = new Color(1f, 1f, 1f, 0.25f);
                badgeImg.sprite = StyleHelper.MakeRoundedSpriteSmall();
                badgeImg.type   = Image.Type.Sliced;
                var ble = badgeGo.AddComponent<LayoutElement>();
                ble.minWidth = 72f; ble.minHeight = 26f; ble.flexibleHeight = 0;

                var btGo = new GameObject("T");
                btGo.transform.SetParent(badgeGo.transform, false);
                var btTmp = btGo.AddComponent<TextMeshProUGUI>();
                ApplyGameFont(btTmp, 10f, bold: true);
                btTmp.text      = "DEFAULT";
                btTmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
                btTmp.color     = White;
                btTmp.raycastTarget = false;
                var brt = btGo.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
                brt.offsetMin = new Vector2(4, 2); brt.offsetMax = new Vector2(-4, -2);
            }

            // Platform count
            var cntGo  = new GameObject("Count");
            cntGo.transform.SetParent(rowGo.transform, false);
            var cntTmp = cntGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(cntTmp, 13f, bold: false);
            cntTmp.text          = map.Platforms.Count + " platforms";
            cntTmp.color         = new Color(1f, 1f, 1f, 0.65f);
            cntTmp.alignment     = TextAlignmentOptions.Right;
            cntTmp.raycastTarget = false;
            cntGo.AddComponent<LayoutElement>().minWidth = 100f;

            // Edit button
            var captureMap = map;
            var editBtn = MakeRowButton(rowGo.transform, "Edit", _blue, 72f, 38f);
            editBtn.onClick.AddListener(() => { Close(); _editorScreen.Open(captureMap.Clone()); });

            // Delete button (user maps only)
            if (!isDefault && fileName != null)
            {
                string capFile = fileName;
                var delBtn = MakeRowButton(rowGo.transform, "Del",
                    new Color(0.80f, 0.15f, 0.15f, 0.90f), 50f, 38f);
                delBtn.onClick.AddListener(() => { MapSerializer.DeleteMap(capFile); Refresh(); });
            }
        }

        Button MakeRowButton(Transform parent, string text, Color color, float w, float h)
        {
            var go = new GameObject("RBtn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor     = color;
            cols.highlightedColor = new Color(
                Mathf.Min(color.r + 0.15f, 1f),
                Mathf.Min(color.g + 0.15f, 1f),
                Mathf.Min(color.b + 0.15f, 1f), 1f);
            cols.pressedColor = _darkBlue;
            cols.fadeDuration = 0.07f;
            btn.colors = cols;

            go.AddComponent<LayoutElement>().minWidth  = w;
            go.GetComponent<LayoutElement>().minHeight = h;
            go.GetComponent<LayoutElement>().flexibleHeight = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(tmp, 13f, bold: true);
            tmp.text = text;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            return btn;
        }

        // ── New map (placeholder — dialog via editorWindow later) ─────────

        void OnNewMap()
        {
            // TODO: open new map dialog
            // For now just create an untitled map and open editor
            string name = "Map_" + DateTime.Now.ToString("HHmmss");
            var newMap = new MapData(name);
            MapSerializer.SaveMap(newMap, name);
            Close();
            _editorScreen.Open(newMap);
        }

        // ── Font helper ───────────────────────────────────────────────────

        static void ApplyGameFont(TextMeshProUGUI tmp, float size, bool bold)
        {
            try
            {
                var font = LocalizedText.localizationTable
                    ?.GetFont(Settings.Get().Language, useFontWithStroke: false);
                if (font != null) tmp.font = font;
            }
            catch { /* font not ready yet — Unity default is fine */ }

            tmp.fontSize  = size;
            tmp.fontStyle = bold ? FontStyles.Bold : FontStyles.Normal;
            tmp.color     = White;
            tmp.enableWordWrapping = false;
        }
    }
}

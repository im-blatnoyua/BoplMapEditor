using System;
using System.Collections.Generic;
using BoplMapEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Full-screen map browser injected into the game's own Canvas.
    // No separate overlay — lives as a child of the CharacterSelect canvas.
    public class MapBrowserScreen : MonoBehaviour
    {
        // ── Color palette ─────────────────────────────────────────────────
        static readonly Color BgDeep        = new Color(0.38f, 0.62f, 0.90f, 1f); // sky blue
        static readonly Color SidebarBg     = new Color(0.22f, 0.45f, 0.75f, 1f); // darker blue sidebar
        static readonly Color CardBg        = new Color(0.28f, 0.52f, 0.82f, 1f); // medium blue card
        static readonly Color CardHover     = new Color(0.35f, 0.60f, 0.90f, 1f);
        static readonly Color StripeColor   = new Color(1f,   1f,   1f,   0.025f);
        static readonly Color AccentLine    = new Color(0.93f, 0.64f, 0.12f, 1f); // matches _orange
        static readonly Color TextPrimary   = new Color(0.95f, 0.95f, 0.95f, 1f);
        static readonly Color TextMuted     = new Color(0.85f, 0.92f, 1.00f, 1f);
        static readonly Color DangerRed     = new Color(0.80f, 0.18f, 0.18f, 1f);
        static readonly Color White         = Color.white;

        const float TOP_BAR_H = 260f;

        // ── References ────────────────────────────────────────────────────
        private RectTransform         _root         = null!;
        private RectTransform         _listContent  = null!;
        private TextMeshProUGUI       _countLabel   = null!;
        private NativeMapEditorScreen _editorScreen = null!;

        private Color _blue;
        private Color _darkBlue;
        private Color _orange;

        // ── Factory ───────────────────────────────────────────────────────

        public static MapBrowserScreen Create(
            Transform canvasRoot,
            NativeMapEditorScreen editorScreen,
            Color blue, Color darkBlue, Color orange)
        {
            var go = new GameObject("MapBrowserScreen");
            go.transform.SetParent(canvasRoot, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var screen = go.AddComponent<MapBrowserScreen>();
            screen._root         = rt;
            screen._editorScreen = editorScreen;
            screen._blue         = blue;
            screen._darkBlue     = darkBlue;
            screen._orange       = orange;
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
            // Sky-blue full background
            _root.gameObject.AddComponent<Image>().color = BgDeep;

            // ── Top bar ───────────────────────────────────────────────────
            var topGo = new GameObject("TopBar");
            topGo.transform.SetParent(_root, false);
            topGo.AddComponent<Image>().color = new Color(0.20f, 0.42f, 0.75f, 1f);
            var trt = topGo.GetComponent<RectTransform>();
            trt.anchorMin = new Vector2(0f, 1f); trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(0f, -TOP_BAR_H); trt.offsetMax = Vector2.zero;

            var thlg = topGo.AddComponent<HorizontalLayoutGroup>();
            thlg.padding               = new RectOffset(24, 16, 0, 0);
            thlg.spacing               = 14;
            thlg.childAlignment         = TextAnchor.MiddleLeft;
            thlg.childForceExpandHeight = true;
            thlg.childForceExpandWidth  = false;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(topGo.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(titleTmp, 90f, true);
            titleTmp.text = "MAP EDITOR";
            titleTmp.color = White;
            titleTmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            titleTmp.raycastTarget = false;
            titleGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Map count label
            var countGo = new GameObject("Count");
            countGo.transform.SetParent(topGo.transform, false);
            _countLabel = countGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(_countLabel, 42f, false);
            _countLabel.color = TextMuted;
            _countLabel.alignment = TextAlignmentOptions.Right;
            _countLabel.raycastTarget = false;
            countGo.AddComponent<LayoutElement>().minWidth = 80f;

            // + NEW MAP button
            var newBtn = MakeButton(topGo.transform, "+ NEW MAP", _orange, 460f, 180f);
            newBtn.onClick.AddListener(OnNewMap);

            // BACK button
            var backBtn = MakeButton(topGo.transform, "\u2190 BACK", _darkBlue, 280f, 180f);
            backBtn.onClick.AddListener(Close);

            // Orange bottom accent on topbar
            var acc = new GameObject("Accent");
            acc.transform.SetParent(topGo.transform, false);
            acc.AddComponent<Image>().color = _orange;
            acc.GetComponent<Image>().raycastTarget = false;
            var art = acc.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(0f, 0f); art.anchorMax = new Vector2(1f, 0f);
            art.pivot = new Vector2(0.5f, 1f);
            art.offsetMin = new Vector2(0f, 0f); art.offsetMax = new Vector2(0f, 2f);

            // ── Scrollable map list (fills rest of screen) ────────────────
            BuildScrollArea(_root);
        }

        // ── Diagonal stripe overlay ───────────────────────────────────────

        void BuildStripes(RectTransform parent)
        {
            // 7 diagonal stripes at 45 degrees, evenly spaced across the screen.
            // Each is a thin rotated Image placed by anchor.
            for (int i = 0; i < 7; i++)
            {
                var sGo = new GameObject("Stripe" + i);
                sGo.transform.SetParent(parent, false);

                var img = sGo.AddComponent<Image>();
                img.color         = StripeColor;
                img.raycastTarget = false;

                var rt = sGo.GetComponent<RectTransform>();
                // Distribute stripes across width (0.05 to 0.95)
                float x = 0.05f + i * (0.90f / 6f);
                rt.anchorMin = new Vector2(x - 0.01f, 0f);
                rt.anchorMax = new Vector2(x + 0.01f, 1f);
                rt.offsetMin = rt.offsetMax = Vector2.zero;

                // Rotate 45 degrees
                sGo.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                // Scale up so rotated stripe doesn't leave gaps at edges
                sGo.transform.localScale = new Vector3(1f, 2.0f, 1f);
            }
        }

        // ── Left sidebar ──────────────────────────────────────────────────

        void BuildSidebar(Transform parent)
        {
            var sGo = new GameObject("Sidebar");
            sGo.transform.SetParent(parent, false);

            var img = sGo.AddComponent<Image>();
            img.color = SidebarBg;

            var le = sGo.AddComponent<LayoutElement>();
            le.minWidth      = 220f;
            le.preferredWidth = 220f;
            le.flexibleWidth = 0;

            // Orange accent line along the right edge of the sidebar
            var accentGo = new GameObject("AccentLine");
            accentGo.transform.SetParent(sGo.transform, false);
            var accentImg = accentGo.AddComponent<Image>();
            accentImg.color         = _orange;
            accentImg.raycastTarget = false;
            var art = accentGo.GetComponent<RectTransform>();
            art.anchorMin = new Vector2(1f, 0f);
            art.anchorMax = Vector2.one;
            art.offsetMin = new Vector2(-2f, 0f);
            art.offsetMax = Vector2.zero;

            // Vertical layout for sidebar contents
            var vlg = sGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(20, 22, 28, 24);
            vlg.spacing               = 0;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.UpperLeft;

            // -- Glowing dots row (indicator lights) --
            BuildIndicatorDots(sGo.transform);

            AddSpacer(sGo.transform, 14f);

            // -- Big bold title "MAP\nEDITOR" --
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(sGo.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(titleTmp, 36f, bold: true);
            titleTmp.text               = "MAP\nEDITOR";
            titleTmp.color              = White;
            titleTmp.fontStyle          = FontStyles.Bold | FontStyles.UpperCase;
            titleTmp.alignment          = TextAlignmentOptions.Left;
            titleTmp.enableWordWrapping = false;
            titleTmp.raycastTarget      = false;
            titleGo.AddComponent<LayoutElement>().minHeight = 88f;

            AddSpacer(sGo.transform, 6f);

            // -- Subtitle --
            var subGo = new GameObject("Subtitle");
            subGo.transform.SetParent(sGo.transform, false);
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(subTmp, 12f, bold: false);
            subTmp.text               = "SELECT A MAP";
            subTmp.color              = TextMuted;
            subTmp.fontStyle          = FontStyles.UpperCase;
            subTmp.alignment          = TextAlignmentOptions.Left;
            subTmp.enableWordWrapping = false;
            subTmp.raycastTarget      = false;
            subGo.AddComponent<LayoutElement>().minHeight = 18f;

            AddSpacer(sGo.transform, 16f);

            // -- Decorative horizontal rule --
            BuildHRule(sGo.transform);

            AddSpacer(sGo.transform, 22f);

            // -- + NEW MAP button --
            var newBtn = MakeButton(sGo.transform, "+ NEW MAP", _orange, 180f, 50f);
            newBtn.onClick.AddListener(OnNewMap);

            AddSpacer(sGo.transform, 12f);

            // -- BACK button --
            var backBtn = MakeButton(sGo.transform, "\u2190 BACK", _darkBlue, 180f, 44f);
            backBtn.onClick.AddListener(Close);

            // Flexible spacer to push version to bottom
            var flexGo = new GameObject("Flex");
            flexGo.transform.SetParent(sGo.transform, false);
            var flexLe = flexGo.AddComponent<LayoutElement>();
            flexLe.flexibleHeight = 1;

            // -- Version label at bottom --
            var verGo = new GameObject("Version");
            verGo.transform.SetParent(sGo.transform, false);
            var verTmp = verGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(verTmp, 11f, bold: false);
            verTmp.text               = "v1.0";
            verTmp.color              = new Color(0.35f, 0.40f, 0.50f, 1f);
            verTmp.alignment          = TextAlignmentOptions.Left;
            verTmp.enableWordWrapping = false;
            verTmp.raycastTarget      = false;
            verGo.AddComponent<LayoutElement>().minHeight = 18f;
        }

        void BuildIndicatorDots(Transform parent)
        {
            var rowGo = new GameObject("IndicatorDots");
            rowGo.transform.SetParent(parent, false);

            var hlg = rowGo.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing               = 8;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = false;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            rowGo.AddComponent<LayoutElement>().minHeight = 14f;

            // Blue dot
            MakeDot(rowGo.transform, _blue);
            // Orange dot
            MakeDot(rowGo.transform, _orange);
            // White dot
            MakeDot(rowGo.transform, new Color(0.9f, 0.9f, 0.9f, 0.75f));
        }

        void MakeDot(Transform parent, Color color)
        {
            var go = new GameObject("Dot");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color         = color;
            img.sprite        = StyleHelper.MakeRoundedSpriteSmall();
            img.type          = Image.Type.Sliced;
            img.raycastTarget = false;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth   = 10f;
            le.minHeight  = 10f;
            le.preferredWidth  = 10f;
            le.preferredHeight = 10f;
        }

        void BuildHRule(Transform parent)
        {
            var go = new GameObject("HRule");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color         = new Color(0.20f, 0.28f, 0.45f, 0.8f);
            img.raycastTarget = false;

            go.AddComponent<LayoutElement>().minHeight = 2f;
        }

        // ── Right content area ────────────────────────────────────────────

        void BuildContentArea(Transform parent)
        {
            var cGo = new GameObject("ContentArea");
            cGo.transform.SetParent(parent, false);

            // Transparent background (inherits the deep BgDeep already on root)
            var img = cGo.AddComponent<Image>();
            img.color = Color.clear;

            var le = cGo.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;

            var vlg = cGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(0, 0, 0, 0);
            vlg.spacing               = 0;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;

            // -- Top strip: map count --
            BuildTopStrip(cGo.transform);

            // -- Scrollable grid --
            BuildScrollArea(cGo.transform);
        }

        void BuildTopStrip(Transform parent)
        {
            var stripGo = new GameObject("TopStrip");
            stripGo.transform.SetParent(parent, false);

            var img = stripGo.AddComponent<Image>();
            img.color = new Color(0f, 0f, 0f, 0.20f);

            var le = stripGo.AddComponent<LayoutElement>();
            le.minHeight      = 40f;
            le.preferredHeight = 40f;
            le.flexibleHeight = 0;

            // Count label — right aligned
            var lblGo = new GameObject("CountLabel");
            lblGo.transform.SetParent(stripGo.transform, false);
            _countLabel = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(_countLabel, 42f, bold: false);
            _countLabel.text               = "0 MAPS";
            _countLabel.color              = TextMuted;
            _countLabel.fontStyle          = FontStyles.UpperCase;
            _countLabel.alignment          = TextAlignmentOptions.Right;
            _countLabel.enableWordWrapping = false;
            _countLabel.raycastTarget      = false;

            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(0f, 0f);
            lrt.offsetMax = new Vector2(-18f, 0f);
        }

        void BuildScrollArea(Transform parent)
        {
            var scrollGo = new GameObject("Scroll");
            scrollGo.transform.SetParent(parent, false);

            // When parent is _root (new layout), anchor below top bar
            bool isRoot = parent == (Transform)_root;
            if (isRoot)
            {
                var srt = scrollGo.AddComponent<RectTransform>();
                srt.anchorMin = Vector2.zero; srt.anchorMax = Vector2.one;
                srt.offsetMin = Vector2.zero; srt.offsetMax = new Vector2(0f, -TOP_BAR_H);
            }
            else
            {
                var scrollLe = scrollGo.AddComponent<LayoutElement>();
                scrollLe.flexibleHeight = 1;
                scrollLe.flexibleWidth  = 1;
            }

            // ScrollRect needs a background Image to receive pointer events
            scrollGo.AddComponent<Image>().color = Color.clear;

            var scroll = scrollGo.AddComponent<ScrollRect>();
            scroll.horizontal        = false;
            scroll.vertical          = true;
            scroll.scrollSensitivity = 40f;
            scroll.movementType      = ScrollRect.MovementType.Elastic;

            // Viewport
            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(scrollGo.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.white;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            // Content — 2-column grid via GridLayoutGroup
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0f, 1f);
            contentRt.anchorMax = new Vector2(1f, 1f);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = contentRt.offsetMax = Vector2.zero;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.padding               = new RectOffset(20, 20, 16, 20);
            vlg.spacing               = 10f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.UpperLeft;
            contentGo.AddComponent<ContentSizeFitter>().verticalFit =
                ContentSizeFitter.FitMode.PreferredSize;

            _listContent = contentRt;
            scroll.content = contentRt;
        }

        // ── Refresh map list ──────────────────────────────────────────────

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

            if (_countLabel != null)
                _countLabel.text = allMaps.Count + " MAPS";

            if (allMaps.Count == 0)
            {
                SpawnEmptyState();
                return;
            }

            // Single-column full-width cards
            foreach (var (map, isDefault, file) in allMaps)
                SpawnCard(_listContent, map, isDefault, file);
        }

        void SpawnEmptyState()
        {
            var cGo = new GameObject("EmptyState");
            cGo.transform.SetParent(_listContent, false);

            var vlg = cGo.AddComponent<VerticalLayoutGroup>();
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.childAlignment         = TextAnchor.MiddleCenter;
            vlg.spacing                = 10f;
            cGo.AddComponent<LayoutElement>().minHeight = 240f;

            // Big "?" icon
            var iconGo = new GameObject("Icon");
            iconGo.transform.SetParent(cGo.transform, false);
            var iconTmp = iconGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(iconTmp, 64f, bold: false);
            iconTmp.text      = "?";
            iconTmp.color     = new Color(0.25f, 0.32f, 0.50f, 1f);
            iconTmp.alignment = TextAlignmentOptions.Center;
            iconTmp.raycastTarget = false;
            iconGo.AddComponent<LayoutElement>().minHeight = 80f;

            // "No maps yet"
            var h1Go = new GameObject("H1");
            h1Go.transform.SetParent(cGo.transform, false);
            var h1Tmp = h1Go.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(h1Tmp, 22f, bold: true);
            h1Tmp.text      = "No maps yet";
            h1Tmp.color     = TextPrimary;
            h1Tmp.alignment = TextAlignmentOptions.Center;
            h1Tmp.fontStyle = FontStyles.Bold;
            h1Tmp.raycastTarget = false;
            h1Go.AddComponent<LayoutElement>().minHeight = 30f;

            // Sub-text
            var h2Go = new GameObject("H2");
            h2Go.transform.SetParent(cGo.transform, false);
            var h2Tmp = h2Go.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(h2Tmp, 15f, bold: false);
            h2Tmp.text      = "Create your first map!";
            h2Tmp.color     = TextMuted;
            h2Tmp.alignment = TextAlignmentOptions.Center;
            h2Tmp.raycastTarget = false;
            h2Go.AddComponent<LayoutElement>().minHeight = 22f;
        }

        // ── Single map card ───────────────────────────────────────────────

        void SpawnCard(Transform rowParent, MapData map, bool isDefault, string? fileName)
        {
            var cardGo = new GameObject("Card_" + map.Name);
            cardGo.transform.SetParent(rowParent, false);

            // Dark elevated panel
            var cardImg = cardGo.AddComponent<Image>();
            cardImg.color  = CardBg;
            cardImg.sprite = StyleHelper.MakeRoundedSprite();
            cardImg.type   = Image.Type.Sliced;

            // Whole-card button for hover highlight
            var cardBtn = cardGo.AddComponent<Button>();
            var cardCols = cardBtn.colors;
            cardCols.normalColor      = CardBg;
            cardCols.highlightedColor = CardHover;
            cardCols.pressedColor     = new Color(0.08f, 0.10f, 0.18f, 1f);
            cardCols.fadeDuration     = 0.08f;
            cardBtn.colors = cardCols;
            cardBtn.targetGraphic = cardImg;
            // Clicking the card opens the editor
            var captureMap = map;
            cardBtn.onClick.AddListener(() => { Close(); _editorScreen.Open(captureMap.Clone()); });

            var cardLe = cardGo.AddComponent<LayoutElement>();
            cardLe.minHeight  = 260f;
            cardLe.flexibleWidth = 1;

            // Inner horizontal layout
            var hlg = cardGo.AddComponent<HorizontalLayoutGroup>();
            hlg.padding               = new RectOffset(12, 10, 0, 0);
            hlg.spacing               = 12;
            hlg.childAlignment         = TextAnchor.MiddleLeft;
            hlg.childForceExpandWidth  = false;
            hlg.childForceExpandHeight = true;

            // ── Thumbnail colored square ──
            var thumbGo = new GameObject("Thumb");
            thumbGo.transform.SetParent(cardGo.transform, false);
            var thumbImg = thumbGo.AddComponent<Image>();
            thumbImg.color        = new Color(_blue.r, _blue.g, _blue.b, 0.85f);
            thumbImg.sprite       = StyleHelper.MakeRoundedSpriteSmall();
            thumbImg.type         = Image.Type.Sliced;
            thumbImg.raycastTarget = false;
            var thumbLe = thumbGo.AddComponent<LayoutElement>();
            thumbLe.minWidth   = 48f;
            thumbLe.minHeight  = 48f;
            thumbLe.preferredWidth  = 48f;
            thumbLe.preferredHeight = 48f;
            thumbLe.flexibleHeight  = 0;

            // Small platform count overlay on the thumb
            var thumbTxtGo = new GameObject("ThumbTxt");
            thumbTxtGo.transform.SetParent(thumbGo.transform, false);
            var thumbTxt = thumbTxtGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(thumbTxt, 14f, bold: true);
            thumbTxt.text               = map.Platforms.Count.ToString();
            thumbTxt.color              = White;
            thumbTxt.alignment          = TextAlignmentOptions.Center;
            thumbTxt.fontStyle          = FontStyles.Bold;
            thumbTxt.enableWordWrapping = false;
            thumbTxt.raycastTarget      = false;
            var trt = thumbTxtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = trt.offsetMax = Vector2.zero;

            // ── Text column ──
            var textColGo = new GameObject("TextCol");
            textColGo.transform.SetParent(cardGo.transform, false);

            var textVlg = textColGo.AddComponent<VerticalLayoutGroup>();
            textVlg.childForceExpandWidth  = true;
            textVlg.childForceExpandHeight = false;
            textVlg.childAlignment         = TextAnchor.MiddleLeft;
            textVlg.spacing                = 3f;
            textColGo.AddComponent<LayoutElement>().flexibleWidth = 1;

            // Map name
            var nameGo = new GameObject("Name");
            nameGo.transform.SetParent(textColGo.transform, false);
            var nameTmp = nameGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(nameTmp, 60f, bold: true);
            nameTmp.text               = map.Name;
            nameTmp.color              = TextPrimary;
            nameTmp.fontStyle          = FontStyles.Bold;
            nameTmp.alignment          = TextAlignmentOptions.Left;
            nameTmp.overflowMode       = TextOverflowModes.Ellipsis;
            nameTmp.enableWordWrapping = false;
            nameTmp.raycastTarget      = false;
            nameGo.AddComponent<LayoutElement>().minHeight = 24f;

            // "X platforms" sub-label
            var subGo = new GameObject("Sub");
            subGo.transform.SetParent(textColGo.transform, false);
            var subTmp = subGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(subTmp, 44f, bold: false);
            subTmp.text               = map.Platforms.Count + " platforms";
            subTmp.color              = TextMuted;
            subTmp.alignment          = TextAlignmentOptions.Left;
            subTmp.enableWordWrapping = false;
            subTmp.raycastTarget      = false;
            subGo.AddComponent<LayoutElement>().minHeight = 18f;

            // DEFAULT badge (if applicable)
            if (isDefault)
            {
                var badgeGo = new GameObject("DefaultBadge");
                badgeGo.transform.SetParent(textColGo.transform, false);

                var badgeImg = badgeGo.AddComponent<Image>();
                badgeImg.color        = _orange;
                badgeImg.sprite       = StyleHelper.MakeRoundedSpriteSmall();
                badgeImg.type         = Image.Type.Sliced;
                badgeImg.raycastTarget = false;

                var badgeLe = badgeGo.AddComponent<LayoutElement>();
                badgeLe.minWidth  = 64f;
                badgeLe.minHeight = 18f;
                badgeLe.flexibleWidth = 0;

                var badgeTxtGo = new GameObject("T");
                badgeTxtGo.transform.SetParent(badgeGo.transform, false);
                var badgeTxt = badgeTxtGo.AddComponent<TextMeshProUGUI>();
                ApplyGameFont(badgeTxt, 9f, bold: true);
                badgeTxt.text               = "DEFAULT";
                badgeTxt.color              = White;
                badgeTxt.fontStyle          = FontStyles.Bold | FontStyles.UpperCase;
                badgeTxt.alignment          = TextAlignmentOptions.Center;
                badgeTxt.enableWordWrapping = false;
                badgeTxt.raycastTarget      = false;
                var brt = badgeTxtGo.GetComponent<RectTransform>();
                brt.anchorMin = Vector2.zero;
                brt.anchorMax = Vector2.one;
                brt.offsetMin = new Vector2(4f, 2f);
                brt.offsetMax = new Vector2(-4f, -2f);
            }

            // ── Action buttons column ──
            var btnColGo = new GameObject("BtnCol");
            btnColGo.transform.SetParent(cardGo.transform, false);

            var btnVlg = btnColGo.AddComponent<VerticalLayoutGroup>();
            btnVlg.childForceExpandWidth  = true;
            btnVlg.childForceExpandHeight = false;
            btnVlg.childAlignment         = TextAnchor.MiddleRight;
            btnVlg.spacing                = 4f;
            btnColGo.AddComponent<LayoutElement>().minWidth = 68f;

            // EDIT button
            var editBtn = MakeSmallButton(btnColGo.transform, "EDIT", _blue, 60f, 28f);
            editBtn.onClick.RemoveAllListeners();
            editBtn.onClick.AddListener(() => { Close(); _editorScreen.Open(captureMap.Clone()); });

            // DEL button — user maps only
            if (!isDefault && fileName != null)
            {
                string capFile = fileName;
                var delBtn = MakeSmallButton(btnColGo.transform, "DEL", DangerRed, 60f, 28f);
                delBtn.onClick.AddListener(() => { MapSerializer.DeleteMap(capFile); Refresh(); });
            }
        }

        // ── Button builders ───────────────────────────────────────────────

        // Sidebar-sized button (wide, tall)
        Button MakeButton(Transform parent, string text, Color color, float minW, float minH)
        {
            var go = new GameObject("Btn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = color;
            cols.highlightedColor = new Color(
                Mathf.Min(color.r + 0.14f, 1f),
                Mathf.Min(color.g + 0.14f, 1f),
                Mathf.Min(color.b + 0.14f, 1f), 1f);
            cols.pressedColor = new Color(
                Mathf.Max(color.r - 0.10f, 0f),
                Mathf.Max(color.g - 0.10f, 0f),
                Mathf.Max(color.b - 0.10f, 0f), 1f);
            cols.fadeDuration = 0.07f;
            btn.colors = cols;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth     = minW;
            le.minHeight    = minH;
            le.flexibleHeight = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(tmp, 15f, bold: true);
            tmp.text               = text;
            tmp.color              = White;
            tmp.fontStyle          = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget      = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8f, 0f);
            lrt.offsetMax = new Vector2(-8f, 0f);

            return btn;
        }

        // Card action button (small)
        Button MakeSmallButton(Transform parent, string text, Color color, float minW, float minH)
        {
            var go = new GameObject("SBtn_" + text);
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            var cols = btn.colors;
            cols.normalColor      = color;
            cols.highlightedColor = new Color(
                Mathf.Min(color.r + 0.15f, 1f),
                Mathf.Min(color.g + 0.15f, 1f),
                Mathf.Min(color.b + 0.15f, 1f), 1f);
            cols.pressedColor = _darkBlue;
            cols.fadeDuration = 0.07f;
            btn.colors = cols;

            var le = go.AddComponent<LayoutElement>();
            le.minWidth     = minW;
            le.minHeight    = minH;
            le.flexibleHeight = 0;

            var lblGo = new GameObject("L");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(tmp, 11f, bold: true);
            tmp.text               = text;
            tmp.color              = White;
            tmp.fontStyle          = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment          = TextAlignmentOptions.Center;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget      = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = lrt.offsetMax = Vector2.zero;

            return btn;
        }

        // ── Layout helpers ────────────────────────────────────────────────

        static void AddSpacer(Transform parent, float height)
        {
            var go = new GameObject("Spacer");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = height;
        }

        // ── New map ───────────────────────────────────────────────────────

        // ── New Map dialog (name + environment) ───────────────────────────

        GameObject?      _newMapDialog;
        TMP_InputField?  _newMapNameInput;
        int              _newMapTheme = 0;
        Image[]          _envBtns = new Image[3];

        void OnNewMap()
        {
            if (_newMapDialog != null) { _newMapDialog.SetActive(true); _newMapNameInput!.text = ""; return; }

            // Dark overlay
            _newMapDialog = new GameObject("NewMapDialog");
            _newMapDialog.transform.SetParent(_root, false);
            var overlay = _newMapDialog.AddComponent<Image>();
            overlay.color = new Color(0f, 0f, 0f, 0.72f);
            var ort = _newMapDialog.GetComponent<RectTransform>();
            ort.anchorMin = Vector2.zero; ort.anchorMax = Vector2.one;
            ort.offsetMin = ort.offsetMax = Vector2.zero;

            // Card — wider and taller
            var card = new GameObject("Card");
            card.transform.SetParent(_newMapDialog.transform, false);
            var cardImg = card.AddComponent<Image>();
            cardImg.color  = new Color(0.12f, 0.20f, 0.38f, 1f);
            cardImg.sprite = StyleHelper.MakeRoundedSprite();
            cardImg.type   = Image.Type.Sliced;
            var crt = card.GetComponent<RectTransform>();
            crt.anchorMin = crt.anchorMax = crt.pivot = new Vector2(0.5f, 0.5f);
            crt.sizeDelta = new Vector2(1100f, 780f);

            var vlg = card.AddComponent<VerticalLayoutGroup>();
            vlg.padding = new RectOffset(32, 32, 28, 28);
            vlg.spacing = 20; vlg.childForceExpandWidth = true; vlg.childForceExpandHeight = false;

            // Title
            var titleGo = new GameObject("Title");
            titleGo.transform.SetParent(card.transform, false);
            var titleTmp = titleGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(titleTmp, 72f, true);
            titleTmp.text = "NEW MAP"; titleTmp.alignment = TextAlignmentOptions.Center;
            titleGo.AddComponent<LayoutElement>().minHeight = 36f;

            // Name input
            _newMapNameInput = UIBuilder.MakeInputField(card.transform, "Map name...",
                Vector2.zero, new Vector2(416f, 54f));
            _newMapNameInput.gameObject.AddComponent<LayoutElement>().minHeight = 120f;

            // Environment label
            var envLblGo = new GameObject("EnvLabel");
            envLblGo.transform.SetParent(card.transform, false);
            var envLblTmp = envLblGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(envLblTmp, 40f, false);
            envLblTmp.text = "ENVIRONMENT"; envLblTmp.alignment = TextAlignmentOptions.Center;
            envLblTmp.color = new Color(0.7f, 0.8f, 1f, 1f);
            envLblGo.AddComponent<LayoutElement>().minHeight = 36f;

            // Environment buttons row — big
            var envRow = new GameObject("EnvRow");
            envRow.transform.SetParent(card.transform, false);
            var ehlg = envRow.AddComponent<HorizontalLayoutGroup>();
            ehlg.spacing = 14; ehlg.childForceExpandWidth = true; ehlg.childForceExpandHeight = true;
            envRow.AddComponent<LayoutElement>().minHeight = 170f;

            string[] envNames  = { "GRASS", "SNOW", "SPACE" };
            Color[]  envColors = {
                new Color(0.22f, 0.58f, 0.15f, 1f),
                new Color(0.45f, 0.68f, 0.92f, 1f),
                new Color(0.06f, 0.06f, 0.18f, 1f),
            };
            _envBtns = new Image[3];

            for (int i = 0; i < 3; i++)
            {
                int t = i;
                var bGo = new GameObject("Env" + i);
                bGo.transform.SetParent(envRow.transform, false);
                var bImg = bGo.AddComponent<Image>();
                bImg.sprite = StyleHelper.MakeRoundedSprite(); bImg.type = Image.Type.Sliced;
                bImg.color  = t == _newMapTheme ? Color.white : envColors[t];
                _envBtns[t] = bImg;
                var bBtn = bGo.AddComponent<Button>();
                bBtn.onClick.AddListener(() => { _newMapTheme = t; RefreshEnvButtons(envColors); });
                var bLGo = new GameObject("L"); bLGo.transform.SetParent(bGo.transform, false);
                var bTmp = bLGo.AddComponent<TextMeshProUGUI>();
                ApplyGameFont(bTmp, 16f, true);
                bTmp.text = envNames[i]; bTmp.alignment = TextAlignmentOptions.Center;
                bTmp.color = t == 0 ? new Color(0.1f,0.1f,0.1f) : Color.white;
                bTmp.raycastTarget = false;
                var blrt = bLGo.GetComponent<RectTransform>();
                blrt.anchorMin = Vector2.zero; blrt.anchorMax = Vector2.one; blrt.offsetMin = blrt.offsetMax = Vector2.zero;
            }

            // Bottom row: Cancel + Create
            var botRow = new GameObject("BotRow");
            botRow.transform.SetParent(card.transform, false);
            var bhlg = botRow.AddComponent<HorizontalLayoutGroup>();
            bhlg.spacing = 14; bhlg.childForceExpandWidth = true; bhlg.childForceExpandHeight = true;
            botRow.AddComponent<LayoutElement>().minHeight = 140f;

            var cancelGo = new GameObject("Cancel");
            cancelGo.transform.SetParent(botRow.transform, false);
            var cancelImg = cancelGo.AddComponent<Image>();
            cancelImg.color  = new Color(0.55f, 0.10f, 0.10f, 1f);
            cancelImg.sprite = StyleHelper.MakeRoundedSprite(); cancelImg.type = Image.Type.Sliced;
            cancelGo.AddComponent<Button>().onClick.AddListener(() => _newMapDialog!.SetActive(false));
            DialogLabel(cancelGo.transform, "CANCEL");

            var createGo = new GameObject("Create");
            createGo.transform.SetParent(botRow.transform, false);
            var createImg = createGo.AddComponent<Image>();
            createImg.color  = _orange;
            createImg.sprite = StyleHelper.MakeRoundedSprite(); createImg.type = Image.Type.Sliced;
            createGo.AddComponent<Button>().onClick.AddListener(ConfirmNewMap);
            DialogLabel(createGo.transform, "CREATE");
        }

        void RefreshEnvButtons(Color[] envColors)
        {
            for (int i = 0; i < _envBtns.Length; i++)
            {
                if (_envBtns[i] == null) continue;
                _envBtns[i].color = i == _newMapTheme ? Color.white : envColors[i];
                var lbl = _envBtns[i].GetComponentInChildren<TextMeshProUGUI>();
                if (lbl != null) lbl.color = i == _newMapTheme ? new Color(0.1f,0.1f,0.1f) : Color.white;
            }
        }

        void DialogLabel(Transform parent, string text)
        {
            var lGo = new GameObject("L"); lGo.transform.SetParent(parent, false);
            var lTmp = lGo.AddComponent<TextMeshProUGUI>();
            ApplyGameFont(lTmp, 44f, true); lTmp.text = text;
            lTmp.alignment = TextAlignmentOptions.Center; lTmp.raycastTarget = false;
            var lrt = lGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one; lrt.offsetMin = lrt.offsetMax = Vector2.zero;
        }

        void ConfirmNewMap()
        {
            string name = _newMapNameInput != null && !string.IsNullOrWhiteSpace(_newMapNameInput.text)
                ? _newMapNameInput.text.Trim()
                : "Map_" + DateTime.Now.ToString("HHmmss");
            if (_newMapDialog != null) _newMapDialog.SetActive(false);
            var newMap = new MapData(name, _newMapTheme);
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
        }
    }
}

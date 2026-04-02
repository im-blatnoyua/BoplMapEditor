using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Factory helpers for creating UGUI elements that match the game's style.
    public static class UIBuilder
    {
        // ── Canvas ────────────────────────────────────────────────────────

        public static Canvas CreateCanvas(string name, int sortOrder = 100)
        {
            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight = 0.5f;
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

        // ── Panels ────────────────────────────────────────────────────────

        // Generic panel with Image background, stretch anchors via offsets.
        public static RectTransform Panel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type = Image.Type.Sliced;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        // Flat panel without rounded sprite — for full-bleed background areas.
        public static RectTransform FlatPanel(Transform parent, string name, Color color,
            Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;
            return rt;
        }

        // One-pixel-tall horizontal rule for visual separation.
        public static void AddRule(Transform parent, Color? color = null)
        {
            var go = new GameObject("Rule");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = color ?? StyleHelper.DarkBorder;
            var le = go.AddComponent<LayoutElement>();
            le.minHeight = 1;
            le.flexibleWidth = 1;
        }

        // ── Buttons ───────────────────────────────────────────────────────

        public static Button MakeButton(Transform parent, string text, Color color,
            Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color  = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize: 15f, bold: true);
            tmp.text = text;
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(6, 0);
            lrt.offsetMax = new Vector2(-6, 0);

            return btn;
        }

        // Icon-button: square, no label padding, centered content.
        public static Button MakeIconButton(Transform parent, string icon, Color color, float size)
        {
            var btn = MakeButton(parent, icon, color, new Vector2(size, size), Vector2.zero);
            // Tighten the label to fill
            var lrt = btn.transform.Find("Label")?.GetComponent<RectTransform>();
            if (lrt != null)
            {
                lrt.offsetMin = Vector2.zero;
                lrt.offsetMax = Vector2.zero;
            }
            return btn;
        }

        // ── Labels ────────────────────────────────────────────────────────

        public static TextMeshProUGUI MakeLabel(Transform parent, string text,
            Vector2 anchoredPos, Vector2 size, float fontSize = 16f, bool bold = false)
        {
            var go = new GameObject($"Label_{text}");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize, bold);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Left;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;
            return tmp;
        }

        // ── Input fields ──────────────────────────────────────────────────

        public static TMP_InputField MakeInputField(Transform parent, string placeholder,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color  = StyleHelper.DarkElevated;
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type   = Image.Type.Sliced;

            var field = go.AddComponent<TMP_InputField>();
            var rt    = go.GetComponent<RectTransform>();
            rt.sizeDelta        = size;
            rt.anchoredPosition = anchoredPos;

            // Placeholder
            var phGo  = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 14f);
            phTmp.text  = placeholder;
            phTmp.color = StyleHelper.TextMuted;
            phTmp.alignment = TextAlignmentOptions.Left;
            var phrt = phGo.GetComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero;
            phrt.anchorMax = Vector2.one;
            phrt.offsetMin = new Vector2(10, 2);
            phrt.offsetMax = new Vector2(-10, -2);

            // Text area
            var textGo  = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(textTmp, 14f);
            textTmp.color     = StyleHelper.TextPrimary;
            textTmp.alignment = TextAlignmentOptions.Left;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(10, 2);
            textRt.offsetMax = new Vector2(-10, -2);

            field.textViewport = textRt;
            field.textComponent = textTmp;
            field.placeholder  = phTmp;
            field.caretColor   = StyleHelper.White;

            return field;
        }

        // ── Toggle button ─────────────────────────────────────────────────

        public static Button MakeToggleButton(Transform parent, string text,
            Color activeColor, Color inactiveColor, Vector2 size, Vector2 pos)
        {
            var btn = MakeButton(parent, text, inactiveColor, size, pos);
            btn.gameObject.name = $"Toggle_{text}";
            return btn;
        }

        // ── Scroll view ───────────────────────────────────────────────────

        public static ScrollRect MakeScrollView(Transform parent, Vector2 anchorMin,
            Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject("ScrollView");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = anchorMin;
            rt.anchorMax = anchorMax;
            rt.offsetMin = offsetMin;
            rt.offsetMax = offsetMax;

            // Transparent background so the parent surface shows through
            var bg = go.AddComponent<Image>();
            bg.color = Color.clear;

            var scroll = go.AddComponent<ScrollRect>();
            scroll.horizontal = false;

            // Viewport
            var vpGo = new GameObject("Viewport");
            vpGo.transform.SetParent(go.transform, false);
            var vpRt = vpGo.AddComponent<RectTransform>();
            vpRt.anchorMin = Vector2.zero;
            vpRt.anchorMax = Vector2.one;
            vpRt.offsetMin = Vector2.zero;
            vpRt.offsetMax = Vector2.zero;
            vpGo.AddComponent<Image>().color = Color.clear;
            vpGo.AddComponent<Mask>().showMaskGraphic = false;
            scroll.viewport = vpRt;

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot     = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;

            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing             = 4f;
            vlg.childForceExpandWidth  = true;
            vlg.childForceExpandHeight = false;
            vlg.padding = new RectOffset(0, 0, 4, 4);

            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            scroll.content = contentRt;

            return scroll;
        }

        // ── Chip / badge ──────────────────────────────────────────────────

        // Small rounded chip label (e.g. "DEFAULT", platform count badge).
        public static TextMeshProUGUI MakeChip(Transform parent, string text,
            Color bgColor, float fontSize = 10f)
        {
            var go = new GameObject("Chip");
            go.transform.SetParent(parent, false);

            var img   = go.AddComponent<Image>();
            img.color  = bgColor;
            img.sprite = StyleHelper.MakeRoundedSpriteSmall();
            img.type   = Image.Type.Sliced;
            img.raycastTarget = false;

            var txtGo  = new GameObject("Text");
            txtGo.transform.SetParent(go.transform, false);
            var tmp    = txtGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize, bold: true);
            tmp.raycastTarget = false;
            tmp.text = text;

            var trt = txtGo.GetComponent<RectTransform>();
            trt.anchorMin = Vector2.zero;
            trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(5, 2);
            trt.offsetMax = new Vector2(-5, -2);

            return tmp;
        }
    }
}

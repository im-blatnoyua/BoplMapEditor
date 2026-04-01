using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Factory helpers for creating UGUI elements that match the game's style.
    public static class UIBuilder
    {
        public static Canvas CreateCanvas(string name, int sortOrder = 100)
        {
            var go = new GameObject(name);
            Object.DontDestroyOnLoad(go);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = sortOrder;
            go.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            go.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
            go.AddComponent<GraphicRaycaster>();
            return canvas;
        }

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

        public static Button MakeButton(Transform parent, string text, Color color,
            Vector2 size, Vector2 anchoredPos)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(parent, false);

            var img = go.AddComponent<Image>();
            img.color = color;           // тематический цвет на Image
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            // Label
            var labelGo = new GameObject("Label");
            labelGo.transform.SetParent(go.transform, false);
            var tmp = labelGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, fontSize: 16f, bold: true);
            tmp.text = text;
            var lrt = labelGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero;
            lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero;
            lrt.offsetMax = Vector2.zero;

            return btn;
        }

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
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;
            return tmp;
        }

        public static TMP_InputField MakeInputField(Transform parent, string placeholder,
            Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject("InputField");
            go.transform.SetParent(parent, false);

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.2f, 1f);
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type = Image.Type.Sliced;

            var field = go.AddComponent<TMP_InputField>();
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            rt.anchoredPosition = anchoredPos;

            // Placeholder
            var phGo = new GameObject("Placeholder");
            phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 15f);
            phTmp.text = placeholder;
            phTmp.color = new Color(0.5f, 0.5f, 0.5f, 1f);
            phTmp.alignment = TextAlignmentOptions.Left;
            var phrt = phGo.GetComponent<RectTransform>();
            phrt.anchorMin = Vector2.zero;
            phrt.anchorMax = Vector2.one;
            phrt.offsetMin = new Vector2(8, 0);
            phrt.offsetMax = new Vector2(-8, 0);

            // Text area
            var textGo = new GameObject("Text");
            textGo.transform.SetParent(go.transform, false);
            var textTmp = textGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(textTmp, 15f);
            textTmp.alignment = TextAlignmentOptions.Left;
            var textRt = textGo.GetComponent<RectTransform>();
            textRt.anchorMin = Vector2.zero;
            textRt.anchorMax = Vector2.one;
            textRt.offsetMin = new Vector2(8, 0);
            textRt.offsetMax = new Vector2(-8, 0);

            field.textViewport = textRt;
            field.textComponent = textTmp;
            field.placeholder = phTmp;
            field.caretColor = StyleHelper.White;

            return field;
        }

        // Colored toggle button (for tool selection)
        public static Button MakeToggleButton(Transform parent, string text,
            Color activeColor, Color inactiveColor, Vector2 size, Vector2 pos)
        {
            var btn = MakeButton(parent, text, inactiveColor, size, pos);
            btn.gameObject.name = $"Toggle_{text}";
            return btn;
        }

        // Scrollable list container
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

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.06f, 0.08f, 0.13f, 0.8f);

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
            var mask = vpGo.AddComponent<Mask>();
            mask.showMaskGraphic = false;
            scroll.viewport = vpRt;

            // Content
            var contentGo = new GameObject("Content");
            contentGo.transform.SetParent(vpGo.transform, false);
            var contentRt = contentGo.AddComponent<RectTransform>();
            contentRt.anchorMin = new Vector2(0, 1);
            contentRt.anchorMax = new Vector2(1, 1);
            contentRt.pivot = new Vector2(0.5f, 1f);
            contentRt.offsetMin = Vector2.zero;
            contentRt.offsetMax = Vector2.zero;
            var vlg = contentGo.AddComponent<VerticalLayoutGroup>();
            vlg.spacing = 4f;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;
            var csf = contentGo.AddComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            scroll.content = contentRt;

            return scroll;
        }
    }
}

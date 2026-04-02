using BoplMapEditor.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace BoplMapEditor.Patches
{
    // Injects a "Map Editor" button into the CharacterSelectHandler scene.
    // Styled to match ReadyButton — uses the game's own AnimationCurve for hover.
    [HarmonyPatch(typeof(CharacterSelectHandler), "Start")]
    public static class LobbyButtonPatch
    {
        static void Postfix(CharacterSelectHandler __instance)
        {
            StyleHelper.LoadGameColors();
            StyleHelper.ScanPlatformMaterials();
            LobbyButtonHelper.Inject(__instance.gameObject);
        }
    }

    [HarmonyPatch(typeof(CharacterSelectHandler_online), "Start")]
    public static class LobbyButtonOnlinePatch
    {
        static void Postfix(CharacterSelectHandler_online __instance)
        {
            StyleHelper.LoadGameColors();
            StyleHelper.ScanPlatformMaterials();
            LobbyButtonHelper.Inject(__instance.gameObject);
        }
    }

    public static class LobbyButtonHelper
    {
        public static void Inject(GameObject lobbyRoot)
        {
            try
            {
                const string TAG = "BoplMapEditorBtn";
                if (GameObject.Find(TAG) != null) return;

                // Overlay canvas so button renders above all lobby UI
                var canvasGo = new GameObject(TAG);
                Object.DontDestroyOnLoad(canvasGo);
                var canvas = canvasGo.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 1000;
                var scaler = canvasGo.AddComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);
                canvasGo.AddComponent<GraphicRaycaster>();

                // Button — matches ReadyButton dimensions and feel
                var btnGo = new GameObject("MapEditorButton");
                btnGo.transform.SetParent(canvasGo.transform, false);

                var img = btnGo.AddComponent<Image>();
                img.color  = StyleHelper.Blue;
                img.sprite = StyleHelper.MakeRoundedSprite();
                img.type   = Image.Type.Sliced;

                var btn = btnGo.AddComponent<Button>();
                StyleHelper.StyleButton(btn, StyleHelper.Blue);
                StyleHelper.AddPressColorSwap(btn);

                // Position: bottom-left, above the corner
                var rt = btnGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.sizeDelta = new Vector2(210, 54);
                rt.anchoredPosition = new Vector2(24, 24);

                // Animated hover scale — uses ReadyButton's AnimationCurve
                var hoverAnim = btnGo.AddComponent<HoverScaleAnimator>();
                hoverAnim.Curve = StyleHelper.GetHoverCurve();

                // Label with game font
                var lblGo = new GameObject("Label");
                lblGo.transform.SetParent(btnGo.transform, false);
                var tmp = lblGo.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(tmp, 18f, bold: true);
                tmp.text = "✏  Map Editor";
                tmp.raycastTarget = false;
                var lrt = lblGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(8, 0);
                lrt.offsetMax = new Vector2(-8, 0);

                btn.onClick.AddListener(() => Plugin.BrowserScreen.Open());

                Plugin.Log.LogInfo("[LobbyButtonPatch] Map Editor button added to lobby.");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[LobbyButtonPatch] Failed to inject button: {ex}");
            }
        }
    }

    // Replicates ReadyButton's hover scale animation using its AnimationCurve.
    public class HoverScaleAnimator : MonoBehaviour,
        UnityEngine.EventSystems.IPointerEnterHandler,
        UnityEngine.EventSystems.IPointerExitHandler
    {
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 1f, 0.15f, 1.08f);

        private bool _hovering;
        private float _t;
        private Vector3 _baseScale;

        void Start() => _baseScale = transform.localScale;

        void Update()
        {
            if (_hovering && _t < 1f) _t = Mathf.Min(_t + Time.deltaTime / 0.15f, 1f);
            else if (!_hovering && _t > 0f) _t = Mathf.Max(_t - Time.deltaTime / 0.15f, 0f);

            float s = Curve.Evaluate(_t);
            transform.localScale = _baseScale * s;
        }

        public void OnPointerEnter(UnityEngine.EventSystems.PointerEventData e) => _hovering = true;
        public void OnPointerExit(UnityEngine.EventSystems.PointerEventData e)  => _hovering = false;
    }
}

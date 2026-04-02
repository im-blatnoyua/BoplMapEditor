using BoplMapEditor.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Reflection;

namespace BoplMapEditor.Patches
{
    // Injects a "Map Editor" button into the CharacterSelectHandler scene.
    // Visually clones the ReadyButton — uses its sprite when available, falls back to
    // StyleHelper.MakeRoundedSprite().  Hover animation reuses ReadyButton's curve.
    [HarmonyPatch(typeof(CharacterSelectHandler), "Start")]
    public static class LobbyButtonPatch
    {
        static void Postfix(CharacterSelectHandler __instance)
        {
            StyleHelper.LoadGameColors();
            StyleHelper.ScanPlatformMaterials();
            // Invalidate cached sprite so we steal a fresh one from the new scene.
            StyleHelper.InvalidateReadyButtonSpriteCache();
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
            StyleHelper.InvalidateReadyButtonSpriteCache();
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

                // ── Find the game's existing overlay canvas ──────────────────
                Transform buttonParent = null;
                var allCanvases = Object.FindObjectsOfType<Canvas>(true);
                Plugin.Log.LogInfo($"[LobbyButtonPatch] Found {allCanvases.Length} canvases in scene.");
                foreach (var c in allCanvases)
                {
                    Plugin.Log.LogInfo($"[LobbyButtonPatch]   Canvas: {c.name} mode={c.renderMode} order={c.sortingOrder} active={c.gameObject.activeInHierarchy}");
                    if (c.renderMode == RenderMode.ScreenSpaceOverlay && c.gameObject.activeInHierarchy)
                    {
                        buttonParent = c.transform;
                        Plugin.Log.LogInfo($"[LobbyButtonPatch] Using existing canvas: {c.name}");
                        break;
                    }
                }

                // Fallback: create our own canvas
                if (buttonParent == null)
                {
                    Plugin.Log.LogWarning("[LobbyButtonPatch] No overlay canvas found — creating own canvas.");
                    var canvasGo = new GameObject(TAG + "_Canvas");
                    Object.DontDestroyOnLoad(canvasGo);
                    var canvas = canvasGo.AddComponent<Canvas>();
                    canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                    canvas.sortingOrder = 1000;
                    var scaler = canvasGo.AddComponent<CanvasScaler>();
                    scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                    scaler.referenceResolution = new Vector2(1920, 1080);
                    canvasGo.AddComponent<GraphicRaycaster>();
                    buttonParent = canvasGo.transform;
                }

                // ── Scale factor: design at 1080p ───────────────────────────
                float scale = Screen.height / 1080f;

                // ── Try to clone ReadyButton's look ──────────────────────────
                // Prefer the game's own sprite; fall back to our procedural one.
                Sprite btnSprite = StyleHelper.TryGetReadyButtonSprite()
                                   ?? StyleHelper.MakeRoundedSprite();

                // Mimic ReadyButton colors: use the game's blue as the normal tint.
                Color btnNormal  = StyleHelper.Blue;
                Color btnPressed = StyleHelper.Orange;

                // ── Button root ─────────────────────────────────────────────
                var btnGo = new GameObject(TAG);
                btnGo.transform.SetParent(buttonParent, false);

                var img    = btnGo.AddComponent<Image>();
                img.sprite = btnSprite;
                img.type   = Image.Type.Sliced;
                img.color  = btnNormal;
                // Preserve pixel-perfect rendering when using the game's sprite
                img.preserveAspect = false;

                var btn = btnGo.AddComponent<Button>();
                // Set up color transitions to match ReadyButton: normal→hover brightens, press→orange
                var cols = btn.colors;
                cols.normalColor      = btnNormal;
                cols.highlightedColor = new Color(
                    Mathf.Min(btnNormal.r + 0.15f, 1f),
                    Mathf.Min(btnNormal.g + 0.15f, 1f),
                    Mathf.Min(btnNormal.b + 0.18f, 1f), 1f);
                cols.pressedColor     = btnPressed;
                cols.selectedColor    = btnPressed;
                cols.disabledColor    = new Color(0.22f, 0.25f, 0.32f, 0.5f);
                cols.colorMultiplier  = 1f;
                cols.fadeDuration     = 0.08f;
                btn.colors = cols;
                btn.transition = Selectable.Transition.ColorTint;

                // PressColorSwapper for the smooth blue→orange flash
                var swapper          = btnGo.AddComponent<PressColorSwapper>();
                swapper.NormalColor  = btnNormal;
                swapper.PressedColor = btnPressed;

                // ── Size / position ─────────────────────────────────────────
                // Bottom-left, same corner that ReadyButton occupies in many UI layouts.
                var rt = btnGo.GetComponent<RectTransform>();
                rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 0f);
                rt.sizeDelta        = new Vector2(220 * scale, 58 * scale);
                rt.anchoredPosition = new Vector2(20 * scale, 20 * scale);
                Plugin.Log.LogInfo($"[LobbyButtonPatch] Screen={Screen.width}x{Screen.height} scale={scale:F3} " +
                                   $"btnSize={rt.sizeDelta} btnPos={rt.anchoredPosition}");

                // ── Hover scale animator ────────────────────────────────────
                var hoverAnim = btnGo.AddComponent<HoverScaleAnimator>();
                hoverAnim.Curve = StyleHelper.GetHoverCurve();

                // ── Label ───────────────────────────────────────────────────
                var lblGo = new GameObject("Label");
                lblGo.transform.SetParent(btnGo.transform, false);
                var tmp = lblGo.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(tmp, Mathf.Round(17f * scale), bold: true);
                tmp.text = "✏  Map Editor";
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.raycastTarget = false;

                var lrt = lblGo.GetComponent<RectTransform>();
                lrt.anchorMin = Vector2.zero;
                lrt.anchorMax = Vector2.one;
                lrt.offsetMin = new Vector2(10 * scale, 0);
                lrt.offsetMax = new Vector2(-10 * scale, 0);

                // ── Click handler ───────────────────────────────────────────
                btn.onClick.AddListener(() => Plugin.BrowserScreen.Open());

                Plugin.Log.LogInfo("[LobbyButtonPatch] Map Editor button injected into lobby.");
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

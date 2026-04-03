using BoplMapEditor.UI;
using HarmonyLib;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Reflection;

namespace BoplMapEditor.Patches
{
    [HarmonyPatch(typeof(CharacterSelectHandler), "Start")]
    public static class LobbyButtonPatch
    {
        static void Postfix(CharacterSelectHandler __instance)
        {
            Plugin.Log.LogInfo("[LobbyButtonPatch] CharacterSelectHandler.Start postfix fired");

            // joinColor is the "CLICK TO JOIN!" button — always visible, game-styled
            var joinSource = LobbyButtonHelper.GetFieldRef<RectTransform>(__instance.characterSelectBoxes[0], "joinColor");

            Color blue     = LobbyButtonHelper.GetField<Color>(__instance, "blue")     ?? new Color(0.20f, 0.42f, 0.80f, 1f);
            Color darkBlue = LobbyButtonHelper.GetField<Color>(__instance, "darkBlue") ?? new Color(0.10f, 0.22f, 0.50f, 1f);
            Color orange   = LobbyButtonHelper.GetField<Color>(__instance, "orange")   ?? new Color(0.95f, 0.55f, 0.10f, 1f);

            LobbyButtonHelper.Inject(joinSource?.gameObject, __instance.gameObject, blue, darkBlue, orange);
        }
    }

    [HarmonyPatch(typeof(CharacterSelectHandler_online), "Start")]
    public static class LobbyButtonOnlinePatch
    {
        static void Postfix(CharacterSelectHandler_online __instance)
        {
            Plugin.Log.LogInfo("[LobbyButtonPatch] CharacterSelectHandler_online.Start postfix fired");

            Color blue     = LobbyButtonHelper.GetField<Color>(__instance, "blue")     ?? new Color(0.20f, 0.42f, 0.80f, 1f);
            Color darkBlue = LobbyButtonHelper.GetField<Color>(__instance, "darkBlue") ?? new Color(0.10f, 0.22f, 0.50f, 1f);
            Color orange   = LobbyButtonHelper.GetField<Color>(__instance, "orange")   ?? new Color(0.95f, 0.55f, 0.10f, 1f);

            LobbyButtonHelper.Inject(null, __instance.gameObject, blue, darkBlue, orange);
        }
    }

    public static class LobbyButtonHelper
    {
        const string TAG = "BoplMapEditorBtn";

        // ── Step 1: clone the button (critical — must always succeed) ─────

        public static void Inject(GameObject? joinSource, GameObject lobbyRoot,
                                  Color blue, Color darkBlue, Color orange)
        {
            // ── Find canvas ───────────────────────────────────────────────
            Canvas? canvas = null;
            if (joinSource != null)
                canvas = joinSource.GetComponentInParent<Canvas>();
            if (canvas == null)
                canvas = lobbyRoot.GetComponentInParent<Canvas>()
                      ?? Object.FindObjectOfType<Canvas>();

            if (canvas == null)
            {
                Plugin.Log.LogError("[LobbyButtonPatch] No canvas found in scene.");
                return;
            }
            Plugin.Log.LogInfo($"[LobbyButtonPatch] Canvas: {canvas.name}");

            // ── Prevent duplicate ─────────────────────────────────────────
            if (canvas.transform.Find(TAG) != null)
            {
                Plugin.Log.LogInfo("[LobbyButtonPatch] Button already exists, skipping.");
                return;
            }

            // ── Clone button ──────────────────────────────────────────────
            try { CreateButton(joinSource, canvas, blue, darkBlue, orange); }
            catch (System.Exception ex)
            { Plugin.Log.LogError($"[LobbyButtonPatch] CreateButton failed: {ex}"); }

            // ── Create screens (separate try — button must survive) ────────
            try { CreateScreens(canvas, blue, darkBlue, orange); }
            catch (System.Exception ex)
            { Plugin.Log.LogError($"[LobbyButtonPatch] CreateScreens failed: {ex}"); }
        }

        static void CreateButton(GameObject? joinSource, Canvas canvas,
                                 Color blue, Color darkBlue, Color orange)
        {
            GameObject btnGo;

            if (joinSource != null)
            {
                // Clone joinColor — the "CLICK TO JOIN!" button, always visible + game-styled
                btnGo = Object.Instantiate(joinSource, canvas.transform, false);
                btnGo.name = TAG;

                // Remove any game-logic components
                foreach (var comp in btnGo.GetComponentsInChildren<MonoBehaviour>(true))
                {
                    var t = comp.GetType().Name;
                    if (t != "Image" && t != "Button" && t != "TextMeshProUGUI"
                        && t != "RectTransform" && t != "CanvasRenderer")
                        Object.Destroy(comp);
                }
                foreach (var et in btnGo.GetComponentsInChildren<EventTrigger>(true))
                    Object.Destroy(et);

                // Set label text
                foreach (var tmp in btnGo.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.text = "MAP EDITOR";
                    Plugin.Log.LogInfo($"[LobbyButtonPatch] Label set: {tmp.gameObject.name}");
                }

                Plugin.Log.LogInfo("[LobbyButtonPatch] Cloned joinColor button.");
            }
            else
            {
                // Fallback: build from scratch using game button sprite if available
                btnGo = BuildFallbackButton(canvas.transform, blue);
                Plugin.Log.LogInfo("[LobbyButtonPatch] Using fallback button.");
            }

            // ── Position: anchor to bottom-left, always visible ───────────
            var rt = btnGo.GetComponent<RectTransform>();
            if (rt == null) rt = btnGo.AddComponent<RectTransform>();
            rt.anchorMin        = new Vector2(0f, 0f);
            rt.anchorMax        = new Vector2(0f, 0f);
            rt.pivot            = new Vector2(0f, 0f);
            rt.sizeDelta        = new Vector2(200f, 56f);
            rt.anchoredPosition = new Vector2(20f, 20f);

            // ── Wire click ────────────────────────────────────────────────
            var trigger = btnGo.AddComponent<EventTrigger>();
            var entry   = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
            entry.callback.AddListener(_ => {
                var browser = Plugin.BrowserScreen;
                if (browser != null) browser.Open();
                else Plugin.Log.LogWarning("[LobbyButtonPatch] BrowserScreen is null on click!");
            });
            trigger.triggers.Add(entry);

            btnGo.SetActive(true);
            Plugin.Log.LogInfo($"[LobbyButtonPatch] Button placed at {rt.anchoredPosition}, size {rt.sizeDelta}");
        }

        static GameObject BuildFallbackButton(Transform parent, Color blue)
        {
            var go  = new GameObject(TAG);
            go.transform.SetParent(parent, false);

            var img    = go.AddComponent<Image>();
            img.color  = blue;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;

            go.AddComponent<Button>();

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "Map Editor";
            tmp.fontSize  = 16f;
            tmp.fontStyle = FontStyles.Bold;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = Vector2.zero; lrt.offsetMax = Vector2.zero;

            return go;
        }

        static void CreateScreens(Canvas canvas, Color blue, Color darkBlue, Color orange)
        {
            // Editor screen needs browser reference — set it after both are created
            var editor = NativeMapEditorScreen.Create(
                canvas.transform, Plugin.Editor, null!, blue, darkBlue, orange);

            var browser = MapBrowserScreen.Create(
                canvas.transform, editor, blue, darkBlue, orange);

            editor.SetBrowser(browser);
            Plugin.SetBrowserScreen(browser);

            Plugin.Log.LogInfo("[LobbyButtonPatch] Browser + Editor screens created.");
        }

        // ── Reflection helpers ────────────────────────────────────────────

        public static T? GetField<T>(object obj, string name) where T : struct
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f == null) return null;
            return (T)f.GetValue(obj);
        }

        public static T? GetFieldRef<T>(object obj, string name) where T : class
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            return f?.GetValue(obj) as T;
        }
    }
}

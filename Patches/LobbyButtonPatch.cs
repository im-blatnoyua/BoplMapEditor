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

            // Safe access — boxes might not be populated yet on re-entry
            GameObject? joinSource = null;
            try
            {
                if (__instance.characterSelectBoxes != null && __instance.characterSelectBoxes.Length > 0)
                {
                    var joinRt = LobbyButtonHelper.GetFieldRef<RectTransform>(
                        __instance.characterSelectBoxes[0], "joinColor");
                    joinSource = joinRt?.gameObject;
                }
            }
            catch { /* ignore — fallback button will be used */ }

            Color blue     = LobbyButtonHelper.GetField<Color>(__instance, "blue")     ?? new Color(0.20f, 0.42f, 0.80f, 1f);
            Color darkBlue = LobbyButtonHelper.GetField<Color>(__instance, "darkBlue") ?? new Color(0.10f, 0.22f, 0.50f, 1f);
            Color orange   = LobbyButtonHelper.GetField<Color>(__instance, "orange")   ?? new Color(0.95f, 0.55f, 0.10f, 1f);

            BoplMapEditor.Core.EditorSceneManager.ReturnScene = "CharacterSelect";
            LobbyButtonHelper.Inject(joinSource, __instance.gameObject, blue, darkBlue, orange);
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

            BoplMapEditor.Core.EditorSceneManager.ReturnScene = "ChSelect_online";
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

            // Reopen browser if returning from editor
            if (BoplMapEditor.Core.EditorSceneManager.ReopenBrowser)
            {
                BoplMapEditor.Core.EditorSceneManager.ReopenBrowser = false;
                // Must use the newly created screen — old one was destroyed with Level1
                Plugin.BrowserScreen?.Open();
            }

            Plugin.Log.LogInfo("[LobbyButtonPatch] Inject complete.");
        }

        static void CreateButton(GameObject? joinSource, Canvas canvas,
                                 Color blue, Color darkBlue, Color orange)
        {
            // Always build from scratch — use game's button sprite if available,
            // so the button looks native but is the right size
            var btnGo = BuildFallbackButton(canvas.transform, blue);
            Plugin.Log.LogInfo("[LobbyButtonPatch] Button built with game sprite.");

            // ── Position: anchor to bottom-left, always visible ───────────
            var rt = btnGo.GetComponent<RectTransform>();
            if (rt == null) rt = btnGo.AddComponent<RectTransform>();
            // Bottom-right corner — away from character boxes which are center/left
            rt.anchorMin        = new Vector2(1f, 0f);
            rt.anchorMax        = new Vector2(1f, 0f);
            rt.pivot            = new Vector2(1f, 0f);
            rt.sizeDelta        = new Vector2(500f, 140f);
            rt.anchoredPosition = new Vector2(-24f, 24f);

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

            var img   = go.AddComponent<Image>();
            // Prefer real game sprite, fall back to our procedural one
            var gameSprite = StyleHelper.GetGameButtonSprite();
            img.sprite = gameSprite ?? StyleHelper.MakeRoundedSprite();
            img.type   = Image.Type.Sliced;
            img.color  = blue;

            go.AddComponent<Button>();

            var lblGo = new GameObject("Label");
            lblGo.transform.SetParent(go.transform, false);
            var tmp = lblGo.AddComponent<TextMeshProUGUI>();
            tmp.text      = "MAP EDITOR";
            tmp.fontSize  = 52f;
            tmp.fontStyle = FontStyles.Bold | FontStyles.UpperCase;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.color     = Color.white;
            tmp.enableWordWrapping = false;
            tmp.raycastTarget = false;
            var lrt = lblGo.GetComponent<RectTransform>();
            lrt.anchorMin = Vector2.zero; lrt.anchorMax = Vector2.one;
            lrt.offsetMin = new Vector2(8f, 0f); lrt.offsetMax = new Vector2(-8f, 0f);

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

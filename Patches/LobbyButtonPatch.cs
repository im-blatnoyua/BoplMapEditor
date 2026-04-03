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
            var startButton = LobbyButtonHelper.GetFieldRef<AnimateInOutUI>(__instance, "startButton");
            if (startButton == null) { Plugin.Log.LogError("[LobbyButtonPatch] startButton not found"); return; }

            var blue     = LobbyButtonHelper.GetField<Color>(__instance, "blue");
            var darkBlue = LobbyButtonHelper.GetField<Color>(__instance, "darkBlue");
            var orange   = LobbyButtonHelper.GetField<Color>(__instance, "orange");

            LobbyButtonHelper.Inject(startButton, blue, darkBlue, orange);
        }
    }

    [HarmonyPatch(typeof(CharacterSelectHandler_online), "Start")]
    public static class LobbyButtonOnlinePatch
    {
        static void Postfix(CharacterSelectHandler_online __instance)
        {
            var startButton = LobbyButtonHelper.GetFieldRef<AnimateInOutUI>(__instance, "startButton");
            if (startButton == null) { Plugin.Log.LogError("[LobbyButtonPatch] online startButton not found"); return; }

            var blue     = LobbyButtonHelper.GetField<Color>(__instance, "blue");
            var darkBlue = LobbyButtonHelper.GetField<Color>(__instance, "darkBlue");
            var orange   = LobbyButtonHelper.GetField<Color>(__instance, "orange");

            LobbyButtonHelper.Inject(startButton, blue, darkBlue, orange);
        }
    }

    public static class LobbyButtonHelper
    {
        const string TAG = "BoplMapEditorBtn";

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

        public static void Inject(AnimateInOutUI startButton, Color? blue, Color? darkBlue, Color? orange)
        {
            try
            {
                if (GameObject.Find(TAG) != null) return;

                Color c_blue     = blue     ?? new Color(0.20f, 0.42f, 0.80f, 1f);
                Color c_darkBlue = darkBlue ?? new Color(0.10f, 0.22f, 0.50f, 1f);
                Color c_orange   = orange   ?? new Color(0.95f, 0.55f, 0.10f, 1f);

                // ── Find the game's canvas that owns startButton ──────────────
                var canvas = startButton.GetComponentInParent<Canvas>();
                if (canvas == null)
                {
                    Plugin.Log.LogError("[LobbyButtonPatch] Could not find parent Canvas of startButton.");
                    return;
                }
                Plugin.Log.LogInfo($"[LobbyButtonPatch] Using canvas: {canvas.name}");

                // ── Create NativeMapEditorScreen first (browser needs reference) ─
                var editor = NativeMapEditorScreen.Create(
                    canvas.transform,
                    Plugin.Editor,
                    null!,           // browser ref set below
                    c_blue, c_darkBlue, c_orange);

                // ── Create MapBrowserScreen inside the game's canvas ──────────
                var browser = MapBrowserScreen.Create(
                    canvas.transform,
                    editor,
                    c_blue, c_darkBlue, c_orange);

                // Wire back-reference: editor needs browser for Close()
                editor.SetBrowser(browser);

                Plugin.SetBrowserScreen(browser);

                // ── Clone startButton ─────────────────────────────────────────
                var clone = UnityEngine.Object.Instantiate(
                    startButton.gameObject,
                    startButton.transform.parent,
                    false);
                clone.name = TAG;

                // Remove EventTriggers that call the original handler
                foreach (var et in clone.GetComponentsInChildren<EventTrigger>(true))
                    UnityEngine.Object.Destroy(et);

                // Change label
                foreach (var tmp in clone.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    tmp.text = "Map Editor";
                    Plugin.Log.LogInfo($"[LobbyButtonPatch] Set label on '{tmp.gameObject.name}'");
                }

                // Wire our click handler
                var ourTrigger = clone.AddComponent<EventTrigger>();
                var click = new EventTrigger.Entry { eventID = EventTriggerType.PointerClick };
                click.callback.AddListener(_ => browser.Open());
                ourTrigger.triggers.Add(click);

                // Offset to the left so it doesn't overlap the real start button
                var rt = clone.GetComponent<RectTransform>();
                if (rt != null)
                    rt.anchoredPosition += new Vector2(-260f, 0f);

                // Animate in immediately (always visible, not tied to ready-state)
                var anim = clone.GetComponent<AnimateInOutUI>();
                anim?.AnimateIn();

                Plugin.Log.LogInfo("[LobbyButtonPatch] Map Editor button injected.");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[LobbyButtonPatch] Inject failed: {ex}");
            }
        }
    }
}

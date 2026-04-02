using BepInEx;
using BepInEx.Logging;
using BoplMapEditor.Core;
using BoplMapEditor.Patches;
using BoplMapEditor.UI;
using HarmonyLib;
using System;
using System.Reflection;
using UnityEngine;

namespace BoplMapEditor
{
    [BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
    public class Plugin : BaseUnityPlugin
    {
        public static Plugin Instance { get; private set; } = null!;
        public static ManualLogSource Log { get; private set; } = null!;
        public static MapEditorController Editor { get; private set; } = null!;
        public static MapEditorWindow EditorWindow { get; private set; } = null!;
        public static MapBrowserScreen BrowserScreen { get; private set; } = null!;

        void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo("[BoplMapEditor] Awake() started");

            Data.MapSerializer.EnsureDirectory();
            Editor = new MapEditorController();
            EditorWindow = MapEditorWindow.Create(Editor);
            BrowserScreen = MapBrowserScreen.Create(EditorWindow);
            Log.LogInfo("[BoplMapEditor] Core objects created OK");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            try { harmony.PatchAll(); }
            catch (Exception ex) { Log.LogWarning($"[BoplMapEditor] PatchAll partial failure (non-fatal): {ex.Message}"); }

            PatchStartRequestCtor(harmony);
            PatchStartRequestHandler(harmony);

            Log.LogInfo($"[BoplMapEditor] Loaded v{MyPluginInfo.PLUGIN_VERSION}.");
        }

        private void PatchStartRequestCtor(Harmony harmony)
        {
            try
            {
                var ctors = typeof(StartRequestPacket).GetConstructors(
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (ctors.Length == 0) { Log.LogWarning("[BoplMapEditor] StartRequestPacket has no constructors."); return; }
                var postfix = typeof(StartRequestPacket_CtorPatch).GetMethod(
                    nameof(StartRequestPacket_CtorPatch.Postfix), BindingFlags.Static | BindingFlags.Public);
                harmony.Patch(ctors[0], postfix: new HarmonyMethod(postfix));
                Log.LogInfo("[BoplMapEditor] Patched StartRequestPacket constructor.");
            }
            catch (Exception ex)
            {
                Log.LogWarning($"[BoplMapEditor] Could not patch StartRequestPacket ctor: {ex.Message}");
            }
        }

        private void PatchStartRequestHandler(Harmony harmony)
        {
            foreach (var methodName in new[] { "Handle", "Execute", "Apply", "Process", "Start" })
            {
                try
                {
                    var target = typeof(StartRequestPacket).GetMethod(methodName,
                        BindingFlags.Instance | BindingFlags.Static |
                        BindingFlags.Public | BindingFlags.NonPublic);
                    if (target == null) continue;

                    var prefix = typeof(StartRequestHandlerPatch).GetMethod(
                        nameof(StartRequestHandlerPatch.Prefix),
                        BindingFlags.Static | BindingFlags.Public);
                    harmony.Patch(target, prefix: new HarmonyMethod(prefix));
                    Log.LogInfo($"[BoplMapEditor] Patched StartRequestPacket.{methodName}");
                    return;
                }
                catch (Exception ex)
                {
                    Log.LogWarning($"[BoplMapEditor] Could not patch StartRequestPacket.{methodName}: {ex.Message}");
                }
            }
            Log.LogWarning("[BoplMapEditor] Could not find StartRequestPacket handler — multiplayer sync may not work.");
        }
    }

    public static class MyPluginInfo
    {
        public const string PLUGIN_GUID    = "com.boplmapeditor.mod";
        public const string PLUGIN_NAME    = "Bopl Map Editor";
        public const string PLUGIN_VERSION = "1.0.0";
    }
}

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

        private static MapEditorWindow? _editorWindow;
        private static MapBrowserScreen? _browserScreen;

        // EditorWindow is created lazily (has its own canvas, always available)
        public static MapEditorWindow EditorWindow
        {
            get { if (_editorWindow == null) CreateEditorWindow(); return _editorWindow!; }
        }

        // BrowserScreen is injected into the lobby canvas by LobbyButtonPatch.
        // Call SetBrowserScreen() from the patch; fallback creates standalone version.
        public static MapBrowserScreen BrowserScreen
        {
            get { if (_browserScreen == null) CreateEditorWindow(); return _browserScreen!; }
        }

        // Called by LobbyButtonPatch when it has access to the game's canvas and colors
        public static void SetBrowserScreen(MapBrowserScreen screen)
        {
            _browserScreen = screen;
        }

        private static void CreateEditorWindow()
        {
            if (_editorWindow != null) return;
            Log.LogInfo("[BoplMapEditor] Creating MapEditorWindow...");
            _editorWindow = MapEditorWindow.Create(Editor);
            Log.LogInfo("[BoplMapEditor] MapEditorWindow created OK.");
        }

        void Awake()
        {
            Instance = this;
            Log = Logger;
            Log.LogInfo("[BoplMapEditor] Awake() started");

            Data.MapSerializer.EnsureDirectory();
            Editor = new MapEditorController();
            Log.LogInfo("[BoplMapEditor] Core objects created OK");

            var harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
            try { harmony.PatchAll(); }
            catch (Exception ex) { Log.LogWarning($"[BoplMapEditor] PatchAll partial failure (non-fatal): {ex.Message}"); }

            PatchStartRequestCtor(harmony);
            PatchStartRequestHandler(harmony);

            Log.LogInfo($"[BoplMapEditor] Loaded v{MyPluginInfo.PLUGIN_VERSION} BUILD-20260402-C. Press Insert to open Map Editor.");
        }

        void Update()
        {
            if (UnityEngine.Input.GetKeyDown(KeyCode.Insert))
            {
                Log.LogInfo("[BoplMapEditor] Insert pressed — opening browser.");
                BrowserScreen.Open();
            }
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

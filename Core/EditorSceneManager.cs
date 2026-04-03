using BoplMapEditor.Data;
using BoplMapEditor.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Core
{
    public static class EditorSceneManager
    {
        public static bool     IsEditorScene { get; private set; }
        public static MapData? PendingMap    { get; private set; }

        static Canvas? _hiddenLobbyCanvas;

        private static readonly string[][] _scenes =
        {
            new[] { "Level1",  "Level2",  "Level3"  },  // Grass
            new[] { "Level22", "Level23", "Level24" },  // Snow
            new[] { "Level35", "Level37", "Level39" },  // Space
        };

        public static void Open(MapData map)
        {
            if (IsEditorScene) return;
            IsEditorScene = true;
            PendingMap    = map;

            // Hide lobby canvas
            _hiddenLobbyCanvas = Object.FindObjectOfType<Canvas>();
            if (_hiddenLobbyCanvas != null) _hiddenLobbyCanvas.enabled = false;

            var names = map.LevelTheme == 2 ? _scenes[2]
                      : map.LevelTheme == 1 ? _scenes[1]
                      : _scenes[0];

            SceneManager.sceneLoaded += OnLevelLoaded;

            foreach (var name in names)
            {
                try
                {
                    // Load as SINGLE — full scene init so all visuals work
                    SceneManager.LoadScene(name, LoadSceneMode.Single);
                    Plugin.Log.LogInfo($"[EditorSceneMgr] Loading '{name}'");
                    return;
                }
                catch { }
            }

            int idx = map.LevelTheme == 2 ? 60 : map.LevelTheme == 1 ? 30 : 6;
            SceneManager.LoadScene(idx, LoadSceneMode.Single);
            Plugin.Log.LogInfo($"[EditorSceneMgr] Loading scene index {idx}");
        }

        public static void Close()
        {
            IsEditorScene = false;
            PendingMap    = null;
            SceneManager.LoadScene("CharacterSelect");
            Plugin.Log.LogInfo("[EditorSceneMgr] → CharacterSelect");
        }

        static void OnLevelLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsEditorScene) return;
            SceneManager.sceneLoaded -= OnLevelLoaded;
            Plugin.Log.LogInfo($"[EditorSceneMgr] Level '{scene.name}' loaded");

            // Hide platforms — sprites scanned later in EditorBootstrap after Awake() runs
            foreach (var root in scene.GetRootGameObjects())
                foreach (var srr in root.GetComponentsInChildren<StickyRoundedRectangle>(true))
                    foreach (var sr in srr.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.enabled = false;

            var go = new GameObject("EditorBootstrap");
            go.AddComponent<EditorBootstrap>();
        }
    }

    // Harmony patch — skip GameSessionHandler.Init() when in editor mode
    // This prevents player spawning and battle start while keeping visuals intact
    [HarmonyPatch(typeof(GameSessionHandler), "Init")]
    public static class GameSessionHandler_EditorPatch
    {
        static bool Prefix()
        {
            if (!EditorSceneManager.IsEditorScene) return true; // run normally
            Plugin.Log.LogInfo("[EditorPatch] Skipped GameSessionHandler.Init (editor mode)");
            return false; // skip
        }
    }

    // Also patch Awake to prevent any pre-init setup
    [HarmonyPatch(typeof(GameSessionHandler), "Awake")]
    public static class GameSessionHandler_AwakePatch
    {
        static bool Prefix()
        {
            if (!EditorSceneManager.IsEditorScene) return true;
            Plugin.Log.LogInfo("[EditorPatch] Skipped GameSessionHandler.Awake (editor mode)");
            return false;
        }
    }

    public class EditorBootstrap : MonoBehaviour
    {
        int _frames;

        void Update()
        {
            if (++_frames < 3) return;

            if (EditorSceneManager.PendingMap == null)
            {
                EditorSceneManager.Close();
                Destroy(gameObject);
                return;
            }

            StyleHelper.LoadGameColors();
            // Scan AFTER Awake() ran — sprites are now valid
            StyleHelper.InvalidateMaterialCache();
            StyleHelper.ScanPlatformAssets();
            StyleHelper.ScanAllPlatformsFromScene();
            // Re-hide platforms (Awake may have re-enabled renderers)
            foreach (var srr in Object.FindObjectsOfType<StickyRoundedRectangle>(true))
                foreach (var sr in srr.GetComponentsInChildren<SpriteRenderer>(true))
                    sr.enabled = false;
            Plugin.Log.LogInfo($"[EditorBootstrap] {StyleHelper.ScannedPlatforms.Count} platforms scanned");

            var blue     = StyleHelper.Blue;
            var darkBlue = StyleHelper.DarkBlue;
            var orange   = StyleHelper.Orange;

            var canvasGo = new GameObject("EditorCanvas");
            var canvas   = canvasGo.AddComponent<Canvas>();
            canvas.renderMode   = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = canvasGo.AddComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode         = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1280, 720);
            scaler.matchWidthOrHeight  = 0.5f;
            canvasGo.AddComponent<UnityEngine.UI.GraphicRaycaster>();

            var screen = NativeMapEditorScreen.CreateInScene(
                canvasGo.transform, Plugin.Editor, blue, darkBlue, orange);

            screen.OpenWithMap(EditorSceneManager.PendingMap);
            Destroy(gameObject);
        }
    }
}

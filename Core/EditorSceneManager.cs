using BoplMapEditor.Data;
using BoplMapEditor.UI;
using HarmonyLib;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Core
{
    public static class EditorSceneManager
    {
        public static bool     IsEditorScene  { get; set; }
        public static MapData? PendingMap     { get; private set; }
        public static string   ReturnScene      { get; set; } = "CharacterSelect";
        public static bool     ReopenBrowser    { get; set; } = false;

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
            ReopenBrowser = true;
            // Clear old references — CharacterSelect reload will create fresh screens
            Plugin.SetBrowserScreen(null!);
            SceneManager.LoadScene(ReturnScene);
            Plugin.Log.LogInfo($"[EditorSceneMgr] → {ReturnScene} (reopen browser)");
        }

        static void OnLevelLoaded(Scene scene, LoadSceneMode mode)
        {
            if (!IsEditorScene) return;
            SceneManager.sceneLoaded -= OnLevelLoaded;
            Plugin.Log.LogInfo($"[EditorSceneMgr] Level '{scene.name}' loaded");

            foreach (var root in scene.GetRootGameObjects())
            {
                // Fully disable platform GameObjects (not just sprites)
                // — removes physics bodies and colliders from simulation
                foreach (var srr in root.GetComponentsInChildren<StickyRoundedRectangle>(true))
                    srr.gameObject.SetActive(false);

                // Disable ability/item spawners so nothing drops from the sky
                foreach (var spawner in root.GetComponentsInChildren<AbilitySpawner>(true))
                    spawner.gameObject.SetActive(false);
            }

            var go = new GameObject("EditorBootstrap");
            go.AddComponent<EditorBootstrap>();
        }
    }

    // Harmony patch — skip GameSessionHandler.Init() when in editor mode
    [HarmonyPatch(typeof(GameSessionHandler), "Init")]
    public static class GameSessionHandler_EditorPatch
    {
        static bool Prefix()
        {
            if (TestModeManager.IsTestMode) return true; // test mode: let it run
            if (!EditorSceneManager.IsEditorScene) return true;
            Plugin.Log.LogInfo("[EditorPatch] Skipped GameSessionHandler.Init (editor mode)");
            return false;
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

    [HarmonyPatch(typeof(AbilitySpawner), "Awake")]
    public static class AbilitySpawner_EditorPatch
    {
        static bool Prefix()
        {
            if (!EditorSceneManager.IsEditorScene) return true;
            return false; // skip — no items should spawn in editor
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
            // Try to find other type sprites from Unity's memory (no scene loading)
            StyleHelper.ScanSpritesFromMemory();
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

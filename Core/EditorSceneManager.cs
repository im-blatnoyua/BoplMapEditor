using BoplMapEditor.Data;
using BoplMapEditor.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Core
{
    // Manages the editor "scene" by:
    // 1. Loading the level additively (keeps its own visual init intact)
    // 2. Hiding the CharacterSelect canvas so only the level shows
    // 3. Showing a ScreenSpaceOverlay canvas with editor UI on top
    // 4. Cleaning up on close
    public static class EditorSceneManager
    {
        public static MapData? PendingMap    { get; private set; }
        public static bool     IsEditorScene { get; private set; }

        static string? _loadedScene;
        static Canvas? _hiddenLobbyCanvas;

        private static readonly string[][] _sceneNames =
        {
            new[] { "Level1", "Level2", "Level3" },
            new[] { "Level22", "Level23", "Level24" },
            new[] { "Level35", "Level37", "Level39" },
        };

        public static void Open(MapData map)
        {
            if (IsEditorScene) return;

            PendingMap    = map;
            IsEditorScene = true;

            // Hide the CharacterSelect canvas so lobby UI doesn't show through
            var lobbyCanvas = Object.FindObjectOfType<Canvas>();
            if (lobbyCanvas != null)
            {
                lobbyCanvas.enabled = false;
                _hiddenLobbyCanvas  = lobbyCanvas;
                Plugin.Log.LogInfo($"[EditorSceneMgr] Hid canvas: {lobbyCanvas.name}");
            }

            var names = map.LevelTheme == 2 ? _sceneNames[2]
                      : map.LevelTheme == 1 ? _sceneNames[1]
                      : _sceneNames[0];

            SceneManager.sceneLoaded += OnLevelLoaded;

            foreach (var name in names)
            {
                try
                {
                    SceneManager.LoadScene(name, LoadSceneMode.Additive);
                    _loadedScene = name;
                    Plugin.Log.LogInfo($"[EditorSceneMgr] Loading '{name}' additively");
                    return;
                }
                catch { }
            }

            // Fallback by index
            int idx = map.LevelTheme == 2 ? 60 : map.LevelTheme == 1 ? 30 : 6;
            try
            {
                SceneManager.LoadScene(idx, LoadSceneMode.Additive);
                _loadedScene = $"idx_{idx}";
                Plugin.Log.LogInfo($"[EditorSceneMgr] Loading index {idx} additively");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[EditorSceneMgr] Failed to load any level: {ex.Message}");
                Cleanup();
            }
        }

        public static void Close()
        {
            IsEditorScene = false;
            PendingMap    = null;
            Cleanup();
        }

        static void Cleanup()
        {
            SceneManager.sceneLoaded -= OnLevelLoaded;

            // Unload the level scene
            if (_loadedScene != null)
            {
                try { SceneManager.UnloadSceneAsync(_loadedScene); } catch { }
                _loadedScene = null;
            }

            // Restore lobby canvas
            if (_hiddenLobbyCanvas != null)
            {
                _hiddenLobbyCanvas.enabled = true;
                _hiddenLobbyCanvas = null;
            }

            Plugin.Log.LogInfo("[EditorSceneMgr] Closed.");
        }

        static void OnLevelLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive) return;
            SceneManager.sceneLoaded -= OnLevelLoaded;

            if (_loadedScene != null && _loadedScene.StartsWith("idx_"))
                _loadedScene = scene.name;

            Plugin.Log.LogInfo($"[EditorSceneMgr] Level '{scene.name}' ready");

            // Disable only gameplay logic — keep visual systems alive
            foreach (var root in scene.GetRootGameObjects())
                DisableGameLogic(root);

            // Set level cameras to depth 1 so they render over lobby camera (depth 0)
            foreach (var root in scene.GetRootGameObjects())
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                {
                    cam.depth = 1f;
                    Plugin.Log.LogInfo($"[EditorSceneMgr] Camera '{cam.name}' depth=1");
                }

            // Scan platform assets
            StyleHelper.InvalidateMaterialCache();
            StyleHelper.ScanPlatformAssets();

            // Spawn bootstrap to build editor UI
            var go = new GameObject("EditorBootstrap");
            SceneManager.MoveGameObjectToScene(go, scene);
            go.AddComponent<EditorBootstrap>();
        }

        static void DisableGameLogic(GameObject root)
        {
            string[] kill = {
                "GameSessionHandler", "PlayerHandler", "PlayerInit",
                "AbilitySpawner", "BoplCharacter", "PlayerBody",
            };
            foreach (var typeName in kill)
            {
                foreach (var asm in System.AppDomain.CurrentDomain.GetAssemblies())
                {
                    var t = asm.GetType(typeName);
                    if (t == null) continue;
                    foreach (var comp in root.GetComponentsInChildren(t, true))
                        if (comp is Behaviour b) b.enabled = false;
                    break;
                }
            }
        }
    }

    public class EditorBootstrap : MonoBehaviour
    {
        int _frames;

        void Update()
        {
            _frames++;
            if (_frames < 3) return;

            if (EditorSceneManager.PendingMap == null)
            {
                EditorSceneManager.Close();
                Destroy(gameObject);
                return;
            }

            StyleHelper.LoadGameColors();
            var blue     = StyleHelper.Blue;
            var darkBlue = StyleHelper.DarkBlue;
            var orange   = StyleHelper.Orange;

            // ScreenSpaceOverlay canvas — always on top regardless of camera depths
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

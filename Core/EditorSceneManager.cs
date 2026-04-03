using BoplMapEditor.Data;
using BoplMapEditor.UI;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Core
{
    // Manages full scene transitions for the map editor.
    // Open()  → loads Level1 as the editor backdrop (replaces current scene)
    // Close() → loads CharacterSelect back
    public static class EditorSceneManager
    {
        // Map waiting to be edited after the scene loads
        public static MapData? PendingMap { get; private set; }
        public static bool IsEditorScene { get; private set; }

        private static readonly string[] _grassScenes = { "Level1", "Level2", "Level3" };
        private static readonly string[] _snowScenes  = { "Level22", "Level23", "Level24" };
        private static readonly string[] _spaceScenes = { "Level35", "Level37", "Level39" };

        public static void Open(MapData map)
        {
            PendingMap    = map;
            IsEditorScene = true;

            string[] candidates = map.LevelTheme == 2 ? _spaceScenes
                                : map.LevelTheme == 1 ? _snowScenes
                                : _grassScenes;

            SceneManager.sceneLoaded += OnEditorSceneLoaded;

            foreach (var name in candidates)
            {
                try
                {
                    SceneManager.LoadScene(name);
                    Plugin.Log.LogInfo($"[EditorSceneManager] Loading scene '{name}'");
                    return;
                }
                catch { }
            }

            // Fallback by index
            int idx = map.LevelTheme == 2 ? 60 : map.LevelTheme == 1 ? 30 : 6;
            SceneManager.LoadScene(idx);
            Plugin.Log.LogInfo($"[EditorSceneManager] Loading scene by index {idx}");
        }

        public static void Close()
        {
            IsEditorScene = false;
            PendingMap    = null;
            SceneManager.LoadScene("CharacterSelect");
            Plugin.Log.LogInfo("[EditorSceneManager] Returning to CharacterSelect");
        }

        private static void OnEditorSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;
            if (!IsEditorScene) return;
            SceneManager.sceneLoaded -= OnEditorSceneLoaded;

            Plugin.Log.LogInfo($"[EditorSceneManager] Scene '{scene.name}' ready — spawning editor UI");

            // Disable game logic (players, abilities etc.)
            foreach (var root in scene.GetRootGameObjects())
                DisableGameLogic(root);

            // Spawn a bootstrap MonoBehaviour that builds the editor UI
            var go = new GameObject("EditorBootstrap");
            go.AddComponent<EditorBootstrap>();
        }

        private static void DisableGameLogic(GameObject root)
        {
            // Only kill player/game-session logic — keep visual/camera systems alive
            string[] kill = {
                "GameSessionHandler", "PlayerHandler", "PlayerInit",
                "AbilitySpawner", "BoplCharacter", "PlayerBody",
                "BoplBody", "DPhysicsManager", "DetPhysics"
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

    // Bootstrap spawned in the editor scene — waits a couple frames for
    // Unity to finish scene init, then builds the editor UI
    public class EditorBootstrap : MonoBehaviour
    {
        int _frames;

        void Update()
        {
            _frames++;
            // Wait 3 frames so cameras, Updater and visual systems fully initialise
            if (_frames < 3) return;

            if (EditorSceneManager.PendingMap == null)
            {
                Plugin.Log.LogWarning("[EditorBootstrap] No pending map — returning to lobby");
                EditorSceneManager.Close();
                Destroy(gameObject);
                return;
            }

            UI.StyleHelper.LoadGameColors();
            UI.StyleHelper.ScanPlatformAssets();
            var blue     = UI.StyleHelper.Blue;
            var darkBlue = UI.StyleHelper.DarkBlue;
            var orange   = UI.StyleHelper.Orange;

            // ScreenSpaceOverlay canvas — Level1 renders behind, our UI on top
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

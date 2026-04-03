using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Util
{
    // Loads a game level scene additively and renders it into a RenderTexture.
    // NativeMapEditorScreen displays that texture via RawImage — works regardless
    // of canvas render mode or camera depth ordering.
    public static class BackgroundSceneLoader
    {
        public static bool IsLoaded => _loadedSceneName != null;

        // The render texture NativeMapEditorScreen polls and assigns to RawImage
        public static RenderTexture? ActiveTexture { get; private set; }

        private static string? _loadedSceneName;
        private static LevelType _loadedLevelType;
        private static Camera? _levelCamera;

        // Confirmed scene names from BoplBattle_Data/globalgamemanagers
        private static readonly string[][] _sceneNames =
        {
            new[] { "Level1", "Level2", "Level3" },          // Grass FFA
            new[] { "Level22", "Level23", "Level24" },        // Snow FFA
            new[] { "Level35", "Level37", "Level39" },        // Space FFA
        };

        private static readonly LevelType[] _themeToLevelType =
        { LevelType.grass, LevelType.snow, LevelType.space };

        // ── Public API ────────────────────────────────────────────────────

        public static void Load(int themeIndex)
        {
            Unload();
            if (themeIndex < 0 || themeIndex >= _sceneNames.Length) return;

            _loadedLevelType = _themeToLevelType[themeIndex];

            foreach (var name in _sceneNames[themeIndex])
            {
                try
                {
                    SceneManager.LoadScene(name, LoadSceneMode.Additive);
                    _loadedSceneName = name;
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    Plugin.Log.LogInfo($"[BGLoader] Loading '{name}'");
                    return;
                }
                catch (Exception ex)
                {
                    Plugin.Log.LogWarning($"[BGLoader] '{name}' failed: {ex.Message}");
                }
            }

            // Fallback: load by build index (grass=index 6, snow≈30+, space≈60+)
            int startIdx = themeIndex == 0 ? 6 : themeIndex == 1 ? 30 : 60;
            for (int i = startIdx; i < startIdx + 10; i++)
            {
                try
                {
                    SceneManager.LoadScene(i, LoadSceneMode.Additive);
                    _loadedSceneName = $"scene_{i}";
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    Plugin.Log.LogInfo($"[BGLoader] Loaded by index {i}");
                    return;
                }
                catch (Exception) { }
            }

            Plugin.Log.LogWarning($"[BGLoader] Could not load any scene for theme {themeIndex}");
        }

        public static void Unload()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Release RenderTexture
            if (ActiveTexture != null)
            {
                if (_levelCamera != null) _levelCamera.targetTexture = null;
                ActiveTexture.Release();
                UnityEngine.Object.Destroy(ActiveTexture);
                ActiveTexture = null;
                _levelCamera = null;
            }

            if (_loadedSceneName == null) return;
            try { SceneManager.UnloadSceneAsync(_loadedSceneName); } catch { }
            _loadedSceneName = null;
            Plugin.Log.LogInfo("[BGLoader] Unloaded.");
        }

        // ── Scene loaded ──────────────────────────────────────────────────

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_loadedSceneName != null && _loadedSceneName.StartsWith("scene_"))
                _loadedSceneName = scene.name;

            Constants.leveltype = _loadedLevelType;
            Plugin.Log.LogInfo($"[BGLoader] Scene '{scene.name}' loaded.");

            // Disable game logic
            foreach (var root in scene.GetRootGameObjects())
                DisableGameLogic(root);

            // Find first camera in scene
            Camera? cam = null;
            foreach (var root in scene.GetRootGameObjects())
            {
                cam = root.GetComponentInChildren<Camera>(true);
                if (cam != null) break;
            }

            if (cam == null)
            {
                Plugin.Log.LogWarning("[BGLoader] No camera found in scene.");
                return;
            }

            // Create RenderTexture matching screen size
            int w = Screen.width  > 0 ? Screen.width  : 1920;
            int h = Screen.height > 0 ? Screen.height : 1080;
            var rt = new RenderTexture(w, h, 24, RenderTextureFormat.ARGB32);
            rt.Create();

            // Route camera output into the texture (camera stays in scene, no depth fighting)
            cam.targetTexture = rt;
            cam.enabled = true;
            _levelCamera = cam;
            ActiveTexture = rt;

            Plugin.Log.LogInfo($"[BGLoader] RenderTexture {w}x{h} assigned to camera '{cam.name}'.");

            UI.StyleHelper.InvalidateMaterialCache();
            UI.StyleHelper.ScanPlatformMaterials();
        }

        private static void DisableGameLogic(GameObject root)
        {
            string[] killTypes = {
                "GameSessionHandler", "PlayerHandler", "PlayerInit",
                "AbilitySpawner", "BoplCharacter", "PlayerBody",
            };
            foreach (var typeName in killTypes)
            {
                var type = FindType(typeName);
                if (type == null) continue;
                foreach (var comp in root.GetComponentsInChildren(type, true))
                    if (comp is Behaviour b) b.enabled = false;
            }
        }

        private static Type? FindType(string name)
        {
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                var t = asm.GetType(name);
                if (t != null) return t;
            }
            return null;
        }
    }
}

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Util
{
    // Loads a game level scene additively as editor background.
    public static class BackgroundSceneLoader
    {
        public static bool IsLoaded => _loadedSceneName != null;
        private static string? _loadedSceneName;
        private static LevelType _loadedLevelType;

        // Confirmed scene names from BoplBattle_Data/globalgamemanagers:
        // Grass FFA:       Assets/Scenes/FFA/Level1-21.unity        → name "Level1"–"Level13" unique
        // Snow FFA:        Assets/Scenes/FFA/Snow/Level22-26.unity  → name "Level22"–"Level26" unique
        // Space FFA:       Assets/Scenes/FFA/Space/Level35+.unity   → name "Level35", "Level37", "Level39"
        private static readonly string[][] _sceneNames =
        {
            // Grass (FFA/Level1-13 are unique to grass)
            new[] { "Level1", "Level2", "Level3" },
            // Snow (FFA/Snow/Level22-26 are unique to snow)
            new[] { "Level22", "Level23", "Level24" },
            // Space (FFA/Space/Level35+ are unique to space)
            new[] { "Level35", "Level37", "Level39" },
        };

        private static readonly LevelType[] _themeToLevelType =
        {
            LevelType.grass,
            LevelType.snow,
            LevelType.space,
        };

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
                    Plugin.Log.LogInfo($"[BackgroundSceneLoader] Loading scene '{name}'");
                    return;
                }
                catch (Exception)
                {
                    // scene not found — try next
                }
            }

            // If short names fail, try build index scanning
            TryLoadByIndex(themeIndex);
        }

        private static void TryLoadByIndex(int themeIndex)
        {
            // Keywords in scene paths to identify grass/snow/space
            string[] keywords = { "FFA/Level", "Snow/Level", "Space/Level" };
            string keyword = keywords[themeIndex];

            int total = SceneManager.sceneCountInBuildSettings;
            Plugin.Log.LogInfo($"[BackgroundSceneLoader] Scanning {total} build scenes for '{keyword}'...");

            for (int i = 6; i < total; i++) // skip menu scenes (0-5)
            {
                try
                {
                    // Try to figure out scene name by loading and checking
                    // We can't easily get path without SceneUtility, so try by index
                    if (themeIndex == 0 && i >= 6 && i <= 30)
                    {
                        SceneManager.LoadScene(i, LoadSceneMode.Additive);
                        _loadedSceneName = $"scene_{i}";
                        SceneManager.sceneLoaded += OnSceneLoaded;
                        Plugin.Log.LogInfo($"[BackgroundSceneLoader] Loaded scene by index {i}");
                        return;
                    }
                    else if (themeIndex == 1 && i >= 30 && i <= 60)
                    {
                        SceneManager.LoadScene(i, LoadSceneMode.Additive);
                        _loadedSceneName = $"scene_{i}";
                        SceneManager.sceneLoaded += OnSceneLoaded;
                        Plugin.Log.LogInfo($"[BackgroundSceneLoader] Loaded scene by index {i}");
                        return;
                    }
                    else if (themeIndex == 2 && i >= 60)
                    {
                        SceneManager.LoadScene(i, LoadSceneMode.Additive);
                        _loadedSceneName = $"scene_{i}";
                        SceneManager.sceneLoaded += OnSceneLoaded;
                        Plugin.Log.LogInfo($"[BackgroundSceneLoader] Loaded scene by index {i}");
                        return;
                    }
                }
                catch (Exception) { break; }
            }

            Plugin.Log.LogWarning($"[BackgroundSceneLoader] Could not find scene for theme {themeIndex}");
        }

        public static void Unload()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_loadedSceneName == null) return;

            try { SceneManager.UnloadSceneAsync(_loadedSceneName); }
            catch { }

            _loadedSceneName = null;
            Plugin.Log.LogInfo("[BackgroundSceneLoader] Background scene unloaded.");
        }

        // ── Scene loaded callback ─────────────────────────────────────────

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_loadedSceneName != null && _loadedSceneName.StartsWith("scene_"))
                _loadedSceneName = scene.name;

            Constants.leveltype = _loadedLevelType;
            Plugin.Log.LogInfo($"[BackgroundSceneLoader] '{scene.name}' ready. LevelType={_loadedLevelType}");

            // Disable game logic, keep visuals
            foreach (var root in scene.GetRootGameObjects())
                DisableGameLogic(root);

            // Set scene cameras to higher depth so they render ON TOP of lobby camera
            // This makes the game background visible through transparent editor areas
            foreach (var root in scene.GetRootGameObjects())
            {
                foreach (var cam in root.GetComponentsInChildren<Camera>(true))
                {
                    // depth=-5: renders BEFORE the UI canvas so ScreenSpaceOverlay sits on top.
                    // Transparent viewport area in the canvas then shows this camera's output.
                    cam.depth = 1f;
                    Plugin.Log.LogInfo($"[BackgroundSceneLoader] Set camera '{cam.name}' depth=1");
                }
            }

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
                foreach (var comp in root.GetComponentsInChildren(type, includeInactive: true))
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

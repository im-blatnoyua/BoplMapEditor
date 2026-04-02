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

        // Short scene names to try for each theme (fallback chain)
        private static readonly string[][] _sceneNames =
        {
            // Grass
            new[] { "Level1", "Level2", "Level3", "Level4", "Level5" },
            // Snow
            new[] { "Level14", "Level15", "Level17", "Level18" },
            // Space
            new[] { "Level35", "Level37", "Level39", "Level40" },
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

            // Update loaded scene name to actual name
            if (_loadedSceneName != null && _loadedSceneName.StartsWith("scene_"))
                _loadedSceneName = scene.name;

            // Set level type so water/background systems work
            Constants.leveltype = _loadedLevelType;
            Plugin.Log.LogInfo($"[BackgroundSceneLoader] '{scene.name}' ready. LevelType={_loadedLevelType}");

            // Disable game logic, keep visuals
            foreach (var root in scene.GetRootGameObjects())
                DisableGameLogic(root);

            // Scan for real platform materials from loaded level
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

using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Util
{
    // Loads a game level scene additively as editor background.
    // Uses SceneUtility to find correct scene indices from build settings.
    public static class BackgroundSceneLoader
    {
        public static bool IsLoaded => _loadedSceneName != null;
        private static string? _loadedSceneName;
        private static LevelType _loadedLevelType;

        // Scene path keywords to search for each theme in build settings
        private static readonly string[][] _pathKeywords =
        {
            // Grass — FFA level, not Snow/Space subfolder
            new[] { "/FFA/Level1.unity", "/FFA/Level2.unity", "/FFA/Level3.unity" },
            // Snow
            new[] { "/Snow/Level14.unity", "/Snow/Level15.unity", "/Snow/Level17.unity" },
            // Space
            new[] { "/Space/Level35.unity", "/Space/Level37.unity", "/Space/Level39.unity" },
        };

        private static readonly LevelType[] _themeToLevelType =
        {
            LevelType.grass,
            LevelType.snow,
            LevelType.space,
        };

        public static readonly string[] Labels = { "🌿 Grass", "❄ Snow", "🌌 Space" };

        // ── Public API ────────────────────────────────────────────────────

        public static void Load(int themeIndex)
        {
            Unload();

            if (themeIndex < 0 || themeIndex >= _pathKeywords.Length) return;

            int sceneIdx = FindSceneIndex(themeIndex);
            if (sceneIdx < 0)
            {
                Plugin.Log.LogWarning($"[BackgroundSceneLoader] No scene found for theme {themeIndex}");
                return;
            }

            _loadedLevelType = _themeToLevelType[themeIndex];

            try
            {
                string path = GetScenePathByIndex(sceneIdx);
                _loadedSceneName = System.IO.Path.GetFileNameWithoutExtension(path);
                SceneManager.sceneLoaded += OnSceneLoaded;
                SceneManager.LoadScene(sceneIdx, LoadSceneMode.Additive);
                Plugin.Log.LogInfo($"[BackgroundSceneLoader] Loading scene idx={sceneIdx} path='{path}'");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[BackgroundSceneLoader] Load error: {ex.Message}");
                _loadedSceneName = null;
            }
        }

        public static void Unload()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_loadedSceneName == null) return;

            try
            {
                SceneManager.UnloadSceneAsync(_loadedSceneName);
                Plugin.Log.LogInfo($"[BackgroundSceneLoader] Unloaded '{_loadedSceneName}'");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogWarning($"[BackgroundSceneLoader] Unload error: {ex.Message}");
            }

            _loadedSceneName = null;
        }

        // ── Scene loaded callback ─────────────────────────────────────────

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Additive) return;
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // Set level type so water/background systems work correctly
            Constants.leveltype = _loadedLevelType;
            Plugin.Log.LogInfo($"[BackgroundSceneLoader] Scene '{scene.name}' loaded. LevelType={_loadedLevelType}");

            // Disable game logic, keep visuals
            foreach (var root in scene.GetRootGameObjects())
                DisableGameLogic(root);

            // Scan new scene for real platform materials
            UI.StyleHelper.InvalidateMaterialCache();
            UI.StyleHelper.ScanPlatformMaterials();
        }

        private static void DisableGameLogic(GameObject root)
        {
            // Disable these component types by name (avoid hard references)
            string[] killTypes = {
                "GameSessionHandler", "PlayerHandler", "PlayerInit",
                "StartHandler", "AbilitySpawner", "RoundTimer",
                "BoplCharacter", "PlayerBody", "PlayerPhysics",
                "AIPlayer", "ManualPlayerController",
            };

            foreach (var typeName in killTypes)
            {
                var type = FindType(typeName);
                if (type == null) continue;
                foreach (var comp in root.GetComponentsInChildren(type, includeInactive: true))
                {
                    if (comp is Behaviour b) b.enabled = false;
                }
            }
        }

        // ── Helpers ───────────────────────────────────────────────────────

        private static int FindSceneIndex(int themeIndex)
        {
            var keywords = _pathKeywords[themeIndex];
            int total = SceneManager.sceneCountInBuildSettings;

            foreach (var keyword in keywords)
            {
                for (int i = 0; i < total; i++)
                {
                    string path = GetScenePathByIndex(i);
                    if (path.IndexOf(keyword, StringComparison.OrdinalIgnoreCase) >= 0)
                        return i;
                }
            }
            return -1;
        }

        // SceneUtility.GetScenePathByIndex via reflection (not in all Unity reference sets)
        private static readonly System.Reflection.MethodInfo? _getScenePath =
            System.Type.GetType("UnityEngine.SceneManagement.SceneUtility, UnityEngine.CoreModule")?
                .GetMethod("GetScenePathByIndex",
                    System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.Public,
                    null, new[] { typeof(int) }, null);

        private static string GetScenePathByIndex(int index)
        {
            if (_getScenePath != null)
                return (_getScenePath.Invoke(null, new object[] { index }) as string) ?? "";
            return "";
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

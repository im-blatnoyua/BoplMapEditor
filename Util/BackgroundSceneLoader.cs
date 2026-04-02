using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Util
{
    // Loads a game level scene additively as an editor background, then
    // disables game-logic components so only visuals remain.
    public static class BackgroundSceneLoader
    {
        public static bool IsLoaded => _loadedSceneName != null;
        private static string? _loadedSceneName;

        // Candidate scene names for each theme (short name first, then full path).
        // Unity will use the first one that exists in Build Settings.
        private static readonly string[][] _candidates =
        {
            // Grass / water
            new[] { "Level1",  "FFA/Level1"  },
            // Snow / ice
            new[] { "Level14", "FFA/Snow/Level14", "Level17", "FFA/Snow/Level17" },
            // Space
            new[] { "Level35", "FFA/Space/Level35", "Level37", "FFA/Space/Level37" },
        };

        public static readonly string[] Labels = { "🌿 Grass", "❄ Snow", "🌌 Space" };

        // ── Public API ────────────────────────────────────────────────────

        public static void Load(int themeIndex)
        {
            Unload();

            if (themeIndex < 0 || themeIndex >= _candidates.Length) return;

            foreach (var name in _candidates[themeIndex])
            {
                try
                {
                    SceneManager.LoadScene(name, LoadSceneMode.Additive);
                    _loadedSceneName = name;
                    SceneManager.sceneLoaded += OnSceneLoaded;
                    Plugin.Log.LogInfo($"[BackgroundSceneLoader] Loaded background scene '{name}'");
                    return;
                }
                catch (Exception)
                {
                    // scene not found — try next candidate
                }
            }

            Plugin.Log.LogWarning($"[BackgroundSceneLoader] No candidate scene found for theme {themeIndex}");
        }

        public static void Unload()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            if (_loadedSceneName == null) return;

            try
            {
                SceneManager.UnloadSceneAsync(_loadedSceneName);
                Plugin.Log.LogInfo($"[BackgroundSceneLoader] Unloaded background scene '{_loadedSceneName}'");
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

            // Disable game logic so only visuals remain
            foreach (var root in scene.GetRootGameObjects())
            {
                DisableGameLogic(root);
            }

            Plugin.Log.LogInfo($"[BackgroundSceneLoader] Background scene '{scene.name}' ready.");
        }

        private static void DisableGameLogic(GameObject root)
        {
            // Disable components that run game logic
            string[] disableTypes =
            {
                "GameSessionHandler", "PlayerManager", "StartHandler",
                "GameStateMachine", "AbilitySelectHandler", "RoundTimer",
                "BoplBody", "BoplCharacter", "AIPlayer",
            };

            foreach (var typeName in disableTypes)
            {
                var type = GetTypeByName(typeName);
                if (type == null) continue;
                foreach (var comp in root.GetComponentsInChildren(type, true))
                {
                    if (comp is Behaviour b) b.enabled = false;
                    else if (comp is Renderer r) { /* keep renderers */ }
                }
            }

            // Keep: Camera, Background, Water, Terrain, Particle, Renderer, Light
            // Destroy: Physics bodies that would run game simulation
        }

        private static Type? GetTypeByName(string name)
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

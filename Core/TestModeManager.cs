using BoplFixedMath;
using BoplMapEditor.Data;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Core
{
    public static class TestModeManager
    {
        public static bool     IsTestMode  { get; private set; }
        public static float    SpawnX      { get; private set; }
        public static float    SpawnY      { get; private set; }
        public static string   ReturnScene { get; private set; } = "CharacterSelect";
        public static MapData? TestMap     { get; private set; }

        static PlayerColors?    _cachedPlayerColors;
        static NamedSpriteList? _cachedAbilityIcons;

        public static void CacheFromLobby(CharacterSelectHandler handler)
        {
            try
            {
                var pcField = typeof(CharacterSelectHandler).GetField("playerColors",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                _cachedPlayerColors = pcField?.GetValue(handler) as PlayerColors;

                if (SteamManager.instance != null)
                    _cachedAbilityIcons = SteamManager.instance.abilityIcons;

                Plugin.Log.LogInfo($"[TestMode] CacheFromLobby: " +
                    $"colors={(_cachedPlayerColors != null ? "OK" : "null")} " +
                    $"icons={(_cachedAbilityIcons != null ? "OK" : "null")}");
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogWarning($"[TestMode] CacheFromLobby failed: {ex.Message}");
            }
        }

        public static void StartTest(MapData map, string returnScene)
        {
            if (map.SpawnPoints.Count == 0)
            {
                Plugin.Log.LogWarning("[TestMode] No spawn points placed.");
                return;
            }

            var sp = map.SpawnPoints.Find(s => s.PlayerId == 1) ?? map.SpawnPoints[0];
            SpawnX      = sp.X;
            SpawnY      = sp.Y;
            ReturnScene = returnScene;
            TestMap     = map;
            IsTestMode  = true;
            EditorSceneManager.IsEditorScene = false;

            Plugin.Log.LogInfo($"[TestMode] Starting test at ({SpawnX:F1},{SpawnY:F1})");

            // Level scene is still loaded here — grab a platform template before Tutorial replaces the scene
            Util.PlatformSpawner.PreserveTemplate();

            // Persistent Escape handler
            var sessionGo = new GameObject("TestModeSession");
            Object.DontDestroyOnLoad(sessionGo);
            sessionGo.AddComponent<TestModeSession>();

            SceneManager.LoadScene("Tutorial");
        }

        public static void End()
        {
            IsTestMode = false;
            Util.PlatformSpawner.ReleaseTemplate();
            Plugin.Log.LogInfo("[TestMode] Ended.");
        }
    }

    // Fires when TutorialGameHandler.PrepareTutorial runs — this is where Tutorial sets
    // up its own platforms. We let it finish, then immediately replace with our map.
    [HarmonyPatch(typeof(TutorialGameHandler), "PrepareTutorial")]
    public static class TutorialGameHandler_TestPatch
    {
        static void Postfix(TutorialGameHandler __instance)
        {
            if (!TestModeManager.IsTestMode) return;

            var map = TestModeManager.TestMap;
            if (map == null) return;

            Plugin.Log.LogInfo($"[TestMode] PrepareTutorial → replacing with '{map.Name}' ({map.Platforms.Count} platforms)");

            Util.PlatformSpawner.DestroyAllGamePlatforms();
            foreach (var p in map.Platforms)
                Util.PlatformSpawner.SpawnPlatform(p);
            Util.EnvironmentApplier.Apply(map.Environment);

            // Stop coroutines AFTER PrepareTutorial ran — kills the timer it just started
            __instance.StopAllCoroutines();
            Plugin.Log.LogInfo("[TestMode] TutorialGameHandler coroutines stopped");

            // Remove tutorial UI/trigger objects
            int removed = 0;
            foreach (var arrow in Object.FindObjectsOfType<TutorialArrow>(true))
                { Object.Destroy(arrow.gameObject); removed++; }
            foreach (var pickup in Object.FindObjectsOfType<TutorialArrowPickup>(true))
                { Object.Destroy(pickup.gameObject); removed++; }
            foreach (var dummy in Object.FindObjectsOfType<TutorialTargetDummy>(true))
                { Object.Destroy(dummy.gameObject); removed++; }
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
            {
                if (root.name.StartsWith("Tutorial", System.StringComparison.OrdinalIgnoreCase)
                    && root.GetComponent<TutorialGameHandler>() == null)
                    { Object.Destroy(root); removed++; }
            }
            Plugin.Log.LogInfo($"[TestMode] Removed {removed} tutorial UI objects");
        }
    }

    // Override teamSpawns[0] with our spawn point
    [HarmonyPatch(typeof(GameSessionHandler), "Init")]
    public static class GameSessionHandler_TestSpawnPatch
    {
        static void Prefix(GameSessionHandler __instance)
        {
            if (!TestModeManager.IsTestMode) return;
            try
            {
                var spawnsField = typeof(GameSessionHandler).GetField("teamSpawns",
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (spawnsField?.GetValue(__instance) is Vec2[] spawns && spawns.Length > 0)
                {
                    spawns[0] = new Vec2((Fix)TestModeManager.SpawnX, (Fix)TestModeManager.SpawnY);
                    Plugin.Log.LogInfo($"[TestMode] teamSpawns[0] → ({TestModeManager.SpawnX:F1},{TestModeManager.SpawnY:F1})");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[TestMode] Spawn override failed: {ex.Message}");
            }
        }
    }

    // Escape key exit — persists across scene loads
    public class TestModeSession : MonoBehaviour
    {
        void Update()
        {
            if (!TestModeManager.IsTestMode) { Destroy(gameObject); return; }

            if (Input.GetKeyDown(KeyCode.Escape))
            {
                Plugin.Log.LogInfo("[TestMode] Escape — returning to editor");
                Destroy(gameObject);
                TestModeManager.End();
                EditorSceneManager.ReopenBrowser = true;
                SceneManager.LoadScene(TestModeManager.ReturnScene);
            }
        }
    }

    // When CharacterSelect reloads after test — mark test as ended
    [HarmonyPatch(typeof(CharacterSelectHandler), "Awake")]
    public static class CharacterSelect_TestEndPatch
    {
        static void Postfix()
        {
            if (TestModeManager.IsTestMode)
            {
                TestModeManager.End();
                EditorSceneManager.ReopenBrowser = true;
            }
        }
    }
}

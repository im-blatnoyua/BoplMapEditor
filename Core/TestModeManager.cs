using BoplMapEditor.Data;
using BoplFixedMath;
using HarmonyLib;
using System.Reflection;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace BoplMapEditor.Core
{
    // Starts a real solo game session using map spawn points.
    // Patches GameSessionHandler to spawn player at our position.
    public static class TestModeManager
    {
        public static bool   IsTestMode  { get; private set; }
        public static float  SpawnX      { get; private set; }
        public static float  SpawnY      { get; private set; }
        public static string ReturnScene { get; private set; } = "CharacterSelect";

        public static void StartTest(MapData map, string returnScene)
        {
            if (map.SpawnPoints.Count == 0)
            {
                Plugin.Log.LogWarning("[TestMode] No spawn points placed.");
                return;
            }

            // Pick spawn 1, fallback to first
            var sp = map.SpawnPoints.Find(s => s.PlayerId == 1) ?? map.SpawnPoints[0];
            SpawnX     = sp.X;
            SpawnY     = sp.Y;
            ReturnScene = returnScene;
            IsTestMode  = true;

            // Set up 1 keyboard player in PlayerHandler
            if (!SetupSoloPlayer())
            {
                Plugin.Log.LogWarning("[TestMode] Could not setup player. Aborting.");
                IsTestMode = false;
                return;
            }

            Plugin.Log.LogInfo($"[TestMode] Starting solo test at ({SpawnX:F1},{SpawnY:F1})");

            // Start the game session
            GameSession.Init();
            GameLobby.isOnlineGame = false;

            // Load the level matching the map theme
            string[] scenesByTheme = { "Level1", "Level22", "Level35" };
            string scene = scenesByTheme[Mathf.Clamp(map.LevelTheme, 0, 2)];
            SceneManager.sceneLoaded += OnTestLevelLoaded;
            try { SceneManager.LoadScene(scene); }
            catch
            {
                SceneManager.LoadScene(6); // fallback by index
            }
        }

        static bool SetupSoloPlayer()
        {
            try
            {
                // Find PlayerColors ScriptableObject (loaded in memory from lobby)
                var allPC = Resources.FindObjectsOfTypeAll<PlayerColors>();
                if (allPC.Length == 0)
                {
                    Plugin.Log.LogWarning("[TestMode] PlayerColors not in memory.");
                    return false;
                }
                var pc = allPC[0];
                var material = pc[0].playerMaterial;
                if (material == null)
                {
                    Plugin.Log.LogWarning("[TestMode] No player material found.");
                    return false;
                }

                // Find ability icons
                var steamMgr = SteamManager.instance;
                if (steamMgr == null || steamMgr.abilityIcons == null)
                {
                    Plugin.Log.LogWarning("[TestMode] SteamManager/abilityIcons not available.");
                    return false;
                }

                var abilityIcons = steamMgr.abilityIcons;

                // Build 1 solo player
                var list = PlayerHandler.Get().PlayerList();
                list.Clear();
                var player = new Player(1, 0);
                player.Color              = material;
                player.UsesKeyboardAndMouse = true;
                player.CanUseAbilities    = true;
                player.Abilities          = new System.Collections.Generic.List<GameObject>();
                player.AbilityIcons       = new System.Collections.Generic.List<Sprite>();

                // Add first ability (e.g. index 0)
                if (abilityIcons.sprites != null && abilityIcons.sprites.Count > 0)
                {
                    player.Abilities.Add(abilityIcons.sprites[0].associatedGameObject);
                    player.AbilityIcons.Add(abilityIcons.sprites[0].sprite);
                }

                list.Add(player);
                Plugin.Log.LogInfo("[TestMode] Solo player set up OK.");
                return true;
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[TestMode] SetupSoloPlayer failed: {ex}");
                return false;
            }
        }

        static void OnTestLevelLoaded(Scene scene, LoadSceneMode mode)
        {
            SceneManager.sceneLoaded -= OnTestLevelLoaded;
            Plugin.Log.LogInfo($"[TestMode] Level '{scene.name}' loaded.");
            // GameSessionHandler.Init patch will override spawn
        }

        public static void End()
        {
            IsTestMode = false;
            Plugin.Log.LogInfo("[TestMode] Ended.");
        }
    }

    // Override teamSpawns[0] with our spawn point when test mode is active
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
                    Plugin.Log.LogInfo($"[TestMode] Overrode teamSpawns[0] → ({TestModeManager.SpawnX:F1},{TestModeManager.SpawnY:F1})");
                }
            }
            catch (System.Exception ex)
            {
                Plugin.Log.LogError($"[TestMode] Spawn override failed: {ex.Message}");
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
                TestModeManager.End();
        }
    }
}

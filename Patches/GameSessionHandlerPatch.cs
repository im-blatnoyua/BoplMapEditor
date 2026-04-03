using System.Linq;
using System.Reflection;
using BoplMapEditor.Sync;
using BoplMapEditor.Util;
using HarmonyLib;
using UnityEngine;

namespace BoplMapEditor.Patches
{
    // After the base scene loads, replace all platforms with the custom map's platforms.
    [HarmonyPatch(typeof(GameSessionHandler), "Awake")]
    public static class GameSessionHandlerPatch
    {
        static void Postfix(GameSessionHandler __instance)
        {
            if (!CustomMapState.PendingLoad) return;
            CustomMapState.PendingLoad = false;

            // Prefer a pending map pushed by the host; fall back to test map or editor map
            Data.MapData? map = null;
            if (LobbySync.HasCustomMap())
                map = LobbySync.PullMap();
            else if (Core.TestModeManager.IsTestMode)
                map = Core.TestModeManager.TestMap;
            else if (Plugin.Editor.IsOpen)
                map = Plugin.Editor.CurrentMap;

            if (map == null)
            {
                Plugin.Log.LogWarning("[GameSessionHandlerPatch] Custom map flag set but no map data found.");
                return;
            }

            Plugin.Log.LogInfo($"[GameSessionHandlerPatch] Loading custom map '{map.Name}' with {map.Platforms.Count} platforms.");

            PlatformSpawner.DestroyAllGamePlatforms();

            var spawned = map.Platforms
                .Select(p => PlatformSpawner.SpawnPlatform(p))
                .Where(s => s != null)
                .Cast<StickyRoundedRectangle>()
                .ToArray();

            // Inject spawned platforms into GameSessionHandler.grounds via reflection
            var groundsField = typeof(GameSessionHandler).GetField("grounds",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (groundsField != null)
                groundsField.SetValue(__instance, spawned);
            else
                Plugin.Log.LogWarning("[GameSessionHandlerPatch] Could not find 'grounds' field on GameSessionHandler.");

            // Apply level theme (background visuals)
            PlatformSpawner.ApplyTheme(map.LevelTheme);

            // Apply all environment & physics overrides
            Util.EnvironmentApplier.Apply(map.Environment);

            // Store env for PlayerSpawnPatch (applied after player spawn)
            PlayerSpawnPatch.WasCustomMapLoaded = true;
            PlayerSpawnPatch._lastEnv = map.Environment;
        }
    }

    public static class CustomMapState
    {
        public static bool PendingLoad;
    }
}

using BoplMapEditor.Util;
using HarmonyLib;

namespace BoplMapEditor.Patches
{
    // After players are spawned, apply gravity and friction overrides.
    // SpawnPlayers is called by GameSessionHandler after platform setup.
    [HarmonyPatch(typeof(GameSessionHandler), "SpawnPlayers")]
    public static class PlayerSpawnPatch
    {
        static void Postfix()
        {
            if (!CustomMapState.PendingLoad && !WasCustomMapLoaded) return;

            EnvironmentApplier.ApplyGravityToSpawnedPlayers();
            EnvironmentApplier.ApplyFrictionToSpawnedPlayers();
            EnvironmentApplier.ApplyRopeColors(_lastEnv);
        }

        public static bool WasCustomMapLoaded;
        public static Data.EnvironmentSettings _lastEnv = new Data.EnvironmentSettings();
    }
}

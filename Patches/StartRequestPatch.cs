using BoplMapEditor.Sync;
using HarmonyLib;

namespace BoplMapEditor.Patches
{
    // Sentinel: use currentLevel = 255 to signal "custom map" in the start packet.
    // This avoids changing the packet struct layout (which would break non-modded clients).

    public const byte CUSTOM_MAP_SENTINEL = 255;

    // Patch 1: When the host builds the StartRequestPacket, inject the sentinel if a custom map is active.
    [HarmonyPatch(typeof(StartRequestPacket), MethodType.Constructor)]
    public static class StartRequestPacket_CtorPatch
    {
        static void Postfix(ref StartRequestPacket __instance)
        {
            if (LobbySync.HasCustomMap())
            {
                __instance.currentLevel = CUSTOM_MAP_SENTINEL;
                Plugin.Log.LogInfo("[StartRequestPatch] Injected custom map sentinel into StartRequestPacket.");
            }
        }
    }

    // Patch 2: When any client (including host) processes the packet,
    // intercept the sentinel and trigger custom map loading.
    // We patch whichever method consumes currentLevel — search by signature at runtime in Plugin.Awake().
    public static class StartRequestHandlerPatch
    {
        // Applied dynamically in Plugin.Awake via AccessTools
        public static bool Prefix(StartRequestPacket packet)
        {
            if (packet.currentLevel != CUSTOM_MAP_SENTINEL) return true; // run original

            // Set the level to a valid real level (0) to avoid LoadScene out-of-bounds
            packet.currentLevel = 0;

            // Flag that after the scene loads we should replace platforms
            CustomMapState.PendingLoad = true;

            Plugin.Log.LogInfo("[StartRequestPatch] Custom map sentinel received — will load map after scene.");
            return true; // still run original to load the base scene
        }
    }
}

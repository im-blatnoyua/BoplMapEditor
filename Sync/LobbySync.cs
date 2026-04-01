using System;
using System.Text;
using BoplMapEditor.Data;
using Steamworks;
using Steamworks.Data;

namespace BoplMapEditor.Sync
{
    // Pushes/pulls custom map data through Steam lobby metadata.
    // Keys: CM_Active, CM_Name, CM_Theme, CM_0..CM_2 (Base64 chunks, 1200 bytes each)
    public static class LobbySync
    {
        private const string KEY_ACTIVE = "CM_Active";
        private const string KEY_NAME   = "CM_Name";
        private const string KEY_THEME  = "CM_Theme";
        private const string KEY_CHUNK  = "CM_";  // + chunk index: CM_0, CM_1, CM_2
        private const int    CHUNK_SIZE = 1200;
        private const int    MAX_CHUNKS = 3;

        private static Lobby CurrentLobby => SteamManager.instance.currentLobby;

        public static bool HasCustomMap()
        {
            try { return CurrentLobby.GetData(KEY_ACTIVE) == "1"; }
            catch { return false; }
        }

        public static void PushMap(MapData map)
        {
            try
            {
                string compressed = MapSerializer.SerializeCompressed(map);
                // Split into chunks
                var chunks = SplitChunks(compressed, CHUNK_SIZE);
                if (chunks.Length > MAX_CHUNKS)
                {
                    Plugin.Log.LogWarning($"[LobbySync] Map too large ({compressed.Length} chars), truncating to {MAX_CHUNKS} chunks.");
                }

                CurrentLobby.SetData(KEY_ACTIVE, "1");
                CurrentLobby.SetData(KEY_NAME, map.Name);
                CurrentLobby.SetData(KEY_THEME, map.LevelTheme.ToString());

                for (int i = 0; i < Math.Min(chunks.Length, MAX_CHUNKS); i++)
                    CurrentLobby.SetData(KEY_CHUNK + i, chunks[i]);

                // Clear leftover chunks from previous maps
                for (int i = chunks.Length; i < MAX_CHUNKS; i++)
                    CurrentLobby.SetData(KEY_CHUNK + i, "");

                Plugin.Log.LogInfo($"[LobbySync] Pushed map '{map.Name}' ({compressed.Length} chars, {chunks.Length} chunks)");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[LobbySync] PushMap failed: {ex.Message}");
            }
        }

        public static MapData? PullMap()
        {
            try
            {
                if (!HasCustomMap()) return null;

                var sb = new StringBuilder();
                for (int i = 0; i < MAX_CHUNKS; i++)
                {
                    string chunk = CurrentLobby.GetData(KEY_CHUNK + i);
                    if (string.IsNullOrEmpty(chunk)) break;
                    sb.Append(chunk);
                }

                string compressed = sb.ToString();
                if (string.IsNullOrEmpty(compressed)) return null;

                var map = MapSerializer.DeserializeCompressed(compressed);
                if (map != null)
                    Plugin.Log.LogInfo($"[LobbySync] Pulled map '{map.Name}' with {map.Platforms.Count} platforms");
                return map;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[LobbySync] PullMap failed: {ex.Message}");
                return null;
            }
        }

        public static void ClearMap()
        {
            try
            {
                CurrentLobby.SetData(KEY_ACTIVE, "0");
                CurrentLobby.SetData(KEY_NAME, "");
                CurrentLobby.SetData(KEY_THEME, "");
                for (int i = 0; i < MAX_CHUNKS; i++)
                    CurrentLobby.SetData(KEY_CHUNK + i, "");
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[LobbySync] ClearMap failed: {ex.Message}");
            }
        }

        public static string GetMapName()
        {
            try { return CurrentLobby.GetData(KEY_NAME); }
            catch { return ""; }
        }

        private static string[] SplitChunks(string s, int size)
        {
            int count = (s.Length + size - 1) / size;
            var chunks = new string[count];
            for (int i = 0; i < count; i++)
            {
                int start = i * size;
                int len = Math.Min(size, s.Length - start);
                chunks[i] = s.Substring(start, len);
            }
            return chunks;
        }
    }
}

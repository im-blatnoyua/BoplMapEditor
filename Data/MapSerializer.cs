using System;
using System.IO;
using System.IO.Compression;
using System.Text;
using UnityEngine;

namespace BoplMapEditor.Data
{
    public static class MapSerializer
    {
        public static string MapsDirectory =>
            Path.Combine(BepInEx.Paths.ConfigPath, "CustomMaps");

        public static void EnsureDirectory()
        {
            if (!Directory.Exists(MapsDirectory))
                Directory.CreateDirectory(MapsDirectory);
        }

        public static void SaveMap(MapData map, string name)
        {
            EnsureDirectory();
            map.Name = name;
            string json = JsonUtility.ToJson(map, prettyPrint: true);
            var path = Path.Combine(MapsDirectory, name + ".json");
            File.WriteAllText(path, json, Encoding.UTF8);
            Plugin.Log.LogInfo($"[MapSerializer] Saved to: {path}");
        }

        public static MapData? LoadMap(string name)
        {
            string path = Path.Combine(MapsDirectory, name + ".json");
            if (!File.Exists(path)) return null;
            try
            {
                string json = File.ReadAllText(path, Encoding.UTF8);
                var map = JsonUtility.FromJson<MapData>(json);
                if (map == null)
                    Plugin.Log.LogWarning($"[MapSerializer] FromJson returned null for '{name}'");
                return map;
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[MapSerializer] Failed to load '{name}': {ex.Message}");
                return null;
            }
        }

        public static string[] ListMaps()
        {
            EnsureDirectory();
            var files = Directory.GetFiles(MapsDirectory, "*.json");
            var names = new string[files.Length];
            for (int i = 0; i < files.Length; i++)
                names[i] = Path.GetFileNameWithoutExtension(files[i]);
            return names;
        }

        public static void DeleteMap(string name)
        {
            string path = Path.Combine(MapsDirectory, name + ".json");
            if (File.Exists(path)) File.Delete(path);
        }

        // Compress JSON to Base64 string for lobby metadata
        public static string Compress(string json)
        {
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            using var output = new MemoryStream();
            using (var deflate = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
                deflate.Write(bytes, 0, bytes.Length);
            return Convert.ToBase64String(output.ToArray());
        }

        // Decompress Base64 lobby metadata back to JSON
        public static string Decompress(string b64)
        {
            byte[] compressed = Convert.FromBase64String(b64);
            using var input = new MemoryStream(compressed);
            using var deflate = new DeflateStream(input, CompressionMode.Decompress);
            using var output = new MemoryStream();
            deflate.CopyTo(output);
            return Encoding.UTF8.GetString(output.ToArray());
        }

        public static string SerializeCompressed(MapData map)
        {
            string json = JsonUtility.ToJson(map);
            return Compress(json);
        }

        public static MapData? DeserializeCompressed(string b64)
        {
            try
            {
                string json = Decompress(b64);
                return JsonUtility.FromJson<MapData>(json);
            }
            catch (Exception ex)
            {
                Plugin.Log.LogError($"[MapSerializer] Failed to decompress map: {ex.Message}");
                return null;
            }
        }
    }
}

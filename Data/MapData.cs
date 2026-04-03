using System;
using System.Collections.Generic;

namespace BoplMapEditor.Data
{
    [Serializable]
    public class MapData
    {
        public string Name = "Untitled";
        public string Version = "1.0";
        public int LevelTheme; // 0=grass, 1=snow, 2=space (background visuals)
        public List<PlatformData> Platforms = new List<PlatformData>();
        public EnvironmentSettings Environment = EnvironmentSettings.ForGrass();

        public MapData() { }

        public MapData(string name, int theme = 0)
        {
            Name = name;
            LevelTheme = theme;
            Environment = theme == 2 ? EnvironmentSettings.ForSpace()
                        : theme == 1 ? EnvironmentSettings.ForSnow()
                        : EnvironmentSettings.ForGrass();
        }

        public List<SpawnPoint> SpawnPoints = new List<SpawnPoint>();

        public bool IsValid => SpawnPoints.Count >= 4;

        public MapData Clone()
        {
            var copy = new MapData(Name, LevelTheme);
            copy.Version = Version;
            copy.Environment = Environment.Clone();
            foreach (var p in Platforms)  copy.Platforms.Add(p.Clone());
            foreach (var s in SpawnPoints) copy.SpawnPoints.Add(s.Clone());
            return copy;
        }
    }

    [Serializable]
    public class SpawnPoint
    {
        public float X, Y;
        public int   PlayerId; // 1-4

        public SpawnPoint() { }
        public SpawnPoint(float x, float y, int playerId)
        { X = x; Y = y; PlayerId = playerId; }

        public SpawnPoint Clone() => new SpawnPoint(X, Y, PlayerId);
    }
}

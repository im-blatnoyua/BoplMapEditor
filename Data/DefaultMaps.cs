using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BoplMapEditor.Data
{
    // Built-in map presets + runtime capture from loaded game levels.
    public static class DefaultMaps
    {
        public static readonly string DEFAULT_TAG = "[Default]";

        // Tries to read the current loaded level's platforms and return a MapData.
        // Call this while a level scene is active (GameSessionHandler is in scene).
        public static MapData? CaptureCurrentLevel(string name)
        {
            var handler = Object.FindObjectOfType<GameSessionHandler>(true);
            if (handler == null)
            {
                Plugin.Log.LogWarning("[DefaultMaps] GameSessionHandler not found in scene.");
                return null;
            }

            // Read grounds array via reflection
            var groundsField = typeof(GameSessionHandler).GetField("grounds",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (groundsField?.GetValue(handler) is not StickyRoundedRectangle[] grounds)
            {
                Plugin.Log.LogWarning("[DefaultMaps] Could not read grounds array.");
                return null;
            }

            var themeField = typeof(GameSessionHandler).GetField("levelType",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            int theme = themeField != null ? (int)(LevelType)themeField.GetValue(handler) : 0;

            var map = new MapData(name, theme);

            foreach (var srr in grounds)
            {
                if (srr == null) continue;
                var ft = srr.GetComponent<FixTransform>();
                var rr = srr.GetComponent<DPhysicsRoundedRect>();
                if (ft == null || rr == null) continue;

                var pos  = ft.position;
                var ext  = rr.CalcExtents();
                var rad  = rr.radius;
                float rot = (float)ft.rotation * Mathf.Rad2Deg;

                map.Platforms.Add(new PlatformData(
                    (float)pos.x, (float)pos.y,
                    (float)ext.x, (float)ext.y,
                    (float)rad, rot,
                    (int)srr.platformType));
            }

            Plugin.Log.LogInfo($"[DefaultMaps] Captured {map.Platforms.Count} platforms from current level.");
            return map;
        }

        // Hardcoded default maps — typical Bopl Battle style layouts
        public static List<MapData> GetDefaults() => new List<MapData>
        {
            Classic(),
            BridgeMap(),
            SpaceArena(),
            SnowLadder(),
            Chaos(),
        };

        // ── Presets ───────────────────────────────────────────────────────

        private static MapData Classic()
        {
            var m = new MapData("Classic", 0);
            m.Platforms = new List<PlatformData>
            {
                new PlatformData(  0f, -12f, 30f, 1.5f, 1.2f, 0f, 0), // большая нижняя
                new PlatformData(-28f,  -2f, 12f, 1.5f, 1.0f, 0f, 0), // левая средняя
                new PlatformData( 28f,  -2f, 12f, 1.5f, 1.0f, 0f, 0), // правая средняя
                new PlatformData(  0f,  10f, 16f, 1.5f, 1.0f, 0f, 0), // верхняя центральная
                new PlatformData(-50f, -14f, 10f, 1.5f, 1.0f, 0f, 0), // дальняя левая
                new PlatformData( 50f, -14f, 10f, 1.5f, 1.0f, 0f, 0), // дальняя правая
            };
            return m;
        }

        private static MapData BridgeMap()
        {
            var m = new MapData("Bridge", 0);
            m.Platforms = new List<PlatformData>
            {
                new PlatformData(  0f, -15f, 50f, 1.5f, 1.2f, 0f, 0), // длинный мост
                new PlatformData(-55f,  -5f,  8f, 1.5f, 1.0f, 0f, 0),
                new PlatformData( 55f,  -5f,  8f, 1.5f, 1.0f, 0f, 0),
                new PlatformData(-25f,   5f,  6f, 1.5f, 1.0f, 0f, 0),
                new PlatformData( 25f,   5f,  6f, 1.5f, 1.0f, 0f, 0),
                new PlatformData(  0f,  18f,  5f, 5f,   5f,   0f, 0), // круглый верхний
            };
            return m;
        }

        private static MapData SpaceArena()
        {
            var m = new MapData("Space Arena", 2);
            m.Environment = EnvironmentSettings.ForSpace();
            m.Platforms = new List<PlatformData>
            {
                new PlatformData(  0f,  -5f, 20f, 1.5f, 1.2f, 0f, 3),
                new PlatformData(-35f,   5f, 10f, 1.5f, 1.0f, 0f, 3),
                new PlatformData( 35f,   5f, 10f, 1.5f, 1.0f, 0f, 3),
                new PlatformData(-20f,  18f,  8f, 1.5f, 1.0f, 15f, 3),
                new PlatformData( 20f,  18f,  8f, 1.5f, 1.0f,-15f, 3),
                new PlatformData(  0f,  30f,  5f, 5f,   5f,   0f, 3),
                new PlatformData(-60f,  -8f,  7f, 1.5f, 1.0f, 20f, 3),
                new PlatformData( 60f,  -8f,  7f, 1.5f, 1.0f,-20f, 3),
            };
            return m;
        }

        private static MapData SnowLadder()
        {
            var m = new MapData("Snow Ladder", 1);
            m.Environment = EnvironmentSettings.ForSnow();
            m.Platforms = new List<PlatformData>
            {
                new PlatformData(-40f, -18f, 18f, 1.5f, 1.0f, 0f, 1),
                new PlatformData( 20f, -10f, 14f, 1.5f, 1.0f, 0f, 2), // ice
                new PlatformData(-30f,  -2f, 14f, 1.5f, 1.0f, 0f, 1),
                new PlatformData( 30f,   6f, 12f, 1.5f, 1.0f, 0f, 2), // ice
                new PlatformData(-15f,  14f, 10f, 1.5f, 1.0f, 0f, 1),
                new PlatformData( 40f,  20f,  9f, 1.5f, 1.0f, 0f, 2),
            };
            return m;
        }

        private static MapData Chaos()
        {
            var m = new MapData("Chaos", 0);
            m.Platforms = new List<PlatformData>
            {
                new PlatformData(  0f, -16f, 15f, 1.5f, 1.2f,  0f, 0),
                new PlatformData(-45f, -10f,  8f, 1.5f, 1.0f,  15f, 0),
                new PlatformData( 45f, -10f,  8f, 1.5f, 1.0f, -15f, 0),
                new PlatformData(-20f,  -3f,  6f, 6f,   6f,    0f, 0), // круглый
                new PlatformData( 20f,  -3f,  6f, 6f,   6f,    0f, 0), // круглый
                new PlatformData(  0f,   8f, 12f, 1.5f, 1.0f, -8f, 0),
                new PlatformData(-50f,   5f,  6f, 1.5f, 1.0f,  30f, 0),
                new PlatformData( 50f,   5f,  6f, 1.5f, 1.0f, -30f, 0),
                new PlatformData(-10f,  20f,  5f, 1.5f, 1.0f,  0f, 0),
                new PlatformData( 10f,  20f,  5f, 1.5f, 1.0f,  0f, 0),
            };
            return m;
        }
    }
}

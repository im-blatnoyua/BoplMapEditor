using System;

namespace BoplMapEditor.Data
{
    // All per-map environment and physics overrides.
    // These map directly to fields in Constants, SceneBounds, PlayerPhysics.
    [Serializable]
    public class EnvironmentSettings
    {
        // ── Level type ────────────────────────────────────────────────────
        // 0=grass, 1=snow, 2=space
        // Affects: water presence, rope color, background visuals
        public int LevelType = 0;

        // ── Gravity ───────────────────────────────────────────────────────
        // Normal = 1.0, Space default = 0.5. Range: 0.1 – 2.0
        public float GravityMultiplier = 1.0f;

        // Rope gravity multiplier (separate from player gravity)
        // Normal = 1.0, Space default = 0.5
        public float RopeGravityMultiplier = 1.0f;

        // ── Water ─────────────────────────────────────────────────────────
        // Whether water exists on this map (false = acts like space re: destruction)
        public bool HasWater = true;

        // Y position of the water surface (default -11.3)
        public float WaterHeight = -11.3f;

        // ── Boundaries ───────────────────────────────────────────────────
        // Destruction Y when no water (default -26 = camera bottom)
        public float DestructionYNoWater = -26f;

        // Blast zone Y max (KO height, default 58)
        public float BlastZoneYMax = 58f;

        // ── Platform friction ─────────────────────────────────────────────
        // Fraction of speed kept per frame on normal platforms (default 0.5)
        public float NormalPlatformFriction = 0.5f;

        // Fraction of speed kept per frame on ice platforms (default 0.87)
        public float IcePlatformFriction = 0.87f;

        // ── Ability physics ───────────────────────────────────────────────
        // Exit velocity multiplier for abilities like Drill (space default 0.5)
        public float AbilityExitSpeedMultiplier = 1.0f;

        // ── Rope visuals ──────────────────────────────────────────────────
        // 0=black (grass), 1=white (snow/space), 2=colored
        public int RopeColorMode = 0;

        // ── Presets ───────────────────────────────────────────────────────
        public static EnvironmentSettings ForGrass() => new EnvironmentSettings
        {
            LevelType = 0, GravityMultiplier = 1f, RopeGravityMultiplier = 1f,
            HasWater = true, WaterHeight = -11.3f, NormalPlatformFriction = 0.5f,
            IcePlatformFriction = 0.87f, AbilityExitSpeedMultiplier = 1f,
            RopeColorMode = 0, BlastZoneYMax = 58f
        };

        public static EnvironmentSettings ForSnow() => new EnvironmentSettings
        {
            LevelType = 1, GravityMultiplier = 1f, RopeGravityMultiplier = 1f,
            HasWater = true, WaterHeight = -11.3f, NormalPlatformFriction = 0.5f,
            IcePlatformFriction = 0.87f, AbilityExitSpeedMultiplier = 1f,
            RopeColorMode = 1, BlastZoneYMax = 58f
        };

        public static EnvironmentSettings ForSpace() => new EnvironmentSettings
        {
            LevelType = 2, GravityMultiplier = 0.5f, RopeGravityMultiplier = 0.5f,
            HasWater = false, WaterHeight = -50f, NormalPlatformFriction = 0.5f,
            IcePlatformFriction = 0.87f, AbilityExitSpeedMultiplier = 0.5f,
            RopeColorMode = 1, BlastZoneYMax = 58f
        };

        public EnvironmentSettings Clone()
        {
            return new EnvironmentSettings
            {
                LevelType = LevelType,
                GravityMultiplier = GravityMultiplier,
                RopeGravityMultiplier = RopeGravityMultiplier,
                HasWater = HasWater,
                WaterHeight = WaterHeight,
                DestructionYNoWater = DestructionYNoWater,
                BlastZoneYMax = BlastZoneYMax,
                NormalPlatformFriction = NormalPlatformFriction,
                IcePlatformFriction = IcePlatformFriction,
                AbilityExitSpeedMultiplier = AbilityExitSpeedMultiplier,
                RopeColorMode = RopeColorMode,
            };
        }
    }
}

using System.Reflection;
using BoplFixedMath;
using BoplMapEditor.Data;
using UnityEngine;

namespace BoplMapEditor.Util
{
    // Applies EnvironmentSettings to the game's runtime systems.
    // Uses reflection to reach private/static fields in Constants, SceneBounds, PlayerPhysics.
    public static class EnvironmentApplier
    {
        public static void Apply(EnvironmentSettings env)
        {
            ApplyLevelType(env);
            ApplyGravity(env);
            ApplyWater(env);
            ApplyFriction(env);
            ApplyBlastZone(env);
            Plugin.Log.LogInfo($"[EnvironmentApplier] Applied environment: " +
                $"type={env.LevelType} gravity={env.GravityMultiplier} water={env.HasWater}");
        }

        // ── LevelType ─────────────────────────────────────────────────────
        private static void ApplyLevelType(EnvironmentSettings env)
        {
            // Constants.leveltype — static field
            SetStaticField(typeof(Constants), "leveltype", (LevelType)env.LevelType);
        }

        // ── Gravity ───────────────────────────────────────────────────────
        // PlayerPhysics uses: body.selfImposedVelocity += Vec2.down * gravity_accel * gravityModifier
        // We override the modifier at the PlayerPhysics component level.
        private static void ApplyGravity(EnvironmentSettings env)
        {
            // Patch every PlayerPhysics in the scene
            var all = Object.FindObjectsOfType<PlayerPhysics>(true);
            foreach (var pp in all)
            {
                SetField(pp, "gravity_modifier", (Fix)env.GravityMultiplier);
                SetField(pp, "gravity_accel",    (Fix)1.6f); // base value
            }

            // Also store in a static we can apply to newly spawned players
            // via GameSessionHandlerPatch
            _pendingGravityMultiplier   = env.GravityMultiplier;
            _pendingRopeGravityMultiplier = env.RopeGravityMultiplier;
        }

        public static float _pendingGravityMultiplier   = 1f;
        public static float _pendingRopeGravityMultiplier = 1f;

        // Called from GameSessionHandlerPatch after players spawn
        public static void ApplyGravityToSpawnedPlayers()
        {
            var all = Object.FindObjectsOfType<PlayerPhysics>(true);
            foreach (var pp in all)
                SetField(pp, "gravity_modifier", (Fix)_pendingGravityMultiplier);

            // Rope gravity is in RopeBody — set ropeGravity field
            var ropes = Object.FindObjectsOfType<RopeBody>(true);
            foreach (var rb in ropes)
                SetField(rb, "ropeGravity", (Fix)(_pendingRopeGravityMultiplier * 3f)); // base≈3
        }

        // ── Water ─────────────────────────────────────────────────────────
        private static void ApplyWater(EnvironmentSettings env)
        {
            // SceneBounds.waterHeight — static field
            SetStaticField(typeof(SceneBounds), "waterHeight", (Fix)env.WaterHeight);

            // If no water: push spaceWaterHeight to match so WaterHeight property returns our value
            if (!env.HasWater)
                SetStaticField(typeof(SceneBounds), "spaceWaterHeight", (Fix)env.WaterHeight);

            // Override isSpaceLevel logic by forcing leveltype
            // (isSpaceLevel property: Constants.leveltype == LevelType.space)
            // If HasWater=false and LevelType is not space, we simulate space water behaviour
            // by setting destruction threshold
            SetStaticField(typeof(SceneBounds), "Camera_YMin", (Fix)env.DestructionYNoWater);
        }

        // ── Friction ──────────────────────────────────────────────────────
        private static void ApplyFriction(EnvironmentSettings env)
        {
            var all = Object.FindObjectsOfType<PlayerPhysics>(true);
            foreach (var pp in all)
            {
                SetField(pp, "PlatformSlipperyness01",    (Fix)env.NormalPlatformFriction);
                SetField(pp, "IcePlatformSlipperyness01", (Fix)env.IcePlatformFriction);
            }

            _pendingNormalFriction = env.NormalPlatformFriction;
            _pendingIceFriction    = env.IcePlatformFriction;
        }

        public static float _pendingNormalFriction = 0.5f;
        public static float _pendingIceFriction    = 0.87f;

        public static void ApplyFrictionToSpawnedPlayers()
        {
            var all = Object.FindObjectsOfType<PlayerPhysics>(true);
            foreach (var pp in all)
            {
                SetField(pp, "PlatformSlipperyness01",    (Fix)_pendingNormalFriction);
                SetField(pp, "IcePlatformSlipperyness01", (Fix)_pendingIceFriction);
            }
        }

        // ── Blast zone ────────────────────────────────────────────────────
        private static void ApplyBlastZone(EnvironmentSettings env)
        {
            SetStaticField(typeof(SceneBounds), "BlastZone_YMax", (Fix)env.BlastZoneYMax);
        }

        // ── Rope color ────────────────────────────────────────────────────
        public static void ApplyRopeColors(EnvironmentSettings env)
        {
            var ropes = Object.FindObjectsOfType<RopeMesh>(true);
            foreach (var rm in ropes)
            {
                var meshRen = rm.GetComponent<MeshRenderer>();
                if (meshRen == null) continue;

                switch (env.RopeColorMode)
                {
                    case 0: meshRen.material.color = Color.black; break;
                    case 1: meshRen.material.color = Color.white; break;
                    // case 2: keep as-is (player color)
                }
            }
        }

        // ── Reflection helpers ────────────────────────────────────────────
        private static void SetField(object obj, string name, object value)
        {
            var f = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
                f.SetValue(obj, value);
            else
                Plugin.Log.LogWarning($"[EnvironmentApplier] Field '{name}' not found on {obj.GetType().Name}");
        }

        private static void SetStaticField(System.Type type, string name, object value)
        {
            var f = type.GetField(name,
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            if (f != null)
                f.SetValue(null, value);
            else
                Plugin.Log.LogWarning($"[EnvironmentApplier] Static field '{name}' not found on {type.Name}");
        }
    }
}

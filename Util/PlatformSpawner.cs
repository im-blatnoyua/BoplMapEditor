using System.Linq;
using System.Reflection;
using BoplFixedMath;
using BoplMapEditor.Data;
using BoplMapEditor.Util;
using UnityEngine;

namespace BoplMapEditor.Util
{
    public static class PlatformSpawner
    {
        private static StickyRoundedRectangle? _template;

        // Find a disabled platform as a cloning template
        private static StickyRoundedRectangle? GetTemplate()
        {
            if (_template != null) return _template;
            var all = Object.FindObjectsOfType<StickyRoundedRectangle>(true);
            _template = all.FirstOrDefault();
            return _template;
        }

        public static StickyRoundedRectangle? SpawnPlatform(PlatformData data)
        {
            var template = GetTemplate();
            if (template == null)
            {
                Plugin.Log.LogError("[PlatformSpawner] No template StickyRoundedRectangle found in scene.");
                return null;
            }

            var go = Object.Instantiate(template.gameObject);
            go.name = $"CustomPlatform_{data.X}_{data.Y}";
            go.SetActive(true);

            var srr = go.GetComponent<StickyRoundedRectangle>();
            if (srr == null) return null;

            // Set platform type (grass/snow/ice/space/robot/slime)
            srr.platformType = (PlatformType)data.Type;

            // Set extents and radius via reflection (fields may be non-public)
            SetField(srr, "extents", FixConvert.ToVec2(new Vector2(data.HalfW, data.HalfH)));
            SetField(srr, "radius", FixConvert.ToFix(data.Radius));

            // Set position and rotation via FixTransform
            var fixTrans = go.GetComponent<FixTransform>();
            if (fixTrans != null)
            {
                fixTrans.position = FixConvert.ToVec2(new Vector2(data.X, data.Y));
                fixTrans.localRotation = FixConvert.ToFix(data.Rotation * Mathf.Deg2Rad);
            }

            // Re-initialize physics bounds
            var updateBounds = srr.GetType().GetMethod("UpdateBounds",
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            updateBounds?.Invoke(srr, null);

            // Apply movement if defined
            if (data.Movement != null && data.Movement.Type != Data.MovementType.None)
                ApplyMovement(go, data);

            return srr;
        }

        public static void DestroyAllGamePlatforms()
        {
            var platforms = Object.FindObjectsOfType<StickyRoundedRectangle>(true);
            foreach (var p in platforms)
                Object.Destroy(p.gameObject);
            _template = null;
        }

        // Apply water height and background based on level theme
        public static void ApplyTheme(int theme)
        {
            // LevelType: 0=grass, 1=snow, 2=space
            var levelTypeField = typeof(Constants).GetField("leveltype",
                BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
            levelTypeField?.SetValue(null, (LevelType)theme);
        }

        private static void ApplyMovement(GameObject go, Data.PlatformData data)
        {
            var mov = data.Movement!;
            var origin = new UnityEngine.Vector2(data.X, data.Y);

            // AnimateVelocity must already exist on the platform (it's part of the prefab)
            var animVel = go.GetComponent<AnimateVelocity>();
            if (animVel == null)
            {
                Plugin.Log.LogWarning("[PlatformSpawner] AnimateVelocity not found — movement skipped.");
                return;
            }

            // Configure AnimateVelocity spring parameters
            SetField(animVel, "speed",  FixConvert.ToFix(mov.Speed));
            SetField(animVel, "mu",     FixConvert.ToFix(mov.Damping));

            // For circular: angular speed controls rotation rate
            if (mov.Type == Data.MovementType.Circular)
                SetField(animVel, "rotationSpeed", FixConvert.ToFix(0f)); // no auto-rotation

            // Build path and attach AnimateGround
            var pathKeys = mov.BuildPathKeys(origin);
            if (pathKeys.Length < 2) return;

            var animGround = go.AddComponent<AnimateGround>();
            SetField(animGround, "PathKeys", pathKeys);

            // Build AnimationCurveFixed for position: 0→1 over time period
            float period = CalculatePeriod(mov, pathKeys);
            var curve = BuildPositionCurve(mov, period);
            SetField(animGround, "PositionCurve", curve);
            SetField(animGround, "DelaySeconds",  BoplFixedMath.Fix.Zero);

            Plugin.Log.LogInfo($"[PlatformSpawner] Applied {mov.Type} movement to platform at ({data.X},{data.Y})");
        }

        private static float CalculatePeriod(Data.PlatformMovement mov, BoplFixedMath.Vec2[] keys)
        {
            switch (mov.Type)
            {
                case Data.MovementType.Circular:
                    return mov.AngularSpeed > 0f ? 1f / mov.AngularSpeed : 10f;
                case Data.MovementType.Linear:
                    return (mov.Distance * 2f) / Mathf.Max(mov.Speed * 0.5f, 0.1f);
                default:
                    // Path: estimate based on total length / speed
                    float len = 0f;
                    for (int i = 1; i < keys.Length; i++)
                        len += UnityEngine.Vector2.Distance(
                            FixConvert.ToVector2(keys[i - 1]),
                            FixConvert.ToVector2(keys[i]));
                    return len / Mathf.Max(mov.Speed, 0.1f);
            }
        }

        private static AnimationCurveFixed BuildPositionCurve(Data.PlatformMovement mov, float period)
        {
            // AnimationCurveFixed: maps time → 0..1 position along path
            // For linear: ping-pong 0→1→0
            // For circular/path: 0→1 loop
            var curve = new AnimationCurveFixed();
            if (mov.Type == Data.MovementType.Linear)
            {
                // Ping-pong
                SetField(curve, "keys", new[]
                {
                    new BoplFixedMath.Fix[] { BoplFixedMath.Fix.Zero, BoplFixedMath.Fix.Zero },
                    new BoplFixedMath.Fix[] { (BoplFixedMath.Fix)(period * 0.5f), BoplFixedMath.Fix.One },
                    new BoplFixedMath.Fix[] { (BoplFixedMath.Fix)period, BoplFixedMath.Fix.Zero },
                });
            }
            else
            {
                // 0 → 1 over one period (loop is handled by AnimateGround wrapping)
                SetField(curve, "keys", new[]
                {
                    new BoplFixedMath.Fix[] { BoplFixedMath.Fix.Zero, BoplFixedMath.Fix.Zero },
                    new BoplFixedMath.Fix[] { (BoplFixedMath.Fix)period, BoplFixedMath.Fix.One },
                });
            }
            return curve;
        }

        private static void SetField(object obj, string name, object value)
        {
            var field = obj.GetType().GetField(name,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (field != null)
                field.SetValue(obj, value);
            else
                Plugin.Log.LogWarning($"[PlatformSpawner] Field '{name}' not found on {obj.GetType().Name}");
        }
    }
}

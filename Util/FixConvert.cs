using BoplFixedMath;
using UnityEngine;

namespace BoplMapEditor.Util
{
    public static class FixConvert
    {
        public static Fix ToFix(float f) => (Fix)f;
        public static float ToFloat(Fix f) => (float)f;

        public static Vec2 ToVec2(Vector2 v) => new Vec2((Fix)v.x, (Fix)v.y);
        public static Vector2 ToVector2(Vec2 v) => new Vector2((float)v.x, (float)v.y);
    }
}

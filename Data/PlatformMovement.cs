using System;
using System.Collections.Generic;
using UnityEngine;

namespace BoplMapEditor.Data
{
    public enum MovementType
    {
        None,       // Статичная платформа
        Linear,     // Туда-обратно по вектору
        Circular,   // Вращение по кругу
        Path        // Произвольный путь из точек
    }

    [Serializable]
    public class PlatformMovement
    {
        public MovementType Type = MovementType.None;

        // ── Linear ────────────────────────────────────────────────────────
        // Платформа движется от стартовой позиции на (DirX*Distance, DirY*Distance) и обратно
        public float DirX = 1f;     // направление (нормализованное)
        public float DirY = 0f;
        public float Distance = 10f; // расстояние в обе стороны
        public float Speed = 5f;     // скорость (AnimateVelocity.speed)

        // ── Circular ──────────────────────────────────────────────────────
        // Центр — стартовая позиция платформы, радиус и угловая скорость
        public float Radius = 8f;
        public float AngularSpeed = 1f;   // обороты в секунду (положительное = против часовой)
        public float StartAngle = 0f;     // начальный угол в градусах

        // ── Path ──────────────────────────────────────────────────────────
        // Список точек в мировых координатах, платформа циклически проходит их
        public List<SerializableVec2> Waypoints = new List<SerializableVec2>();
        public bool LoopPath = true;

        // ── Общие ─────────────────────────────────────────────────────────
        public float Damping = 3f;  // AnimateVelocity.mu — плавность (больше = жёстче)

        public PlatformMovement Clone()
        {
            var c = new PlatformMovement
            {
                Type = Type,
                DirX = DirX, DirY = DirY, Distance = Distance, Speed = Speed,
                Radius = Radius, AngularSpeed = AngularSpeed, StartAngle = StartAngle,
                LoopPath = LoopPath, Damping = Damping
            };
            foreach (var w in Waypoints) c.Waypoints.Add(new SerializableVec2(w.x, w.y));
            return c;
        }

        // Генерирует массив PathKeys для AnimateGround из текущих настроек
        public BoplFixedMath.Vec2[] BuildPathKeys(Vector2 origin)
        {
            switch (Type)
            {
                case MovementType.Linear:
                    return BuildLinearPath(origin);
                case MovementType.Circular:
                    return BuildCircularPath(origin);
                case MovementType.Path:
                    return BuildCustomPath();
                default:
                    return new[] { Util.FixConvert.ToVec2(origin) };
            }
        }

        private BoplFixedMath.Vec2[] BuildLinearPath(Vector2 origin)
        {
            var dir = new Vector2(DirX, DirY).normalized;
            var a = origin - dir * Distance;
            var b = origin + dir * Distance;
            // Туда-обратно: A → B → A
            return new[]
            {
                Util.FixConvert.ToVec2(a),
                Util.FixConvert.ToVec2(b),
                Util.FixConvert.ToVec2(a),
            };
        }

        private BoplFixedMath.Vec2[] BuildCircularPath(Vector2 origin)
        {
            const int STEPS = 32;
            var keys = new BoplFixedMath.Vec2[STEPS + 1];
            for (int i = 0; i <= STEPS; i++)
            {
                float angle = StartAngle * Mathf.Deg2Rad + (i / (float)STEPS) * Mathf.PI * 2f;
                var pos = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * Radius;
                keys[i] = Util.FixConvert.ToVec2(pos);
            }
            return keys;
        }

        private BoplFixedMath.Vec2[] BuildCustomPath()
        {
            if (Waypoints.Count == 0) return Array.Empty<BoplFixedMath.Vec2>();
            var keys = new BoplFixedMath.Vec2[Waypoints.Count];
            for (int i = 0; i < Waypoints.Count; i++)
                keys[i] = Util.FixConvert.ToVec2(new Vector2(Waypoints[i].x, Waypoints[i].y));
            return keys;
        }
    }

    [Serializable]
    public class SerializableVec2
    {
        public float x, y;
        public SerializableVec2(float x, float y) { this.x = x; this.y = y; }
    }
}

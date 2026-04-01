using BoplMapEditor.Data;
using UnityEngine;

namespace BoplMapEditor.Core
{
    public enum HandleType
    {
        MoveCenter,
        ResizeN, ResizeS, ResizeE, ResizeW,
        ResizeNE, ResizeNW, ResizeSE, ResizeSW
    }

    public static class SelectionHandle
    {
        private const float HANDLE_WORLD_SIZE = 1.2f; // world units for hit area

        public static HandleType? HitTest(PlatformData p, Vector2 mouseWorld)
        {
            float dx = mouseWorld.x - p.X;
            float dy = mouseWorld.y - p.Y;
            float hs = HANDLE_WORLD_SIZE;

            // Corner handles
            if (Near(dx, p.HalfW, hs) && Near(dy, p.HalfH, hs))  return HandleType.ResizeNE;
            if (Near(dx, -p.HalfW, hs) && Near(dy, p.HalfH, hs)) return HandleType.ResizeNW;
            if (Near(dx, p.HalfW, hs) && Near(dy, -p.HalfH, hs)) return HandleType.ResizeSE;
            if (Near(dx, -p.HalfW, hs) && Near(dy, -p.HalfH, hs))return HandleType.ResizeSW;

            // Edge handles
            if (Near(dy, p.HalfH, hs) && Mathf.Abs(dx) < p.HalfW)  return HandleType.ResizeN;
            if (Near(dy, -p.HalfH, hs) && Mathf.Abs(dx) < p.HalfW) return HandleType.ResizeS;
            if (Near(dx, p.HalfW, hs) && Mathf.Abs(dy) < p.HalfH)  return HandleType.ResizeE;
            if (Near(dx, -p.HalfW, hs) && Mathf.Abs(dy) < p.HalfH) return HandleType.ResizeW;

            // Body move
            if (Mathf.Abs(dx) < p.HalfW && Mathf.Abs(dy) < p.HalfH)
                return HandleType.MoveCenter;

            return null;
        }

        public static void ApplyDrag(ref PlatformData p, HandleType h, Vector2 worldDelta)
        {
            switch (h)
            {
                case HandleType.MoveCenter:
                    p.X += worldDelta.x;
                    p.Y += worldDelta.y;
                    break;
                case HandleType.ResizeN:
                    p.Y += worldDelta.y * 0.5f;
                    p.HalfH = Mathf.Max(0.5f, p.HalfH + worldDelta.y * 0.5f);
                    break;
                case HandleType.ResizeS:
                    p.Y += worldDelta.y * 0.5f;
                    p.HalfH = Mathf.Max(0.5f, p.HalfH - worldDelta.y * 0.5f);
                    break;
                case HandleType.ResizeE:
                    p.X += worldDelta.x * 0.5f;
                    p.HalfW = Mathf.Max(0.5f, p.HalfW + worldDelta.x * 0.5f);
                    break;
                case HandleType.ResizeW:
                    p.X += worldDelta.x * 0.5f;
                    p.HalfW = Mathf.Max(0.5f, p.HalfW - worldDelta.x * 0.5f);
                    break;
                case HandleType.ResizeNE:
                    p.X += worldDelta.x * 0.5f;
                    p.Y += worldDelta.y * 0.5f;
                    p.HalfW = Mathf.Max(0.5f, p.HalfW + worldDelta.x * 0.5f);
                    p.HalfH = Mathf.Max(0.5f, p.HalfH + worldDelta.y * 0.5f);
                    break;
                case HandleType.ResizeNW:
                    p.X += worldDelta.x * 0.5f;
                    p.Y += worldDelta.y * 0.5f;
                    p.HalfW = Mathf.Max(0.5f, p.HalfW - worldDelta.x * 0.5f);
                    p.HalfH = Mathf.Max(0.5f, p.HalfH + worldDelta.y * 0.5f);
                    break;
                case HandleType.ResizeSE:
                    p.X += worldDelta.x * 0.5f;
                    p.Y += worldDelta.y * 0.5f;
                    p.HalfW = Mathf.Max(0.5f, p.HalfW + worldDelta.x * 0.5f);
                    p.HalfH = Mathf.Max(0.5f, p.HalfH - worldDelta.y * 0.5f);
                    break;
                case HandleType.ResizeSW:
                    p.X += worldDelta.x * 0.5f;
                    p.Y += worldDelta.y * 0.5f;
                    p.HalfW = Mathf.Max(0.5f, p.HalfW - worldDelta.x * 0.5f);
                    p.HalfH = Mathf.Max(0.5f, p.HalfH - worldDelta.y * 0.5f);
                    break;
            }
        }

        private static bool Near(float a, float b, float threshold) =>
            Mathf.Abs(a - b) <= threshold;
    }
}

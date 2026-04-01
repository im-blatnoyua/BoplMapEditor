using UnityEngine;

namespace BoplMapEditor.UI
{
    // Manages zoom and pan for the editor canvas.
    // World space: X [-97.27, 97.6], Y [-26, 40]
    // Canvas space: pixels relative to the viewport RectTransform center.
    public static class EditorViewport
    {
        public static float Zoom { get; private set; } = 4.5f;
        public static Vector2 Pan { get; private set; } = Vector2.zero;

        public static Vector2 WorldToCanvas(Vector2 world)
        {
            return new Vector2(world.x * Zoom + Pan.x, world.y * Zoom + Pan.y);
        }

        public static Vector2 CanvasToWorld(Vector2 canvas)
        {
            return new Vector2((canvas.x - Pan.x) / Zoom, (canvas.y - Pan.y) / Zoom);
        }

        public static void ApplyZoom(float delta, Vector2 pivotCanvas)
        {
            float oldZoom = Zoom;
            Zoom = Mathf.Clamp(Zoom * (1f + delta), 1.5f, 25f);
            // Adjust pan to zoom toward pivot
            Pan = pivotCanvas - (pivotCanvas - Pan) * (Zoom / oldZoom);
        }

        public static void ApplyPan(Vector2 delta)
        {
            Pan += delta;
        }

        public static void Reset()
        {
            Zoom = 4.5f;
            Pan = Vector2.zero;
        }
    }
}

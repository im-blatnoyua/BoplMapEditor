using BoplMapEditor.Data;
using UnityEngine;

namespace BoplMapEditor.UI
{
    // Generates a small preview Texture2D from MapData by drawing platforms as colored pixels.
    public static class ThumbnailGenerator
    {
        private const int SIZE = 128;

        // World bounds used for projection
        private const float WORLD_W = 194.87f;  // -97.27 to 97.6
        private const float WORLD_H = 66f;       // -26 to 40
        private const float WORLD_X_MIN = -97.27f;
        private const float WORLD_Y_MIN = -26f;

        public static Texture2D Generate(MapData map)
        {
            var tex = new Texture2D(SIZE, SIZE, TextureFormat.RGBA32, false);
            tex.filterMode = FilterMode.Bilinear;

            // Background color based on theme
            Color bg = map.LevelTheme == 2
                ? new Color(0.04f, 0.04f, 0.12f, 1f)   // space: dark blue
                : map.LevelTheme == 1
                ? new Color(0.65f, 0.75f, 0.90f, 1f)   // snow: light blue
                : new Color(0.18f, 0.42f, 0.18f, 1f);  // grass: dark green

            var pixels = new Color[SIZE * SIZE];
            for (int i = 0; i < pixels.Length; i++) pixels[i] = bg;

            // Water line (grass/snow only)
            if (map.LevelTheme != 2)
            {
                float waterY = map.Environment?.WaterHeight ?? -11.3f;
                int waterPy = WorldToPixelY(waterY);
                if (waterPy >= 0 && waterPy < SIZE)
                {
                    Color waterColor = new Color(0.2f, 0.45f, 0.75f, 0.8f);
                    for (int x = 0; x < SIZE; x++)
                        for (int y = 0; y < Mathf.Max(1, waterPy); y++)
                            pixels[y * SIZE + x] = Color.Lerp(pixels[y * SIZE + x], waterColor, 0.6f);
                }
            }

            // Draw platforms
            foreach (var p in map.Platforms)
            {
                Color c = StyleHelper.PlatformColors[Mathf.Clamp(p.Type, 0, 4)];
                c.a = 1f;
                DrawPlatform(pixels, p, c);
            }

            tex.SetPixels(pixels);
            tex.Apply();
            return tex;
        }

        private static void DrawPlatform(Color[] pixels, PlatformData p, Color c)
        {
            // Convert world rect to pixel rect (ignoring rotation for simplicity)
            int px = WorldToPixelX(p.X);
            int py = WorldToPixelY(p.Y);
            int pw = Mathf.Max(1, Mathf.RoundToInt(p.HalfW * 2f * SIZE / WORLD_W));
            int ph = Mathf.Max(1, Mathf.RoundToInt(p.HalfH * 2f * SIZE / WORLD_H));

            int x0 = Mathf.Clamp(px - pw / 2, 0, SIZE - 1);
            int x1 = Mathf.Clamp(px + pw / 2, 0, SIZE - 1);
            int y0 = Mathf.Clamp(py - ph / 2, 0, SIZE - 1);
            int y1 = Mathf.Clamp(py + ph / 2, 0, SIZE - 1);

            for (int y = y0; y <= y1; y++)
                for (int x = x0; x <= x1; x++)
                    pixels[y * SIZE + x] = c;

            // Draw outline
            Color outline = c * 0.6f; outline.a = 1f;
            for (int x = x0; x <= x1; x++)
            {
                if (y0 < SIZE) pixels[y0 * SIZE + x] = outline;
                if (y1 < SIZE) pixels[y1 * SIZE + x] = outline;
            }
            for (int y = y0; y <= y1; y++)
            {
                if (x0 < SIZE) pixels[y * SIZE + x0] = outline;
                if (x1 < SIZE) pixels[y * SIZE + x1] = outline;
            }
        }

        private static int WorldToPixelX(float wx)
            => Mathf.Clamp(Mathf.RoundToInt((wx - WORLD_X_MIN) / WORLD_W * SIZE), 0, SIZE - 1);

        private static int WorldToPixelY(float wy)
            => Mathf.Clamp(Mathf.RoundToInt((wy - WORLD_Y_MIN) / WORLD_H * SIZE), 0, SIZE - 1);
    }
}

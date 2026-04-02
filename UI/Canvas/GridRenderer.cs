using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Draws a grid on the canvas viewport. Redraws only when zoom/pan changes.
    public class GridRenderer : MonoBehaviour
    {
        public EditorCanvasController CanvasCtrl = null!;

        private RectTransform _viewport = null!;
        private Transform _lineParent = null!;

        // Keep a pool of up to 50 line GameObjects
        private const int MAX_LINES = 50;
        private readonly GameObject[] _lines = new GameObject[MAX_LINES];

        // Cache last known zoom/pan to avoid redrawing every frame
        private float _lastZoom = -1f;
        private Vector2 _lastPan = new Vector2(float.NaN, float.NaN);

        public void Init(RectTransform viewport)
        {
            _viewport = viewport;

            var go = new GameObject("GridLines");
            go.transform.SetParent(viewport, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            _lineParent = go.transform;

            // Pre-allocate line pool
            for (int i = 0; i < MAX_LINES; i++)
            {
                var lineGo = new GameObject("GridLine_" + i);
                lineGo.transform.SetParent(_lineParent, false);
                var img = lineGo.AddComponent<Image>();
                img.raycastTarget = false;
                img.color = Color.clear;
                lineGo.AddComponent<RectTransform>();
                lineGo.SetActive(false);
                _lines[i] = lineGo;
            }
        }

        void Update()
        {
            if (_viewport == null) return;

            float zoom = EditorViewport.Zoom;
            Vector2 pan = EditorViewport.Pan;

            // Only redraw when viewport state changes
            if (Mathf.Approximately(zoom, _lastZoom) && pan == _lastPan)
                return;

            _lastZoom = zoom;
            _lastPan = pan;

            DrawGrid();
        }

        private void DrawGrid()
        {
            // Disable all lines first
            for (int i = 0; i < MAX_LINES; i++)
                _lines[i].SetActive(false);

            Color lineColor   = new Color(0.25f, 0.30f, 0.42f, 0.4f);
            Color originColor = new Color(0.35f, 0.40f, 0.55f, 0.7f);

            int idx = 0;
            float gridSize = 4f;  // draw a line every 4 world units for reasonable density

            // Vertical lines: x from -40 to +40, step gridSize => 21 lines
            for (float x = -40f; x <= 40f && idx < MAX_LINES; x += gridSize)
            {
                bool isOrigin = Mathf.Abs(x) < 0.01f;
                Vector2 top = EditorViewport.WorldToCanvas(new Vector2(x, 50f));
                Vector2 bot = EditorViewport.WorldToCanvas(new Vector2(x, -50f));
                SetLine(idx, top, bot, isOrigin ? originColor : lineColor, isOrigin ? 2f : 1f);
                idx++;
            }

            // Horizontal lines: y from -25 to +25, step gridSize => ~13 lines
            for (float y = -24f; y <= 24f && idx < MAX_LINES; y += gridSize)
            {
                bool isOrigin = Mathf.Abs(y) < 0.01f;
                Vector2 left  = EditorViewport.WorldToCanvas(new Vector2(-100f, y));
                Vector2 right = EditorViewport.WorldToCanvas(new Vector2( 100f, y));
                SetLine(idx, left, right, isOrigin ? originColor : lineColor, isOrigin ? 2f : 1f);
                idx++;
            }
        }

        private void SetLine(int idx, Vector2 a, Vector2 b, Color color, float thickness)
        {
            var go = _lines[idx];
            go.SetActive(true);

            var img = go.GetComponent<Image>();
            img.color = color;

            Vector2 mid = (a + b) * 0.5f;
            float len = Vector2.Distance(a, b);
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;

            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = mid;
            rt.sizeDelta = new Vector2(len, thickness);
            rt.localRotation = Quaternion.Euler(0f, 0f, angle);
        }
    }
}

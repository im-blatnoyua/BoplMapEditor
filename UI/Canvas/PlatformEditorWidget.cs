using BoplMapEditor.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Attached to each platform widget in the editor viewport.
    // Handles Mario Maker 2-style interaction:
    //   - Click/tap   → select (shows border + handles)
    //   - Drag body   → move platform
    //   - Drag handle → resize platform
    public class PlatformEditorWidget : MonoBehaviour,
        IPointerClickHandler,
        IBeginDragHandler, IDragHandler, IEndDragHandler
    {
        // ── Data ─────────────────────────────────────────────────────────
        public PlatformData Data   { get; private set; } = null!;
        public int          Index  { get; private set; }

        // ── References ────────────────────────────────────────────────────
        NativeMapEditorScreen _editor = null!;
        RectTransform         _viewportRt = null!;
        RectTransform         _rt         = null!;
        Image                 _body       = null!;
        GameObject            _selectionBorder = null!;
        GameObject            _handles    = null!;

        // ── State ─────────────────────────────────────────────────────────
        bool   _selected;
        bool   _draggingHandle;
        int    _dragHandleDir; // 0=TL 1=TR 2=BL 3=BR
        Vector2 _dragStartPointer;
        Vector2 _dragStartSize;
        Vector2 _dragStartPos;

        // World units per viewport pixel
        float _pxToWorldX;
        float _pxToWorldY;

        const float WORLD_W = 97.6f - (-97.27f);  // 194.87
        const float WORLD_H = 40f   - (-26f);      // 66

        static PlatformEditorWidget? _currentSelected;

        // ── Factory ───────────────────────────────────────────────────────

        public static PlatformEditorWidget Attach(
            GameObject go, PlatformData data, int index,
            NativeMapEditorScreen editor, RectTransform viewportRt)
        {
            var w = go.AddComponent<PlatformEditorWidget>();
            w.Data       = data;
            w.Index      = index;
            w._editor    = editor;
            w._viewportRt = viewportRt;
            w._rt        = go.GetComponent<RectTransform>();
            w._body      = go.GetComponent<Image>();
            w.BuildDecorations();
            return w;
        }

        // ── Build ─────────────────────────────────────────────────────────

        void BuildDecorations()
        {
            // Selection border (hidden by default)
            _selectionBorder = new GameObject("Border");
            _selectionBorder.transform.SetParent(transform, false);
            var bImg = _selectionBorder.AddComponent<Image>();
            bImg.color = new Color(1f, 1f, 1f, 0.85f);
            bImg.raycastTarget = false;
            var brt = _selectionBorder.GetComponent<RectTransform>();
            brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one;
            brt.offsetMin = new Vector2(-3f, -3f); brt.offsetMax = new Vector2(3f, 3f);
            _selectionBorder.SetActive(false);

            // Resize handles (4 corners)
            _handles = new GameObject("Handles");
            _handles.transform.SetParent(transform, false);
            var hrt = _handles.AddComponent<RectTransform>();
            hrt.anchorMin = Vector2.zero; hrt.anchorMax = Vector2.one;
            hrt.offsetMin = hrt.offsetMax = Vector2.zero;

            Vector2[] anchorDirs = {
                new Vector2(0f, 1f), // TL
                new Vector2(1f, 1f), // TR
                new Vector2(0f, 0f), // BL
                new Vector2(1f, 0f), // BR
            };
            for (int i = 0; i < 4; i++)
            {
                int dir = i;
                var hGo = new GameObject("H" + i);
                hGo.transform.SetParent(_handles.transform, false);
                var hImg = hGo.AddComponent<Image>();
                hImg.color = Color.white;
                var hRt = hGo.GetComponent<RectTransform>();
                hRt.anchorMin = hRt.anchorMax = hRt.pivot = anchorDirs[i];
                hRt.sizeDelta = new Vector2(14f, 14f);
                hRt.anchoredPosition = Vector2.zero;

                // Drag handler on handle
                var hDrag = hGo.AddComponent<HandleDrag>();
                hDrag.Init(this, dir);
            }
            _handles.SetActive(false);
        }

        // ── Selection ─────────────────────────────────────────────────────

        public void Select()
        {
            if (_currentSelected != null && _currentSelected != this)
                _currentSelected.Deselect();
            _selected = true;
            _currentSelected = this;
            _selectionBorder.SetActive(true);
            _handles.SetActive(true);
            transform.SetAsLastSibling(); // bring to front
        }

        public void Deselect()
        {
            _selected = false;
            if (_currentSelected == this) _currentSelected = null;
            _selectionBorder.SetActive(false);
            _handles.SetActive(false);
        }

        public static void DeselectAll()
        {
            if (_currentSelected != null) _currentSelected.Deselect();
        }

        // ── IPointerClickHandler ──────────────────────────────────────────

        public void OnPointerClick(PointerEventData e)
        {
            if (_selected) return; // already selected — don't toggle off
            Select();
        }

        // ── Body drag (move) ──────────────────────────────────────────────

        public void OnBeginDrag(PointerEventData e)
        {
            if (!_selected) Select();
            _draggingHandle = false;
            _dragStartPointer = e.position;
            _dragStartPos = _rt.anchoredPosition;
            CalcScale();
        }

        public void OnDrag(PointerEventData e)
        {
            if (_draggingHandle) return;
            Vector2 delta = e.position - _dragStartPointer;
            var newPos = _dragStartPos + delta;
            _rt.anchoredPosition = newPos;

            // Update world data
            var size = _viewportRt.rect.size;
            float nx = (newPos.x - _viewportRt.rect.xMin) / size.x;
            float ny = (newPos.y - _viewportRt.rect.yMin) / size.y;
            float wx = -97.27f + nx * WORLD_W;
            float wy = -26f   + ny * WORLD_H;
            if (_editor.SnapToGrid)
            {
                wx = Mathf.Round(wx / _editor.GridSize) * _editor.GridSize;
                wy = Mathf.Round(wy / _editor.GridSize) * _editor.GridSize;
            }
            Data.X = wx; Data.Y = wy;
        }

        public void OnEndDrag(PointerEventData e)
        {
            if (_draggingHandle) return;
            // Snap visual to final data position
            RefreshPosition();
        }

        // ── Handle drag (resize) ──────────────────────────────────────────

        public void BeginHandleDrag(PointerEventData e, int dir)
        {
            _draggingHandle = true;
            _dragHandleDir  = dir;
            _dragStartPointer = e.position;
            _dragStartSize  = _rt.sizeDelta;
            CalcScale();
        }

        public void HandleDragDelta(PointerEventData e)
        {
            Vector2 delta = e.position - _dragStartPointer;

            // dir: 0=TL 1=TR 2=BL 3=BR
            // TR/BR: drag right → wider; TL/BL: drag left → wider
            float dx = (_dragHandleDir == 1 || _dragHandleDir == 3) ? delta.x : -delta.x;
            float dy = (_dragHandleDir == 0 || _dragHandleDir == 1) ? delta.y : -delta.y;

            float newW = Mathf.Max(_dragStartSize.x + dx, 20f);
            float newH = Mathf.Max(_dragStartSize.y + dy, 8f);
            _rt.sizeDelta = new Vector2(newW, newH);

            // Update world data
            var size = _viewportRt.rect.size;
            Data.HalfW = Mathf.Max((newW / size.x) * WORLD_W * 0.5f, 0.5f);
            Data.HalfH = Mathf.Max((newH / size.y) * WORLD_H * 0.5f, 0.2f);
        }

        public void EndHandleDrag(PointerEventData e)
        {
            _draggingHandle = false;
        }

        // ── Helpers ───────────────────────────────────────────────────────

        void CalcScale()
        {
            var size = _viewportRt.rect.size;
            _pxToWorldX = WORLD_W / size.x;
            _pxToWorldY = WORLD_H / size.y;
        }

        public void RefreshPosition()
        {
            var size = _viewportRt.rect.size;
            float cx = (Data.X - (-97.27f)) / WORLD_W * size.x + _viewportRt.rect.xMin;
            float cy = (Data.Y - (-26f))    / WORLD_H * size.y + _viewportRt.rect.yMin;
            _rt.anchoredPosition = new Vector2(cx, cy);

            float ww = (Data.HalfW * 2f) / WORLD_W * size.x;
            float wh = (Data.HalfH * 2f) / WORLD_H * size.y;
            _rt.sizeDelta = new Vector2(Mathf.Max(ww, 8f), Mathf.Max(wh, 4f));
        }
    }

    // Small MonoBehaviour on each resize handle
    public class HandleDrag : MonoBehaviour,
        IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
    {
        PlatformEditorWidget _widget = null!;
        int _dir;

        public void Init(PlatformEditorWidget widget, int dir)
        { _widget = widget; _dir = dir; }

        public void OnBeginDrag(PointerEventData e) => _widget.BeginHandleDrag(e, _dir);
        public void OnDrag(PointerEventData e)      => _widget.HandleDragDelta(e);
        public void OnEndDrag(PointerEventData e)   => _widget.EndHandleDrag(e);
        public void OnPointerClick(PointerEventData e) { } // absorb click
    }
}

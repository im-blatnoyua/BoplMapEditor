using System.Collections.Generic;
using BoplMapEditor.Data;
using BoplMapEditor.Core;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Manages the interactive canvas inside the editor where platforms live.
    // Handles zoom/pan, platform placement, selection, drag.
    public class EditorCanvasController : MonoBehaviour,
        IPointerClickHandler, IScrollHandler, IDragHandler, IBeginDragHandler
    {
        private MapEditorController _ctrl = null!;
        private RectTransform _content = null!;     // parent of all PlatformWidgets
        private RectTransform _viewport = null!;
        private readonly List<PlatformWidget> _widgets = new();

        private bool _isPanning;
        private int _draggingPlatform = -1;
        private Vector2 _dragStartWorld;

        public void Init(MapEditorController ctrl, RectTransform viewport, RectTransform content)
        {
            _ctrl = ctrl;
            _viewport = viewport;
            _content = content;
        }

        // Rebuild all platform widgets from current map data
        public void Refresh()
        {
            foreach (var w in _widgets) Destroy(w.gameObject);
            _widgets.Clear();

            var platforms = _ctrl.CurrentMap.Platforms;
            for (int i = 0; i < platforms.Count; i++)
            {
                var widget = PlatformWidget.Create(_content, platforms[i], i, this);
                widget.SetSelected(i == _ctrl.SelectedPlatformIndex);
                _widgets.Add(widget);
            }

            UpdateWorldBoundsVisual();
        }

        // Lightweight refresh: update positions/sizes without recreating widgets
        public void RefreshPositions()
        {
            var platforms = _ctrl.CurrentMap.Platforms;
            for (int i = 0; i < _widgets.Count; i++)
            {
                if (i < platforms.Count)
                {
                    _widgets[i].ApplyData(platforms[i]);
                    _widgets[i].SetSelected(i == _ctrl.SelectedPlatformIndex);
                }
            }
        }

        // Called from PlatformWidget
        public void OnPlatformPointerDown(int index, PointerEventData e)
        {
            if (_ctrl.ActiveTool == EditorTool.Delete)
            {
                _ctrl.SelectedPlatformIndex = index;
                _ctrl.DeleteSelected();
                Refresh();
                return;
            }

            _ctrl.SelectedPlatformIndex = index;
            _draggingPlatform = index;
            _dragStartWorld = EditorViewport.CanvasToWorld(ScreenToCanvas(e.position));
            RefreshPositions();
        }

        public void OnPlatformPointerUp(int index, PointerEventData e)
        {
            _draggingPlatform = -1;
        }

        public void OnPlatformDrag(int index, PointerEventData e)
        {
            if (_draggingPlatform != index) return;
            if (index < 0 || index >= _ctrl.CurrentMap.Platforms.Count) return;

            Vector2 worldNow = EditorViewport.CanvasToWorld(ScreenToCanvas(e.position));
            Vector2 delta = worldNow - _dragStartWorld;
            _dragStartWorld = worldNow;

            var p = _ctrl.CurrentMap.Platforms[index];
            p.X += delta.x;
            p.Y += delta.y;
            _ctrl.CurrentMap.Platforms[index] = p;
            RefreshPositions();
        }

        // Click on empty canvas → place platform
        public void OnPointerClick(PointerEventData e)
        {
            if (_ctrl.ActiveTool != EditorTool.Place) return;
            if (e.button != PointerEventData.InputButton.Left) return;

            Vector2 worldPos = EditorViewport.CanvasToWorld(ScreenToCanvas(e.position));
            _ctrl.AddPlatform(worldPos);
            Refresh();
        }

        // Scroll to zoom
        public void OnScroll(PointerEventData e)
        {
            Vector2 canvasPos = ScreenToCanvas(e.position);
            EditorViewport.ApplyZoom(e.scrollDelta.y * 0.08f, canvasPos);
            RefreshPositions();
            UpdateWorldBoundsVisual();
        }

        // Middle mouse / right mouse to pan
        public void OnBeginDrag(PointerEventData e)
        {
            _isPanning = e.button == PointerEventData.InputButton.Middle ||
                         e.button == PointerEventData.InputButton.Right;
        }

        public void OnDrag(PointerEventData e)
        {
            if (!_isPanning) return;
            EditorViewport.ApplyPan(e.delta);
            RefreshPositions();
            UpdateWorldBoundsVisual();
        }

        private Vector2 ScreenToCanvas(Vector2 screenPos)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                _viewport, screenPos, null, out Vector2 local);
            return local;
        }

        private GameObject? _boundsGo;

        // Draw movement preview (arrows/circle) for the selected platform
        private readonly List<GameObject> _movementPreview = new();

        public void RefreshMovementPreview()
        {
            foreach (var g in _movementPreview) Destroy(g);
            _movementPreview.Clear();

            int sel = _ctrl.SelectedPlatformIndex;
            if (sel < 0 || sel >= _ctrl.CurrentMap.Platforms.Count) return;

            var p = _ctrl.CurrentMap.Platforms[sel];
            var mov = p.Movement;
            if (mov == null || mov.Type == Data.MovementType.None) return;

            switch (mov.Type)
            {
                case Data.MovementType.Linear:
                    DrawLinearPreview(p, mov);
                    break;
                case Data.MovementType.Circular:
                    DrawCirclePreview(p, mov);
                    break;
                case Data.MovementType.Path:
                    DrawPathPreview(mov);
                    break;
            }
        }

        private void DrawLinearPreview(Data.PlatformData p, Data.PlatformMovement mov)
        {
            var dir = new Vector2(mov.DirX, mov.DirY).normalized;
            var a = new Vector2(p.X, p.Y) - dir * mov.Distance;
            var b = new Vector2(p.X, p.Y) + dir * mov.Distance;
            SpawnLine(a, b, new Color(0.3f, 0.7f, 1f, 0.8f));
            SpawnDot(a, new Color(0.3f, 0.7f, 1f, 1f), 6f);
            SpawnDot(b, new Color(0.3f, 0.7f, 1f, 1f), 6f);
        }

        private void DrawCirclePreview(Data.PlatformData p, Data.PlatformMovement mov)
        {
            const int SEG = 40;
            var origin = new Vector2(p.X, p.Y);
            Vector2? prev = null;
            for (int i = 0; i <= SEG; i++)
            {
                float angle = (i / (float)SEG) * Mathf.PI * 2f + mov.StartAngle * Mathf.Deg2Rad;
                var pt = origin + new Vector2(Mathf.Cos(angle), Mathf.Sin(angle)) * mov.Radius;
                if (prev.HasValue) SpawnLine(prev.Value, pt, new Color(0.8f, 0.3f, 1f, 0.7f));
                prev = pt;
            }
            SpawnDot(origin, new Color(0.8f, 0.3f, 1f, 0.5f), 5f);
        }

        private void DrawPathPreview(Data.PlatformMovement mov)
        {
            if (mov.Waypoints.Count < 1) return;
            for (int i = 0; i < mov.Waypoints.Count; i++)
            {
                var w = mov.Waypoints[i];
                var wp = new Vector2(w.x, w.y);
                SpawnDot(wp, new Color(1f, 0.7f, 0.1f, 0.9f), 7f);
                if (i > 0)
                {
                    var prev = mov.Waypoints[i - 1];
                    SpawnLine(new Vector2(prev.x, prev.y), wp, new Color(1f, 0.7f, 0.1f, 0.6f));
                }
            }
            if (mov.LoopPath && mov.Waypoints.Count > 1)
            {
                var first = mov.Waypoints[0];
                var last  = mov.Waypoints[mov.Waypoints.Count - 1];
                SpawnLine(new Vector2(last.x, last.y), new Vector2(first.x, first.y),
                    new Color(1f, 0.7f, 0.1f, 0.3f));
            }
        }

        private void SpawnDot(Vector2 world, Color color, float size)
        {
            var go = new GameObject("MovPreview_Dot");
            go.transform.SetParent(_content, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(size, size);
            rt.anchoredPosition = EditorViewport.WorldToCanvas(world);
            _movementPreview.Add(go);
        }

        private void SpawnLine(Vector2 a, Vector2 b, Color color)
        {
            var go = new GameObject("MovPreview_Line");
            go.transform.SetParent(_content, false);
            var img = go.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;

            Vector2 sa = EditorViewport.WorldToCanvas(a);
            Vector2 sb = EditorViewport.WorldToCanvas(b);
            Vector2 mid = (sa + sb) * 0.5f;
            float len = Vector2.Distance(sa, sb);
            float angle = Mathf.Atan2(sb.y - sa.y, sb.x - sa.x) * Mathf.Rad2Deg;

            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = new Vector2(len, 2f);
            rt.anchoredPosition = mid;
            rt.localRotation = Quaternion.Euler(0, 0, angle);
            _movementPreview.Add(go);
        }

        private void UpdateWorldBoundsVisual()
        {
            if (_boundsGo == null)
            {
                _boundsGo = new GameObject("WorldBounds");
                _boundsGo.transform.SetParent(_content, false);
                _boundsGo.transform.SetAsFirstSibling();
                var img = _boundsGo.AddComponent<Image>();
                img.color = new Color(0.3f, 0.35f, 0.5f, 0.15f);
                img.sprite = StyleHelper.MakeRoundedSprite();
                img.type = Image.Type.Sliced;
                img.raycastTarget = false;
            }

            // World bounds: X [-97.27, 97.6], Y [-26, 40]
            Vector2 tl = EditorViewport.WorldToCanvas(new Vector2(-97.27f, 40f));
            Vector2 br = EditorViewport.WorldToCanvas(new Vector2(97.6f, -26f));
            var rt = _boundsGo.GetComponent<RectTransform>();
            rt.anchoredPosition = (tl + br) * 0.5f;
            rt.sizeDelta = new Vector2(Mathf.Abs(br.x - tl.x), Mathf.Abs(br.y - tl.y));
        }
    }
}

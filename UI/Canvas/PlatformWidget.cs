using BoplMapEditor.Data;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // UGUI widget representing one platform in the editor canvas.
    // Uses the game's actual platform shader material when available.
    public class PlatformWidget : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IDragHandler
    {
        public int PlatformIndex;
        public bool IsSelected;

        private RawImage _body = null!;        // RawImage supports custom Materials
        private RectTransform _rt = null!;
        private GameObject _selectionBorder = null!;
        private EditorCanvasController _canvas = null!;
        private readonly GameObject[] _handles = new GameObject[8];

        public static PlatformWidget Create(Transform parent, PlatformData data,
            int index, EditorCanvasController canvas)
        {
            var go = new GameObject($"Plat_{index}");
            go.transform.SetParent(parent, false);

            var w = go.AddComponent<PlatformWidget>();
            w.PlatformIndex = index;
            w._canvas = canvas;
            w._rt = go.GetComponent<RectTransform>();
            w._rt.pivot = new Vector2(0.5f, 0.5f);

            // Use RawImage so we can apply the game's shader material
            w._body = go.AddComponent<RawImage>();
            w._body.raycastTarget = true;

            w.ApplyMaterial(data.Type);

            // Selection border (rendered behind the body)
            w._selectionBorder = CreateBorder(go.transform);
            w._selectionBorder.SetActive(false);

            // Resize handles
            string[] hNames = { "N", "S", "E", "W", "NE", "NW", "SE", "SW" };
            for (int i = 0; i < 8; i++)
                w._handles[i] = CreateHandle(go.transform, hNames[i]);

            w.ApplyData(data);
            return w;
        }

        private void ApplyMaterial(int type)
        {
            // Try to get the game's actual platform material
            var mat = StyleHelper.GetPlatformMaterial(type);
            if (mat != null)
            {
                // Clone it so we can set per-platform parameters without affecting the original
                _body.material = new Material(mat);
                _body.color = Color.white;
            }
            else
            {
                // Fallback: solid color
                _body.material = null;
                _body.color = StyleHelper.PlatformColors[Mathf.Clamp(type, 0, 5)];
            }
        }

        public void ApplyData(PlatformData data)
        {
            float zoom = EditorViewport.Zoom;
            float w = data.HalfW * 2f * zoom;
            float h = data.HalfH * 2f * zoom;

            _rt.sizeDelta = new Vector2(w, h);
            _rt.anchoredPosition = EditorViewport.WorldToCanvas(new Vector2(data.X, data.Y));
            _rt.localRotation = Quaternion.Euler(0, 0, data.Rotation);

            // Update shader parameters so the rounded corners scale correctly
            if (_body.material != null)
            {
                _body.material.SetFloat("_RWidth",     data.HalfW * 2f);
                _body.material.SetFloat("_RHeight",    data.HalfH * 2f);
                _body.material.SetFloat("_BevelRadius", data.Radius);
                _body.material.SetFloat("_Scale",      zoom);
            }

            PositionHandles(data, zoom);
        }

        private void PositionHandles(PlatformData d, float zoom)
        {
            float hw = d.HalfW * zoom;
            float hh = d.HalfH * zoom;

            Vector2[] positions = {
                new Vector2(0,   hh),   // N
                new Vector2(0,  -hh),   // S
                new Vector2(hw,  0),    // E
                new Vector2(-hw, 0),    // W
                new Vector2(hw,  hh),   // NE
                new Vector2(-hw, hh),   // NW
                new Vector2(hw, -hh),   // SE
                new Vector2(-hw,-hh),   // SW
            };

            for (int i = 0; i < 8; i++)
            {
                _handles[i].GetComponent<RectTransform>().anchoredPosition = positions[i];
                _handles[i].SetActive(IsSelected);
            }
        }

        public void SetSelected(bool selected)
        {
            IsSelected = selected;
            _selectionBorder.SetActive(selected);
            for (int i = 0; i < 8; i++)
                _handles[i].SetActive(selected);
        }

        private static GameObject CreateBorder(Transform parent)
        {
            var go = new GameObject("Border");
            go.transform.SetParent(parent, false);
            go.transform.SetAsFirstSibling();
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 0.85f, 0.1f, 0.9f);
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type = Image.Type.Sliced;
            img.raycastTarget = false;
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(-3, -3);
            rt.offsetMax = new Vector2(3, 3);
            return go;
        }

        private static GameObject CreateHandle(Transform parent, string name)
        {
            var go = new GameObject($"H_{name}");
            go.transform.SetParent(parent, false);
            var img = go.AddComponent<Image>();
            img.color = new Color(1f, 0.85f, 0.1f, 1f);
            img.raycastTarget = false;
            go.GetComponent<RectTransform>().sizeDelta = new Vector2(10, 10);
            go.SetActive(false);
            return go;
        }

        public void OnPointerDown(PointerEventData e) =>
            _canvas.OnPlatformPointerDown(PlatformIndex, e);
        public void OnPointerUp(PointerEventData e) =>
            _canvas.OnPlatformPointerUp(PlatformIndex, e);
        public void OnDrag(PointerEventData e) =>
            _canvas.OnPlatformDrag(PlatformIndex, e);
    }
}

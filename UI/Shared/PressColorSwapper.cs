using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Changes the Image color between NormalColor and PressedColor on pointer down/up.
    // Gives the exact blue → orange press effect like the game's buttons.
    [RequireComponent(typeof(Button))]
    public class PressColorSwapper : MonoBehaviour,
        IPointerDownHandler, IPointerUpHandler, IPointerExitHandler
    {
        public Color NormalColor  = StyleHelper.Blue;   // set after construction
        public Color PressedColor = StyleHelper.Orange;
        public float FadeDuration = 0.08f;

        private Image? _img;
        private bool _pressed;
        private float _t;
        private Color _from, _to;

        void Awake()
        {
            _img = GetComponent<Image>();
            // Start at NormalColor (will be updated once StyleHelper loads)
        }

        void Start()
        {
            // Re-read after StyleHelper has loaded game colors
            NormalColor = StyleHelper.Blue;
            if (_img != null) _img.color = NormalColor;
        }

        void Update()
        {
            if (_img == null || _t >= 1f) return;
            _t = Mathf.Min(_t + Time.unscaledDeltaTime / Mathf.Max(FadeDuration, 0.001f), 1f);
            _img.color = Color.Lerp(_from, _to, _t);
        }

        public void OnPointerDown(PointerEventData e)
        {
            if (e.button != PointerEventData.InputButton.Left) return;
            _pressed = true;
            StartTransition(_img?.color ?? NormalColor, PressedColor);
        }

        public void OnPointerUp(PointerEventData e)
        {
            if (!_pressed) return;
            _pressed = false;
            StartTransition(_img?.color ?? PressedColor, NormalColor);
        }

        public void OnPointerExit(PointerEventData e)
        {
            if (!_pressed) return;
            _pressed = false;
            StartTransition(_img?.color ?? PressedColor, NormalColor);
        }

        private void StartTransition(Color from, Color to)
        {
            _from = from;
            _to   = to;
            _t    = 0f;
        }
    }
}

using UnityEngine;

namespace BoplMapEditor.UI
{
    // Plays a scale-in bounce animation when a platform is placed.
    // Attach to a platform widget and call Play().
    public class PlacementAnimator : MonoBehaviour
    {
        private RectTransform _rt = null!;
        private float _timer;
        private bool _playing;
        private const float Duration = 0.28f;

        public void Play()
        {
            _rt = GetComponent<RectTransform>();
            if (_rt == null) { Destroy(this); return; }
            _rt.localScale = Vector3.zero;
            _timer = 0f;
            _playing = true;
        }

        void Update()
        {
            if (!_playing || _rt == null) return;

            _timer += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(_timer / Duration);

            if (t >= 1f)
            {
                _rt.localScale = Vector3.one;
                _playing = false;
                Destroy(this);
                return;
            }

            float scale = EaseOutBack(t);
            _rt.localScale = Vector3.one * scale;
        }

        // Cubic ease-out with slight overshoot (back easing).
        private static float EaseOutBack(float t)
        {
            const float c1 = 1.70158f;
            const float c3 = c1 + 1f;
            float u = t - 1f;
            return 1f + c3 * u * u * u + c1 * u * u;
        }
    }
}

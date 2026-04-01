using UnityEngine;

namespace BoplMapEditor.UI
{
    // Replicates AnimateInOutUI behavior: slides a RectTransform in/out on the Y axis.
    // Uses an AnimationCurve for easing — matches the game's menu style.
    public class SlideAnimator : MonoBehaviour
    {
        public RectTransform Target = null!;
        public float OffscreenY = 1200f;     // Starting Y (off-screen bottom)
        public AnimationCurve Curve = AnimationCurve.EaseInOut(0f, 0f, 0.35f, 1f);

        private float _targetY;
        private float _progress;
        private bool _animating;
        private bool _inDirection; // true = animating in, false = out

        private float _startYPos;

        void Awake()
        {
            if (Target == null) Target = GetComponent<RectTransform>();
            _targetY = Target.anchoredPosition.y;
            _startYPos = _targetY;
        }

        public void AnimateIn()
        {
            Target.anchoredPosition = new Vector2(Target.anchoredPosition.x, -OffscreenY);
            _progress = 0f;
            _inDirection = true;
            _animating = true;
            gameObject.SetActive(true);
        }

        public void AnimateOut(System.Action? onComplete = null)
        {
            _progress = 0f;
            _inDirection = false;
            _animating = true;
            _onOutComplete = onComplete;
        }

        private System.Action? _onOutComplete;

        void Update()
        {
            if (!_animating) return;

            float duration = Curve.keys[Curve.length - 1].time;
            _progress = Mathf.Min(_progress + Time.unscaledDeltaTime, duration);
            float t = Curve.Evaluate(_progress);

            if (_inDirection)
            {
                float y = Mathf.Lerp(-OffscreenY, _startYPos, t);
                Target.anchoredPosition = new Vector2(Target.anchoredPosition.x, y);
            }
            else
            {
                float y = Mathf.Lerp(_startYPos, -OffscreenY, t);
                Target.anchoredPosition = new Vector2(Target.anchoredPosition.x, y);
            }

            if (_progress >= duration)
            {
                _animating = false;
                if (!_inDirection)
                {
                    gameObject.SetActive(false);
                    _onOutComplete?.Invoke();
                    _onOutComplete = null;
                }
            }
        }
    }
}

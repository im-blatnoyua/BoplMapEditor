using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

namespace BoplMapEditor.UI
{
    // Simple in-editor test mode.
    // Hides editor UI, spawns a character at spawn point 1,
    // lets the user walk/jump around with WASD/arrows to preview the level.
    public class TestModeController : MonoBehaviour
    {
        // ── Character state ───────────────────────────────────────────────
        RectTransform _charRt    = null!;
        Vector2       _velocity  = Vector2.zero;
        bool          _grounded;

        const float GRAVITY     = -1800f; // canvas units/s²
        const float JUMP_FORCE  =  700f;
        const float MOVE_SPEED  =  400f;
        const float CHAR_W      =  28f;
        const float CHAR_H      =  36f;

        // ── Platform cache ────────────────────────────────────────────────
        readonly List<RectTransform> _platforms = new List<RectTransform>();

        // ── Factory ───────────────────────────────────────────────────────

        public static TestModeController Create(
            Transform parent,
            Vector2 spawnCanvasPos,
            IEnumerable<RectTransform> platformRects)
        {
            var go = new GameObject("TestModeController");
            go.transform.SetParent(parent, false);
            var c = go.AddComponent<TestModeController>();

            foreach (var r in platformRects) c._platforms.Add(r);

            // Character — simple colored circle
            var charGo = new GameObject("Character");
            charGo.transform.SetParent(parent, false);
            var charImg = charGo.AddComponent<Image>();
            charImg.sprite = StyleHelper.MakeRoundedSprite();
            charImg.type   = Image.Type.Sliced;
            charImg.color  = new Color(0.9f, 0.3f, 0.1f, 1f); // red/orange

            var numGo = new GameObject("Num"); numGo.transform.SetParent(charGo.transform, false);
            var numTmp = numGo.AddComponent<TextMeshProUGUI>();
            numTmp.text = "1"; numTmp.fontSize = 20f; numTmp.fontStyle = FontStyles.Bold;
            numTmp.alignment = TextAlignmentOptions.Center; numTmp.raycastTarget = false;
            numTmp.color = Color.white;
            var nrt = numGo.GetComponent<RectTransform>();
            nrt.anchorMin = Vector2.zero; nrt.anchorMax = Vector2.one;
            nrt.offsetMin = nrt.offsetMax = Vector2.zero;

            c._charRt = charGo.GetComponent<RectTransform>();
            c._charRt.anchorMin = c._charRt.anchorMax = c._charRt.pivot = new Vector2(0.5f, 0f);
            c._charRt.sizeDelta = new Vector2(CHAR_W, CHAR_H);
            c._charRt.anchoredPosition = spawnCanvasPos;

            return c;
        }

        // ── Update ────────────────────────────────────────────────────────

        void Update()
        {
            float dt = Time.deltaTime;

            // Horizontal input (WASD or arrows)
            float hInput = 0f;
            if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow))  hInput = -1f;
            if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) hInput =  1f;

            _velocity.x = hInput * MOVE_SPEED;

            // Gravity
            _velocity.y += GRAVITY * dt;

            // Jump
            if (_grounded && (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.UpArrow)
                           || Input.GetKeyDown(KeyCode.Space)))
                _velocity.y = JUMP_FORCE;

            // Move
            Vector2 pos = _charRt.anchoredPosition;
            pos += _velocity * dt;

            // Platform collision (simple AABB from above)
            _grounded = false;
            var charRect = new Rect(
                pos.x - CHAR_W * 0.5f,
                pos.y,
                CHAR_W, CHAR_H);

            foreach (var plat in _platforms)
            {
                if (plat == null) continue;
                var pr = GetRect(plat);

                // Standing on top
                if (_velocity.y <= 0f &&
                    charRect.xMax > pr.xMin + 4f && charRect.xMin < pr.xMax - 4f &&
                    pos.y >= pr.yMax - 8f && pos.y <= pr.yMax + Mathf.Abs(_velocity.y) * dt + 4f)
                {
                    pos.y = pr.yMax;
                    _velocity.y = 0f;
                    _grounded = true;
                }

                // Head bump
                if (_velocity.y > 0f &&
                    charRect.xMax > pr.xMin + 4f && charRect.xMin < pr.xMax - 4f &&
                    pos.y + CHAR_H >= pr.yMin && pos.y + CHAR_H <= pr.yMin + 12f)
                {
                    pos.y = pr.yMin - CHAR_H;
                    _velocity.y = 0f;
                }
            }

            _charRt.anchoredPosition = pos;
        }

        static Rect GetRect(RectTransform rt)
        {
            var corners = new Vector3[4];
            rt.GetWorldCorners(corners);
            // corners: 0=BL 1=TL 2=TR 3=BR in world (canvas) space
            // For ScreenSpaceOverlay, world == screen
            return new Rect(corners[0].x, corners[0].y,
                            corners[2].x - corners[0].x,
                            corners[2].y - corners[0].y);
        }
    }
}

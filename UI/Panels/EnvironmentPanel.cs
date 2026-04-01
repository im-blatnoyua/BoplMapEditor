using BoplMapEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Sidebar panel for editing EnvironmentSettings.
    // Shown as a second tab in the map editor sidebar.
    public class EnvironmentPanel : MonoBehaviour
    {
        private EnvironmentSettings _env = null!;
        private System.Action _onChange = null!;

        // UI references
        private readonly Button[] _presetButtons = new Button[3];
        private Toggle _hasWaterToggle = null!;
        private TMP_InputField _waterHeightField = null!;
        private TMP_InputField _gravityField = null!;
        private TMP_InputField _ropeGravityField = null!;
        private TMP_InputField _normalFrictionField = null!;
        private TMP_InputField _iceFrictionField = null!;
        private TMP_InputField _abilitySpeedField = null!;
        private TMP_InputField _blastZoneField = null!;
        private readonly Button[] _ropeColorButtons = new Button[3];

        public static EnvironmentPanel Create(Transform parent, System.Action onChange)
        {
            var go = new GameObject("EnvironmentPanel");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 4, 4);
            layout.spacing = 6;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var panel = go.AddComponent<EnvironmentPanel>();
            panel._onChange = onChange;
            panel.BuildUI(go.GetComponent<RectTransform>());
            return panel;
        }

        public void SetData(EnvironmentSettings env)
        {
            _env = env;
            RefreshAll();
        }

        private void BuildUI(RectTransform root)
        {
            // ── Presets ───────────────────────────────────────────────────
            AddLabel(root, "PRESETS", bold: true);
            var presetRow = AddRow(root);
            string[] presetNames = { "Grass", "Snow", "Space" };
            Color[]  presetColors = {
                StyleHelper.PlatformColors[0],
                StyleHelper.PlatformColors[1],
                new Color(0.08f, 0.08f, 0.18f, 1f)
            };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                _presetButtons[i] = AddRowButton(presetRow, presetNames[i], presetColors[i]);
                _presetButtons[i].onClick.AddListener(() => ApplyPreset(idx));
            }

            AddDivider(root);

            // ── Level background ──────────────────────────────────────────
            AddLabel(root, "BACKGROUND");
            // (LevelTheme is set via the main toolbar, this just shows it)

            AddDivider(root);

            // ── Gravity ───────────────────────────────────────────────────
            AddLabel(root, "GRAVITY", bold: true);

            AddLabeledField(root, "Player gravity ×", ref _gravityField, "1.00",
                "Normal=1.0 | Space=0.5 | Moon=0.3");
            _gravityField.onEndEdit.AddListener(v => {
                if (float.TryParse(v, out float f)) { _env.GravityMultiplier = Mathf.Clamp(f, 0.05f, 3f); Notify(); }
            });

            AddLabeledField(root, "Rope gravity ×", ref _ropeGravityField, "1.00",
                "Normal=1.0 | Space=0.5");
            _ropeGravityField.onEndEdit.AddListener(v => {
                if (float.TryParse(v, out float f)) { _env.RopeGravityMultiplier = Mathf.Clamp(f, 0.05f, 3f); Notify(); }
            });

            AddDivider(root);

            // ── Water ─────────────────────────────────────────────────────
            AddLabel(root, "WATER", bold: true);

            var waterRow = AddRow(root);
            AddRowLabel(waterRow, "Has water");
            _hasWaterToggle = AddToggle(waterRow, true);
            _hasWaterToggle.onValueChanged.AddListener(v => { _env.HasWater = v; Notify(); });

            AddLabeledField(root, "Water height Y", ref _waterHeightField, "-11.3",
                "Surface Y level (default -11.3)");
            _waterHeightField.onEndEdit.AddListener(v => {
                if (float.TryParse(v, out float f)) { _env.WaterHeight = f; Notify(); }
            });

            AddLabeledField(root, "Death Y (no water)", ref _blastZoneField, "-26",
                "Y where objects die if no water (-26=bottom)");
            _blastZoneField.onEndEdit.AddListener(v => {
                if (float.TryParse(v, out float f)) { _env.DestructionYNoWater = f; Notify(); }
            });

            AddDivider(root);

            // ── Friction ──────────────────────────────────────────────────
            AddLabel(root, "FRICTION", bold: true);

            AddLabeledField(root, "Normal platforms", ref _normalFrictionField, "0.50",
                "Speed kept/frame (0=ice, 1=no slip)");
            _normalFrictionField.onEndEdit.AddListener(v => {
                if (float.TryParse(v, out float f)) { _env.NormalPlatformFriction = Mathf.Clamp01(f); Notify(); }
            });

            AddLabeledField(root, "Ice platforms", ref _iceFrictionField, "0.87",
                "Speed kept/frame on ice (default 0.87)");
            _iceFrictionField.onEndEdit.AddListener(v => {
                if (float.TryParse(v, out float f)) { _env.IcePlatformFriction = Mathf.Clamp01(f); Notify(); }
            });

            AddDivider(root);

            // ── Abilities ─────────────────────────────────────────────────
            AddLabel(root, "ABILITIES", bold: true);

            AddLabeledField(root, "Exit speed ×", ref _abilitySpeedField, "1.00",
                "Velocity on ability exit (space=0.5)");
            _abilitySpeedField.onEndEdit.AddListener(v => {
                if (float.TryParse(v, out float f)) { _env.AbilityExitSpeedMultiplier = Mathf.Clamp(f, 0.1f, 2f); Notify(); }
            });

            AddDivider(root);

            // ── Rope color ────────────────────────────────────────────────
            AddLabel(root, "ROPE COLOR", bold: true);
            var ropeRow = AddRow(root);
            string[] ropeNames = { "Black", "White", "Player" };
            for (int i = 0; i < 3; i++)
            {
                int idx = i;
                Color c = i == 0 ? Color.black : i == 1 ? Color.white : new Color(0.4f, 0.6f, 1f);
                _ropeColorButtons[i] = AddRowButton(ropeRow, ropeNames[i], c);
                _ropeColorButtons[i].onClick.AddListener(() => {
                    _env.RopeColorMode = idx; Notify(); UpdateRopeHighlights();
                });
            }
        }

        private void ApplyPreset(int idx)
        {
            var preset = idx == 0 ? EnvironmentSettings.ForGrass()
                       : idx == 1 ? EnvironmentSettings.ForSnow()
                       : EnvironmentSettings.ForSpace();

            // Copy all values into existing reference
            _env.LevelType                = preset.LevelType;
            _env.GravityMultiplier        = preset.GravityMultiplier;
            _env.RopeGravityMultiplier    = preset.RopeGravityMultiplier;
            _env.HasWater                 = preset.HasWater;
            _env.WaterHeight              = preset.WaterHeight;
            _env.DestructionYNoWater      = preset.DestructionYNoWater;
            _env.BlastZoneYMax            = preset.BlastZoneYMax;
            _env.NormalPlatformFriction   = preset.NormalPlatformFriction;
            _env.IcePlatformFriction      = preset.IcePlatformFriction;
            _env.AbilityExitSpeedMultiplier = preset.AbilityExitSpeedMultiplier;
            _env.RopeColorMode            = preset.RopeColorMode;

            RefreshAll();
            Notify();
        }

        public void RefreshAll()
        {
            if (_env == null) return;
            _hasWaterToggle.SetIsOnWithoutNotify(_env.HasWater);
            _waterHeightField.SetTextWithoutNotify(_env.WaterHeight.ToString("F2"));
            _gravityField.SetTextWithoutNotify(_env.GravityMultiplier.ToString("F2"));
            _ropeGravityField.SetTextWithoutNotify(_env.RopeGravityMultiplier.ToString("F2"));
            _normalFrictionField.SetTextWithoutNotify(_env.NormalPlatformFriction.ToString("F2"));
            _iceFrictionField.SetTextWithoutNotify(_env.IcePlatformFriction.ToString("F2"));
            _abilitySpeedField.SetTextWithoutNotify(_env.AbilityExitSpeedMultiplier.ToString("F2"));
            _blastZoneField.SetTextWithoutNotify(_env.DestructionYNoWater.ToString("F1"));
            UpdateRopeHighlights();
        }

        private void UpdateRopeHighlights()
        {
            for (int i = 0; i < _ropeColorButtons.Length; i++)
            {
                var img = _ropeColorButtons[i].GetComponent<Image>();
                if (img == null) continue;
                Color[] c = { Color.black, Color.white, new Color(0.4f, 0.6f, 1f) };
                img.color = i == _env.RopeColorMode ? c[i] : c[i] * 0.45f;
            }
        }

        private void Notify() => _onChange?.Invoke();

        // ── Builder helpers ───────────────────────────────────────────────

        private TextMeshProUGUI AddLabel(RectTransform parent, string text, bool bold = false)
        {
            var go = new GameObject($"Lbl_{text}");
            go.transform.SetParent(parent, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, bold ? 13f : 12f, bold);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Left;
            go.AddComponent<LayoutElement>().minHeight = bold ? 20 : 16;
            return tmp;
        }

        private void AddLabeledField(RectTransform parent, string label,
            ref TMP_InputField fieldRef, string defaultVal, string tooltip)
        {
            var row = AddRow(parent);
            AddRowLabel(row, label);

            var go = new GameObject("Input");
            go.transform.SetParent(row, false);
            var le = go.AddComponent<LayoutElement>();
            le.flexibleWidth = 1;
            le.minHeight = 26;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.2f, 1f);
            bg.sprite = StyleHelper.MakeRoundedSprite();
            bg.type = Image.Type.Sliced;

            var field = go.AddComponent<TMP_InputField>();

            var phGo = new GameObject("PH");
            phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 12f);
            phTmp.color = new Color(0.4f, 0.4f, 0.4f);
            phTmp.text = defaultVal;
            phTmp.alignment = TextAlignmentOptions.Left;
            FullRect(phGo, 5, 2);

            var txtGo = new GameObject("Txt");
            txtGo.transform.SetParent(go.transform, false);
            var txtTmp = txtGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(txtTmp, 12f);
            txtTmp.alignment = TextAlignmentOptions.Left;
            FullRect(txtGo, 5, 2);

            field.textViewport = txtGo.GetComponent<RectTransform>();
            field.textComponent = txtTmp;
            field.placeholder = phTmp;
            field.caretColor = Color.white;
            field.text = defaultVal;
            fieldRef = field;
        }

        private RectTransform AddRow(RectTransform parent)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            go.AddComponent<LayoutElement>().minHeight = 28;
            return go.GetComponent<RectTransform>();
        }

        private Button AddRowButton(RectTransform row, string text, Color color)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(row, false);

            var img = go.AddComponent<Image>();
            img.color = color;
            img.sprite = StyleHelper.MakeRoundedSprite();
            img.type = Image.Type.Sliced;

            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;

            var lgo = new GameObject("L");
            lgo.transform.SetParent(go.transform, false);
            var tmp = lgo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 12f, bold: true);
            tmp.text = text;
            FullRect(lgo, 2, 1);

            return btn;
        }

        private void AddRowLabel(RectTransform row, string text)
        {
            var go = new GameObject($"RL_{text}");
            go.transform.SetParent(row, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 11f);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Right;
            go.AddComponent<LayoutElement>().minWidth = 90;
        }

        private Toggle AddToggle(RectTransform parent, bool defaultOn)
        {
            var go = new GameObject("Toggle");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minWidth = 32;

            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.2f, 0.25f, 0.4f, 1f);

            var toggle = go.AddComponent<Toggle>();
            toggle.isOn = defaultOn;

            var checkGo = new GameObject("Check");
            checkGo.transform.SetParent(go.transform, false);
            var checkImg = checkGo.AddComponent<Image>();
            checkImg.color = StyleHelper.Orange;
            FullRect(checkGo, 3, 3);

            toggle.graphic = checkImg;
            return toggle;
        }

        private void AddDivider(RectTransform parent)
        {
            var go = new GameObject("Div");
            go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.45f, 0.5f);
            go.AddComponent<LayoutElement>().minHeight = 1;
        }

        private void FullRect(GameObject go, float px = 0, float py = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(px, py);
            rt.offsetMax = new Vector2(-px, -py);
        }
    }
}

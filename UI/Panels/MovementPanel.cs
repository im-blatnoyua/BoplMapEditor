using BoplMapEditor.Data;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace BoplMapEditor.UI
{
    // Panel shown in the sidebar when a platform is selected.
    // Lets the user attach/configure movement to any platform.
    public class MovementPanel : MonoBehaviour
    {
        private PlatformData _data;
        private System.Action _onChange = null!;

        private readonly Button[] _typeButtons = new Button[4];
        private GameObject _linearSection = null!;
        private GameObject _circleSection = null!;
        private GameObject _pathSection = null!;

        // Linear fields
        private TMP_InputField _dirX = null!, _dirY = null!;
        private TMP_InputField _distance = null!, _speed = null!;

        // Circular fields
        private TMP_InputField _radius = null!, _angSpeed = null!, _startAngle = null!;

        // Common
        private TMP_InputField _damping = null!;

        // Path waypoint list
        private RectTransform _waypointContent = null!;
        private TextMeshProUGUI _waypointCountLabel = null!;

        public static MovementPanel Create(Transform parent, System.Action onChange)
        {
            var go = new GameObject("MovementPanel");
            go.transform.SetParent(parent, false);
            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;

            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.padding = new RectOffset(0, 0, 4, 4);
            layout.spacing = 5;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            var panel = go.AddComponent<MovementPanel>();
            panel._onChange = onChange;
            panel.BuildUI(go.GetComponent<RectTransform>());
            return panel;
        }

        public void SetData(PlatformData data)
        {
            _data = data;
            if (_data.Movement == null) _data.Movement = new PlatformMovement();
            RefreshAll();
        }

        private void BuildUI(RectTransform root)
        {
            AddLabel(root, "MOVEMENT", bold: true);

            // Type selector buttons
            var typeRow = AddRow(root);
            string[] names  = { "None", "Linear", "Circle", "Path" };
            Color[]  colors = {
                new Color(0.3f, 0.3f, 0.35f, 1f),
                StyleHelper.Blue,
                new Color(0.6f, 0.2f, 0.7f, 1f),
                new Color(0.7f, 0.5f, 0.1f, 1f),
            };
            for (int i = 0; i < 4; i++)
            {
                int idx = i;
                _typeButtons[i] = AddRowButton(typeRow, names[i], colors[i]);
                _typeButtons[i].onClick.AddListener(() => SetType((MovementType)idx));
            }

            AddDivider(root);

            // ── Linear section ────────────────────────────────────────────
            _linearSection = AddSection(root, "LINEAR");
            var lsRt = _linearSection.GetComponent<RectTransform>();

            AddLabel(lsRt, "Direction (X Y)");
            var dirRow = AddRow(lsRt);
            _dirX = AddRowInput(dirRow, "1.00"); _dirX.text = "1.00";
            _dirY = AddRowInput(dirRow, "0.00"); _dirY.text = "0.00";
            AddLabel(lsRt, "Distance");
            _distance = AddFullInput(lsRt, "10.0");
            AddLabel(lsRt, "Speed");
            _speed = AddFullInput(lsRt, "5.0");

            _dirX.onEndEdit.AddListener(v    => { if (float.TryParse(v, out float f)) { _data.Movement!.DirX = f; _onChange(); } });
            _dirY.onEndEdit.AddListener(v    => { if (float.TryParse(v, out float f)) { _data.Movement!.DirY = f; _onChange(); } });
            _distance.onEndEdit.AddListener(v => { if (float.TryParse(v, out float f)) { _data.Movement!.Distance = f; _onChange(); } });
            _speed.onEndEdit.AddListener(v   => { if (float.TryParse(v, out float f)) { _data.Movement!.Speed = f; _onChange(); } });

            // ── Circular section ──────────────────────────────────────────
            _circleSection = AddSection(root, "CIRCULAR");
            var csRt = _circleSection.GetComponent<RectTransform>();

            AddLabel(csRt, "Radius");
            _radius = AddFullInput(csRt, "8.0");
            AddLabel(csRt, "Speed (rev/sec)");
            _angSpeed = AddFullInput(csRt, "1.0");
            AddLabel(csRt, "Start angle (°)");
            _startAngle = AddFullInput(csRt, "0.0");

            _radius.onEndEdit.AddListener(v    => { if (float.TryParse(v, out float f)) { _data.Movement!.Radius = f; _onChange(); } });
            _angSpeed.onEndEdit.AddListener(v  => { if (float.TryParse(v, out float f)) { _data.Movement!.AngularSpeed = f; _onChange(); } });
            _startAngle.onEndEdit.AddListener(v=> { if (float.TryParse(v, out float f)) { _data.Movement!.StartAngle = f; _onChange(); } });

            // ── Path section ──────────────────────────────────────────────
            _pathSection = AddSection(root, "PATH");
            var psRt = _pathSection.GetComponent<RectTransform>();

            _waypointCountLabel = AddLabel(psRt, "Waypoints: 0");

            var addBtn = AddSideButton(psRt, "+ Add Waypoint (click canvas)", StyleHelper.Blue);
            addBtn.onClick.AddListener(OnAddWaypointMode);

            var clearBtn = AddSideButton(psRt, "Clear all waypoints",
                new Color(0.5f, 0.15f, 0.15f, 1f));
            clearBtn.onClick.AddListener(ClearWaypoints);

            // Waypoint list (scrollable)
            var scroll = UIBuilder.MakeScrollView(psRt,
                Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
            scroll.gameObject.AddComponent<LayoutElement>().minHeight = 80;
            _waypointContent = scroll.content;

            // Loop toggle
            var loopRow = AddRow(psRt);
            AddRowLabel(loopRow, "Loop path");
            var loopToggle = AddToggle(loopRow, true);
            loopToggle.onValueChanged.AddListener(v => { _data.Movement!.LoopPath = v; _onChange(); });

            // ── Common: Damping ───────────────────────────────────────────
            AddDivider(root);
            AddLabel(root, "Damping (smoothness)");
            _damping = AddFullInput(root, "3.0");
            _damping.onEndEdit.AddListener(v => { if (float.TryParse(v, out float f)) { _data.Movement!.Damping = f; _onChange(); } });
        }

        private void SetType(MovementType t)
        {
            _data.Movement!.Type = t;
            RefreshAll();
            _onChange();
        }

        public void RefreshAll()
        {
            var mov = _data.Movement!;

            // Update type button highlights
            Color[] colors = {
                new Color(0.3f, 0.3f, 0.35f, 1f), StyleHelper.Blue,
                new Color(0.6f, 0.2f, 0.7f, 1f), new Color(0.7f, 0.5f, 0.1f, 1f),
            };
            for (int i = 0; i < 4; i++)
            {
                var img = _typeButtons[i].GetComponent<Image>();
                if (img != null) img.color = i == (int)mov.Type ? colors[i] : colors[i] * 0.45f;
            }

            // Show/hide sections
            _linearSection.SetActive(mov.Type == MovementType.Linear);
            _circleSection.SetActive(mov.Type == MovementType.Circular);
            _pathSection.SetActive(mov.Type == MovementType.Path);

            if (mov.Type == MovementType.None) return;

            // Populate fields
            _dirX.SetTextWithoutNotify(mov.DirX.ToString("F2"));
            _dirY.SetTextWithoutNotify(mov.DirY.ToString("F2"));
            _distance.SetTextWithoutNotify(mov.Distance.ToString("F1"));
            _speed.SetTextWithoutNotify(mov.Speed.ToString("F1"));
            _radius.SetTextWithoutNotify(mov.Radius.ToString("F1"));
            _angSpeed.SetTextWithoutNotify(mov.AngularSpeed.ToString("F2"));
            _startAngle.SetTextWithoutNotify(mov.StartAngle.ToString("F1"));
            _damping.SetTextWithoutNotify(mov.Damping.ToString("F1"));

            RefreshWaypointList();
        }

        private void RefreshWaypointList()
        {
            var mov = _data.Movement!;
            _waypointCountLabel.text = $"Waypoints: {mov.Waypoints.Count}";

            foreach (Transform child in _waypointContent) Destroy(child.gameObject);

            for (int i = 0; i < mov.Waypoints.Count; i++)
            {
                int idx = i;
                var w = mov.Waypoints[i];
                var row = AddRow(_waypointContent);
                row.gameObject.AddComponent<LayoutElement>().minHeight = 26;

                AddRowLabel(row, $"#{i + 1}");

                var lbl = new GameObject("Lbl");
                lbl.transform.SetParent(row, false);
                var tmp = lbl.AddComponent<TextMeshProUGUI>();
                StyleHelper.StyleText(tmp, 11f);
                tmp.text = $"{w.x:F1}, {w.y:F1}";
                tmp.alignment = TextAlignmentOptions.Left;
                lbl.AddComponent<LayoutElement>().flexibleWidth = 1;

                var delBtn = AddRowButton(row, "✕", new Color(0.5f, 0.15f, 0.15f, 1f));
                delBtn.GetComponent<LayoutElement>().minWidth = 28;
                delBtn.onClick.AddListener(() => {
                    mov.Waypoints.RemoveAt(idx);
                    RefreshWaypointList();
                    _onChange();
                });
            }
        }

        // Called by EditorCanvasController when in waypoint-add mode
        public void AddWaypoint(Vector2 worldPos)
        {
            _data.Movement!.Waypoints.Add(new SerializableVec2(worldPos.x, worldPos.y));
            RefreshWaypointList();
            _onChange();
        }

        private bool _waypointAddMode;
        public bool WaypointAddMode => _waypointAddMode;

        private void OnAddWaypointMode()
        {
            _waypointAddMode = true;
            Plugin.Log.LogInfo("[MovementPanel] Click on canvas to add waypoint.");
        }

        public void CancelWaypointMode() => _waypointAddMode = false;

        private void ClearWaypoints()
        {
            _data.Movement!.Waypoints.Clear();
            RefreshWaypointList();
            _onChange();
        }

        // ── Builder helpers ───────────────────────────────────────────────

        private TextMeshProUGUI AddLabel(RectTransform p, string text, bool bold = false)
        {
            var go = new GameObject($"L_{text}");
            go.transform.SetParent(p, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, bold ? 13f : 11f, bold);
            tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Left;
            go.AddComponent<LayoutElement>().minHeight = bold ? 18 : 15;
            return tmp;
        }

        private GameObject AddSection(RectTransform parent, string title)
        {
            var go = new GameObject($"Section_{title}");
            go.transform.SetParent(parent, false);
            var layout = go.AddComponent<VerticalLayoutGroup>();
            layout.spacing = 4;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;
            go.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            go.SetActive(false);
            return go;
        }

        private RectTransform AddRow(RectTransform parent)
        {
            var go = new GameObject("Row");
            go.transform.SetParent(parent, false);
            var hlg = go.AddComponent<HorizontalLayoutGroup>();
            hlg.spacing = 3;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = true;
            go.AddComponent<LayoutElement>().minHeight = 26;
            return go.GetComponent<RectTransform>();
        }

        private Button AddRowButton(RectTransform row, string text, Color color)
        {
            var go = new GameObject($"Btn_{text}");
            go.transform.SetParent(row, false);
            var img = go.AddComponent<Image>();
            img.color = color; img.sprite = StyleHelper.MakeRoundedSprite(); img.type = Image.Type.Sliced;
            var btn = go.AddComponent<Button>();
            StyleHelper.StyleButton(btn, color);
            StyleHelper.AddPressColorSwap(btn);
            go.AddComponent<LayoutElement>().flexibleWidth = 1;
            var lgo = new GameObject("L"); lgo.transform.SetParent(go.transform, false);
            var tmp = lgo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 11f, bold: true); tmp.text = text;
            FullRect(lgo);
            return btn;
        }

        private Button AddSideButton(RectTransform parent, string text, Color color)
        {
            var btn = UIBuilder.MakeButton(parent, text, color, Vector2.zero, Vector2.zero);
            btn.gameObject.AddComponent<LayoutElement>().minHeight = 28;
            return btn;
        }

        private TMP_InputField AddFullInput(RectTransform parent, string val)
        {
            var go = new GameObject("Input");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minHeight = 26;
            var bg = go.AddComponent<Image>();
            bg.color = new Color(0.1f, 0.12f, 0.2f, 1f);
            bg.sprite = StyleHelper.MakeRoundedSprite(); bg.type = Image.Type.Sliced;
            var field = go.AddComponent<TMP_InputField>();
            var phGo = new GameObject("PH"); phGo.transform.SetParent(go.transform, false);
            var phTmp = phGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(phTmp, 12f); phTmp.color = new Color(0.4f, 0.4f, 0.4f);
            phTmp.text = val; phTmp.alignment = TextAlignmentOptions.Left; FullRect(phGo, 5, 2);
            var tGo = new GameObject("T"); tGo.transform.SetParent(go.transform, false);
            var tTmp = tGo.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tTmp, 12f); tTmp.alignment = TextAlignmentOptions.Left; FullRect(tGo, 5, 2);
            field.textViewport = tGo.GetComponent<RectTransform>();
            field.textComponent = tTmp; field.placeholder = phTmp;
            field.caretColor = Color.white; field.text = val;
            return field;
        }

        private TMP_InputField AddRowInput(RectTransform row, string val)
        {
            var f = AddFullInput(row, val);
            f.gameObject.AddComponent<LayoutElement>().flexibleWidth = 1;
            return f;
        }

        private void AddRowLabel(RectTransform row, string text)
        {
            var go = new GameObject($"RL_{text}");
            go.transform.SetParent(row, false);
            var tmp = go.AddComponent<TextMeshProUGUI>();
            StyleHelper.StyleText(tmp, 11f); tmp.text = text;
            tmp.alignment = TextAlignmentOptions.Right;
            go.AddComponent<LayoutElement>().minWidth = 30;
        }

        private Toggle AddToggle(RectTransform parent, bool on)
        {
            var go = new GameObject("Toggle");
            go.transform.SetParent(parent, false);
            go.AddComponent<LayoutElement>().minWidth = 28;
            go.AddComponent<Image>().color = new Color(0.2f, 0.25f, 0.4f, 1f);
            var t = go.AddComponent<Toggle>(); t.isOn = on;
            var chk = new GameObject("Chk"); chk.transform.SetParent(go.transform, false);
            var ci = chk.AddComponent<Image>(); ci.color = StyleHelper.Orange;
            FullRect(chk, 3, 3); t.graphic = ci;
            return t;
        }

        private void AddDivider(RectTransform parent)
        {
            var go = new GameObject("Div"); go.transform.SetParent(parent, false);
            go.AddComponent<Image>().color = new Color(0.25f, 0.3f, 0.45f, 0.5f);
            go.AddComponent<LayoutElement>().minHeight = 1;
        }

        private void FullRect(GameObject go, float px = 0, float py = 0)
        {
            var rt = go.GetComponent<RectTransform>();
            rt.anchorMin = Vector2.zero; rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(px, py); rt.offsetMax = new Vector2(-px, -py);
        }
    }
}

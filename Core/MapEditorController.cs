using System.Collections.Generic;
using BoplMapEditor.Data;
using BoplMapEditor.Sync;
using UnityEngine;

namespace BoplMapEditor.Core
{
    public enum EditorTool { Select, Place, Delete }

    public class MapEditorController
    {
        public bool IsOpen { get; private set; }
        public bool ShowMapBrowser { get; private set; }

        public MapData CurrentMap { get; private set; } = new MapData("Untitled");

        public EditorTool ActiveTool = EditorTool.Select;
        public int SelectedPlatformIndex = -1;
        public int PlacePlatformType = 0;

        // Drag state
        private HandleType? _activeHandle;
        private Vector2 _lastMouseWorld;
        private bool _isDragging;

        public void Open(MapData? existing = null)
        {
            IsOpen = true;
            CurrentMap = existing?.Clone() ?? new MapData("Untitled");
            SelectedPlatformIndex = -1;
        }

        public void Close()
        {
            IsOpen = false;
            ShowMapBrowser = false;
        }

        public void OpenMapBrowser() => ShowMapBrowser = true;
        public void CloseMapBrowser() => ShowMapBrowser = false;

        public void NewMap()
        {
            CurrentMap = new MapData("Untitled");
            SelectedPlatformIndex = -1;
        }

        public void SaveCurrentMap(string name)
        {
            MapSerializer.SaveMap(CurrentMap, name);
            Plugin.Log.LogInfo($"[Editor] Saved map '{name}'");
        }

        public void LoadFromFile(string name)
        {
            var map = MapSerializer.LoadMap(name);
            if (map != null)
            {
                CurrentMap = map;
                SelectedPlatformIndex = -1;
                Plugin.Log.LogInfo($"[Editor] Loaded map '{name}' with {map.Platforms.Count} platforms");
            }
        }

        public void PushToLobby()
        {
            LobbySync.PushMap(CurrentMap);
        }

        // Called every OnGUI frame when editor is open
        public void HandleInput(Event e, Rect canvas)
        {
            Vector2 mouseScreen = e.mousePosition;
            if (!canvas.Contains(mouseScreen)) return;

            Vector2 mouseWorld = EditorCamera.ScreenToWorld(mouseScreen);

            if (e.type == EventType.MouseDown && e.button == 0)
            {
                OnMouseDown(mouseWorld);
                e.Use();
            }
            else if (e.type == EventType.MouseDrag && e.button == 0 && _isDragging)
            {
                Vector2 delta = mouseWorld - _lastMouseWorld;
                OnMouseDrag(delta);
                _lastMouseWorld = mouseWorld;
                e.Use();
            }
            else if (e.type == EventType.MouseUp && e.button == 0)
            {
                _isDragging = false;
                _activeHandle = null;
                e.Use();
            }
            else if (e.type == EventType.KeyDown && e.keyCode == KeyCode.Delete)
            {
                DeleteSelected();
                e.Use();
            }

            _lastMouseWorld = mouseWorld;
        }

        private void OnMouseDown(Vector2 mouseWorld)
        {
            switch (ActiveTool)
            {
                case EditorTool.Place:
                    AddPlatform(mouseWorld);
                    break;

                case EditorTool.Delete:
                    int hitDel = HitTestPlatforms(mouseWorld);
                    if (hitDel >= 0)
                    {
                        CurrentMap.Platforms.RemoveAt(hitDel);
                        SelectedPlatformIndex = -1;
                    }
                    break;

                case EditorTool.Select:
                    // First check handles on selected platform
                    if (SelectedPlatformIndex >= 0 && SelectedPlatformIndex < CurrentMap.Platforms.Count)
                    {
                        var handle = SelectionHandle.HitTest(CurrentMap.Platforms[SelectedPlatformIndex], mouseWorld);
                        if (handle.HasValue)
                        {
                            _activeHandle = handle;
                            _isDragging = true;
                            return;
                        }
                    }
                    // Then check for platform selection
                    int hit = HitTestPlatforms(mouseWorld);
                    SelectedPlatformIndex = hit;
                    if (hit >= 0)
                    {
                        _activeHandle = HandleType.MoveCenter;
                        _isDragging = true;
                    }
                    break;
            }
        }

        private void OnMouseDrag(Vector2 worldDelta)
        {
            if (!_isDragging || !_activeHandle.HasValue) return;
            if (SelectedPlatformIndex < 0 || SelectedPlatformIndex >= CurrentMap.Platforms.Count) return;

            var p = CurrentMap.Platforms[SelectedPlatformIndex];
            SelectionHandle.ApplyDrag(ref p, _activeHandle.Value, worldDelta);
            CurrentMap.Platforms[SelectedPlatformIndex] = p;
        }

        public void AddPlatform(Vector2 worldPos)
        {
            var p = new PlatformData(worldPos.x, worldPos.y, 8f, 1.5f, 1f, 0f, PlacePlatformType);
            CurrentMap.Platforms.Add(p);
            SelectedPlatformIndex = CurrentMap.Platforms.Count - 1;
            ActiveTool = EditorTool.Select;
        }

        public void DeleteSelected()
        {
            if (SelectedPlatformIndex < 0 || SelectedPlatformIndex >= CurrentMap.Platforms.Count) return;
            CurrentMap.Platforms.RemoveAt(SelectedPlatformIndex);
            SelectedPlatformIndex = Mathf.Clamp(SelectedPlatformIndex - 1, -1, CurrentMap.Platforms.Count - 1);
        }

        private int HitTestPlatforms(Vector2 mouseWorld)
        {
            // Iterate in reverse so topmost (last drawn) is hit first
            for (int i = CurrentMap.Platforms.Count - 1; i >= 0; i--)
            {
                var p = CurrentMap.Platforms[i];
                if (Mathf.Abs(mouseWorld.x - p.X) <= p.HalfW &&
                    Mathf.Abs(mouseWorld.y - p.Y) <= p.HalfH)
                    return i;
            }
            return -1;
        }
    }
}

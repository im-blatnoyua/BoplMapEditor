using System.Collections.Generic;
using BoplMapEditor.Data;

namespace BoplMapEditor.Core
{
    public interface IEditorCommand
    {
        void Execute(MapData map);
        void Undo(MapData map);
    }

    public class AddPlatformCommand : IEditorCommand
    {
        private readonly PlatformData _p;

        public AddPlatformCommand(PlatformData p) { _p = p; }

        public void Execute(MapData map)
        {
            map.Platforms.Add(_p);
        }

        public void Undo(MapData map)
        {
            if (map.Platforms.Count > 0)
                map.Platforms.RemoveAt(map.Platforms.Count - 1);
        }
    }

    public class DeletePlatformCommand : IEditorCommand
    {
        private readonly PlatformData _p;
        private readonly int _index;

        public DeletePlatformCommand(int index, PlatformData p)
        {
            _index = index;
            _p = p;
        }

        public void Execute(MapData map)
        {
            if (_index < map.Platforms.Count)
                map.Platforms.RemoveAt(_index);
        }

        public void Undo(MapData map)
        {
            map.Platforms.Insert(System.Math.Min(_index, map.Platforms.Count), _p);
        }
    }

    public class MovePlatformCommand : IEditorCommand
    {
        private readonly int _index;
        private readonly PlatformData _before;
        private readonly PlatformData _after;

        public MovePlatformCommand(int index, PlatformData before, PlatformData after)
        {
            _index = index;
            _before = before;
            _after = after;
        }

        public void Execute(MapData map)
        {
            if (_index < map.Platforms.Count)
                map.Platforms[_index] = _after;
        }

        public void Undo(MapData map)
        {
            if (_index < map.Platforms.Count)
                map.Platforms[_index] = _before;
        }
    }

    public class EditorHistory
    {
        private readonly Stack<IEditorCommand> _undo = new Stack<IEditorCommand>();
        private readonly Stack<IEditorCommand> _redo = new Stack<IEditorCommand>();
        private readonly MapEditorController _ctrl;

        public bool CanUndo { get { return _undo.Count > 0; } }
        public bool CanRedo { get { return _redo.Count > 0; } }

        public EditorHistory(MapEditorController ctrl) { _ctrl = ctrl; }

        // Execute a command and push it onto the undo stack, clearing redo.
        public void Push(IEditorCommand cmd)
        {
            cmd.Execute(_ctrl.CurrentMap);
            _undo.Push(cmd);
            _redo.Clear();
        }

        // Push a command that has already been applied (drag moves, etc.) without
        // calling Execute again. Clears the redo stack.
        public void PushDone(IEditorCommand cmd)
        {
            _undo.Push(cmd);
            _redo.Clear();
        }

        public void Undo()
        {
            if (!CanUndo) return;
            var cmd = _undo.Pop();
            cmd.Undo(_ctrl.CurrentMap);
            _redo.Push(cmd);
        }

        public void Redo()
        {
            if (!CanRedo) return;
            var cmd = _redo.Pop();
            cmd.Execute(_ctrl.CurrentMap);
            _undo.Push(cmd);
        }

        public void Clear()
        {
            _undo.Clear();
            _redo.Clear();
        }
    }
}

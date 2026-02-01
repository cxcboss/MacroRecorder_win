using System.Text.Json.Serialization;

namespace MacroRecorder.Models
{
    public enum ActionType
    {
        MouseMove,
        MouseClick,
        MouseWheel,
        KeyDown,
        KeyUp
    }

    public abstract class InputAction
    {
        public TimeSpan Timestamp { get; set; }
        public abstract ActionType ActionType { get; }
    }

    public class MouseMoveAction : InputAction
    {
        public override ActionType ActionType => ActionType.MouseMove;
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class MouseClickAction : InputAction
    {
        public override ActionType ActionType => ActionType.MouseClick;
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsLeftButton { get; set; }
        public bool IsDown { get; set; }
    }

    public class MouseWheelAction : InputAction
    {
        public override ActionType ActionType => ActionType.MouseWheel;
        public int Delta { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class KeyAction : InputAction
    {
        public override ActionType ActionType => ActionType.KeyDown;
        public int KeyCode { get; set; }
        public bool IsDown { get; set; }
        public string? KeyName { get; set; }
    }

    public class Recording
    {
        public Guid Id { get; set; } = Guid.NewGuid();
        public string Name { get; set; } = "新建录制";
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        public List<InputAction> Actions { get; set; } = new();
        public TimeSpan TotalDuration => Actions.Count > 0 
            ? Actions.Max(a => a.Timestamp) - Actions.Min(a => a.Timestamp) 
            : TimeSpan.Zero;
    }
}

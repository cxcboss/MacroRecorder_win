using System.Runtime.InteropServices;

namespace MacroRecorder
{
    public class HotKeyService : IDisposable
    {
        private const int WM_HOTKEY = 0x0312;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private IntPtr _hWnd;
        private int _currentId;

        public event Action<int>? OnHotKeyPressed;

        public void Initialize(IntPtr hWnd)
        {
            _hWnd = hWnd;
        }

        public bool Register(int id, uint modifiers, uint vk)
        {
            return RegisterHotKey(_hWnd, id, modifiers, vk);
        }

        public void Unregister(int id)
        {
            UnregisterHotKey(_hWnd, id);
        }

        public void Dispose()
        {
        }
    }
}

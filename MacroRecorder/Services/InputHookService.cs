using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace MacroRecorder.Services
{
    public class InputHookService : IDisposable
    {
        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;

        private IntPtr _mouseHook = IntPtr.Zero;
        private IntPtr _keyboardHook = IntPtr.Zero;
        private IntPtr _mouseHookProc = IntPtr.Zero;
        private IntPtr _keyboardHookProc = IntPtr.Zero;

        private bool _isRecording = false;
        private DateTime _recordingStartTime;
        private readonly List<Models.InputAction> _actions = [];

        public event Action<Models.InputAction>? OnActionCaptured;
        public event Action? OnRecordingStarted;
        public event Action? OnRecordingStopped;

        public bool IsRecording => _isRecording;
        public IReadOnlyList<Models.InputAction> RecordedActions => _actions;

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern int GetKeyNameTextW(int lParam, StringBuilder lpString, int nSize);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr GetModuleHandle(string? lpModuleName);

        private struct MSLLHOOKSTRUCT
        {
            public int ptX;
            public int ptY;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }

        public void StartRecording()
        {
            if (_isRecording) return;

            _actions.Clear();
            _recordingStartTime = DateTime.Now;
            _isRecording = true;

            using Process currentProcess = Process.GetCurrentProcess();
            using ProcessModule? module = currentProcess.MainModule;
            IntPtr hMod = module != null ? GetModuleHandle(module.ModuleName) : GetModuleHandle(null);

            _mouseHookProc = Marshal.GetFunctionPointerForDelegate<MouseProc>(MouseHookCallback);
            _keyboardHookProc = Marshal.GetFunctionPointerForDelegate<KeyboardProc>(KeyboardHookCallback);

            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, hMod, 0);
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProc, hMod, 0);

            if (_mouseHook == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _isRecording = false;
                throw new Exception($"无法安装鼠标钩子，错误码: {error}");
            }

            if (_keyboardHook == IntPtr.Zero)
            {
                int error = Marshal.GetLastWin32Error();
                _isRecording = false;
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
                throw new Exception($"无法安装键盘钩子，错误码: {error}");
            }

            OnRecordingStarted?.Invoke();
        }

        public List<Models.InputAction> StopRecording()
        {
            if (!_isRecording) return [];

            _isRecording = false;

            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }

            OnRecordingStopped?.Invoke();

            return [.. _actions];
        }

        private void CaptureAction(Models.InputAction action)
        {
            if (!_isRecording) return;
            _actions.Add(action);
            OnActionCaptured?.Invoke(action);
        }

        private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isRecording)
            {
                try
                {
                    var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                    var timestamp = DateTime.Now - _recordingStartTime;
                    int actionType = wParam.ToInt32();

                    Models.InputAction? action = actionType switch
                    {
                        0x200 => new Models.MouseMoveAction
                        {
                            Timestamp = timestamp,
                            X = mouseStruct.ptX,
                            Y = mouseStruct.ptY
                        },
                        0x201 or 0x204 => new Models.MouseClickAction
                        {
                            Timestamp = timestamp,
                            X = mouseStruct.ptX,
                            Y = mouseStruct.ptY,
                            IsLeftButton = actionType == 0x201,
                            IsDown = true
                        },
                        0x202 or 0x205 => new Models.MouseClickAction
                        {
                            Timestamp = timestamp,
                            X = mouseStruct.ptX,
                            Y = mouseStruct.ptY,
                            IsLeftButton = actionType == 0x202,
                            IsDown = false
                        },
                        0x20A => new Models.MouseWheelAction
                        {
                            Timestamp = timestamp,
                            Delta = (short)(mouseStruct.mouseData >> 16),
                            X = mouseStruct.ptX,
                            Y = mouseStruct.ptY
                        },
                        _ => null
                    };

                    if (action != null)
                        CaptureAction(action);
                }
                catch
                {
                }
            }

            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }

        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isRecording)
            {
                try
                {
                    var keyboardStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                    var timestamp = DateTime.Now - _recordingStartTime;
                    bool isDown = wParam.ToInt32() == 0x100 || wParam.ToInt32() == 0x104;

                    var keyName = GetKeyName(keyboardStruct.vkCode, keyboardStruct.scanCode);

                    var action = new Models.KeyAction
                    {
                        Timestamp = timestamp,
                        KeyCode = keyboardStruct.vkCode,
                        KeyName = keyName,
                        IsDown = isDown
                    };
                    CaptureAction(action);
                }
                catch
                {
                }
            }

            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }

        private string GetKeyName(int vkCode, int scanCode)
        {
            var sb = new StringBuilder(256);
            int scanCodeForName = (scanCode << 16) | 1;
            GetKeyNameTextW(scanCodeForName, sb, 256);

            if (sb.Length > 0)
                return sb.ToString();

            return vkCode.ToString();
        }

        public void Dispose()
        {
            if (_mouseHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_mouseHook);
                _mouseHook = IntPtr.Zero;
            }

            if (_keyboardHook != IntPtr.Zero)
            {
                UnhookWindowsHookEx(_keyboardHook);
                _keyboardHook = IntPtr.Zero;
            }
        }

        private delegate IntPtr MouseProc(int nCode, IntPtr wParam, IntPtr lParam);
        private delegate IntPtr KeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    }
}

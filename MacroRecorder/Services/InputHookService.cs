using System.Diagnostics;
using System.Runtime.InteropServices;

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
        private readonly List<Models.InputAction> _actions = new();
        
        public event Action<Models.InputAction>? OnActionCaptured;
        public event Action? OnRecordingStarted;
        public event Action OnRecordingStopped;
        
        public bool IsRecording => _isRecording;
        
        public IReadOnlyList<Models.InputAction> RecordedActions => _actions.AsReadOnly();
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern short GetAsyncKeyState(int vKey);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int GetKeyNameText(int lParam, StringBuilder lpString, int nSize);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern int MapVirtualKey(int uCode, int uMapType);
        
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
            
            _mouseHookProc = Marshal.GetFunctionPointerForDelegate(MouseHookCallback);
            _keyboardHookProc = Marshal.GetFunctionPointerForDelegate(KeyboardHookCallback);
            
            _mouseHook = SetWindowsHookEx(WH_MOUSE_LL, _mouseHookProc, IntPtr.Zero, 0);
            _keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, _keyboardHookProc, IntPtr.Zero, 0);
            
            if (_mouseHook == IntPtr.Zero || _keyboardHook == IntPtr.Zero)
            {
                throw new Exception("无法安装系统钩子");
            }
            
            OnRecordingStarted?.Invoke();
        }
        
        public List<Models.InputAction> StopRecording()
        {
            if (!_isRecording) return new List<Models.InputAction>();
            
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
            
            return new List<Models.InputAction>(_actions);
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
                var mouseStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                var timestamp = DateTime.Now - _recordingStartTime;
                
                int actionType = wParam.ToInt32();
                
                if (actionType == 0x200) 
                {
                    var action = new Models.MouseMoveAction
                    {
                        Timestamp = timestamp,
                        X = mouseStruct.ptX,
                        Y = mouseStruct.ptY
                    };
                    CaptureAction(action);
                }
                else if (actionType == 0x201 || actionType == 0x204) 
                {
                    var action = new Models.MouseClickAction
                    {
                        Timestamp = timestamp,
                        X = mouseStruct.ptX,
                        Y = mouseStruct.ptY,
                        IsLeftButton = actionType == 0x201,
                        IsDown = true
                    };
                    CaptureAction(action);
                }
                else if (actionType == 0x202 || actionType == 0x205) 
                {
                    var action = new Models.MouseClickAction
                    {
                        Timestamp = timestamp,
                        X = mouseStruct.ptX,
                        Y = mouseStruct.ptY,
                        IsLeftButton = actionType == 0x202,
                        IsDown = false
                    };
                    CaptureAction(action);
                }
                else if (actionType == 0x20A) 
                {
                    var action = new Models.MouseWheelAction
                    {
                        Timestamp = timestamp,
                        Delta = (short)(mouseStruct.mouseData >> 16),
                        X = mouseStruct.ptX,
                        Y = mouseStruct.ptY
                    };
                    CaptureAction(action);
                }
            }
            
            return CallNextHookEx(_mouseHook, nCode, wParam, lParam);
        }
        
        private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && _isRecording)
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
            
            return CallNextHookEx(_keyboardHook, nCode, wParam, lParam);
        }
        
        private string GetKeyName(int vkCode, int scanCode)
        {
            var sb = new StringBuilder(256);
            int scanCodeForName = (scanCode << 16) | 1;
            GetKeyNameText(scanCodeForName, sb, 256);
            
            if (sb.Length > 0)
            {
                return sb.ToString();
            }
            
            return ((System.Windows.Forms.Keys)vkCode).ToString();
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
    }
}

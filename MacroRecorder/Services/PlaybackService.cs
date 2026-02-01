using System.Diagnostics;
using System.Runtime.InteropServices;
using MacroRecorder.Models;

namespace MacroRecorder.Services
{
    public class PlaybackService : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, IntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, IntPtr dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern int SetCursorPos(int x, int y);
        
        private const int MOUSEEVENTF_MOVE = 0x0001;
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_RIGHTDOWN = 0x0008;
        private const int MOUSEEVENTF_RIGHTUP = 0x00010;
        private const int MOUSEEVENTF_WHEEL = 0x0800;
        
        private const int KEYEVENTF_KEYDOWN = 0x0000;
        private const int KEYEVENTF_KEYUP = 0x0002;
        
        private CancellationTokenSource? _playbackCts;
        private bool _isPlaying = false;
        
        public bool IsPlaying => _isPlaying;
        
        public event Action? OnPlaybackStarted;
        public event Action? OnPlaybackStopped;
        public event Action<double>? OnProgressUpdated;
        
        public async Task PlayAsync(Recording recording, int repeatCount = 1, bool infinite = false)
        {
            if (_isPlaying)
            {
                Stop();
            }
            
            _isPlaying = true;
            _playbackCts = new CancellationTokenSource();
            
            OnPlaybackStarted?.Invoke();
            
            try
            {
                int currentRepeat = 0;
                
                while (infinite || currentRepeat < repeatCount)
                {
                    if (_playbackCts.Token.IsCancellationRequested)
                    {
                        break;
                    }
                    
                    await PlayActionsAsync(recording.Actions);
                    
                    currentRepeat++;
                    
                    if (!infinite && currentRepeat < repeatCount)
                    {
                        await Task.Delay(500, _playbackCts.Token);
                    }
                }
            }
            catch (OperationCanceledException)
            {
            }
            finally
            {
                _isPlaying = false;
                OnPlaybackStopped?.Invoke();
            }
        }
        
        public void Stop()
        {
            _playbackCts?.Cancel();
        }
        
        private async Task PlayActionsAsync(List<InputAction> actions)
        {
            if (actions.Count == 0) return;
            
            var baseTime = actions.Min(a => a.Timestamp);
            DateTime? lastActionTime = null;
            
            foreach (var action in actions)
            {
                if (_playbackCts.Token.IsCancellationRequested)
                {
                    break;
                }
                
                var actionTime = baseTime == DateTime.MinValue ? action.Timestamp : action.Timestamp - baseTime;
                
                if (lastActionTime.HasValue)
                {
                    var delay = actionTime - lastActionTime.Value;
                    if (delay.TotalMilliseconds > 0)
                    {
                        await Task.Delay(delay, _playbackCts.Token);
                    }
                }
                
                PlayAction(action);
                lastActionTime = actionTime;
            }
        }
        
        private void PlayAction(InputAction action)
        {
            switch (action)
            {
                case MouseMoveAction moveAction:
                    SetCursorPos(moveAction.X, moveAction.Y);
                    break;
                    
                case MouseClickAction clickAction:
                    var flags = clickAction.IsDown 
                        ? (clickAction.IsLeftButton ? MOUSEEVENTF_LEFTDOWN : MOUSEEVENTF_RIGHTDOWN)
                        : (clickAction.IsLeftButton ? MOUSEEVENTF_LEFTUP : MOUSEEVENTF_RIGHTUP);
                    
                    mouse_event(flags, clickAction.X, clickAction.Y, 0, IntPtr.Zero);
                    break;
                    
                case MouseWheelAction wheelAction:
                    mouse_event(MOUSEEVENTF_WHEEL, wheelAction.X, wheelAction.Y, wheelAction.Delta * 120, IntPtr.Zero);
                    break;
                    
                case KeyAction keyAction:
                    var keyFlags = keyAction.IsDown ? KEYEVENTF_KEYDOWN : KEYEVENTF_KEYUP;
                    keybd_event((byte)keyAction.KeyCode, 0, keyFlags, IntPtr.Zero);
                    break;
            }
        }
        
        public void Dispose()
        {
            Stop();
            _playbackCts?.Dispose();
        }
    }
}

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using MacroRecorder.Models;

namespace MacroRecorder.Services
{
    public class PythonRecorderService : IDisposable
    {
        private Process? _recorderProcess;
        private Process? _playerProcess;
        private bool _isRecording = false;
        private bool _isPlaying = false;
        private List<Models.InputAction> _recordedActions = new();

        public event Action<Models.InputAction>? OnActionCaptured;
        public event Action? OnRecordingStarted;
        public event Action? OnRecordingStopped;
        public event Action? OnPlaybackStarted;
        public event Action? OnPlaybackStopped;

        public bool IsRecording => _isRecording;
        public bool IsPlaying => _isPlaying;
        public IReadOnlyList<Models.InputAction> RecordedActions => _recordedActions;

        private string GetScriptPath(string scriptName)
        {
            string appDir = AppDomain.CurrentDomain.BaseDirectory;
            string projectDir = Path.GetDirectoryName(appDir) ?? appDir;
            return Path.Combine(projectDir, scriptName);
        }

        public void StartRecording()
        {
            if (_isRecording) return;

            _recordedActions.Clear();
            _isRecording = true;

            string recorderScript = GetScriptPath("recorder.py");

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{recorderScript}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _recorderProcess = Process.Start(startInfo);
            _recorderProcess!.OutputDataReceived += (sender, args) =>
            {
                if (args.Data == "RECORDING_STARTED")
                {
                    OnRecordingStarted?.Invoke();
                }
                else if (args.Data == "RECORDING_STOPPED")
                {
                    _isRecording = false;
                    OnRecordingStopped?.Invoke();
                }
            };
            _recorderProcess.BeginOutputReadLine();

            _recorderProcess.StandardInput.WriteLine("START");
        }

        public List<Models.InputAction> StopRecording()
        {
            if (!_isRecording || _recorderProcess == null) return new List<Models.InputAction>();

            _isRecording = false;
            _recorderProcess.StandardInput.WriteLine("STOP");
            _recorderProcess.StandardInput.WriteLine("QUIT");
            _recorderProcess.WaitForExit(1000);

            if (!_recorderProcess.HasExited)
            {
                _recorderProcess.Kill();
            }
            _recorderProcess.Dispose();
            _recorderProcess = null;

            OnRecordingStopped?.Invoke();
            return new List<Models.InputAction>(_recordedActions);
        }

        public void PlayActions(IReadOnlyList<Models.InputAction> actions)
        {
            if (_isPlaying || actions.Count == 0) return;

            _isPlaying = true;
            string playerScript = GetScriptPath("player.py");

            var startInfo = new ProcessStartInfo
            {
                FileName = "python",
                Arguments = $"\"{playerScript}\"",
                RedirectStandardInput = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            _playerProcess = Process.Start(startInfo);
            _playerProcess!.OutputDataReceived += (sender, args) =>
            {
                if (args.Data == "PLAYBACK_STARTED")
                {
                    OnPlaybackStarted?.Invoke();
                }
                else if (args.Data == "PLAYBACK_STOPPED")
                {
                    _isPlaying = false;
                    OnPlaybackStopped?.Invoke();
                }
            };
            _playerProcess.BeginOutputReadLine();

            var actionsData = actions.Select(a => new
            {
                type = a.ActionType switch
                {
                    ActionType.MouseMove => "mouse_move",
                    ActionType.MouseClick => "mouse_click",
                    ActionType.MouseWheel => "mouse_scroll",
                    ActionType.KeyDown => "key_press",
                    ActionType.KeyUp => "key_release",
                    _ => "unknown"
                },
                time = a.Timestamp.TotalSeconds,
                x = a switch
                {
                    MouseMoveAction ma => ma.X,
                    MouseClickAction ca => ca.X,
                    MouseWheelAction wa => wa.X,
                    _ => 0
                },
                y = a switch
                {
                    MouseMoveAction ma => ma.Y,
                    MouseClickAction ca => ca.Y,
                    MouseWheelAction wa => wa.Y,
                    _ => 0
                },
                button = (a as MouseClickAction)?.IsLeftButton == true ? "Button.left" : "Button.right",
                pressed = (a as MouseClickAction)?.IsDown,
                dx = (a as MouseWheelAction)?.Delta ?? 0,
                dy = 0,
                key = (a as KeyAction)?.KeyName ?? "",
                @checked = (a as KeyAction)?.IsDown
            }).ToList();

            var jsonData = System.Text.Json.JsonSerializer.Serialize(new { actions = actionsData });
            _playerProcess.StandardInput.WriteLine($"PLAY:{jsonData}");
        }

        public void StopPlayback()
        {
            if (_playerProcess == null) return;

            _playerProcess.StandardInput.WriteLine("STOP");
            _playerProcess.StandardInput.WriteLine("QUIT");
            _playerProcess.WaitForExit(1000);

            if (!_playerProcess.HasExited)
            {
                _playerProcess.Kill();
            }
            _playerProcess.Dispose();
            _playerProcess = null;

            _isPlaying = false;
            OnPlaybackStopped?.Invoke();
        }

        public void Dispose()
        {
            StopRecording();
            StopPlayback();
        }
    }
}

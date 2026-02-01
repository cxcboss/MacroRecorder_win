using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Media;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;

namespace MacroRecorder
{
    static class Program
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
        }
    }

    public class MainForm : Form
    {
        private ListView recordingList = null!;
        private ColumnHeader colName = null!;
        private ColumnHeader colCount = null!;
        private ColumnHeader colDuration = null!;
        private ColumnHeader colDate = null!;
        
        private ToolStripButton recordBtn = null!;
        private ToolStripButton playBtn = null!;
        private ToolStripButton stopPlayBtn = null!;
        private Button renameBtn = null!;
        private Button deleteBtn = null!;
        
        private Label repeatLabel = null!;
        private NumericUpDown repeatNumeric = null!;
        private Label statusLabel = null!;
        
        private bool recording = false;
        private bool playing = false;
        private List<Recording> recordings = new();
        private Recording? selectedRecording;
        
        private LowLevelHook? hook;
        private string recordingsPath;
        private DateTime recordStartTime;
        private List<InputEvent> currentEvents = new();
        
        public MainForm()
        {
            recordingsPath = Path.Combine(Application.StartupPath, "Recordings");
            Directory.CreateDirectory(recordingsPath);
            
            Text = "宏录制器";
            Size = new Size(700, 500);
            StartPosition = FormStartPosition.CenterScreen;
            
            InitializeComponents();
            LoadRecordings();
            ApplyTheme();
        }
        
        private void InitializeComponents()
        {
            TableLayoutPanel mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 4,
                Padding = new Padding(10)
            };
            
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));  // 工具栏
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));  // 列表
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));  // 设置区
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 30));  // 状态栏
            
            ToolStrip toolStrip = CreateToolStrip();
            mainPanel.Controls.Add(toolStrip, 0, 0);
            
            recordingList = new ListView
            {
                Dock = DockStyle.Fill,
                View = View.Details,
                FullRowSelect = true,
                GridLines = true,
                HideSelection = false
            };
            recordingList.SelectedIndexChanged += OnRecordingSelected;
            
            colName = new ColumnHeader { Text = "名称", Width = 250 };
            colCount = new ColumnHeader { Text = "动作数", Width = 80, TextAlign = HorizontalAlignment.Center };
            colDuration = new ColumnHeader { Text = "时长", Width = 80, TextAlign = HorizontalAlignment.Center };
            colDate = new ColumnHeader { Text = "创建时间", Width = 150, TextAlign = HorizontalAlignment.Center };
            
            recordingList.Columns.AddRange(new ColumnHeader[] { colName, colCount, colDuration, colDate });
            mainPanel.Controls.Add(recordingList, 0, 1);
            
            Panel settingsPanel = CreateSettingsPanel();
            mainPanel.Controls.Add(settingsPanel, 0, 2);
            
            statusLabel = new Label
            {
                Text = "准备就绪",
                Dock = DockStyle.Left,
                ForeColor = SystemColors.GrayText
            };
            mainPanel.Controls.Add(statusLabel, 0, 3);
            
            Controls.Add(mainPanel);
        }
        
        private ToolStrip CreateToolStrip()
        {
            ToolStrip toolStrip = new ToolStrip { Dock = DockStyle.Fill, RenderMode = ToolStripRenderMode.System };
            
            recordBtn = new ToolStripButton
            {
                Text = "开始录制",
                Image = null,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            recordBtn.Click += (s, e) => ToggleRecord();
            
            playBtn = new ToolStripButton
            {
                Text = "播放",
                Enabled = false,
                Image = null,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            playBtn.Click += (s, e) => PlaySelected();
            
            stopPlayBtn = new ToolStripButton
            {
                Text = "停止播放",
                Enabled = false,
                Image = null,
                DisplayStyle = ToolStripItemDisplayStyle.Text
            };
            stopPlayBtn.Click += (s, e) => StopPlayback();
            
            toolStrip.Items.Add(recordBtn);
            toolStrip.Items.Add(new ToolStripSeparator());
            toolStrip.Items.Add(playBtn);
            toolStrip.Items.Add(stopPlayBtn);
            
            return toolStrip;
        }
        
        private Panel CreateSettingsPanel()
        {
            Panel panel = new Panel { Dock = DockStyle.Fill };
            
            repeatLabel = new Label
            {
                Text = "播放次数:",
                Location = new Point(10, 15),
                AutoSize = true
            };
            
            repeatNumeric = new NumericUpDown
            {
                Location = new Point(80, 12),
                Width = 60,
                Minimum = 1,
                Maximum = 999,
                Value = 1
            };
            
            renameBtn = new Button
            {
                Text = "重命名",
                Location = new Point(200, 10),
                Size = new Size(80, 30),
                Enabled = false
            };
            renameBtn.Click += (s, e) => RenameSelected();
            
            deleteBtn = new Button
            {
                Text = "删除",
                Location = new Point(290, 10),
                Size = new Size(80, 30),
                Enabled = false
            };
            deleteBtn.Click += (s, e) => DeleteSelected();
            
            panel.Controls.AddRange(new Control[] { repeatLabel, repeatNumeric, renameBtn, deleteBtn });
            
            return panel;
        }
        
        private void ToggleRecord()
        {
            if (!recording)
                StartRecord();
            else
                StopRecord();
        }
        
        private void StartRecord()
        {
            recording = true;
            recordStartTime = DateTime.Now;
            currentEvents.Clear();
            selectedRecording = null;
            recordingList.SelectedIndices.Clear();
            
            recordBtn.Text = "停止录制";
            playBtn.Enabled = false;
            stopPlayBtn.Enabled = false;
            renameBtn.Enabled = false;
            deleteBtn.Enabled = false;
            
            statusLabel.Text = "正在录制...";
            
            hook = new LowLevelHook();
            hook.OnMouseMove += OnMouseMove;
            hook.OnMouseClick += OnMouseClick;
            hook.OnMouseWheel += OnMouseWheel;
            hook.OnKeyDown += OnKeyDown;
            hook.OnKeyUp += OnKeyUp;
            hook.Install();
        }
        
        private void StopRecord()
        {
            recording = false;
            hook?.Uninstall();
            hook = null;
            
            recordBtn.Text = "开始录制";
            
            if (currentEvents.Count > 0)
            {
                var recording = new Recording
                {
                    Id = Guid.NewGuid(),
                    Name = $"录制 {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    CreatedAt = DateTime.Now,
                    Events = currentEvents
                };
                
                SaveRecording(recording);
                recordings.Add(recording);
                AddRecordingToList(recording);
                
                statusLabel.Text = $"录制完成 ({currentEvents.Count} 个动作)";
            }
            else
            {
                statusLabel.Text = "未录制到任何动作";
            }
            
            currentEvents.Clear();
        }
        
        private void OnMouseMove(int x, int y)
        {
            GetCursorPos(out Point screenPos);
            currentEvents.Add(new InputEvent(EventType.MouseMove, GetTimestamp(), screenPos.X, screenPos.Y));
        }
        
        private void OnMouseClick(int x, int y, bool isDown, bool isLeft)
        {
            GetCursorPos(out Point screenPos);
            currentEvents.Add(new InputEvent(EventType.MouseClick, GetTimestamp(), screenPos.X, screenPos.Y, isDown, isLeft));
        }
        
        private void OnMouseWheel(int x, int y, int delta)
        {
            currentEvents.Add(new InputEvent(EventType.MouseWheel, GetTimestamp(), x, y, false, false, delta));
        }
        
        private void OnKeyDown(int keyCode)
        {
            currentEvents.Add(new InputEvent(EventType.KeyDown, GetTimestamp(), 0, 0, true, false, 0, keyCode));
        }
        
        private void OnKeyUp(int keyCode)
        {
            currentEvents.Add(new InputEvent(EventType.KeyUp, GetTimestamp(), 0, 0, false, false, 0, keyCode));
        }
        
        private double GetTimestamp()
        {
            return (DateTime.Now - recordStartTime).TotalMilliseconds;
        }
        
        private void PlaySelected()
        {
            if (selectedRecording == null) return;
            
            playing = true;
            playBtn.Enabled = false;
            stopPlayBtn.Enabled = true;
            recordBtn.Enabled = false;
            renameBtn.Enabled = false;
            deleteBtn.Enabled = false;
            recordingList.Enabled = false;
            
            int repeatCount = (int)repeatNumeric.Value;
            Thread playThread = new Thread(() => PlayRecording(selectedRecording, repeatCount));
            playThread.IsBackground = true;
            playThread.Start();
        }
        
        private void StopPlayback()
        {
            playing = false;
        }
        
        private void PlayRecording(Recording recording, int repeatCount)
        {
            try
            {
                for (int i = 0; i < repeatCount && playing; i++)
                {
                    if (repeatCount > 1)
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            statusLabel.Text = $"播放中... ({i + 1}/{repeatCount})";
                        });
                    }
                    else
                    {
                        this.Invoke((MethodInvoker)delegate
                        {
                            statusLabel.Text = "播放中...";
                        });
                    }
                    
                    DateTime playStart = DateTime.Now;
                    
                    foreach (var evt in recording.Events)
                    {
                        if (!playing) break;
                        
                        double targetTime = evt.Timestamp;
                        double elapsed = (DateTime.Now - playStart).TotalMilliseconds;
                        double wait = targetTime - elapsed;
                        
                        if (wait > 0)
                            Thread.Sleep((int)wait);
                        
                        this.Invoke((MethodInvoker)delegate
                        {
                            switch (evt.Type)
                            {
                                case EventType.MouseMove:
                                    SetCursorPos(evt.X, evt.Y);
                                    break;
                                case EventType.MouseClick:
                                    if (evt.IsDown)
                                        mouse_event(MOUSEEVENTF_LEFTDOWN, 0, 0, 0, 0);
                                    else
                                        mouse_event(MOUSEEVENTF_LEFTUP, 0, 0, 0, 0);
                                    break;
                                case EventType.MouseWheel:
                                    mouse_event(MOUSEEVENTF_WHEEL, 0, 0, evt.Delta, 0);
                                    break;
                                case EventType.KeyDown:
                                    keybd_event((byte)evt.KeyCode, 0, 0, 0);
                                    break;
                                case EventType.KeyUp:
                                    keybd_event((byte)evt.KeyCode, 0, KEYEVENTF_KEYUP, 0);
                                    break;
                            }
                        });
                    }
                }
                
                playing = false;
                this.Invoke((MethodInvoker)delegate
                {
                    playBtn.Enabled = true;
                    stopPlayBtn.Enabled = false;
                    recordBtn.Enabled = true;
                    renameBtn.Enabled = selectedRecording != null;
                    deleteBtn.Enabled = selectedRecording != null;
                    recordingList.Enabled = true;
                    statusLabel.Text = "播放完成";
                });
            }
            catch (Exception ex)
            {
                playing = false;
                MessageBox.Show("播放错误: " + ex.Message);
            }
        }
        
        private void RenameSelected()
        {
            if (selectedRecording == null) return;
            
            using var inputBox = new InputDialog("重命名", "输入新名称:", selectedRecording.Name);
            if (inputBox.ShowDialog() == DialogResult.OK && !string.IsNullOrWhiteSpace(inputBox.Result))
            {
                selectedRecording.Name = inputBox.Result;
                SaveRecording(selectedRecording);
                RefreshList();
                statusLabel.Text = "已重命名";
            }
        }
        
        private void DeleteSelected()
        {
            if (selectedRecording == null) return;
            
            if (MessageBox.Show($"确定要删除 \"{selectedRecording.Name}\" 吗？", 
                "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                string filePath = GetRecordingPath(selectedRecording.Id);
                if (File.Exists(filePath))
                    File.Delete(filePath);
                
                recordings.Remove(selectedRecording);
                selectedRecording = null;
                
                playBtn.Enabled = false;
                renameBtn.Enabled = false;
                deleteBtn.Enabled = false;
                
                RefreshList();
                statusLabel.Text = "已删除";
            }
        }
        
        private void OnRecordingSelected(object? sender, EventArgs e)
        {
            if (recordingList.SelectedIndices.Count > 0)
            {
                int index = recordingList.SelectedIndices[0];
                if (index >= 0 && index < recordings.Count)
                {
                    selectedRecording = recordings[index];
                    playBtn.Enabled = true;
                    renameBtn.Enabled = true;
                    deleteBtn.Enabled = true;
                }
            }
            else
            {
                selectedRecording = null;
                playBtn.Enabled = false;
                renameBtn.Enabled = false;
                deleteBtn.Enabled = false;
            }
        }
        
        private void LoadRecordings()
        {
            recordings.Clear();
            recordingList.Items.Clear();
            
            if (!Directory.Exists(recordingsPath)) return;
            
            foreach (var file in Directory.GetFiles(recordingsPath, "*.json"))
            {
                try
                {
                    string json = File.ReadAllText(file);
                    var recording = JsonSerializer.Deserialize<Recording>(json);
                    if (recording != null && recording.Events.Count > 0)
                    {
                        recordings.Add(recording);
                        AddRecordingToList(recording);
                    }
                }
                catch { }
            }
        }
        
        private void SaveRecording(Recording recording)
        {
            string filePath = GetRecordingPath(recording.Id);
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(recording, options);
            File.WriteAllText(filePath, json);
        }
        
        private string GetRecordingPath(Guid id)
        {
            return Path.Combine(recordingsPath, $"{id}.json");
        }
        
        private void AddRecordingToList(Recording recording)
        {
            var item = new ListViewItem(recording.Name);
            item.SubItems.Add(recording.Events.Count.ToString());
            item.SubItems.Add(FormatDuration(recording.Events.Last().Timestamp));
            item.SubItems.Add(recording.CreatedAt.ToString("yyyy-MM-dd HH:mm"));
            item.Tag = recording.Id;
            recordingList.Items.Add(item);
        }
        
        private void RefreshList()
        {
            recordingList.Items.Clear();
            foreach (var recording in recordings)
            {
                AddRecordingToList(recording);
            }
        }
        
        private string FormatDuration(double milliseconds)
        {
            var ts = TimeSpan.FromMilliseconds(milliseconds);
            if (ts.TotalMinutes >= 1)
                return $"{ts.Minutes}:{ts.Seconds:D2}";
            return $"0:{ts.Seconds:D2}";
        }
        
        private void ApplyTheme()
        {
            if (IsDarkTheme())
            {
                BackColor = Color.FromArgb(30, 30, 30);
                ForeColor = Color.White;
                recordingList.BackColor = Color.FromArgb(45, 45, 45);
                recordingList.ForeColor = Color.White;
            }
        }
        
        private bool IsDarkTheme()
        {
            try
            {
                string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
                using var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(keyPath);
                if (key != null)
                {
                    var value = key.GetValue("AppsUseLightTheme");
                    if (value is int intValue && intValue == 0) return true;
                }
            }
            catch { }
            return false;
        }
        
        [DllImport("user32.dll")]
        private static extern void mouse_event(int dwFlags, int dx, int dy, int dwData, int dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern void keybd_event(byte bVk, byte bScan, int dwFlags, int dwExtraInfo);
        
        [DllImport("user32.dll")]
        private static extern bool SetCursorPos(int X, int Y);
        
        [DllImport("user32.dll")]
        private static extern bool GetCursorPos(out Point lpPoint);
        
        private const int MOUSEEVENTF_LEFTDOWN = 0x0002;
        private const int MOUSEEVENTF_LEFTUP = 0x0004;
        private const int MOUSEEVENTF_WHEEL = 0x0800;
        private const int KEYEVENTF_KEYUP = 0x0002;
    }
    
    public enum EventType { MouseMove, MouseClick, MouseWheel, KeyDown, KeyUp }
    
    public class InputEvent
    {
        public EventType Type { get; set; }
        public double Timestamp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public bool IsDown { get; set; }
        public bool IsLeft { get; set; }
        public int Delta { get; set; }
        public int KeyCode { get; set; }
        
        public InputEvent() { }
        
        public InputEvent(EventType type, double timestamp, int x, int y)
        {
            Type = type;
            Timestamp = timestamp;
            X = x;
            Y = y;
        }
        
        public InputEvent(EventType type, double timestamp, int x, int y, bool isDown, bool isLeft)
        {
            Type = type;
            Timestamp = timestamp;
            X = x;
            Y = y;
            IsDown = isDown;
            IsLeft = isLeft;
        }
        
        public InputEvent(EventType type, double timestamp, int x, int y, bool isDown, bool isLeft, int delta)
        {
            Type = type;
            Timestamp = timestamp;
            X = x;
            Y = y;
            IsDown = isDown;
            IsLeft = isLeft;
            Delta = delta;
        }
        
        public InputEvent(EventType type, double timestamp, int x, int y, bool isDown, bool isLeft, int delta, int keyCode)
        {
            Type = type;
            Timestamp = timestamp;
            X = x;
            Y = y;
            IsDown = isDown;
            IsLeft = isLeft;
            Delta = delta;
            KeyCode = keyCode;
        }
    }
    
    public class Recording
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = "";
        public DateTime CreatedAt { get; set; }
        public List<InputEvent> Events { get; set; } = new();
    }
    
    public class InputDialog : Form
    {
        public string Result { get; private set; } = "";
        
        public InputDialog(string title, string prompt, string defaultValue = "")
        {
            Text = title;
            Size = new Size(350, 150);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            StartPosition = FormStartPosition.CenterParent;
            MaximizeBox = false;
            MinimizeBox = false;
            
            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 1,
                RowCount = 3,
                Padding = new Padding(10)
            };
            
            var label = new Label
            {
                Text = prompt,
                Dock = DockStyle.Top,
                AutoSize = true
            };
            
            var textBox = new TextBox
            {
                Text = defaultValue,
                Dock = DockStyle.Top,
                Margin = new Padding(0, 5, 0, 10)
            };
            
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft
            };
            
            var okBtn = new Button { Text = "确定", DialogResult = DialogResult.OK, Width = 70 };
            var cancelBtn = new Button { Text = "取消", DialogResult = DialogResult.Cancel, Width = 70, Margin = new Padding(5, 0, 0, 0) };
            
            okBtn.Click += (s, e) => { Result = textBox.Text; };
            
            buttonPanel.Controls.AddRange(new Control[] { okBtn, cancelBtn });
            
            panel.Controls.Add(label);
            panel.Controls.Add(textBox);
            panel.Controls.Add(buttonPanel);
            
            Controls.Add(panel);
            AcceptButton = okBtn;
            CancelButton = cancelBtn;
        }
    }
    
    public delegate IntPtr LowLevelMouseProc(int nCode, IntPtr wParam, IntPtr lParam);
    public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);
    
    public class LowLevelHook : IDisposable
    {
        private IntPtr mouseHook = IntPtr.Zero;
        private IntPtr keyboardHook = IntPtr.Zero;
        private LowLevelMouseProc? mouseProc;
        private LowLevelKeyboardProc? keyboardProc;
        
        public event Action<int, int>? OnMouseMove;
        public event Action<int, int, bool, bool>? OnMouseClick;
        public event Action<int, int, int>? OnMouseWheel;
        public event Action<int>? OnKeyDown;
        public event Action<int>? OnKeyUp;
        
        public LowLevelHook()
        {
            mouseProc = MouseCallback;
            keyboardProc = KeyboardCallback;
        }
        
        public void Install()
        {
            mouseHook = SetWindowsHookEx(WH_MOUSE_LL, Marshal.GetFunctionPointerForDelegate(mouseProc!), IntPtr.Zero, 0);
            keyboardHook = SetWindowsHookEx(WH_KEYBOARD_LL, Marshal.GetFunctionPointerForDelegate(keyboardProc!), IntPtr.Zero, 0);
        }
        
        public void Uninstall()
        {
            if (mouseHook != IntPtr.Zero) UnhookWindowsHookEx(mouseHook);
            if (keyboardHook != IntPtr.Zero) UnhookWindowsHookEx(keyboardHook);
            mouseHook = IntPtr.Zero;
            keyboardHook = IntPtr.Zero;
        }
        
        private IntPtr MouseCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                MSLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<MSLLHOOKSTRUCT>(lParam);
                int message = wParam.ToInt32();
                
                if (message == WM_MOUSEMOVE)
                    OnMouseMove?.Invoke(hookStruct.ptX, hookStruct.ptY);
                else if (message == WM_LBUTTONDOWN)
                    OnMouseClick?.Invoke(hookStruct.ptX, hookStruct.ptY, true, true);
                else if (message == WM_LBUTTONUP)
                    OnMouseClick?.Invoke(hookStruct.ptX, hookStruct.ptY, false, true);
                else if (message == WM_RBUTTONDOWN)
                    OnMouseClick?.Invoke(hookStruct.ptX, hookStruct.ptY, true, false);
                else if (message == WM_RBUTTONUP)
                    OnMouseClick?.Invoke(hookStruct.ptX, hookStruct.ptY, false, false);
                else if (message == WM_MOUSEWHEEL)
                {
                    int delta = (short)(hookStruct.mouseData >> 16);
                    OnMouseWheel?.Invoke(hookStruct.ptX, hookStruct.ptY, delta);
                }
            }
            return CallNextHookEx(mouseHook, nCode, wParam, lParam);
        }
        
        private IntPtr KeyboardCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                KBDLLHOOKSTRUCT hookStruct = Marshal.PtrToStructure<KBDLLHOOKSTRUCT>(lParam);
                int message = wParam.ToInt32();
                
                if (message == WM_KEYDOWN || message == WM_SYSKEYDOWN)
                    OnKeyDown?.Invoke(hookStruct.vkCode);
                else if (message == WM_KEYUP || message == WM_SYSKEYUP)
                    OnKeyUp?.Invoke(hookStruct.vkCode);
            }
            return CallNextHookEx(keyboardHook, nCode, wParam, lParam);
        }
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, IntPtr lpfn, IntPtr hMod, uint dwThreadId);
        
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);
        
        [DllImport("user32.dll")]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);
        
        private const int WH_MOUSE_LL = 14;
        private const int WH_KEYBOARD_LL = 13;
        private const int WM_MOUSEMOVE = 0x0200;
        private const int WM_LBUTTONDOWN = 0x0201;
        private const int WM_LBUTTONUP = 0x0202;
        private const int WM_RBUTTONDOWN = 0x0204;
        private const int WM_RBUTTONUP = 0x0205;
        private const int WM_MOUSEWHEEL = 0x020A;
        private const int WM_KEYDOWN = 0x0100;
        private const int WM_KEYUP = 0x0101;
        private const int WM_SYSKEYDOWN = 0x0104;
        private const int WM_SYSKEYUP = 0x0105;
        
        [StructLayout(LayoutKind.Sequential)]
        private struct MSLLHOOKSTRUCT
        {
            public int ptX;
            public int ptY;
            public int mouseData;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
        
        [StructLayout(LayoutKind.Sequential)]
        private struct KBDLLHOOKSTRUCT
        {
            public int vkCode;
            public int scanCode;
            public int flags;
            public int time;
            public IntPtr dwExtraInfo;
        }
        
        public void Dispose()
        {
            Uninstall();
        }
    }
}

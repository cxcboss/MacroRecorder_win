using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using MacroRecorder.Models;
using MacroRecorder.Services;

namespace MacroRecorder.ViewModels
{
    public class MainViewModel : IDisposable
    {
        private readonly PythonRecorderService _recorderService;
        private readonly RecordingService _recordingService;
        private readonly ObservableCollection<Recording> _recordings;
        
        private Recording? _currentRecording;
        private bool _isRecording = false;
        private bool _isPlaying = false;
        private int _repeatCount = 1;
        private bool _infiniteLoop = false;
        
        public ICommand StartRecordingCommand { get; }
        public ICommand StopRecordingCommand { get; }
        public ICommand PlayCommand { get; }
        public ICommand StopPlaybackCommand { get; }
        public ICommand DeleteRecordingCommand { get; }
        public ICommand RenameRecordingCommand { get; }
        public ICommand RefreshRecordingsCommand { get; }
        
        public ObservableCollection<Recording> Recordings => _recordings;
        
        public Recording? CurrentRecording
        {
            get => _currentRecording;
            set => SetProperty(ref _currentRecording, value);
        }
        
        public bool IsRecording
        {
            get => _isRecording;
            set => SetProperty(ref _isRecording, value);
        }
        
        public bool IsPlaying
        {
            get => _isPlaying;
            set => SetProperty(ref _isPlaying, value);
        }
        
        public int RepeatCount
        {
            get => _repeatCount;
            set => SetProperty(ref _repeatCount, value);
        }
        
        public bool InfiniteLoop
        {
            get => _infiniteLoop;
            set => SetProperty(ref _infiniteLoop, value);
        }
        
        public string StatusMessage { get; set; } = "准备就绪";
        
        public event Action<string>? OnStatusMessageChanged;
        
        public MainViewModel()
        {
            _recorderService = new PythonRecorderService();
            _recordingService = new RecordingService();
            _recordings = new ObservableCollection<Recording>();
            
            StartRecordingCommand = new RelayCommand<object>(_ => StartRecording());
            StopRecordingCommand = new RelayCommand<object>(_ => StopRecording());
            PlayCommand = new RelayCommand<object>(_ => PlayRecording(), _ => CanPlay());
            StopPlaybackCommand = new RelayCommand<object>(_ => StopPlayback());
            DeleteRecordingCommand = new RelayCommand<Recording>(DeleteRecording);
            RenameRecordingCommand = new RelayCommand<Recording>(RenameRecording);
            RefreshRecordingsCommand = new RelayCommand<object>(_ => LoadRecordings());
            
            _recorderService.OnRecordingStarted += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsRecording = true;
                    UpdateStatus("正在录制...");
                });
            };
            
            _recorderService.OnRecordingStopped += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsRecording = false;
                    UpdateStatus("录制完成");
                });
            };
            
            _recorderService.OnPlaybackStarted += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsPlaying = true;
                    UpdateStatus("正在播放...");
                });
            };
            
            _recorderService.OnPlaybackStopped += () =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    IsPlaying = false;
                    UpdateStatus("播放完成");
                });
            };
            
            LoadRecordings();
        }
        
        private void StartRecording()
        {
            try
            {
                _recorderService.StartRecording();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"启动录制失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private void StopRecording()
        {
            _recorderService.StopRecording();
            
            if (_recorderService.RecordedActions.Count > 0)
            {
                var recording = new Recording
                {
                    Name = $"录制 {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    Actions = _recorderService.RecordedActions.ToList()
                };
                
                _recordingService.SaveRecording(recording);
                LoadRecordings();
                CurrentRecording = recording;
            }
        }
        
        private async void PlayRecording()
        {
            if (CurrentRecording == null) return;
            
            try
            {
                _recorderService.PlayActions(CurrentRecording.Actions);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"播放失败: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
        
        private bool CanPlay()
        {
            return CurrentRecording != null && !IsPlaying;
        }
        
        private void StopPlayback()
        {
            _recorderService.StopPlayback();
        }
        
        private void DeleteRecording(Recording? recording)
        {
            if (recording == null) return;
            
            var result = MessageBox.Show(
                $"确定要删除 \"{recording.Name}\" 吗？", 
                "确认删除", 
                MessageBoxButton.YesNo, 
                MessageBoxImage.Question
            );
            
            if (result == MessageBoxResult.Yes)
            {
                _recordingService.DeleteRecording(recording.Id);
                
                if (CurrentRecording?.Id == recording.Id)
                {
                    CurrentRecording = null;
                }
                
                LoadRecordings();
                UpdateStatus("已删除录制");
            }
        }
        
        private void RenameRecording(Recording? recording)
        {
            if (recording == null) return;
            
            var dialog = new RenameDialog(recording.Name);
            if (dialog.ShowDialog() == true && !string.IsNullOrWhiteSpace(dialog.NewName))
            {
                _recordingService.RenameRecording(recording.Id, dialog.NewName);
                LoadRecordings();
                UpdateStatus("已重命名录制");
            }
        }
        
        private void LoadRecordings()
        {
            _recordings.Clear();
            
            var recordings = _recordingService.GetAllRecordings();
            foreach (var recording in recordings)
            {
                _recordings.Add(recording);
            }
        }
        
        private void UpdateStatus(string message)
        {
            StatusMessage = message;
            OnStatusMessageChanged?.Invoke(message);
        }
        
        private bool SetProperty<T>(ref T field, T value, [System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        public event System.ComponentModel.PropertyChangedEventHandler? PropertyChanged;
        
        private void OnPropertyChanged([System.Runtime.CompilerServices.CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new System.ComponentModel.PropertyChangedEventArgs(propertyName));
        }
        
        public void Dispose()
        {
            _recorderService.Dispose();
        }
    }
    
    public class RelayCommand<T> : ICommand
    {
        private readonly Action<T?> _execute;
        private readonly Func<T?, bool>? _canExecute;
        
        public RelayCommand(Action<T?> execute, Func<T?, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }
        
        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T?)parameter) ?? true;
        
        public void Execute(object? parameter) => _execute((T?)parameter);
        
        public event EventHandler? CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }
    
    public class RenameDialog : Window
    {
        public string NewName { get; private set; } = string.Empty;
        
        public RenameDialog(string currentName)
        {
            Title = "重命名";
            Width = 300;
            Height = 150;
            ResizeMode = ResizeMode.NoResize;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            
            var stackPanel = new StackPanel { Margin = new Thickness(10) };
            
            var label = new TextBlock { Text = "输入新名称:", Margin = new Thickness(0, 0, 0, 5) };
            var textBox = new TextBox { Text = currentName, Margin = new Thickness(0, 0, 0, 10) };
            
            var buttonPanel = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right };
            var okButton = new Button { Content = "确定", Width = 70, Margin = new Thickness(5, 0, 5, 0), IsDefault = true };
            var cancelButton = new Button { Content = "取消", Width = 70, IsCancel = true };
            
            okButton.Click += (s, e) =>
            {
                NewName = textBox.Text;
                DialogResult = true;
            };
            
            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            
            stackPanel.Children.Add(label);
            stackPanel.Children.Add(textBox);
            stackPanel.Children.Add(buttonPanel);
            
            Content = stackPanel;
        }
    }
}

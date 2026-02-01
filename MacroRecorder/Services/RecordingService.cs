using System.Text.Json;
using MacroRecorder.Models;

namespace MacroRecorder.Services
{
    public class RecordingService
    {
        private readonly string _recordingsPath;
        private readonly JsonSerializerOptions _jsonOptions;
        
        public RecordingService()
        {
            _recordingsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Recordings");
            Directory.CreateDirectory(_recordingsPath);
            
            _jsonOptions = new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNameCaseInsensitive = true
            };
        }
        
        public void SaveRecording(Recording recording)
        {
            var fileName = $"{recording.Id}.json";
            var filePath = Path.Combine(_recordingsPath, fileName);
            
            var json = JsonSerializer.Serialize(recording, _jsonOptions);
            File.WriteAllText(filePath, json);
        }
        
        public Recording LoadRecording(Guid id)
        {
            var filePath = Path.Combine(_recordingsPath, $"{id}.json");
            
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("录制文件不存在");
            }
            
            var json = File.ReadAllText(filePath);
            return JsonSerializer.Deserialize<Recording>(json, _jsonOptions) ?? throw new InvalidOperationException("录制文件解析失败");
        }
        
        public List<Recording> GetAllRecordings()
        {
            var recordings = new List<Recording>();
            
            foreach (var file in Directory.GetFiles(_recordingsPath, "*.json"))
            {
                try
                {
                    var json = File.ReadAllText(file);
                    var recording = JsonSerializer.Deserialize<Recording>(json, _jsonOptions);
                    if (recording != null)
                    {
                        recordings.Add(recording);
                    }
                }
                catch
                {
                    continue;
                }
            }
            
            return recordings.OrderByDescending(r => r.CreatedAt).ToList();
        }
        
        public void DeleteRecording(Guid id)
        {
            var filePath = Path.Combine(_recordingsPath, $"{id}.json");
            
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
        
        public void RenameRecording(Guid id, string newName)
        {
            var recording = LoadRecording(id);
            recording.Name = newName;
            SaveRecording(recording);
        }
    }
}

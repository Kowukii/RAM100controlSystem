using Newtonsoft.Json;
using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace RAM100_PRISM_2.ViewModels
{
    public class AutoControlViewModel : BindableBase, INotifyPropertyChanged
    {
        #region 属性和字段

        //private string _currentPresetName = "未选择";
        private PresetFileInfo _selectedPresetFile;
        private bool _isRunning = false;
        private string _currentStage = "等待开始";
        private string _timeRemaining = "00:00:00";
        private double _progressPercentage = 0;
        private string _totalRunTime = "00:00:00";
        private string _completedStages = "0/0";
        private string _statusBackground = "#F8F9F9";
        private string _selectedPresetFileName = "未选择";
        public string SelectedPresetFileName
        {
            get => _selectedPresetFileName;
            set => SetProperty(ref _selectedPresetFileName, value);
        }

        //public string CurrentPresetName
        //{
        //    get => _currentPresetName;
        //    set => SetProperty(ref _currentPresetName, value);
        //}

        public ObservableCollection<PresetFileInfo> PresetFiles { get; } = new ObservableCollection<PresetFileInfo>();

        public PresetFileInfo SelectedPresetFile
        {
            get => _selectedPresetFile;
            set
            {
                if (SetProperty(ref _selectedPresetFile, value))
                {
                    SelectedPresetFileName = value?.Name ?? "未选择";
                    LoadPresetDetails();
                    OnPropertyChanged(nameof(HasPresetSelected));
                    OnPropertyChanged(nameof(CanStart));
                    OnPropertyChanged(nameof(SelectedPresetFileName));
                }
            }
        }

        public bool IsRunning
        {
            get => _isRunning;
            set
            {
                if (SetProperty(ref _isRunning, value))
                {
                    OnPropertyChanged(nameof(IsNotRunning));
                    OnPropertyChanged(nameof(CanStart));
                    OnPropertyChanged(nameof(CanStop));
                    StatusBackground = value ? "#E8F5E8" : "#F8F9F9";
                }
            }
        }

        public bool IsNotRunning => !IsRunning;
        public bool CanStart => !IsRunning && SelectedPresetFile != null;
        public bool CanStop => true; // 停止按钮始终可点击

        public bool HasPresetSelected => SelectedPresetFile != null;

       

        public ObservableCollection<PresetStageInfo> SelectedPresetDetails { get; } = new ObservableCollection<PresetStageInfo>();

        public string CurrentStage
        {
            get => _currentStage;
            set => SetProperty(ref _currentStage, value);
        }

        public string TimeRemaining
        {
            get => _timeRemaining;
            set => SetProperty(ref _timeRemaining, value);
        }

        public double ProgressPercentage
        {
            get => _progressPercentage;
            set => SetProperty(ref _progressPercentage, value);
        }

        public string TotalRunTime
        {
            get => _totalRunTime;
            set => SetProperty(ref _totalRunTime, value);
        }

        public string CompletedStages
        {
            get => _completedStages;
            set => SetProperty(ref _completedStages, value);
        }

        public string StatusBackground
        {
            get => _statusBackground;
            set => SetProperty(ref _statusBackground, value);
        }

        private double _currentFrequency;
        public double CurrentFrequency
        {
            get => _currentFrequency;
            set => SetProperty(ref _currentFrequency, value);
        }

        private double _currentIntensity;
        public double CurrentIntensity
        {
            get => _currentIntensity;
            set => SetProperty(ref _currentIntensity, value);
        }

        private double _currentAcceleration;
        public double CurrentAcceleration
        {
            get => _currentAcceleration;
            set => SetProperty(ref _currentAcceleration, value);
        }

        #endregion

        #region 命令

        public ICommand RefreshPresetsCommand { get; }
        public ICommand StartCommand { get; }
        public ICommand StopCommand { get; }

        #endregion

        #region 构造函数

        public AutoControlViewModel()
        {
            RefreshPresetsCommand = new DelegateCommand(RefreshPresets);
            StartCommand = new DelegateCommand(StartProcess);
            StopCommand = new DelegateCommand(StopProcess);

            // 初始化时加载预设文件
            RefreshPresets();
        }

        #endregion

        #region 方法

        private void RefreshPresets()
        {
            try
            {
                PresetFiles.Clear();

                // 拼接当前目录与presets文件夹的完整路径
                string presetsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "presets");

                // 获取presets文件夹下的所有JSON文件
                var jsonFiles = Directory.GetFiles(presetsFolderPath, "*.json");

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        var preset = JsonConvert.DeserializeObject<DevicePreset>(File.ReadAllText(file));

                        if (preset != null)
                        {
                            PresetFiles.Add(new PresetFileInfo
                            {
                                FilePath = file,
                                Name = preset.Name,
                                TimeSlotCount = preset.TimeSlots?.Count ?? 0,
                                TotalDuration = preset.TimeSlots?.Sum(ts => ts.Duration) ?? 0
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.WriteLine($"加载预设文件失败 {file}: {ex.Message}");
                    }
                }

                if (PresetFiles.Any())
                {
                    SelectedPresetFile = PresetFiles.First();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"刷新预设列表失败: {ex.Message}");
            }
        }

        private void LoadPresetDetails()
        {
            SelectedPresetDetails.Clear();

            if (SelectedPresetFile == null) return;

            try
            {
                var preset = JsonConvert.DeserializeObject<DevicePreset>(File.ReadAllText(SelectedPresetFile.FilePath));

                if (preset?.TimeSlots != null)
                {
                    int index = 1;
                    foreach (var timeSlot in preset.TimeSlots)
                    {
                        SelectedPresetDetails.Add(new PresetStageInfo
                        {
                            Index = index++,
                            Duration = timeSlot.Duration,
                            Frequency = timeSlot.Frequency,
                            Intensity = timeSlot.Intensity,
                            Description = $"频率: {timeSlot.Frequency}Hz, 强度: {timeSlot.Intensity}%"
                        });
                    }

                    //CurrentPresetName = preset.Name;
                    Debug.WriteLine($"应该给名字了");
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"加载预设详情失败: {ex.Message}");
                //CurrentPresetName = "加载失败";
            }
        }

        private System.Timers.Timer _executionTimer;
        private int _totalDurationSeconds;
        private int _elapsedSeconds;

        private void StartProcess()
        {
            if (SelectedPresetFile == null) return;

            // 计算总时长（秒）
            _totalDurationSeconds = SelectedPresetDetails.Sum(d => d.Duration) * 60;
            _elapsedSeconds = 0;

            // 初始化计时器
            _executionTimer = new System.Timers.Timer(1000);
            _executionTimer.Elapsed += (s, e) => 
            {
                Debug.WriteLine($"计时器触发，已执行: {_elapsedSeconds}秒");
                Application.Current.Dispatcher.Invoke(() => UpdateExecutionProgress());
            };
            _executionTimer.Start();

            IsRunning = true;
            CurrentStage = "正在执行阶段 1";
            ProgressPercentage = 0;
            CompletedStages = "0/" + SelectedPresetDetails.Count;
            TotalRunTime = "00:00:00";
            TimeRemaining = TimeSpan.FromSeconds(_totalDurationSeconds).ToString(@"hh\:mm\:ss");

            Debug.WriteLine($"开始执行配方: {SelectedPresetFile.Name}, 总时长: {_totalDurationSeconds}秒");
        }

        private void UpdateExecutionProgress()
        {
            _elapsedSeconds++;
            Debug.WriteLine($"更新进度: {_elapsedSeconds}/{_totalDurationSeconds}秒");
            
            // 更新剩余时间
            var remainingSeconds = _totalDurationSeconds - _elapsedSeconds;
            TimeRemaining = TimeSpan.FromSeconds(remainingSeconds).ToString(@"hh\:mm\:ss");
            
            // 更新进度条
            ProgressPercentage = (_elapsedSeconds * 100.0) / _totalDurationSeconds;
            
            // 更新总运行时间
            TotalRunTime = TimeSpan.FromSeconds(_elapsedSeconds).ToString(@"hh\:mm\:ss");
            
            // 更新已完成阶段
            if (_elapsedSeconds >= _totalDurationSeconds)
            {
                Debug.WriteLine("执行完成");
                CompletedStages = $"{SelectedPresetDetails.Count}/{SelectedPresetDetails.Count}";
                StopProcess();
            }
            else
            {
                var completed = (int)(_elapsedSeconds / (_totalDurationSeconds / (double)SelectedPresetDetails.Count));
                CompletedStages = $"{completed}/{SelectedPresetDetails.Count}";
            }

            // 强制触发属性变更通知
            OnPropertyChanged(nameof(TimeRemaining));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(TotalRunTime));
            OnPropertyChanged(nameof(CompletedStages));
        }

        private void StopProcess()
        {
            if (_executionTimer != null)
            {
                _executionTimer.Stop();
                _executionTimer.Dispose();
                _executionTimer = null;
            }

            IsRunning = false;
            CurrentStage = "已停止";
            TimeRemaining = "00:00:00";
            ProgressPercentage = 0;
            TotalRunTime = "00:00:00";
            _elapsedSeconds = 0;
            _totalDurationSeconds = 0;

            // 触发所有相关属性变更通知
            OnPropertyChanged(nameof(TimeRemaining));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(TotalRunTime));
            OnPropertyChanged(nameof(CompletedStages));

            Debug.WriteLine("停止执行配方");
        }

        #endregion

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion
    }

    #region 辅助类

    public class PresetFileInfo
    {
        public string FilePath { get; set; }
        public string Name { get; set; }
        public int TimeSlotCount { get; set; }
        public int TotalDuration { get; set; } // 总时长（分钟）

        public override string ToString() => Name;
        
        public override bool Equals(object obj)
        {
            return obj is PresetFileInfo other && 
                   FilePath == other.FilePath && 
                   Name == other.Name;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(FilePath, Name);
        }
    }

    public class PresetStageInfo
    {
        public int Index { get; set; }
        public int Duration { get; set; }
        public double Frequency { get; set; }
        public double Intensity { get; set; }
        public string Description { get; set; }
    }

    // 从PresetManager复制的类
    public class DevicePreset
    {
        public string Name { get; set; }
        public ObservableCollection<TimeSlot> TimeSlots { get; set; }
    }

    public class TimeSlot
    {
        public int Duration { get; set; }
        public double Frequency { get; set; }
        public double Intensity { get; set; }
    }

    #endregion
}
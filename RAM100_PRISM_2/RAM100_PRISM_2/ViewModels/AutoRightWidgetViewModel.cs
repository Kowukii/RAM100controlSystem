﻿using Prism.Commands;
using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Management;
using System.Runtime.CompilerServices;
using System.Text;
using System.Timers;
using System.Windows;
using System.Windows.Threading;
using Newtonsoft.Json;
using Prism.Regions;

namespace RAM100_PRISM_2.ViewModels
{
    public class AutoRightWidgetViewModel : BindableBase, INotifyPropertyChanged, IDisposable, INavigationAware
    {
        // Debug日志相关字段
        private StreamWriter debugLogWriter;
        private string debugLogPath;
        private object debugLogLock = new object();
        // 常量字段（从Manual复制）
        const float freqMin = 56;
        const float freqMax = 64;
        const float powerMin = 0;
        const float powerMax = 40;

        // 私有字段（电机控制相关）
        private SerialPort mySerialPort;
        private bool isSystemRunning = false;

        // 预设文件相关字段
        private PresetFileInfo _selectedPresetFile;
        private string _selectedPresetFileName = "未选择";
        private bool _isAutoRunning = false;

        // 自动执行状态字段
        private string _currentStage = "等待开始";
        private string _timeRemaining = "00:00:00";
        private double _progressPercentage = 0;
        private string _totalRunTime = "00:00:00";
        private string _completedStages = "0/0";
        private string _statusBackground = "#F8F9F9";
        private int _currentStageIndex = 0;

        // 强度相关属性
        private double _currentIntensity = 0.01;
        private double _targetIntensity = 0.01;
        private double _intensityStep = 0.01;

        // 频率相关属性
        private double _currentFrequency = 60.0;
        private double _targetFrequency = 60.0;
        private double _frequencyStep = 0.1;

        private string _currentTime = DateTime.Now.ToString("HH:mm:ss");

        // 定时器
        private DispatcherTimer updateTimeTimer;
        private Timer executionTimer;
        private int totalDurationSeconds;
        private int elapsedSeconds;
        private int currentStageDurationSeconds;
        private int currentStageElapsedSeconds;

        #region 属性

        // 预设文件相关属性
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
                    OnPropertyChanged(nameof(CanStartAuto));
                }
            }
        }

        public string SelectedPresetFileName
        {
            get => _selectedPresetFileName;
            set => SetProperty(ref _selectedPresetFileName, value);
        }

        public bool HasPresetSelected => SelectedPresetFile != null;

        public ObservableCollection<PresetStageInfo> SelectedPresetDetails { get; } = new ObservableCollection<PresetStageInfo>();

        // 自动运行状态
        public bool IsAutoRunning
        {
            get => _isAutoRunning;
            set
            {
                if (SetProperty(ref _isAutoRunning, value))
                {
                    OnPropertyChanged(nameof(CanStartAuto));
                    OnPropertyChanged(nameof(CanStopAuto));
                    StatusBackground = value ? "#E8F5E8" : "#F8F9F9";
                }
            }
        }

        public bool CanStartAuto => !IsAutoRunning && HasPresetSelected;
        public bool CanStopAuto => IsAutoRunning;

        // 执行状态属性
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

        public int CurrentStageIndex
        {
            get => _currentStageIndex;
            set => SetProperty(ref _currentStageIndex, value);
        }

        // 强度属性
        public double CurrentIntensity
        {
            get => _currentIntensity;
            set
            {
                _currentIntensity = value;
                System.Diagnostics.Debug.WriteLine($"CurrentIntensity 设置为: {value}");
                OnPropertyChanged();
            }
        }

        public double TargetIntensity
        {
            get => _targetIntensity;
            set
            {
                _targetIntensity = Math.Round(value, 2);
                OnPropertyChanged();
            }
        }

        public double IntensityStep
        {
            get => _intensityStep;
            set
            {
                _intensityStep = value;
                OnPropertyChanged();
            }
        }

        // 频率属性
        public double CurrentFrequency
        {
            get => _currentFrequency;
            set
            {
                _currentFrequency = value;
                System.Diagnostics.Debug.WriteLine($"CurrentFreq 设置为: {value}");
                OnPropertyChanged();
            }
        }

        public double TargetFrequency
        {
            get => _targetFrequency;
            set
            {
                _targetFrequency = Math.Round(value, 1);
                OnPropertyChanged();
            }
        }

        public double FrequencyStep
        {
            get => _frequencyStep;
            set
            {
                _frequencyStep = value;
                OnPropertyChanged();
            }
        }

        public string CurrentTime
        {
            get => _currentTime;
            set
            {
                _currentTime = value;
                OnPropertyChanged();
            }
        }

        // 系统运行状态
        public bool IsSystemRunning
        {
            get => isSystemRunning;
            set
            {
                isSystemRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartSystem));
                OnPropertyChanged(nameof(CanStopSystem));
                OnPropertyChanged(nameof(CanStartAuto));
            }
        }

        public bool CanStartSystem => !IsSystemRunning;
        public bool CanStopSystem => IsSystemRunning;

        #endregion

        #region 命令

        // 启动和停止命令
        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }

        // 预设文件命令
        public DelegateCommand RefreshPresetsCommand { get; }
        public DelegateCommand StartAutoCommand { get; }
        public DelegateCommand StopAutoCommand { get; }

        #endregion

        public AutoRightWidgetViewModel()
        {
            // 初始化Debug日志
            InitializeDebugLog();

            WriteDebugLog("AutoRightWidgetViewModel 已创建！");

            // 初始化命令
            StartCommand = new DelegateCommand(StartSystem, () => !IsSystemRunning).ObservesProperty(() => IsSystemRunning);
            StopCommand = new DelegateCommand(StopSystem, () => IsSystemRunning).ObservesProperty(() => IsSystemRunning);

            RefreshPresetsCommand = new DelegateCommand(RefreshPresets);
            StartAutoCommand = new DelegateCommand(StartAutoProcess, () => CanStartAuto).ObservesProperty(() => CanStartAuto);
            StopAutoCommand = new DelegateCommand(StopAutoProcess, () => CanStopAuto).ObservesProperty(() => CanStopAuto);

            

            

            
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {

            InitializeTimers();


            if (InitMoter())
            {

            }
            else
            {
                MessageBox.Show("InitMoter Fail");
            }

            // 加载预设文件
            RefreshPresets();
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            StopMoter();
            DestroyMoter();
        }

        private void InitializeTimers()
        {
            // 更新时间定时器
            updateTimeTimer = new DispatcherTimer();
            updateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimeTimer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            };
            updateTimeTimer.Start();
        }

        #region 预设文件管理

        private void RefreshPresets()
        {
            try
            {
                PresetFiles.Clear();
                WriteDebugLog("开始刷新预设列表");

                string presetsFolderPath = Path.Combine(Directory.GetCurrentDirectory(), "presets");
                WriteDebugLog($"预设文件夹路径: {presetsFolderPath}");

                if (!Directory.Exists(presetsFolderPath))
                {
                    Directory.CreateDirectory(presetsFolderPath);
                    WriteDebugLog("创建预设文件夹");
                    return;
                }

                var jsonFiles = Directory.GetFiles(presetsFolderPath, "*.json");
                WriteDebugLog($"找到 {jsonFiles.Length} 个预设文件");

                foreach (var file in jsonFiles)
                {
                    try
                    {
                        var preset = JsonConvert.DeserializeObject<DevicePreset>(File.ReadAllText(file));
                        if (preset != null)
                        {
                            var presetInfo = new PresetFileInfo
                            {
                                FilePath = file,
                                Name = preset.Name,
                                TimeSlotCount = preset.TimeSlots?.Count ?? 0,
                                TotalDuration = preset.TimeSlots?.Sum(ts => ts.Duration) ?? 0
                            };
                            PresetFiles.Add(presetInfo);
                            WriteDebugLog($"加载预设成功: {presetInfo.Name}");
                        }
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"加载预设文件失败 {file}: {ex.Message}");
                    }
                }

                if (PresetFiles.Any())
                {
                    SelectedPresetFile = PresetFiles.First();
                    WriteDebugLog($"默认选中第一个预设: {SelectedPresetFile.Name}");
                }
                else
                {
                    WriteDebugLog("未找到任何预设文件");
                    MessageBox.Show("未找到任何预设文件，请检查presets文件夹");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"刷新预设列表失败: {ex.Message}");
                MessageBox.Show($"加载预设文件失败: {ex.Message}");
            }
        }

        private void LoadPresetDetails()
        {
            SelectedPresetDetails.Clear();
            WriteDebugLog("开始加载预设详情");

            if (SelectedPresetFile == null)
            {
                WriteDebugLog("没有选中的预设文件");
                return;
            }

            try
            {
                WriteDebugLog($"加载预设文件: {SelectedPresetFile.FilePath}");
                var preset = JsonConvert.DeserializeObject<DevicePreset>(File.ReadAllText(SelectedPresetFile.FilePath));

                if (preset?.TimeSlots != null)
                {
                    WriteDebugLog($"找到 {preset.TimeSlots.Count} 个时间段");
                    int index = 1;
                    foreach (var timeSlot in preset.TimeSlots)
                    {
                        var stageInfo = new PresetStageInfo
                        {
                            Index = index++,
                            Duration = timeSlot.Duration,
                            Frequency = timeSlot.Frequency,
                            Intensity = timeSlot.Intensity,
                            Description = $"阶段{index-1}: {timeSlot.Duration}分钟, 频率: {timeSlot.Frequency}Hz, 强度: {timeSlot.Intensity}%"
                        };
                        SelectedPresetDetails.Add(stageInfo);
                        WriteDebugLog($"添加阶段: {stageInfo.Description}");
                    }

                    WriteDebugLog($"成功加载 {SelectedPresetDetails.Count} 个阶段详情");
                }
                else
                {
                    WriteDebugLog("预设中没有时间段数据");
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"加载预设详情失败: {ex.Message}");
                MessageBox.Show($"加载预设详情失败: {ex.Message}");
            }
        }

        //private void StartAutoProcess()
        //{
        //    if (SelectedPresetFile == null || SelectedPresetDetails.Count == 0)
        //    {
        //        MessageBox.Show("请先选择有效的预设文件！");
        //        return;
        //    }

        //    if (!IsSystemRunning)
        //    {
        //        MessageBox.Show("请先启动系统！");
        //        return;
        //    }

        //    WriteDebugLog($"开始自动执行配方: {SelectedPresetFile.Name}");

        //    // 计算总时长（秒）
        //    totalDurationSeconds = SelectedPresetDetails.Sum(d => d.Duration) * 60;
        //    elapsedSeconds = 0;
        //    CurrentStageIndex = 0;
        //    currentStageElapsedSeconds = 0;

        //    // 设置第一阶段参数
        //    if (SelectedPresetDetails.Count > 0)
        //    {
        //        StartCurrentStage();
        //    }

        //    // 初始化执行计时器
        //    executionTimer = new Timer(1000);
        //    executionTimer.Elapsed += OnExecutionTimerElapsed;
        //    executionTimer.Start();

        //    IsAutoRunning = true;

        //    WriteDebugLog($"开始执行配方: {SelectedPresetFile.Name}, 总时长: {totalDurationSeconds}秒, 共 {SelectedPresetDetails.Count} 个阶段");
        //}

        private void StartAutoProcess()
        {
            if (SelectedPresetFile == null || SelectedPresetDetails.Count == 0)
            {
                MessageBox.Show("请先选择有效的预设文件");
                return;
            }

            

            try
            {
                // 停止已有的计时器
                if (executionTimer != null)
                {
                    executionTimer.Stop();
                    executionTimer.Dispose();
                    executionTimer = null;
                }

                // 计算总时长（秒）
                totalDurationSeconds = SelectedPresetDetails.Sum(d => d.Duration) * 60;
                elapsedSeconds = 0;
                
                // 初始化阶段索引和计时器
                CurrentStageIndex = 0;
                currentStageElapsedSeconds = 0;
                currentStageDurationSeconds = 0;

                // 启动第一个阶段
                if (SelectedPresetDetails.Count > 0)
                {
                    StartCurrentStage();
                }

                // 创建并启动计时器
                executionTimer = new System.Timers.Timer(1000);
                executionTimer.AutoReset = true; // 确保自动重置
                executionTimer.Elapsed += OnTimerElapsed;
                
                // 先设置UI状态
                ProgressPercentage = 0;
                CompletedStages = $"0/{SelectedPresetDetails.Count}";
                TotalRunTime = "00:00:00";
                TimeRemaining = TimeSpan.FromSeconds(totalDurationSeconds).ToString(@"hh\:mm\:ss");
                
                // 延迟100ms后启动计时器，避免立即触发阶段切换
                System.Threading.Thread.Sleep(100);
                executionTimer.Start();
                
                // 最后设置运行状态
                IsAutoRunning = true;

                WriteDebugLog($"开始执行配方: {SelectedPresetFile.Name}, 总时长: {totalDurationSeconds}秒, 共 {SelectedPresetDetails.Count} 个阶段");
                WriteDebugLog($"当前阶段索引: {CurrentStageIndex}, 阶段总数: {SelectedPresetDetails.Count}");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"启动过程失败: {ex.Message}");
                MessageBox.Show($"启动失败: {ex.Message}");
            }
        }

        // 单独的计时器事件处理方法
        private void OnTimerElapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            Debug.WriteLine($"计时器触发，已执行: {elapsedSeconds}秒");

            // 确保在 UI 线程上更新
            if (Application.Current != null && Application.Current.Dispatcher != null)
            {
                Application.Current.Dispatcher.Invoke(() => UpdateExecutionProgress());
            }
            else
            {
                // 备用方案：直接调用（可能在测试环境下）
                UpdateExecutionProgress();
            }
        }


        private void OnExecutionTimerElapsed(object sender, ElapsedEventArgs e)
        {
            try
            {
                Application.Current.Dispatcher.Invoke(() => UpdateExecutionProgress());
            }
            catch (Exception ex)
            {
                WriteDebugLog($"定时器更新失败: {ex.Message}");
            }
        }

        private void UpdateExecutionProgress()
        {
            try
            {
                elapsedSeconds++;
                currentStageElapsedSeconds++;

                // 检查是否需要切换到下一个阶段
                if (currentStageElapsedSeconds >= currentStageDurationSeconds)
                {
                    CurrentStageIndex++;

                    if (CurrentStageIndex < SelectedPresetDetails.Count)
                    {
                        // 重置当前阶段计时器
                        currentStageElapsedSeconds = 0;
                        currentStageDurationSeconds = SelectedPresetDetails[CurrentStageIndex].Duration * 60;

                        // 立即应用新阶段参数
                        var currentStage = SelectedPresetDetails[CurrentStageIndex];
                        CurrentFrequency = currentStage.Frequency;
                        CurrentIntensity = currentStage.Intensity;

                        // 启动新阶段的电机
                        if (StartMoter())
                        {
                            WriteDebugLog($"切换到阶段 {CurrentStageIndex + 1}: 频率={CurrentFrequency}Hz, 强度={CurrentIntensity * 100}%");
                        }
                        else
                        {
                            WriteDebugLog($"切换阶段失败: 阶段 {CurrentStageIndex + 1}");
                            StopAutoProcess();
                            return;
                        }

                        CurrentStage = $"正在执行阶段 {CurrentStageIndex + 1}/{SelectedPresetDetails.Count}";
                    }
                    else
                    {
                        // 所有阶段完成
                        WriteDebugLog("所有阶段执行完成");
                        StopAutoProcess();
                        return;
                    }
                }

                // 更新UI状态
                UpdateProgressUI();
            }
            catch (Exception ex)
            {
                WriteDebugLog($"更新执行进度时出错: {ex.Message}");
                StopAutoProcess();
            }
        }

        //private void UpdateExecutionProgress()
        //{
        //    try
        //    {
        //        elapsedSeconds++;

        //        // 计算当前阶段
        //        int accumulatedSeconds = 0;
        //        int currentStageIndex = 0;
        //        PresetStageInfo currentStageInfo = null;

        //        for (int i = 0; i < SelectedPresetDetails.Count; i++)
        //        {
        //            var stage = SelectedPresetDetails[i];
        //            int stageSeconds = stage.Duration * 60;
        //            accumulatedSeconds += stageSeconds;

        //            if (elapsedSeconds <= accumulatedSeconds)
        //            {
        //                currentStageIndex = i + 1;
        //                currentStageInfo = stage;
        //                break;
        //            }
        //        }

        //        // 更新当前阶段信息
        //        if (currentStageInfo != null)
        //        {
        //            CurrentStage = $"正在执行阶段 {currentStageIndex}";
        //            CurrentFrequency = currentStageInfo.Frequency;
        //            CurrentIntensity = currentStageInfo.Intensity;
        //            // 更新加速度（如果有）
        //            // CurrentAcceleration = currentStageInfo.Acceleration;
        //        }

        //        // 更新进度显示
        //        var remainingSeconds = Math.Max(0, totalDurationSeconds - elapsedSeconds);
        //        TimeRemaining = TimeSpan.FromSeconds(remainingSeconds).ToString(@"hh\:mm\:ss");

        //        ProgressPercentage = (elapsedSeconds * 100.0) / totalDurationSeconds;
        //        TotalRunTime = TimeSpan.FromSeconds(elapsedSeconds).ToString(@"hh\:mm\:ss");

        //        // 计算已完成阶段数
        //        int completedStages = 0;
        //        int tempSeconds = 0;
        //        foreach (var stage in SelectedPresetDetails)
        //        {
        //            tempSeconds += stage.Duration * 60;
        //            if (elapsedSeconds >= tempSeconds)
        //            {
        //                completedStages++;
        //            }
        //        }
        //        CompletedStages = $"{completedStages}/{SelectedPresetDetails.Count}";

        //        // 检查是否完成
        //        if (elapsedSeconds >= totalDurationSeconds)
        //        {
        //            Debug.WriteLine("执行完成");
        //            CompletedStages = $"{SelectedPresetDetails.Count}/{SelectedPresetDetails.Count}";
        //            StopAutoProcess();
        //        }

        //        // 强制属性变更通知
        //        OnPropertyChanged(nameof(CurrentStage));
        //        OnPropertyChanged(nameof(CurrentFrequency));
        //        OnPropertyChanged(nameof(CurrentIntensity));
        //        OnPropertyChanged(nameof(TimeRemaining));
        //        OnPropertyChanged(nameof(ProgressPercentage));
        //        OnPropertyChanged(nameof(TotalRunTime));
        //        OnPropertyChanged(nameof(CompletedStages));
        //    }
        //    catch (Exception ex)
        //    {
        //        Debug.WriteLine($"更新进度失败: {ex.Message}");
        //    }
        //}
        private void StartCurrentStage()
        {
            if (CurrentStageIndex >= SelectedPresetDetails.Count) return;

            var currentStageInfo = SelectedPresetDetails[CurrentStageIndex];
            currentStageDurationSeconds = currentStageInfo.Duration * 60;
            currentStageElapsedSeconds = 0;

            // 设置电机参数
            CurrentFrequency = currentStageInfo.Frequency;
            CurrentIntensity = currentStageInfo.Intensity;

            // 启动电机
            if (StartMoter())
            {
                WriteDebugLog($"电机启动成功: 频率={CurrentFrequency}Hz, 强度={CurrentIntensity*100}%");
            }
            else
            {
                WriteDebugLog($"电机启动失败: 频率={CurrentFrequency}Hz, 强度={CurrentIntensity*100}%");
                MessageBox.Show("电机启动失败，请检查连接！");
                StopAutoProcess();
                return;
            }

            CurrentStage = $"正在执行阶段 {CurrentStageIndex + 1}/{SelectedPresetDetails.Count}";
            WriteDebugLog($"开始阶段 {CurrentStageIndex + 1}: 频率={CurrentFrequency}Hz, 强度={CurrentIntensity*100}%, 时长={currentStageInfo.Duration}分钟");
        }

        private void UpdateProgressUI()
        {
            try
            {
                // 计算剩余时间（总时间 - 已用时间）
                var remainingSeconds = totalDurationSeconds - elapsedSeconds;
                TimeRemaining = TimeSpan.FromSeconds(remainingSeconds).ToString(@"hh\:mm\:ss");

                // 计算阶段剩余时间
                var stageRemaining = currentStageDurationSeconds - currentStageElapsedSeconds;
                var stageRemainingTime = TimeSpan.FromSeconds(stageRemaining).ToString(@"mm\:ss");

                // 更新进度条
                ProgressPercentage = (elapsedSeconds * 100.0) / totalDurationSeconds;

                // 更新总运行时间
                TotalRunTime = TimeSpan.FromSeconds(elapsedSeconds).ToString(@"hh\:mm\:ss");

                // 更新已完成阶段
                CompletedStages = $"{CurrentStageIndex + 1}/{SelectedPresetDetails.Count}";
                CurrentStage = $"阶段 {CurrentStageIndex + 1} (剩余: {stageRemainingTime})";

                // 触发UI更新
                OnPropertyChanged(nameof(TimeRemaining));
                OnPropertyChanged(nameof(ProgressPercentage));
                OnPropertyChanged(nameof(TotalRunTime));
                OnPropertyChanged(nameof(CompletedStages));
                OnPropertyChanged(nameof(CurrentStage));
                OnPropertyChanged(nameof(CurrentStageIndex));
                OnPropertyChanged(nameof(CurrentFrequency));
                OnPropertyChanged(nameof(CurrentIntensity));
            }
            catch (Exception ex)
            {
                WriteDebugLog($"更新UI状态时出错: {ex.Message}");
            }
        }

        private void StopAutoProcess()
        {
            WriteDebugLog("停止自动执行");

            if (executionTimer != null)
            {
                executionTimer.Stop();
                executionTimer.Dispose();
                executionTimer = null;
            }

            IsAutoRunning = false;
            CurrentStage = "已停止";
            TimeRemaining = "00:00:00";
            ProgressPercentage = 0;
            TotalRunTime = "00:00:00";
            CompletedStages = "0/0";
            CurrentStageIndex = 0;
            elapsedSeconds = 0;
            totalDurationSeconds = 0;

            // 停止电机
            StopSystem();

            // 触发所有相关属性变更通知
            OnPropertyChanged(nameof(TimeRemaining));
            OnPropertyChanged(nameof(ProgressPercentage));
            OnPropertyChanged(nameof(TotalRunTime));
            OnPropertyChanged(nameof(CompletedStages));
            OnPropertyChanged(nameof(CurrentStageIndex));

            WriteDebugLog("自动执行已停止");
        }

        #endregion

        #region 电机控制方法（从Manual复制）

        private bool InitMoter()
        {
            string comPort = "COM8";

            WriteDebugLog($"Serial open {comPort}");

            if (!string.IsNullOrEmpty(comPort))
            {
                try
                {
                    mySerialPort = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);
                    mySerialPort.DataReceived += SerialDataReceived;
                    mySerialPort.Open();
                  
                    return true;
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"串口初始化失败: {ex}");
                    return false;
                }
            }
            else
            {
                WriteDebugLog("未找到匹配的端口");
                MessageBox.Show("未找到匹配的端口，请连接电机！");
                return false;
            }
        }

        private void DestroyMoter()
        {
            mySerialPort?.Close();
        }

        private bool StartMoter()
        {
            if (mySerialPort == null || !mySerialPort.IsOpen)
            {
                WriteDebugLog("串口未打开，尝试重新初始化");
                if (!InitMoter())
                {
                    MessageBox.Show("串口初始化失败！");
                    return false;
                }
            }

            ushort nFreq, nAmp;

            double powerValue = CurrentIntensity * 100; // 转换为百分比
            if (powerValue < powerMin || powerValue > powerMax)
            {
                WriteDebugLog($"强度值超出范围: {powerValue}");
                MessageBox.Show("强度错误！");
                return false;
            }

            nAmp = (ushort)(powerValue * 32.767);

            double freqValue = CurrentFrequency;
            if (freqValue < freqMin || freqValue > freqMax)
            {
                WriteDebugLog($"频率值超出范围: {freqValue}");
                MessageBox.Show("频率错误！");
                return false;
            }

            nFreq = (ushort)(freqValue * 200);

            try
            {
                string cmd1 = "s r0x98 8449";
                WriteDebugLog($"发送电机命令: {cmd1}");
                mySerialPort.WriteLine(cmd1);

                string cmd2 = $"s r0x99 {nFreq}";
                WriteDebugLog($"发送电机命令: {cmd2}");
                mySerialPort.WriteLine(cmd2);

                string cmd3 = $"s r0x9a {nAmp}";
                WriteDebugLog($"发送电机命令: {cmd3}");
                mySerialPort.WriteLine(cmd3);

                string cmd4 = "s r0x24 4";
                WriteDebugLog($"发送电机命令: {cmd4}");
                mySerialPort.WriteLine(cmd4);

                WriteDebugLog("电机启动命令发送成功");
                return true;
            }
            catch (Exception ex)
            {
                WriteDebugLog($"启动电机失败: {ex.Message}");
                // 尝试重新初始化串口
                try
                {
                    mySerialPort?.Close();
                    InitMoter();
                }
                catch (Exception ex2)
                {
                    WriteDebugLog($"重新初始化串口失败: {ex2.Message}");
                }
                return false;
            }
        }

        private void StopMoter()
        {
            if (mySerialPort != null && mySerialPort.IsOpen)
            {
                try
                {
                    string cmd = "s r0x24 0";
                    WriteDebugLog($"发送停止电机命令: {cmd}");
                    mySerialPort.WriteLine(cmd);
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"停止电机失败: {ex.Message}");
                }
            }
        }

        private static string GetComPortFromDisplayName(string displayName)
        {
            string[] sPorts = SerialPort.GetPortNames();

            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                string caption = obj["Caption"].ToString();

                if (caption.Contains(displayName))
                {
                    return caption.Substring(caption.IndexOf("(COM")).Replace("(", string.Empty).Replace(")", string.Empty);
                }
            }

            return null;
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string data = sp.ReadExisting();
            WriteDebugLog($"串口接收: {data}");
        }

        #endregion

        #region 系统控制方法

        private void StartSystem()
        {
            IsSystemRunning = true;
            try
            {
                WriteDebugLog("系统启动");

                // 首先设置系统运行状态为true
                
                
                // 然后尝试启动电机
                if (StartMoter())
                {
                    WriteDebugLog("系统启动成功");
                }
                else
                {
                    WriteDebugLog("启动电机失败，但系统状态已设置为运行");
                    // 即使电机启动失败，系统状态仍然保持为运行
                    // 这样用户可以重试启动电机而不需要重新启动系统
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"启动系统时发生错误: {ex.Message}");
                MessageBox.Show($"启动失败: {ex.Message}");
                IsSystemRunning = false;
            }
        }

        private void StopSystem()
        {
            WriteDebugLog("系统停止");
            StopMoter();
            IsSystemRunning = false;

            // 如果自动执行正在运行，也停止自动执行
            if (IsAutoRunning)
            {
                StopAutoProcess();
            }
        }

        #endregion

        #region 设置参数方法（供自动控制使用）

        public void SetFrequency(double frequency)
        {
            CurrentFrequency = frequency;
            WriteDebugLog($"设置频率: {frequency} Hz");
        }

        public void SetIntensity(double intensity)
        {
            CurrentIntensity = intensity;
            WriteDebugLog($"设置强度: {intensity}");
        }

        public void ApplyParameters()
        {
            if (IsSystemRunning)
            {
                StartMoter();
            }
        }

        #endregion

        #region INotifyPropertyChanged 实现

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        #endregion

        #region IDisposable 实现

        private bool disposed = false;

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    updateTimeTimer?.Stop();
                    executionTimer?.Stop();
                    executionTimer?.Dispose();
                    StopAutoProcess();
                    StopMoter();
                    DestroyMoter();
                    mySerialPort?.Close();

                    // 关闭Debug日志写入器
                    lock (debugLogLock)
                    {
                        debugLogWriter?.Close();
                        debugLogWriter?.Dispose();
                        debugLogWriter = null;
                    }
                }

                disposed = true;
            }
        }

        ~AutoRightWidgetViewModel()
        {
            Dispose(false);
        }

        #endregion

        #region Debug日志方法

        private void InitializeDebugLog()
        {
            try
            {
                // 创建日志目录
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLogs2");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                    WriteDebugLog($"创建日志目录: {logDirectory}");
                }

                // 设置日志文件路径
                debugLogPath = Path.Combine(logDirectory, $"DebugLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                
                // 确保日志文件可以写入
                debugLogWriter = new StreamWriter(debugLogPath, true, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                // 写入初始日志
                WriteDebugLog("=== 应用程序启动 ===");
                WriteDebugLog($"日志文件路径: {debugLogPath}");
                WriteDebugLog($"当前工作目录: {Directory.GetCurrentDirectory()}");
                
                // 检查日志文件是否可访问
                if (!File.Exists(debugLogPath))
                {
                    System.Diagnostics.Debug.WriteLine($"警告: 日志文件创建失败 {debugLogPath}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化Debug日志失败: {ex.Message}");
                // 尝试创建备用日志路径
                try
                {
                    string tempLogPath = Path.Combine(Path.GetTempPath(), $"RAM100_DebugLog2_{DateTime.Now:yyyyMMdd_HHmmss}.txt");
                    debugLogWriter = new StreamWriter(tempLogPath, true, Encoding.UTF8)
                    {
                        AutoFlush = true
                    };
                    debugLogPath = tempLogPath;
                    WriteDebugLog($"使用临时日志路径: {tempLogPath}");
                }
                catch
                {
                    // 最终回退到Debug输出
                    System.Diagnostics.Debug.WriteLine("无法创建任何日志文件，仅使用Debug输出");
                }
            }
        }

        private void WriteDebugLog(string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";

            // 始终输出到Debug
            System.Diagnostics.Debug.WriteLine(logMessage);

            lock (debugLogLock)
            {
                try
                {
                    // 检查写入器是否可用
                    if (debugLogWriter == null || !debugLogWriter.BaseStream.CanWrite)
                    {
                        // 尝试重新创建写入器
                        try
                        {
                            debugLogWriter = new StreamWriter(debugLogPath, true, Encoding.UTF8)
                            {
                                AutoFlush = true
                            };
                            debugLogWriter.WriteLine($"[恢复日志写入] {logMessage}");
                        }
                        catch (Exception ex)
                        {
                            System.Diagnostics.Debug.WriteLine($"无法重新创建日志写入器: {ex.Message}");
                            return;
                        }
                    }

                    // 写入日志
                    debugLogWriter.WriteLine(logMessage);
                    
                    // 立即刷新确保写入
                    debugLogWriter.Flush();
                    
                    // 验证日志文件存在
                    if (!File.Exists(debugLogPath))
                    {
                        System.Diagnostics.Debug.WriteLine($"警告: 日志写入后文件不存在 {debugLogPath}");
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"写入Debug日志失败: {ex.Message}");
                    
                    // 尝试写入临时文件
                    try
                    {
                        string tempLog = Path.Combine(Path.GetTempPath(), "RAM100_FallbackLog.txt");
                        File.AppendAllText(tempLog, $"{logMessage}", Encoding.UTF8);
                        System.Diagnostics.Debug.WriteLine($"日志已写入临时文件: {tempLog}");
                    }
                    catch
                    {
                        System.Diagnostics.Debug.WriteLine("无法写入任何日志文件");
                    }
                }
            }
        }

        #endregion
    }

    

    
}
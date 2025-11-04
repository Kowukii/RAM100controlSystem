using Prism.Commands;
using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Prism.Mvvm;
using Prism.Regions;
using RAM100_PRISM_2.Models;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.IO.Ports;

using System.Management;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using myPAC = PACImport.PACWrap;

namespace RAM100_PRISM_2.ViewModels
{
    class ManulRightWidgetViewModel : BindableBase, INotifyPropertyChanged, IDisposable, INavigationAware
    {
        // 常量字段
        const float freqMin = 56;
        const float freqMax = 64;
        const float powerMin = 0;
        const float powerMax = 40;
        const bool renderChat = true;
        const bool recordData = true;

        // 私有字段
        private TcpListener tcpListener;
        private TcpClient clientSocket;
        private NetworkStream networkStream;
        private Int64 captureIndex = 0;
        private double currentValue = 0;
        private byte[] recvDataBuf = new byte[1032];
        private List<KeyValuePair<DateTime, double>> dataList = new List<KeyValuePair<DateTime, double>>();
        private List<string> logBuffer = new List<string>();
        private SerialPort mySerialPort;// 定义了一个 SerialPort 类型的变量，用于实现与外部设备的串行通信
        private string logName;
        private bool plotEnable = false;
        private bool isAcquisitionRunning = false;

        // 定时器
        private System.Windows.Threading.DispatcherTimer updateTimeTimer;
        private System.Windows.Threading.DispatcherTimer updateValueTimer;


        // 强度相关属性
        private double _currentIntensity = 0.05;
        private double _targetIntensity = 0.05;
        private double _intensityStep = 0.01; // 强度补偿步长


        // 频率相关属性
        private double _currentFrequency = 60;
        private double _targetFrequency = 60;
        private double _frequencyStep = 0.1; // 频率补偿步长

        private string _currentTime = DateTime.Now.ToString("HH:mm:ss");


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

        public double CurrentAccelerationValue
        {
            get => currentValue;
            set
            {
                currentValue = value;
                OnPropertyChanged();
            }
        }

        // 在 IsSystemRunning 属性的 setter 中通知属性更改
        public bool IsSystemRunning
        {
            get => isAcquisitionRunning;
            set
            {
                isAcquisitionRunning = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(CanStartSystem));
                OnPropertyChanged(nameof(CanStopSystem));
            }
        }

        // 命令
        public ICommand ApplyIntensityCommand { get; }
        public ICommand ApplyFrequencyCommand { get; }
        public ICommand IncreaseIntensityCommand { get; }
        public ICommand DecreaseIntensityCommand { get; }
        public ICommand IncreaseFrequencyCommand { get; }
        public ICommand DecreaseFrequencyCommand { get; }

        // 强度置零命令
        public ICommand ZeroIntensityCommand { get; }

        // 启动和停止命令
        public DelegateCommand StartCommand { get; }
        public DelegateCommand StopCommand { get; }

        // 在 ViewModel 中添加这两个属性
        public bool CanStartSystem => !IsSystemRunning;
        public bool CanStopSystem => IsSystemRunning;

        public bool flag;

        public ManulRightWidgetViewModel()
        {
            System.Diagnostics.Debug.WriteLine("ManulRightWidgetViewModel 已创建！");

            // 初始化默认值
            CurrentIntensity = 0.02;
            TargetIntensity = 0.02;
            IntensityStep = 0.01;

            CurrentFrequency = 62.0;
            TargetFrequency = 62;
            FrequencyStep = 0.1;

            // 初始化命令
            ApplyIntensityCommand = new DelegateCommand(ApplyIntensity);
            ApplyFrequencyCommand = new DelegateCommand(ApplyFrequency);
            IncreaseIntensityCommand = new DelegateCommand(IncreaseIntensity);
            DecreaseIntensityCommand = new DelegateCommand(DecreaseIntensity);
            IncreaseFrequencyCommand = new DelegateCommand(IncreaseFrequency);
            DecreaseFrequencyCommand = new DelegateCommand(DecreaseFrequency);

            ZeroIntensityCommand = new DelegateCommand(ZeroIntensity);
            StartCommand = new DelegateCommand(StartSystem, () => !IsSystemRunning).ObservesProperty(() => IsSystemRunning);
            StopCommand = new DelegateCommand(StopSystem, () => IsSystemRunning).ObservesProperty(() => IsSystemRunning);


            // 初始化设备和数据采集
            //Task.Run(() => InitializeSystem());

            //MessageBox.Show("进入InitializeSystem");
            flag = false;

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
            updateTimeTimer = new System.Windows.Threading.DispatcherTimer();
            updateTimeTimer.Interval = TimeSpan.FromSeconds(1);
            updateTimeTimer.Tick += (s, e) =>
            {
                CurrentTime = DateTime.Now.ToString("HH:mm:ss");
            };
            updateTimeTimer.Start();

            // 更新数值定时器
            updateValueTimer = new System.Windows.Threading.DispatcherTimer();
            updateValueTimer.Interval = TimeSpan.FromMilliseconds(500);
            updateValueTimer.Tick += (s, e) =>
            {
                OnPropertyChanged(nameof(CurrentAccelerationValue));
            };
            updateValueTimer.Start();
        }

        private Task InitializeSystem()
        {
            // 初始化电机
            if (!InitMoter())
            {
                MessageBox.Show("电机初始化失败！");
                return Task.CompletedTask;
            }
            MessageBox.Show("InitMoter结束");
            // 准备数据采集
           
            return Task.CompletedTask;
        }

        // 启动系统方法
        private void StartSystem()
        {
            try
            {
                System.Diagnostics.Debug.WriteLine("系统启动");



                if (StartMoter())
                {
                    IsSystemRunning = true;
                    plotEnable = true;
                    captureIndex = 1;

                    logName = "log/" + DateTime.Now.ToString("yyyyMMdd-HHmmss") + ".log";

                    // 检查目录是否存在
                    if (!System.IO.Directory.Exists("log/"))
                    {
                        System.IO.Directory.CreateDirectory("log/");
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"启动系统时发生错误: {ex.Message}");
                MessageBox.Show($"启动失败: {ex.Message}");
                IsSystemRunning = false;
            }
        }

        // 停止系统方法
        private void StopSystem()
        {
            System.Diagnostics.Debug.WriteLine("系统停止");
            StopMoter();
            IsSystemRunning = false;
            //StopAcquist();
            IsSystemRunning = false;
            plotEnable = false;
            captureIndex = 0;
        }

        #region 电机控制方法

        private bool InitMoter()
        {
            //string comPort = GetComPortFromDisplayName("COM8");
            string comPort = "COM8";

            System.Diagnostics.Debug.WriteLine($"Serial open {comPort}");

            if (!string.IsNullOrEmpty(comPort))
            {
                try
                {
                    mySerialPort = new SerialPort(comPort, 9600, Parity.None, 8, StopBits.One);
                    mySerialPort.DataReceived += SerialDataReceived;
                    mySerialPort.Open();
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine(ex.ToString());
                    return false;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("No matching port found");
                MessageBox.Show("No matching port found 请连接电机！");
                return false;
            }
            return true;
        }

        private void DestroyMoter()
        {
            mySerialPort?.Close();
            IsSystemRunning = false;
        }


        private bool StartMoter()
        {
            ushort nFreq, nAmp;

            double powerValue = CurrentIntensity * 100; // 转换为百分比
            if (powerValue < powerMin || powerValue > powerMax)
            {
                MessageBox.Show("强度错误！");
                return false;
            }

            nAmp = (ushort)(powerValue * 32.767);

            double freqValue = CurrentFrequency;
            if (freqValue < freqMin || freqValue > freqMax)
            {
                MessageBox.Show("频率错误！");
                return false;
            }

            nFreq = (ushort)(freqValue * 200);

            string cmd1 = "s r0x98 8449";
            System.Diagnostics.Debug.WriteLine(cmd1);
            mySerialPort.WriteLine(cmd1);

            string cmd2 = $"s r0x99 {nFreq}";
            System.Diagnostics.Debug.WriteLine(cmd2);
            mySerialPort.WriteLine(cmd2);

            string cmd3 = $"s r0x9a {nAmp}";
            System.Diagnostics.Debug.WriteLine(cmd3);
            mySerialPort.WriteLine(cmd3);

            string cmd4 = "s r0x24 4";
            System.Diagnostics.Debug.WriteLine(cmd4);
            mySerialPort.WriteLine(cmd4);

            return true;
        }

        private bool UpdateMoter()
        {
            if (!IsSystemRunning) {
                return false;
            }
            ushort nFreq, nAmp;

            double powerValue = CurrentIntensity * 100; // 转换为百分比
            if (powerValue < powerMin || powerValue > powerMax)
            {
                MessageBox.Show("强度错误！");
                return false;
            }

            nAmp = (ushort)(powerValue * 32.767);

            double freqValue = CurrentFrequency;
            if (freqValue < freqMin || freqValue > freqMax)
            {
                MessageBox.Show("频率错误！");
                return false;
            }

            nFreq = (ushort)(freqValue * 200);

            //string cmd1 = "s r0x98 8449";
            //System.Diagnostics.Debug.WriteLine(cmd1);
            //mySerialPort.WriteLine(cmd1);

            string cmd2 = $"s r0x99 {nFreq}";
            System.Diagnostics.Debug.WriteLine(cmd2);
            mySerialPort.WriteLine(cmd2);

            string cmd3 = $"s r0x9a {nAmp}";
            System.Diagnostics.Debug.WriteLine(cmd3);
            mySerialPort.WriteLine(cmd3);

            string cmd4 = "s r0x24 4";
            System.Diagnostics.Debug.WriteLine(cmd4);
            mySerialPort.WriteLine(cmd4);

            return true;
        }

        private void StopMoter()
        {
            if (mySerialPort != null && mySerialPort.IsOpen)
            {
                string cmd = "s r0x24 0";
                System.Diagnostics.Debug.WriteLine(cmd);
                mySerialPort.WriteLine(cmd);
            }
        }

        private static string GetComPortFromDisplayName(string displayName)
        {
            string[] sPorts = SerialPort.GetPortNames();

            // Create WMI query to get info about COM ports
            var searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity WHERE Caption like '%(COM%'");

            foreach (ManagementObject obj in searcher.Get())
            {
                string caption = obj["Caption"].ToString();

                // If the caption contains the display name, return the COM port
                if (caption.Contains(displayName))
                {
                    return caption.Substring(caption.IndexOf("(COM")).Replace("(", string.Empty).Replace(")", string.Empty);
                }
            }

            // Return null if no matching port found
            return null;
        }

        private void SerialDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            SerialPort sp = (SerialPort)sender;
            string data = sp.ReadExisting();
            System.Diagnostics.Debug.WriteLine(data);
        }
        #endregion

      
        // 强度补偿方法
        private void IncreaseIntensity()
        {
            TargetIntensity += IntensityStep;
            System.Diagnostics.Debug.WriteLine($"增加强度: {TargetIntensity} (步长: {IntensityStep})");
        }

        private void DecreaseIntensity()
        {
            TargetIntensity = Math.Max(0, TargetIntensity - IntensityStep); // 确保不小于0
            System.Diagnostics.Debug.WriteLine($"减少强度: {TargetIntensity} (步长: {IntensityStep})");
        }

        private void ApplyIntensity()
        {
            CurrentIntensity = TargetIntensity;
            System.Diagnostics.Debug.WriteLine($"应用强度: {CurrentIntensity}");
            UpdateMoter();
        }

        // 频率补偿方法
        private void IncreaseFrequency()
        {
            TargetFrequency += FrequencyStep;
            System.Diagnostics.Debug.WriteLine($"增加频率: {TargetFrequency} (步长: {FrequencyStep})");
        }

        private void DecreaseFrequency()
        {
            TargetFrequency = Math.Max(0, TargetFrequency - FrequencyStep); // 确保不小于0
            System.Diagnostics.Debug.WriteLine($"减少频率: {TargetFrequency} (步长: {FrequencyStep})");
        }

        private void ApplyFrequency()
        {
            CurrentFrequency = TargetFrequency;
            System.Diagnostics.Debug.WriteLine($"应用频率: {CurrentFrequency} Hz");
            UpdateMoter();
        }

        // 强度置零方法
        private void ZeroIntensity()
        {
            TargetIntensity = 0;
            CurrentIntensity = TargetIntensity;
            System.Diagnostics.Debug.WriteLine("强度已置零");
            UpdateMoter();
            StopMoter();
        }



        // INotifyPropertyChanged 实现


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
                    updateValueTimer?.Stop();
                    StopMoter();
                    DestroyMoter();

                    networkStream?.Close();
                    clientSocket?.Close();
                    tcpListener?.Stop();
                    mySerialPort?.Close();
                }

                disposed = true;
            }
        }

        ~ManulRightWidgetViewModel()
        {
            Dispose(false);
        }
        #endregion
    }
}

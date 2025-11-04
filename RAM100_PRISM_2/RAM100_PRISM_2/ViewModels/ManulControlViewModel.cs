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
using ScottPlot;
using myPAC = PACImport.PACWrap;


namespace RAM100_PRISM_2.ViewModels
{
    internal class ManulControlViewModel : BindableBase, INotifyPropertyChanged, IDisposable
    {
        // 常量字段
        const short moterType = 3;
        const float freqMin = 50;
        const float freqMax = 70;
        const float powerMin = 0;
        const float powerMax = 1000;
        const int COIL_TORQUE_TEST = 8;
        const int RW_REG_TQ_TEST_CUR = 42;
        const int RW_REG_TQ_TEST_DELAY = 43;
        const byte driverAddr = 1;
        const bool renderChat = true;
        const bool recordData = true;

        // 私有字段
        private TcpListener tcpListener;
        private TcpClient clientSocket;
        private NetworkStream networkStream;
        private static object lockObject = new object();
        private Int64 captureIndex = 0;
        private double currentValue = 0;
        private byte[] recvDataBuf = new byte[1032];
        private bool freqModified = false;
        private bool powerModified = false;
        private List<KeyValuePair<DateTime, double>> dataList = new List<KeyValuePair<DateTime, double>>();
        private List<string> logBuffer = new List<string>();
        private SerialPort mySerialPort;// 定义了一个 SerialPort 类型的变量，用于实现与外部设备的串行通信
        private DateTime lastCaptureTime;
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

        // 构造函数
        public ManulControlViewModel()
        {
            System.Diagnostics.Debug.WriteLine("ManulControlViewModel 已创建！");

            // 初始化默认值
            CurrentIntensity = 0.02;
            TargetIntensity = 0.02;
            IntensityStep = 0.01;

            CurrentFrequency = 60.0;
            TargetFrequency = 60;
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
            // 初始化定时器
            InitializeTimers();

            // 初始化设备和数据采集
            //Task.Run(() => InitializeSystem());
            if (InitMoter())
            {
                MessageBox.Show("InitMoter Success");
            }
            else
            {
                MessageBox.Show("InitMoter Fail");
            }
            //MessageBox.Show("进入InitializeSystem");

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
            PrepareAcqust();
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

        #region 数据采集方法
        private void PrepareAcqust()
        {
            // Create a TCP listener
            tcpListener = new TcpListener(IPAddress.Any, 8234);
            System.Diagnostics.Debug.WriteLine("Wait for connection ...");
            try
            {
                tcpListener.Start();
            }
            catch
            {
                MessageBox.Show("连接采集卡失败！");
            }

            Thread.Sleep(30);

            // 异步接受客户端连接
            tcpListener.BeginAcceptTcpClient(new AsyncCallback(AcceptTcpClientCallback), null);
        }

        private void AcceptTcpClientCallback(IAsyncResult ar)
        {
            try
            {
                clientSocket = tcpListener.EndAcceptTcpClient(ar);
                networkStream = clientSocket.GetStream();

                // 配置采集卡
                if (ConfigAcquist())
                {
                    // 开始采集
                    StartAcquist();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"AcceptTcpClientCallback error: {ex.Message}");
            }
        }

        private bool ConfigAcquist()
        {
            // 采样配置命令
            byte[] paramSetBuf = {
                0x55, 0xEE,                 // 固定前缀
                0x80,                       // 写入指令
                0x0E,                       // 命令长度 14
                0x10, 0x01,                 // 固定保留字
                0x81,                       // ADC设置指令字
                0x00, 0x00, 0x00, 0x00,     // 采样范围
                0x01,                       // 参考选择
                0x01,                       // 采样深度
                0x00, 0x20, 0x00,           // 采样频率
                0x00,                       // 采样通道
            };

            // 结尾追加校验和
            paramSetBuf = AppendChecksum(paramSetBuf);

            // 配置成功返回数据
            byte[] paramSetSuccessBuf = { 0x55, 0xEE, 0x00, 0x05, 0x10, 0x01, 0x81, 0x22, 0xFC };

            // 读取配置结果缓冲区
            byte[] paramSetRecvBuf = new byte[9];

            ClearNetworkBuffer();

            // Set ADC param.
            System.Diagnostics.Debug.WriteLine("配置ADC参数 " + ConvertByteArrayToHexStr(paramSetBuf));
            networkStream.Write(paramSetBuf, 0, paramSetBuf.Length);

            networkStream.Read(paramSetRecvBuf, 0, paramSetRecvBuf.Length);
            System.Diagnostics.Debug.WriteLine("配置ADC返回 " + ConvertByteArrayToHexStr(paramSetRecvBuf));

            if (CompareTwoHexStr(paramSetSuccessBuf, paramSetRecvBuf))
            {
                System.Diagnostics.Debug.WriteLine("ADC配置成功");
                return true;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("ADC配置失败");
                return false;
            }
        }

        private void StartAcquist()
        {
            // 开始采样命令
            byte[] samplingStartBuf = { 0x55, 0xEE, 0x00, 0x05, 0x10, 0x01, 0x24, 0x10, 0x8D };

            // 发送指令
            System.Diagnostics.Debug.WriteLine("开始采样 " + ConvertByteArrayToHexStr(samplingStartBuf));
            networkStream.Write(samplingStartBuf, 0, samplingStartBuf.Length);

            // 开始异步读取数据
            networkStream.BeginRead(recvDataBuf, 0, recvDataBuf.Length, new AsyncCallback(ReceiveCallback), clientSocket);
        }

        private void StopAcquist()
        {
            // 停止采样指令
            byte[] samplingStopBuf = { 0x55, 0xEE, 0x80, 0x09, 0x10, 0x01, 0x24, 0x33, 0x00, 0x00, 0x00, 0x00, 0x34 };

            // 发送指令
            System.Diagnostics.Debug.WriteLine("停止采样 " + ConvertByteArrayToHexStr(samplingStopBuf));
            networkStream.Write(samplingStopBuf, 0, samplingStopBuf.Length);
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            try
            {
                int bytesRead = networkStream.EndRead(result);
                if (bytesRead > 0)
                {
                    ProcessReceivedData();

                    // 继续读取下一个数据包
                    networkStream.BeginRead(recvDataBuf, 0, recvDataBuf.Length, new AsyncCallback(ReceiveCallback), clientSocket);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"网络读取错误: {ex.Message}");
            }
        }

        private void ProcessReceivedData()
        {
            if (recvDataBuf[0] == 0x55 && recvDataBuf[1] == 0xEE)
            {
                DateTime now = DateTime.Now;
                lastCaptureTime = now;
                Int16 dataLength = (Int16)(recvDataBuf[2] << 8 | recvDataBuf[3] - 4);
                int channels = 4;
                int bitwidth = 2;

                Int32 max1 = 0;

                for (int i = 0; i < dataLength / channels / bitwidth; i++)
                {
                    Int32 data1 = BitConverter.ToInt16(recvDataBuf, 7 + i * channels * bitwidth);

                    // 获取这批数据绝对值最大值
                    if (max1 < Math.Abs(data1))
                    {
                        max1 = Math.Abs(data1);
                    }
                }

                // 如果需要采集数据
                if (plotEnable)
                {
                    // 计算对应加速度值
                    double number = max1 * 4.0 / 0x8000 / 0.344 / 0.05;

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 更新当前值
                        CurrentAccelerationValue = number;

                        // 若需要渲染波形图
                        if (renderChat)
                        {
                            // 追加最新值
                            dataList.Add(new KeyValuePair<DateTime, double>(now, number));
                        }
                    });

                    // 若需要记录数据 
                    if (recordData)
                    {
                        // 获取当前的毫秒时间戳
                        string timeString = now.ToString("yyyy-MM-dd HH:mm:ss.fff");

                        // 定义要写入的文本格式
                        string text = $"{timeString},{captureIndex:0000000000},{CurrentFrequency},{CurrentIntensity * 100},{number:F10}";

                        lock (logBuffer)
                        {
                            logBuffer.Add(text);

                            if (captureIndex % 100 == 0)
                            {
                                System.IO.File.AppendAllLines(logName, logBuffer);
                                logBuffer.Clear();
                            }
                        }
                    }

                    captureIndex += 1;
                }
            }
        }

        private void ClearNetworkBuffer()
        {
            int availableBytes;
            byte[] releaseBuf = new byte[2048];

            // Process data within the buffer of the current socket.
            availableBytes = clientSocket.Available;
            while (availableBytes > 0)
            {
                if (releaseBuf.Length > availableBytes)
                {
                    networkStream.Read(releaseBuf, 0, availableBytes);
                }
                else
                {
                    networkStream.Read(releaseBuf, 0, releaseBuf.Length);
                }
                availableBytes = clientSocket.Available;
            }

            // Processing pulse signals
            networkStream.Read(releaseBuf, 0, releaseBuf.Length);
        }

        private byte[] AppendChecksum(byte[] data)
        {
            int checksum = 0;
            foreach (byte b in data)
            {
                checksum += b;
            }

            byte lowByte = (byte)(checksum & 0xFF);
            byte[] result = new byte[data.Length + 1];
            Array.Copy(data, result, data.Length);
            result[^1] = lowByte;

            return result;
        }

        private string ConvertByteArrayToHexStr(byte[] byteDatas)
        {
            StringBuilder builder = new StringBuilder();

            for (int i = 0; i < byteDatas.Length; i++)
            {
                builder.Append(string.Format("0x{0:X2},", byteDatas[i]));
            }

            return builder.ToString().Trim();
        }

        private bool CompareTwoHexStr(byte[] string1, byte[] string2)
        {
            if (string1.Length != string2.Length)
                return false;

            for (int i = 0; i < string1.Length; i++)
            {
                if (string1[i] != string2[i])
                    return false;
            }

            return true;
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
            StartMoter();
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
            StartMoter();
        }

        // 强度置零方法
        private void ZeroIntensity()
        {
            TargetIntensity = 0;
            CurrentIntensity = TargetIntensity;
            System.Diagnostics.Debug.WriteLine("强度已置零");
            StartMoter();
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
                    StopAcquist();
                    DestroyMoter();

                    networkStream?.Close();
                    clientSocket?.Close();
                    tcpListener?.Stop();
                    mySerialPort?.Close();
                }

                disposed = true;
            }
        }

        ~ManulControlViewModel()
        {
            Dispose(false);
        }
        #endregion
    }

}
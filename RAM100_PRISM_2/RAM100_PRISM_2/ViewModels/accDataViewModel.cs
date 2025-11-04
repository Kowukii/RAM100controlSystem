using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RAM100_PRISM_2.ViewModels
{
    class accDataViewModel : INotifyPropertyChanged, INavigationAware, IRegionMemberLifetime
    {
        public bool KeepAlive => false;
        // 私有字段
        private TcpListener tcpListener;
        private TcpClient clientSocket;
        private NetworkStream networkStream;
        private byte[] recvDataBuf = new byte[1032];
        private DateTime lastCaptureTime;
        private bool plotEnable = true; // 默认启用绘图
        private List<KeyValuePair<DateTime, double>> dataList;
        private List<string> logBuffer;
        private System.Windows.Threading.DispatcherTimer updateTimeTimer;
        private System.Windows.Threading.DispatcherTimer updateValueTimer;
        private string logName;
        private Int64 captureIndex = 0;
        const bool renderChat = true;
        const bool recordData = true;

        // 添加Debug日志相关字段
        private StreamWriter debugLogWriter;
        private string debugLogPath;
        private object debugLogLock = new object();

        // 添加后台任务相关字段
        private Task acquisitionTask;
        private CancellationTokenSource cancellationTokenSource;

        private double currentValue = 0;
        public List<KeyValuePair<DateTime, double>> DataList => dataList;

        // 连接状态属性
        private string _connectionStatus = "未连接";
        public string ConnectionStatus
        {
            get => _connectionStatus;
            set
            {
                _connectionStatus = value;
                OnPropertyChanged();
                WriteDebugLog($"连接状态: {value}");
            }
        }

        public accDataViewModel()
        {
           

            // 初始化数据结构
            dataList = new List<KeyValuePair<DateTime, double>>();
            logBuffer = new List<string>();

           

            // 设置默认日志文件名
            logName = $"accDataLog_{DateTime.Now:yyyyMMdd_HHmmss}.csv";

            // 设置默认参数
            _currentFrequency = 60;
            _targetFrequency = 60;
            _currentIntensity = 0.05;
            _targetIntensity = 0.05;

            
        }

        private void InitializeDebugLog()
        {
            try
            {
                // 创建日志目录
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLogs");
                if (!Directory.Exists(logDirectory))
                {
                    Directory.CreateDirectory(logDirectory);
                }

                // 设置日志文件路径
                debugLogPath = Path.Combine(logDirectory, $"DebugLog_{DateTime.Now:yyyyMMdd_HHmmss}.txt");

                // 创建日志文件写入器
                debugLogWriter = new StreamWriter(debugLogPath, true, Encoding.UTF8)
                {
                    AutoFlush = true
                };

                WriteDebugLog("=== 应用程序启动 ===");
                WriteDebugLog($"日志文件路径: {debugLogPath}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"初始化Debug日志失败: {ex.Message}");
            }
        }

        private void WriteDebugLog(string message)
        {
            string logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";

            System.Diagnostics.Debug.WriteLine(logMessage);

            lock (debugLogLock)
            {
                try
                {
                    if (debugLogWriter != null && !debugLogWriter.BaseStream.CanWrite)
                    {
                        debugLogWriter = new StreamWriter(debugLogPath, true, Encoding.UTF8)
                        {
                            AutoFlush = true
                        };
                    }

                    debugLogWriter?.WriteLine(logMessage);
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"写入Debug日志失败: {ex.Message}");
                }
            }
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

        // 强度相关属性
        private double _currentIntensity = 0.05;
        private double _targetIntensity = 0.05;
        private double _intensityStep = 0.01;

        // 频率相关属性
        private double _currentFrequency = 60;
        private double _targetFrequency = 60;
        private double _frequencyStep = 0.1;

        private string _currentTime = DateTime.Now.ToString("HH:mm:ss");
        private bool isAcquisitionRunning = false;

        // 强度属性
        public double CurrentIntensity
        {
            get => _currentIntensity;
            set
            {
                _currentIntensity = value;
                WriteDebugLog($"CurrentIntensity 设置为: {value}");
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
                WriteDebugLog($"CurrentFreq 设置为: {value}");
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
                WriteDebugLog($"[{DateTime.Now:HH:mm:ss.fff}] CurrentAccelerationValue = {value:F6}");
                OnPropertyChanged();
            }
        }

        public bool CanStartSystem => !IsSystemRunning;
        public bool CanStopSystem => IsSystemRunning;

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

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 初始化Debug日志
            InitializeDebugLog();
            // 进入页面时的初始化
            WriteDebugLog("进入加速度数据页面");
            // 初始化定时器
            InitializeTimers();
            WriteDebugLog("accDataViewModel 初始化完成");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            WriteDebugLog("在OnNavigatedFrom开始离开页面，停止采集任务...");

            try
            {
                // 1. 取消任务
                cancellationTokenSource?.Cancel();

                // 2. 同步等待任务停止（带超时）
                if (acquisitionTask != null && !acquisitionTask.IsCompleted)
                {
                    WriteDebugLog("在OnNavigatedFrom等待采集任务停止...");

                    // 使用Wait而不是await，设置超时时间
                    bool completed = acquisitionTask.Wait(500); // 最多等待3秒

                    if (!completed)
                    {
                        WriteDebugLog("在OnNavigatedFrom采集任务停止超时，强制清理资源");
                        StopAcquist();
                    }
                    else
                    {
                        WriteDebugLog("在OnNavigatedFrom采集任务已正常停止");
                    }
                }

                // 3. 清理资源
                Dispose();

                WriteDebugLog("在OnNavigatedFrom页面离开处理完成");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"在OnNavigatedFrom离开页面时发生错误: {ex.Message}");
                Dispose();
            }
        }






        private async Task PrepareAcqustAsync(CancellationToken cancellationToken)
        {
            WriteDebugLog("正在准备采集...");

            try
            {
                tcpListener = new TcpListener(IPAddress.Any, 8234);
                WriteDebugLog("等待8234端口连接...");

                tcpListener.Start();
                ConnectionStatus = "等待连接...";
                WriteDebugLog("TCP监听器已启动，等待8234端口连接...");

                // 使用异步方式等待连接，并支持取消
                clientSocket = await tcpListener.AcceptTcpClientAsync(cancellationToken);
                networkStream = clientSocket.GetStream();

                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "已连接";
                });
                WriteDebugLog("8234端口连接成功，客户端已接入！");

                // 配置采集卡
                bool configResult = ConfigAcquist();

                if (configResult && !cancellationToken.IsCancellationRequested)
                {
                    // 开始采集
                    StartAcquist(cancellationToken);
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConnectionStatus = "数据采集中";
                    });
                }
            }
            catch (OperationCanceledException)
            {
                WriteDebugLog("采集任务被取消");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "已取消";
                });
            }
            catch (SocketException ex)
            {
                WriteDebugLog($"Socket异常: {ex.Message}");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "采集中断";
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "连接失败";
                });
                WriteDebugLog($"连接采集卡失败！错误: {ex.Message}");
            }
        }

        #region TCP数据采集方法
        public async void StartTcpDataAcquisition()
        {
            if (!IsSystemRunning)
            {
                ConnectionStatus = "在StartTcpDataAcquisition正在启动...";
                WriteDebugLog("在StartTcpDataAcquisition开始TCP数据采集");

                try
                {
                    // 使用CancellationTokenSource来控制任务取消
                    CancellationTokenSource source = new CancellationTokenSource();
                    source.Token.Register(() =>
                    {
                        Console.WriteLine("在StartTcpDataAcquisition调用任务结束函数操作1011");
                    });

                    // 在后台线程中执行采集任务
                    acquisitionTask = Task.Run(() => PrepareAcqust(source.Token), source.Token);

                    // 使用异步任务启动采集
                    //acquisitionTask = PrepareAcqustAsync(cancellationTokenSource.Token);

                    IsSystemRunning = true;
                }
                catch (Exception ex)
                {
                    ConnectionStatus = "在StartTcpDataAcquisition启动失败";
                    WriteDebugLog($"启动采集失败: {ex.Message}");
                    IsSystemRunning = false;
                }
            }
        }

        public async void StopTcpDataAcquisition()
        {
            if (IsSystemRunning)
            {
                ConnectionStatus = "正在停止...";
                WriteDebugLog("在StopTcpDataAcquisition停止TCP数据采集");

                try
                {
                    WriteDebugLog("在StopTcpDataAcquisition停止TCP监听器...");
                    try
                    {
                        tcpListener?.Stop();
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"停止监听器时发生错误（预期内）: {ex.Message}");
                    }

                    // 取消任务
                    cancellationTokenSource?.Cancel();

                    // 等待任务完成（最多等待5秒）
                    if (acquisitionTask != null && !acquisitionTask.IsCompleted)
                    {
                        await Task.WhenAny(acquisitionTask, Task.Delay(2000));
                    }

                    StopAcquist();
                    IsSystemRunning = false;
                    ConnectionStatus = "已停止";
                    WriteDebugLog("在StopTcpDataAcquisition采集已停止");
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"在StopTcpDataAcquisition停止采集时发生错误: {ex.Message}");
                    IsSystemRunning = false;
                    ConnectionStatus = "停止异常";
                }
                finally
                {
                    try
                    {
                        WriteDebugLog("在StopTcpDataAcquisition清理资源...");

                        // 停止定时器
                        updateTimeTimer?.Stop();
                        updateValueTimer?.Stop();

                        // 停止监听器（确保已经停止）
                        try
                        {
                            tcpListener?.Stop();
                        }
                        catch (Exception ex)
                        {
                            WriteDebugLog($"在StopTcpDataAcquisition停止监听器时发生错误: {ex.Message}");
                        }

                        // 清理网络资源
                        networkStream?.Close();
                        networkStream?.Dispose();
                        networkStream = null;

                        clientSocket?.Close();
                        clientSocket?.Dispose();
                        clientSocket = null;

                        tcpListener = null;

                        // 取消令牌源
                        cancellationTokenSource?.Cancel();
                        cancellationTokenSource?.Dispose();
                        cancellationTokenSource = null;

                        WriteDebugLog("在StopTcpDataAcquisition资源清理完成");
                    }
                    catch (Exception ex)
                    {
                        WriteDebugLog($"在StopTcpDataAcquisition清理资源时发生错误: {ex.Message}");
                    }
                }
            }
        }

        private void PrepareAcqust(CancellationToken cancellationToken)
        {
            WriteDebugLog("在PrepareAcqust正在准备采集...");

            try
            {
                // Create a TCP listener
                tcpListener = new TcpListener(IPAddress.Any, 8234);
                WriteDebugLog("在PrepareAcqust等待8234端口连接...");

                tcpListener.Start();
                ConnectionStatus = "等待连接...";
                WriteDebugLog("在PrepareAcqustTCP监听器已启动，等待8234端口连接...");

                // 设置接受连接超时（5秒）
                //var acceptTask = Task.Run(() => tcpListener.AcceptTcpClient(), cancellationToken);

                var acceptTask = Task.Run(() =>
                {
                    try
                    {
                        return tcpListener.AcceptTcpClient();
                    }
                    catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
                    {
                        WriteDebugLog("在PrepareAcqustTCPAcceptTcpClient 被正常中断，忽略此异常");
                        return null;
                    }
                    catch (ObjectDisposedException)
                    {
                        // 监听器已被释放
                        WriteDebugLog("TCP监听器已被释放，任务取消");
                        return null;
                    }
                }, cancellationToken);

                if (acceptTask.Wait((int)TimeSpan.FromSeconds(15).TotalMilliseconds, cancellationToken))
                {
                    clientSocket = acceptTask.Result;

                    if (clientSocket == null)
                    {
                        WriteDebugLog("连接过程被取消");
                        return;
                    }

                    networkStream = clientSocket.GetStream();

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConnectionStatus = "已连接";
                    });
                    WriteDebugLog("在PrepareAcqust8234端口连接成功，客户端已接入！");

                    // 配置采集卡
                    bool configResult = ConfigAcquist();

                    if (configResult && !cancellationToken.IsCancellationRequested)
                    {
                        // 开始采集
                        StartAcquist(cancellationToken);
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ConnectionStatus = "数据采集中";
                        });
                    }
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ConnectionStatus = "连接超时";
                    });
                    WriteDebugLog("在PrepareAcqust连接超时，未检测到客户端连接");
                }
            }
            catch (OperationCanceledException ex)
            {
                WriteDebugLog("在PrepareAcqust采集任务被取消");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "已取消";
                });
            }
            catch (SocketException ex) when (ex.SocketErrorCode == SocketError.Interrupted)
            {
                // 专门处理 WSACancelBlockingCall 异常
                WriteDebugLog("在PrepareAcqustSocket操作被正常中断，忽略此异常");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "已取消";
                });
            }
            catch (ObjectDisposedException)
            {
                // 处理对象已释放的异常
                WriteDebugLog("在PrepareAcqustTCP监听器已被释放，采集任务正常结束");
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "已取消";
                });
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ConnectionStatus = "连接失败";
                });
                WriteDebugLog($"在PrepareAcqust连接采集卡失败！错误: {ex.Message}");
            }
        }

        private bool ConfigAcquist()
        {
            const int maxRetries = 3;
            const int retryDelayMs = 1000;
            int retryCount = 0;

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

            // 配置成功返回数据
            byte[] paramSetSuccessBuf = { 0x55, 0xEE, 0x00, 0x05, 0x10, 0x01, 0x81, 0x22, 0xFC };

            while (retryCount < maxRetries)
            {
                try
                {
                    WriteDebugLog($"开始第 {retryCount + 1} 次ADC配置尝试...");

                    // 结尾追加校验和
                    byte[] currentParamSetBuf = AppendChecksum(paramSetBuf);
                    byte[] paramSetRecvBuf = new byte[9];

                    ClearNetworkBuffer();

                    // Set ADC param.
                    WriteDebugLog("配置ADC参数 " + ConvertByteArrayToHexStr(currentParamSetBuf));
                    networkStream.Write(currentParamSetBuf, 0, currentParamSetBuf.Length);

                    networkStream.Read(paramSetRecvBuf, 0, paramSetRecvBuf.Length);
                    WriteDebugLog("配置ADC返回 " + ConvertByteArrayToHexStr(paramSetRecvBuf));

                    if (CompareTwoHexStr(paramSetSuccessBuf, paramSetRecvBuf))
                    {
                        WriteDebugLog("ADC配置成功");
                        return true;
                    }
                    else
                    {
                        WriteDebugLog($"ADC配置失败 (尝试 {retryCount + 1}/{maxRetries})");
                        retryCount++;
                        if (retryCount < maxRetries)
                        {
                            Thread.Sleep(retryDelayMs);
                        }
                    }
                }
                catch (Exception ex)
                {
                    WriteDebugLog($"ADC配置过程中发生异常 (尝试 {retryCount + 1}/{maxRetries}): {ex.Message}");
                    retryCount++;
                    if (retryCount < maxRetries)
                    {
                        Thread.Sleep(retryDelayMs);
                    }
                }
            }

            WriteDebugLog("ADC配置达到最大重试次数仍失败");
            Application.Current.Dispatcher.Invoke(() =>
            {
                ConnectionStatus = "配置失败";
            });
            return false;
        }

        private void StartAcquist(CancellationToken cancellationToken)
        {
            WriteDebugLog("开始采集");

            // 开始采样命令
            byte[] samplingStartBuf = { 0x55, 0xEE, 0x00, 0x05, 0x10, 0x01, 0x24, 0x10, 0x8D };

            // 发送指令
            WriteDebugLog("开始采样 " + ConvertByteArrayToHexStr(samplingStartBuf));
            networkStream.Write(samplingStartBuf, 0, samplingStartBuf.Length);

            // 开始数据读取循环
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    int bytesRead = networkStream.Read(recvDataBuf, 0, recvDataBuf.Length);
                    if (bytesRead > 0)
                    {
                        ProcessReceivedData();
                    }

                    // 短暂延迟避免CPU占用过高
                    Thread.Sleep(1);
                }
                catch (Exception ex)
                {
                    if (!cancellationToken.IsCancellationRequested)
                    {
                        WriteDebugLog($"网络读取错误: {ex.Message}");
                        Application.Current.Dispatcher.Invoke(() =>
                        {
                            ConnectionStatus = "读取错误";
                        });
                    }
                    break;
                }
            }

            WriteDebugLog("数据采集循环结束");
        }

        private void StopAcquist()
        {
            try
            {
                if (networkStream != null && networkStream.CanWrite)
                {
                    // 停止采样指令
                    byte[] samplingStopBuf = { 0x55, 0xEE, 0x80, 0x09, 0x10, 0x01, 0x24, 0x33, 0x00, 0x00, 0x00, 0x00, 0x34 };

                    // 发送指令
                    WriteDebugLog("停止采样 " + ConvertByteArrayToHexStr(samplingStopBuf));
                    networkStream.Write(samplingStopBuf, 0, samplingStopBuf.Length);
                }

                // 关闭连接
                networkStream?.Close();
                clientSocket?.Close();
                tcpListener?.Stop();

                WriteDebugLog("采集资源已释放");
            }
            catch (Exception ex)
            {
                WriteDebugLog($"停止采集时发生错误: {ex.Message}");
            }
        }

        private void ProcessReceivedData()
        {
            try
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

                    // 计算对应加速度值
                    double number = max1 * 4.0 / 0x8000 / 0.344 / 0.05;

                    // 在 UI 线程中更新数据
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        // 更新当前加速度值
                        CurrentAccelerationValue = number;

                        // 如果需要记录到数据列表
                        if (renderChat)
                        {
                            dataList.Add(new KeyValuePair<DateTime, double>(now, number));

                            // 限制数据量，保留最近15秒的数据
                            DateTime threshold = DateTime.Now.AddSeconds(-15);
                            var itemsToRemove = dataList.Where(point => point.Key < threshold).ToList();
                            foreach (var item in itemsToRemove)
                            {
                                dataList.Remove(item);
                            }

                            OnPropertyChanged(nameof(DataList));
                        }
                    });

                    // 数据记录逻辑
                    if (recordData)
                    {
                        string timeString = now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                        string text = $"{timeString},{captureIndex:0000000000},{CurrentFrequency},{CurrentIntensity * 100},{number:F10}";

                        lock (logBuffer)
                        {
                            logBuffer.Add(text);

                            if (captureIndex % 100 == 0 && logBuffer.Count > 0)
                            {
                                try
                                {
                                    File.AppendAllLines(logName, logBuffer);
                                    logBuffer.Clear();
                                }
                                catch (Exception ex)
                                {
                                    WriteDebugLog($"日志写入错误: {ex.Message}");
                                }
                            }
                        }
                    }

                    captureIndex += 1;
                }
            }
            catch (Exception ex)
            {
                WriteDebugLog($"数据处理错误: {ex.Message}");
            }
        }

        private void ClearNetworkBuffer()
        {
            if (clientSocket == null || networkStream == null) return;

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
            if (availableBytes > 0)
            {
                networkStream.Read(releaseBuf, 0, Math.Min(releaseBuf.Length, availableBytes));
            }
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
            WriteDebugLog("=== Dispose启动 ===");
            if (!disposed)
            {
                if (disposing)
                {
                    // 清理托管资源
                    WriteDebugLog("=== 应用程序关闭 ===");

                    // 取消采集任务
                    cancellationTokenSource?.Cancel();

                    updateTimeTimer?.Stop();
                    updateValueTimer?.Stop();
                    StopTcpDataAcquisition();

                    networkStream?.Close();
                    clientSocket?.Close();
                    tcpListener?.Stop();

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

        ~accDataViewModel()
        {
            Dispose(false);
        }
        #endregion
    }
}
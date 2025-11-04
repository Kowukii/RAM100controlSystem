using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using System;
using System.IO;
using System.Text;
using System.Windows.Input;

namespace RAM100_PRISM_2.ViewModels
{
    public class IndexViewModel : BindableBase
    {
        private readonly IRegionManager _regionManager;
        IRegionNavigationJournal journal;

        public IndexViewModel(IRegionManager regionManager)
        {
            InitializeDebugLog();

            _regionManager = regionManager;
            
            
            NavigateToMonitorCommand = new DelegateCommand(NavigateToMonitor);
            NavigateToRecordCommand = new DelegateCommand(NavigateToRecord);
            NavigateToSettingsCommand = new DelegateCommand(NavigateToSettings);
            NavigateToAlarmCommand = new DelegateCommand(NavigateToAlarm);
            NavigateToHelpCommand = new DelegateCommand(NavigateToHelp);
            NavigateToUserCommand = new DelegateCommand(NavigateToUser);
            NavigateToLogCommand = new DelegateCommand(NavigateToLog);
            NavigateToAboutCommand = new DelegateCommand(NavigateToAbout);

            NavigateToSplitViewCommand = new DelegateCommand<string>(OnNavigateToSplitView);

        }
        public DelegateCommand<string> NavigateToSplitViewCommand { get; private set; }
        
        public ICommand NavigateToMonitorCommand { get; }
        public ICommand NavigateToRecordCommand { get; }
        public ICommand NavigateToSettingsCommand { get; }
        public ICommand NavigateToAlarmCommand { get; }
        public ICommand NavigateToHelpCommand { get; }
        public ICommand NavigateToUserCommand { get; }
        public ICommand NavigateToLogCommand { get; }
        public ICommand NavigateToAboutCommand { get; }

        // 添加Debug日志相关字段
        private StreamWriter debugLogWriter;
        private string debugLogPath;
        private object debugLogLock = new object();

        private void InitializeDebugLog()
        {
            try
            {
                // 创建日志目录
                string logDirectory = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "DebugLogs3");
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

        private void OnNavigateToSplitView(string viewType)
        {
            var parameters = new NavigationParameters();

            switch (viewType)
            {
                case "Scenario1":
                    parameters.Add("LeftView", "PageAcc");
                    parameters.Add("RightView", "PageMRW");
                    break;
                case "Scenario2":
                    parameters.Add("LeftView", "PageAcc");
                    parameters.Add("RightView", "PageARW");
                    break;
                case "Scenario3":
                    parameters.Add("LeftView", "PageAcc");
                    parameters.Add("RightView", "PageMRW");
                    break;
                default:
                    parameters.Add("LeftView", "PageAcc");
                    parameters.Add("RightView", "PageMRW");
                    break;
            }
            WriteDebugLog("在OnNavigateToSplitView调用RequestNavigate");

            //_regionManager.RequestNavigate("ContentRegion", "PageSM", parameters);
            try
            {
                WriteDebugLog($"在OnNavigateToSplitView时parameters: {parameters}");
                _regionManager.RequestNavigate("ContentRegion", "PageSM", parameters);
            }
            catch (Exception ex)
            {
                // 处理异常，可能需要重新初始化区域

                WriteDebugLog($"在OnNavigateToSplitView时发生错误: {ex.Message}");


            }
        }
        
        private void NavigateToMonitor()
        {
            //_regionManager.RequestNavigate("ContentRegion", "PageMC");
            _regionManager.RequestNavigate("ContentRegion", "PageMC", arg =>
            {
                journal = arg.Context.NavigationService.Journal;

            });
        }

        private void NavigateToRecord()
        {
            //_regionManager.RequestNavigate("ContentRegion", "PageAC");
            _regionManager.RequestNavigate("ContentRegion", "PageAC", arg =>
            {
                journal = arg.Context.NavigationService.Journal;

            });
        }

        private void NavigateToSettings()
        {
            //_regionManager.RequestNavigate("ContentRegion", "PagePM");
            _regionManager.RequestNavigate("ContentRegion", "PagePM", arg =>
            {
                journal = arg.Context.NavigationService.Journal;

            });
        }

        private void NavigateToAlarm()
        {
            _regionManager.RequestNavigate("ContentRegion", "AlarmView");
        }

        private void NavigateToHelp()
        {
            _regionManager.RequestNavigate("ContentRegion", "PageDirection");
        }

        private void NavigateToUser()
        {
            _regionManager.RequestNavigate("ContentRegion", "UserView");
        }

        private void NavigateToLog()
        {
            _regionManager.RequestNavigate("ContentRegion", "LogView");
        }

        private void NavigateToAbout()
        {
            _regionManager.RequestNavigate("ContentRegion", "PageTool");
        }
    }
}
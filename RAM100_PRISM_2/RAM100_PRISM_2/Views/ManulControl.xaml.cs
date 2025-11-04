using RAM100_PRISM_2.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;


using System.Timers;

namespace RAM100_PRISM_2.Views
{
    /// <summary>
    /// ManulControl.xaml 的交互逻辑
    /// </summary>
    public partial class ManulControl : UserControl, IDisposable
    {
        private Timer _dataUpdateTimer;
        private bool _isDisposed = false;
        private ScottPlot.Plottable.SignalPlot _signalPlot;
        private double[] _dataBuffer;
        private const int BufferSize = 500;
        private bool _isPaused = false;
        private ManulControlViewModel _viewModel;

        public ManulControl()
        {
            InitializeComponent();
            _viewModel = new ViewModels.ManulControlViewModel();
            this.DataContext = _viewModel;
            this.Loaded += SpView_Loaded;
            this.Unloaded += SpView_Unloaded;

            // 监听应用程序退出事件
            Application.Current.Exit += Application_Exit;

            System.Diagnostics.Debug.WriteLine($"DataContext 类型: {this.DataContext?.GetType().Name}");
        }

        private void SpView_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isDisposed) return;

            var plt = wpfPlot.Plot;

            // 如果是第一次加载，初始化图表
            if (_signalPlot == null)
            {
                // 设置图表标题和标签
                plt.Title("RAM设备实时加速度");
                plt.XLabel("时间点");
                plt.YLabel("加速度大小");

                // 初始化数据缓冲区
                _dataBuffer = new double[BufferSize];

                // 初始填充零数据
                Array.Fill(_dataBuffer, 0);

                // 添加初始信号并配置
                _signalPlot = plt.AddSignal(_dataBuffer);
                _signalPlot.Color = System.Drawing.Color.Blue;
                plt.SetAxisLimitsX(0, BufferSize);
                plt.SetAxisLimitsY(-6, 6); // 根据数据范围设置Y轴范围

                wpfPlot.Refresh();

                // 创建定时器，每100毫秒更新一次数据
                _dataUpdateTimer = new Timer(100);
                _dataUpdateTimer.Elapsed += DataUpdateTimer_Elapsed;
            }

            // 恢复定时器（如果之前暂停）
            if (_isPaused)
            {
                _dataUpdateTimer.Start();
                _isPaused = false;
                System.Diagnostics.Debug.WriteLine("图表更新已恢复");
            }
            else
            {
                _dataUpdateTimer.Start();
            }
        }

        private void DataUpdateTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (_isDisposed || _isPaused) return;

            try
            {
                // 在UI线程更新数据
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_isDisposed || _isPaused) return;

                    // 从ViewModel获取最新数据
                    double newValue = _viewModel.CurrentAccelerationValue;

                    // 滚动数据：移除第一个元素，添加新元素到末尾
                    Array.Copy(_dataBuffer, 1, _dataBuffer, 0, BufferSize - 1);
                    _dataBuffer[BufferSize - 1] = newValue;

                    // 更新图表数据
                    _signalPlot.Ys = _dataBuffer;

                    // 刷新图表
                    wpfPlot.Refresh();
                });
            }
            catch (Exception ex)
            {
                // 应用程序关闭时可能会抛出异常，忽略这些异常
                System.Diagnostics.Debug.WriteLine($"定时器异常: {ex.Message}");
            }
        }

        private void SpView_Unloaded(object sender, RoutedEventArgs e)
        {
            // 暂停定时器而不是完全清理
            if (_dataUpdateTimer != null && _dataUpdateTimer.Enabled)
            {
                _dataUpdateTimer.Stop();
                _isPaused = true;
                System.Diagnostics.Debug.WriteLine("图表更新已暂停");
            }
        }

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Dispose();
        }

        private void CleanupResources()
        {
            if (_isDisposed) return;

            // 停止定时器
            if (_dataUpdateTimer != null)
            {
                _dataUpdateTimer.Stop();
                _dataUpdateTimer.Elapsed -= DataUpdateTimer_Elapsed;
                _dataUpdateTimer.Dispose();
                _dataUpdateTimer = null;
            }

            // 清理图表
            if (wpfPlot != null && wpfPlot.Plot != null)
            {
                wpfPlot.Plot.Clear();
            }

            // 取消事件订阅
            Application.Current.Exit -= Application_Exit;
            this.Loaded -= SpView_Loaded;
            this.Unloaded -= SpView_Unloaded;

            _isDisposed = true;
            _isPaused = false;

            System.Diagnostics.Debug.WriteLine("ManulControl 资源已清理");
        }

        #region IDisposable Implementation

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (!_isDisposed)
            {
                if (disposing)
                {
                    CleanupResources();
                }

                _isDisposed = true;
            }
        }

        ~ManulControl()
        {
            Dispose(false);
        }

        #endregion
    }
}
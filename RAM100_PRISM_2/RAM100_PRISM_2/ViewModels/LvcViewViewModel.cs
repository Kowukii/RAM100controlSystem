using ImTools;
using LiveCharts;
using LiveCharts.Defaults;
using Prism.Mvvm;
using Prism.Regions;
using RAM100_PRISM_2.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace RAM100_PRISM_2.ViewModels
{
    internal class LvcViewViewModel : BindableBase, INavigationAware, IRegionMemberLifetime
    {
        double[] doubles = new double[] { 50, 10, 20, 30, 50, 60, 70, 100, 65, 10 };
        public int[] labels = new int[] { 00, 01, 02, 03, 04, 05, 06, 07, 08, 09 };
        public ChartValues<ObservableValue> ChartData { get; set; } = new ChartValues<ObservableValue>();
        public List<string> XLabels { get; set; } = new List<string>();
        Random random = new Random();

        // 用于控制后台任务的取消令牌
        private CancellationTokenSource _cancellationTokenSource;

        public LvcViewViewModel()
        {
            // 初始化取消令牌
            _cancellationTokenSource = new CancellationTokenSource();

            for (int i = 0; i < 100; i++)
            {
                ChartData.Add(new ObservableValue(random.Next(30, 80)));
            }

            for (int i = 0; i < 100; i++)
            {
                XLabels.Add(DateTime.Now.ToString("mm:sss"));
            }

            // 启动后台任务并传入取消令牌
            StartBackgroundTask();
        }

        private void StartBackgroundTask()
        {
            Task.Run(async () =>
            {
                while (!_cancellationTokenSource.Token.IsCancellationRequested)
                {
                    await Task.Delay(500, _cancellationTokenSource.Token);

                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ChartData.Add(new ObservableValue(random.Next(30, 80)));
                        XLabels.Add(DateTime.Now.ToString("mm:ss"));
                        ChartData.RemoveAt(0);
                        XLabels.RemoveAt(0);
                    });
                }
            }, _cancellationTokenSource.Token);
        }

        public bool KeepAlive
        {
            get { return true; }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return false;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            // 当离开视图时，取消后台任务
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();
            _cancellationTokenSource = null;

            // 清空图表数据释放资源
            ChartData.Clear();
            XLabels.Clear();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            // 如果需要在导航到视图时重新启动任务，可以在这里处理
            if (_cancellationTokenSource == null || _cancellationTokenSource.IsCancellationRequested)
            {
                _cancellationTokenSource = new CancellationTokenSource();
                StartBackgroundTask();
            }
        }
    }
}

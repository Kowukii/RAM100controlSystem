
using ScottPlot;
using System;
using System.Collections.Generic;
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

namespace RAM100_PRISM_2.Views
{
    /// <summary>
    /// SpView.xaml 的交互逻辑
    /// </summary>
    public partial class SpView : UserControl
    {
        public SpView()
        {
            InitializeComponent();

            this.Loaded += SpView_Loaded;
        }

        //private void SpView_Loaded(object sender, RoutedEventArgs e)
        //{

        //    var plt = wpfPlot.Plot;
        //    // ===================================================
        //    Random random = new Random();

        //    double[] datas = DataGen.RandomWalk(random, (int)1e6);
        //    plt.AddSignal(datas);
        //    // ===================================================





        //    wpfPlot.Refresh();

        //    Task.Run(async () =>
        //    {
        //        for (int i = 0; i < 3; i++)
        //        {
        //            await Task.Delay(2000);
        //            datas = DataGen.RandomWalk(random, (int)1e6);

        //            this.Dispatcher.Invoke(() =>
        //            {
        //                plt.AddSignal(datas);
        //            });
        //        }
        //    });
        //}

        private void SpView_Loaded(object sender, RoutedEventArgs e)
        {
            var plt = wpfPlot.Plot;
            Random random = new Random();

            // 设置图表标题和标签
            plt.Title("实时滚动数据");
            plt.XLabel("数据点");
            plt.YLabel("值");

            // 初始化数据缓冲区（固定长度为1000个点）
            int bufferSize = 1000;
            double[] dataBuffer = new double[bufferSize];

            // 初始填充随机数据
            for (int i = 0; i < bufferSize; i++)
            {
                dataBuffer[i] = random.NextDouble() * 10 - 5; // 生成-5到5之间的随机数
            }

            // 添加初始信号并配置
            var signal = plt.AddSignal(dataBuffer);
            signal.Color = System.Drawing.Color.Blue;
            plt.SetAxisLimitsX(0, bufferSize);
            plt.SetAxisLimitsY(-6, 6); // 根据数据范围设置Y轴范围

            wpfPlot.Refresh();

            // 创建定时器，每秒更新一次数据
            var timer = new System.Timers.Timer(100); // 1000ms = 1秒
            timer.Elapsed += (s, e) =>
            {
                // 在UI线程更新数据
                this.Dispatcher.Invoke(() =>
                {
                    // 生成新数据点
                    double newValue = random.NextDouble() * 10 - 5;

                    // 滚动数据：移除第一个元素，添加新元素到末尾
                    Array.Copy(dataBuffer, 1, dataBuffer, 0, bufferSize - 1);
                    dataBuffer[bufferSize - 1] = newValue;

                    // 更新图表数据
                    signal.Ys = dataBuffer;

                    // 刷新图表
                    wpfPlot.Refresh();
                });
            };

            // 启动定时器
            timer.Start();

            // 窗口关闭时停止定时器
            this.Unloaded += (s, e) =>
            {
                timer.Stop();
                timer.Dispose();
            };
        }
    }
}

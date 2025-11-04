using Prism.Regions;
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

namespace RAM100_PRISM_2.Views
{
    /// <summary>
    /// AutoRightWidget.xaml 的交互逻辑
    /// </summary>
    public partial class AutoRightWidget : UserControl, INavigationAware
    {
        public AutoRightWidget()
        {
            InitializeComponent();
        }
        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Debug.WriteLine("AutoRightWidget: 导航到页面");
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            Debug.WriteLine("AutoRightWidget: 离开页面");

            // 确保 ViewModel 资源被释放
            if (DataContext is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }
}

using Prism.Mvvm;
using Prism.Regions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RAM100_PRISM_2.ViewModels
{

    public class SplitMiddleViewViewModel : BindableBase, INavigationAware
    {
        private readonly IRegionManager _regionManager;
        private bool _isInitialized;
        private bool _isNavigating;

        public SplitMiddleViewViewModel(IRegionManager regionManager)
        {
            _regionManager = regionManager ?? throw new ArgumentNullException(nameof(regionManager), "IRegionManager未正确注入，请检查Prism容器配置");
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            if (_regionManager == null || _isNavigating) return;

            try
            {
                _isNavigating = true;

                // 从导航参数获取左右区域要显示的页面
                var leftView = navigationContext.Parameters["LeftView"] as string ?? "PageAcc"; // 默认值
                var rightView = navigationContext.Parameters["RightView"] as string ?? "PageMRW"; // 默认值

                // 根据参数导航到不同的页面
                _regionManager.RequestNavigate("ContentRegionLeft", leftView);
                _regionManager.RequestNavigate("ContentRegionRight", rightView);

                _isInitialized = true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"导航失败: {ex.Message}");
            }
            finally
            {
                _isNavigating = false;
            }
        }

        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            //// 根据参数决定是否重用实例
            //var leftView = navigationContext.Parameters["LeftView"] as string;
            //var rightView = navigationContext.Parameters["RightView"] as string;

            // 如果参数相同则重用，否则创建新实例
            //return leftView == null && rightView == null;
            return true;
        }

        public void OnNavigatedFrom(NavigationContext navigationContext) { }
    }
}

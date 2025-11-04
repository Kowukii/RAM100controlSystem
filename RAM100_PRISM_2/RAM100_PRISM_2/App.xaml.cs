using Prism.DryIoc;
using Prism.Ioc;
using Prism.Regions;
using RAM100_PRISM_2.ViewModels;
using RAM100_PRISM_2.Views;
using System.Windows;

namespace RAM100_PRISM_2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : PrismApplication
    {
        protected override Window CreateShell()
        {
            return Container.Resolve<MainWindow>();
        }

        protected override void RegisterTypes(IContainerRegistry containerRegistry)
        {

            containerRegistry.RegisterForNavigation<ViewA>("PageManulRun");
            containerRegistry.RegisterForNavigation<ViewB>("PageAutoRun");
            containerRegistry.RegisterForNavigation<Index>("PageIndex");
            containerRegistry.RegisterForNavigation<LvcView>("PageLvc");
            containerRegistry.RegisterForNavigation<SpView>("PageSp");
            containerRegistry.RegisterForNavigation<ManulControl>("PageMC");
            containerRegistry.RegisterForNavigation<PresetManager>("PagePM");
            containerRegistry.RegisterForNavigation<AutoControl>("PageAC");



            containerRegistry.RegisterForNavigation<SplitMiddleView>("PageSM");
            containerRegistry.RegisterForNavigation<ManulRightWidget>("PageMRW");
            containerRegistry.RegisterForNavigation<AutoRightWidget>("PageARW");
            containerRegistry.RegisterForNavigation<acceDataView>("PageAcc");
            containerRegistry.RegisterDialog<MsgView, MsgViewModel>("Question");

            containerRegistry.RegisterForNavigation<DirectionPDF>("PageDirection");
            containerRegistry.RegisterForNavigation<ToolPage>("PageTool");
        }

        protected override void OnInitialized()
        {
            base.OnInitialized();

            // 获取 PRISM 区域管理器，用于导航
            var regionManager = Container.Resolve<IRegionManager>();
            // 启动时导航到 Index 页面，填充 ContentRegion
            regionManager.RequestNavigate("ContentRegion", "PageIndex");
        }

    }
}

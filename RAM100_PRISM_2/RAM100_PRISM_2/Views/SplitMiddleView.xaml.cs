using System.Windows.Controls;
using Prism.Regions;
using RAM100_PRISM_2.ViewModels;

namespace RAM100_PRISM_2.Views
{
    public partial class SplitMiddleView : UserControl
    {
        public SplitMiddleView(IRegionManager regionManager)
        {
            InitializeComponent();
            DataContext = new SplitMiddleViewViewModel(regionManager);
        }
    }
}

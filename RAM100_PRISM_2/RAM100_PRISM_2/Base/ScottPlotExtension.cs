using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace RAM100_PRISM_2.Base
{
    // 为什么需要这个对象
    // 因为页面中的Plot对象没有属性可以绑定，提供图表数据
    // <WpfPlot Grid.Row="1" b:ScottPlotExtension.Values="{Binding}"/>

    public class ScottPlotExtension
    {
        // 附加属性的声明 
        // 从GetValues方法获取被附加对象的附加Values属性值
        public static readonly DependencyProperty ValuesProperty =
            DependencyProperty.RegisterAttached(
                "Values",
                typeof(ObservableCollection<double>),
                typeof(ScottPlotExtension),
                new PropertyMetadata(null));
        // 附加属性
        public static ObservableCollection<double> GetValues(DependencyObject obj)
        {
            return (ObservableCollection<double>)obj.GetValue(ValuesProperty);
        }

        public static void SetValues(DependencyObject obj, ObservableCollection<double> value)
        {
            obj.SetValue(ValuesProperty, value);
        } 
    }
}

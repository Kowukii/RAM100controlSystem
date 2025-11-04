using Prism.Mvvm;
using Prism.Regions;
using RAM100_PRISM_2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;


namespace RAM100_PRISM_2.ViewModels
{
    internal class ViewAViewModel :BindableBase, INavigationAware, IConfirmNavigationRequest
    {
        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; RaisePropertyChanged(); }
        }
        public bool IsNavigationTarget(NavigationContext navigationContext)
        {
            return true;
        }

        // 导航离开当前页时触发
        // <param name="navigationContext"></param>
        public void OnNavigatedFrom(NavigationContext navigationContext)
        {
            //throw new NotImplementedException();
        }

        public void OnNavigatedTo(NavigationContext navigationContext)
        {
            Title = navigationContext.Parameters.GetValue<string>("Value");
        }

        public void ConfirmNavigationRequest(NavigationContext navigationContext, Action<bool> continuationCallback)
        {
            bool result = true;

            if (MessageBox.Show("确认离开？", "温馨提示", MessageBoxButton.YesNo) == MessageBoxResult.No)
                result = false;

            continuationCallback(result);
        }

        public ObservableCollection<PrimaryItemModel> PrimaryList { get; set; }

        public ViewAViewModel()
        {
            Random random = new Random();
            // 默认提供15条记录 即15个柱形
            PrimaryList = new ObservableCollection<PrimaryItemModel>();
            for(int i = 0; i < 15; i++)
            {
                PrimaryList.Add(new PrimaryItemModel
                {
                    Label = DateTime.Now.ToString("mm:ss"),
                    Value = random.Next(30, 200)
                });
            }

            Task.Run(async () =>
            {
                while (true)
                {
                    await Task.Delay(1000);
                    // 每1秒钟添加1个子项进来
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        PrimaryList.Add(new PrimaryItemModel
                        {
                            Label = DateTime.Now.ToString("mm:ss"),
                            Value = random.Next(30, 200)
                        });
                        PrimaryList.RemoveAt(0); // 目的图标滚动
                        var test = DateTime.Now.ToString("mm:ss"); // 输出当前分钟:秒（如 "45:12"）
                        Console.WriteLine(test); // 查看控制台是否有值
                    });
                }
            });



        }

    }
}

using Prism.Commands;
using Prism.Mvvm;
using Prism.Regions;
using Prism.Services.Dialogs;
using System;
using System.Windows;
using System.Windows.Threading;
using System.ComponentModel;

namespace RAM100_PRISM_2.ViewModels
{
    public class MainWindowViewModel : BindableBase
    {
        private string _currentTime;
        private readonly DispatcherTimer _timer;

    
        public string CurrentTime
        {
            get => _currentTime;
            set => SetProperty(ref _currentTime, value);
        }

        private readonly IRegionManager regionManager;
        private readonly IDialogService dialog;
        IRegionNavigationJournal journal;

        public DelegateCommand OpenACommand { get; private set; }

        public DelegateCommand OpenBCommand { get; private set; }

        public DelegateCommand OpenCCommand { get; private set; }

        public DelegateCommand GoBackCommand { get; private set; }

        public DelegateCommand GoForwardCommand { get; private set; }

        public DelegateCommand OpenEditCommand { get; private set; }

        public DelegateCommand ShutDownCommand { get; private set; }

        private string _title = "Prism Application";
        public string Title
        {
            get { return _title; }
            set { SetProperty(ref _title, value); }
        }

        

        public MainWindowViewModel(IRegionManager regionManager, IDialogService dialog)
        {
            
            OpenACommand = new DelegateCommand(OpenA);
            OpenBCommand = new DelegateCommand(OpenB);
            OpenCCommand = new DelegateCommand(OpenC);
            OpenEditCommand = new DelegateCommand(OpenEdit);
            GoBackCommand = new DelegateCommand(GoBack);
            GoForwardCommand = new DelegateCommand(GoForward);

            ShutDownCommand = new DelegateCommand(ShutDown);
            this.regionManager = regionManager;
            this.dialog = dialog;

            // 初始化时间
            UpdateTime();

            // 设置定时器
            _timer = new DispatcherTimer();
            _timer.Interval = TimeSpan.FromSeconds(1);
            _timer.Tick += (s, e) => UpdateTime();
            _timer.Start();
        }

        private void OpenEdit()
        {
            // 导航到SplitMiddleView
            regionManager.RequestNavigate("ContentRegion", "PageSM", arg => {
                journal = arg.Context.NavigationService.Journal;
            });
        }


        private void OpenA() {
            NavigationParameters param = new NavigationParameters();
            param.Add("Value", "Hello");

            //regionManager.RequestNavigate("ContentRegion", 
            //                              "PageManulRun", 
            //                              //param,
            //                              arg =>
            //                              {
            //    journal = arg.Context.NavigationService.Journal;

            //});

            regionManager.RequestNavigate("ContentRegion", $"PageAC?Value=Nice", arg =>
            {
                journal = arg.Context.NavigationService.Journal;

            });
            // 指向ContentRegion区域，将内容设置为PageManulRun
        }

        private void OpenB()
        {
            regionManager.RequestNavigate("ContentRegion", "PageMC", arg =>
            {
                journal = arg.Context.NavigationService.Journal;

            });
        }

        private void OpenC()
        {
            regionManager.RequestNavigate("ContentRegion", "PagePM", arg =>
            {
                journal = arg.Context.NavigationService.Journal;

            });
        }

        private void GoForward()
        {
            regionManager.RequestNavigate("ContentRegion", "PageIndex");
            //if(journal != null && journal.CanGoForward)
            //{
            //    journal.GoForward();
            //}
        }

        

        //private void GoBack()
        //{
        //    //regionManager.RequestNavigate("ContentRegion", "PageManulRun");
        //    //journal.GoBack();
        //    if (journal != null && journal.CanGoBack)
        //    {
        //        journal.GoBack();
        //    }
        //}

        private void GoBack()
        {
            regionManager.RequestNavigate("ContentRegion", "PageSM");
            //journal.GoBack();
            //if (journal != null && journal.CanGoBack)
            //{
            //    journal.GoBack();
            //}
        }
        private void ShutDown()
        {

            Application.Current.Shutdown();
        }
        private void UpdateTime()
        {
            CurrentTime = DateTime.Now.ToString("HH:mm:ss");
        }

        
    }
}

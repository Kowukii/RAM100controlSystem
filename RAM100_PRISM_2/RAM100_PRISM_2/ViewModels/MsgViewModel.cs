using Prism.Commands;
using Prism.Mvvm;
using Prism.Services.Dialogs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAM100_PRISM_2.ViewModels
{
    internal class MsgViewModel : BindableBase, IDialogAware
    {
        private string title;

        public string Title
        {
            get { return title; }
            set { title = value; RaisePropertyChanged(); }
        }

        public MsgViewModel() {

            SaveCommand = new DelegateCommand(() =>
            {
                DialogParameters param = new DialogParameters();
                param.Add("Value", Title);
                RequestClose?.Invoke(new DialogResult(ButtonResult.OK, param));
            });

            CancelCommand = new DelegateCommand(() =>
            {
                RequestClose?.Invoke(new DialogResult(ButtonResult.No));
            });
        }

        public DelegateCommand SaveCommand { get; set; }
        public DelegateCommand CancelCommand { get; set; }


        public event Action<IDialogResult> RequestClose;

        public bool CanCloseDialog()
        {
            return true;
        }

        public void OnDialogClosed()
        {
           
        }

        public void OnDialogOpened(IDialogParameters parameters)
        {
            var title = parameters.GetValue<string>("Value");
        }
    }
}

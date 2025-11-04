using Prism.Mvvm;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAM100_PRISM_2.Models
{
    public class Ingredient : BindableBase
    {
        private string _name;
        private double _percentage;
        private double _weight;

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public double Percentage
        {
            get => _percentage;
            set => SetProperty(ref _percentage, value);
        }

        public double Weight
        {
            get => _weight;
            set => SetProperty(ref _weight, value);
        }
    }
}

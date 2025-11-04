using Prism.Commands;
using Prism.Mvvm;
using RAM100_PRISM_2.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RAM100_PRISM_2.ViewModels
{
    public class ToolPageViewModel : BindableBase
    {
        private ObservableCollection<Ingredient> _ingredients;
        private double _totalWeight;
        private string _validationMessage;
        private int _ingredientCount;
        private double _totalPercentageValue;

        public ToolPageViewModel()
        {
            Ingredients = new ObservableCollection<Ingredient>();
            AddIngredientCommand = new DelegateCommand(AddIngredient);
            RemoveIngredientCommand = new DelegateCommand<Ingredient>(RemoveIngredient);
            ClearCommand = new DelegateCommand(ClearAll);

            // 添加初始成分
            AddInitialIngredients();
            UpdateStatistics();

            // 监听成分列表变化
            Ingredients.CollectionChanged += (s, e) =>
            {
                UpdateStatistics();
                CalculateWeights();
            };
        }

        public ObservableCollection<Ingredient> Ingredients
        {
            get => _ingredients;
            set => SetProperty(ref _ingredients, value);
        }

        public double TotalWeight
        {
            get => _totalWeight;
            set
            {
                SetProperty(ref _totalWeight, value);
                CalculateWeights(); // 总重变化时自动计算
            }
        }

        public string ValidationMessage
        {
            get => _validationMessage;
            set => SetProperty(ref _validationMessage, value);
        }

        public int IngredientCount
        {
            get => _ingredientCount;
            set => SetProperty(ref _ingredientCount, value);
        }

        public double TotalPercentageValue
        {
            get => _totalPercentageValue;
            set => SetProperty(ref _totalPercentageValue, value);
        }

        public DelegateCommand AddIngredientCommand { get; }
        public DelegateCommand<Ingredient> RemoveIngredientCommand { get; }
        public DelegateCommand ClearCommand { get; }

        private void AddInitialIngredients()
        {
            Ingredients.Add(new Ingredient { Name = "成分A", Percentage = 40 });
            Ingredients.Add(new Ingredient { Name = "成分B", Percentage = 35 });
            Ingredients.Add(new Ingredient { Name = "成分C", Percentage = 25 });

            // 监听每个成分的属性变化
            foreach (var ingredient in Ingredients)
            {
                ingredient.PropertyChanged += (s, e) =>
                {
                    if (e.PropertyName == nameof(Ingredient.Percentage) ||
                        e.PropertyName == nameof(Ingredient.Name))
                    {
                        CalculateWeights();
                    }
                };
            }
        }

        private void AddIngredient()
        {
            var newIngredient = new Ingredient { Name = "新成分", Percentage = 0 };

            // 监听新成分的属性变化
            newIngredient.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(Ingredient.Percentage) ||
                    e.PropertyName == nameof(Ingredient.Name))
                {
                    CalculateWeights();
                }
            };

            Ingredients.Add(newIngredient);
        }

        private void RemoveIngredient(Ingredient ingredient)
        {
            if (ingredient != null)
            {
                Ingredients.Remove(ingredient);
            }
        }

        private void CalculateWeights()
        {
            if (TotalWeight <= 0)
            {
                // 总重为0或负数时，清空所有成分的重量
                foreach (var ingredient in Ingredients)
                {
                    ingredient.Weight = 0;
                }
                ValidationMessage = "";
                UpdateStatistics();
                return;
            }

            double totalPercentage = Ingredients.Sum(i => i.Percentage);

            // 验证占比总和是否为100%
            bool isValid = IsPercentageValid(totalPercentage);

            if (!isValid)
            {
                ValidationMessage = "错误：各成分占比之和必须为100%！";
                // 验证失败时，清空所有成分的重量
                foreach (var ingredient in Ingredients)
                {
                    ingredient.Weight = 0;
                }
                UpdateStatistics();
                return;
            }

            // 验证通过时，清空错误信息
            ValidationMessage = "";

            // 计算各成分重量
            foreach (var ingredient in Ingredients)
            {
                ingredient.Weight = TotalWeight * ingredient.Percentage / 100;
            }

            UpdateStatistics();
        }

        private void UpdateStatistics()
        {
            IngredientCount = Ingredients.Count;

            double total = Ingredients.Sum(i => i.Percentage);
            TotalPercentageValue = total;
        }

        private bool IsPercentageValid(double totalPercentage)
        {
            return System.Math.Abs(totalPercentage - 100) < 0.01;
        }

        private void ClearAll()
        {
            Ingredients.Clear();
            TotalWeight = 0;
            ValidationMessage = "";
            AddInitialIngredients();
        }
    }
}

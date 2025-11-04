using Newtonsoft.Json;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
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



namespace RAM100_PRISM_2.Views
{
    /// <summary>
    /// PresetManager.xaml 的交互逻辑
    /// </summary>
    /// 

    public class RelayCommand : ICommand
    {
        private readonly Action<object> _execute;
        private readonly Predicate<object> _canExecute;

        public RelayCommand(Action<object> execute, Predicate<object> canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        public bool CanExecute(object parameter) => _canExecute?.Invoke(parameter) ?? true;

        public void Execute(object parameter) => _execute(parameter);

        public event EventHandler CanExecuteChanged
        {
            add => CommandManager.RequerySuggested += value;
            remove => CommandManager.RequerySuggested -= value;
        }
    }

    public partial class PresetManager : UserControl, INotifyPropertyChanged
    {

        public PresetManager()
        {
            InitializeComponent();
            DataContext = this;

            // 初始化命令
            
            NewPresetCommand = new RelayCommand(NewPreset);
            DeletePresetCommand = new RelayCommand(DeletePreset, CanDeletePreset);
            AddTimeSlotCommand = new RelayCommand(AddTimeSlot, CanAddTimeSlot);
            DeleteTimeSlotCommand = new RelayCommand(DeleteTimeSlot, CanDeleteTimeSlot);
            SavePresetCommand = new RelayCommand(SavePreset, CanSavePreset);
            LoadPresetCommand = new RelayCommand(LoadPreset);
            ApplyPresetCommand = new RelayCommand(ApplyPreset, CanApplyPreset);
            RefreshPresetsCommand = new RelayCommand(RefreshPresets);

            // 初始化预设列表
            Presets = new ObservableCollection<DevicePreset>();

            // 绑定选择变化事件
            this.Loaded += (s, e) => 
            {
                if (PresetListBox != null)
                {
                    PresetListBox.SelectionChanged += (s, e) => OnPropertyChanged(nameof(IsPresetSelected));
                }
                
                if (TimeSlotsGrid != null)
                {
                    TimeSlotsGrid.SelectionChanged += (s, e) =>
                    {
                        SelectedTimeSlot = TimeSlotsGrid.SelectedItem as TimeSlot;
                        OnPropertyChanged(nameof(IsTimeSlotSelected));
                    };
                }
                
                RefreshPresets();
            };
        }

        private ObservableCollection<DevicePreset> _presets;
        public ObservableCollection<DevicePreset> Presets
        {
            get => _presets;
            set
            {
                _presets = value;
                OnPropertyChanged();
            }
        }

        private DevicePreset _selectedPreset;
        public DevicePreset SelectedPreset
        {
            get => _selectedPreset;
            set
            {
                _selectedPreset = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsPresetSelected));
            }
        }

        private TimeSlot _selectedTimeSlot;
        public TimeSlot SelectedTimeSlot
        {
            get => _selectedTimeSlot;
            set
            {
                _selectedTimeSlot = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(IsTimeSlotSelected));
            }
        }

        public bool IsPresetSelected => SelectedPreset != null;
        public bool IsTimeSlotSelected => SelectedTimeSlot != null;

        public ICommand NewPresetCommand { get; }
        public ICommand DeletePresetCommand { get; }
        public ICommand AddTimeSlotCommand { get; }
        public ICommand DeleteTimeSlotCommand { get; }
        public ICommand SavePresetCommand { get; }
        public ICommand LoadPresetCommand { get; }
        public ICommand ApplyPresetCommand { get; }

        public ICommand RefreshPresetsCommand { get; }
        // 刷新预设列表

        private void TimeSlotsGrid_LoadingRow(object sender, DataGridRowEventArgs e)
        {
            // 设置行号（从1开始）
            e.Row.Tag = (e.Row.GetIndex() + 1).ToString();
        }
        private void RefreshPresets(object parameter = null)
        {
            try
            {
                Presets.Clear();
                // 构建presets文件夹路径
                string presetsFolder = Path.Combine(Directory.GetCurrentDirectory(), "presets");
                // 确保文件夹存在
                Directory.CreateDirectory(presetsFolder);
                // 获取当前目录下的所有JSON预设文件
                var presetFiles = Directory.GetFiles(presetsFolder, "*.json");

                foreach (var file in presetFiles)
                {
                    try
                    {
                        var json = File.ReadAllText(file);
                        var preset = JsonConvert.DeserializeObject<DevicePreset>(json);

                        if (preset != null)
                        {
                            // 确保TimeSlots不为null
                            preset.TimeSlots = preset.TimeSlots ?? new ObservableCollection<TimeSlot>();
                            Presets.Add(preset);
                        }
                    }
                    catch (Exception ex)
                    {
                        // 跳过无法解析的文件
                        System.Diagnostics.Debug.WriteLine($"无法加载预设文件 {file}: {ex.Message}");
                    }
                }

                if (Presets.Any())
                {
                    SelectedPreset = Presets.First();
                    System.Diagnostics.Debug.WriteLine($"已加载 {Presets.Count} 个预设");
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("未找到预设文件");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"加载预设时出错: {ex.Message}");
                MessageBox.Show($"加载预设时出错: {ex.Message}", "错误", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }


        private void NewPreset(object parameter)
        {
            var newPreset = new DevicePreset
            {
                Name = $"预设 {Presets.Count + 1}",
                TimeSlots = new ObservableCollection<TimeSlot>()
            };
            Presets.Add(newPreset);
            SelectedPreset = newPreset;
        }

        private bool CanDeletePreset(object parameter) => IsPresetSelected;

        private void DeletePreset(object parameter)
        {
            if (SelectedPreset != null)
            {
                Presets.Remove(SelectedPreset);
                SelectedPreset = Presets.FirstOrDefault();
            }
        }

        private bool CanAddTimeSlot(object parameter) => IsPresetSelected;

        private void AddTimeSlot(object parameter)
        {
            if (SelectedPreset != null)
            {
                var newSlot = new TimeSlot
                {
                    Duration = 5,
                    Frequency = 60,
                    Intensity = 0.01
                };
                SelectedPreset.TimeSlots.Add(newSlot);
                SelectedTimeSlot = newSlot;
            }
        }

        private bool CanDeleteTimeSlot(object parameter) => IsTimeSlotSelected;

        private void DeleteTimeSlot(object parameter)
        {
            if (SelectedPreset != null && SelectedTimeSlot != null)
            {
                SelectedPreset.TimeSlots.Remove(SelectedTimeSlot);
                SelectedTimeSlot = SelectedPreset.TimeSlots.FirstOrDefault();
            }
        }

        private bool CanSavePreset(object parameter) => IsPresetSelected && SelectedPreset.TimeSlots.Any();

        private void SavePreset(object parameter)
        {
            try
            {
                // 构建presets文件夹路径
                string presetsFolder = Path.Combine(Directory.GetCurrentDirectory(), "presets");
                // 确保文件夹存在
                Directory.CreateDirectory(presetsFolder);

                // 构建完整文件路径
                var fileName = $"{SelectedPreset.Name}.json";
                var filePath = Path.Combine(presetsFolder, fileName);

                // 序列化并保存文件
                var json = JsonConvert.SerializeObject(SelectedPreset, Formatting.Indented);
                File.WriteAllText(filePath, json);
                MessageBox.Show($"预设 '{SelectedPreset.Name}' 已保存到 presets 文件夹！");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"保存失败: {ex.Message}");
            }
        }

        private void LoadPreset(object parameter)
        {
            try
            {
                // 构建presets文件夹路径
                string presetsFolder = Path.Combine(Directory.GetCurrentDirectory(), "presets");
                // 确保文件夹存在
                Directory.CreateDirectory(presetsFolder);

                var dialog = new Microsoft.Win32.OpenFileDialog
                {
                    Filter = "JSON文件 (*.json)|*.json|所有文件 (*.*)|*.*",
                    Title = "选择预设文件",
                    // 设置初始目录为presets文件夹
                    InitialDirectory = presetsFolder
                };

                if (dialog.ShowDialog() == true)
                {
                    var json = File.ReadAllText(dialog.FileName);
                    var preset = JsonConvert.DeserializeObject<DevicePreset>(json);

                    if (preset != null)
                    {
                        Presets.Add(preset);
                        SelectedPreset = preset;
                        MessageBox.Show($"预设 '{preset.Name}' 加载成功！");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"加载失败: {ex.Message}");
            }
        }

        private bool CanApplyPreset(object parameter) => IsPresetSelected && SelectedPreset.TimeSlots.Any();

        private void ApplyPreset(object parameter)
        {
            MessageBox.Show($"预设 '{SelectedPreset.Name}' 已应用！\n" +
                           $"包含 {SelectedPreset.TimeSlots.Count} 个时间段");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        
    }

    public class DevicePreset : INotifyPropertyChanged
    {
        private string _name;
        private ObservableCollection<TimeSlot> _timeSlots;

        public string Name
        {
            get => _name;
            set
            {
                _name = value;
                OnPropertyChanged();
            }
        }

        public ObservableCollection<TimeSlot> TimeSlots
        {
            get => _timeSlots;
            set
            {
                _timeSlots = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    public class TimeSlot : INotifyPropertyChanged
    {
        private int _duration;
        private double _frequency;
        private double _intensity;

        public int Duration
        {
            get => _duration;
            set
            {
                _duration = value;
                OnPropertyChanged();
            }
        }

        public double Frequency
        {
            get => _frequency;
            set
            {
                _frequency = value;
                OnPropertyChanged();
            }
        }

        public double Intensity
        {
            get => _intensity;
            set
            {
                _intensity = value;
                OnPropertyChanged();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}

using Dct.Models;
using Dct.Models.Entity;
using Dct.Models.Repository;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Dct.UI.Alarm.ViewModels.Product
{
    public class ProductDataViewModel : NotifyPropertyChangedObject
    {
        private readonly ProductResultRepository _productDataRepository;

        public ProductDataViewModel()
        {

            QueryCommand = new RelayCommand(ExecuteQueryCommand);
            ExportCommand = new RelayCommand(ExecuteExportCommand);
            ProductDatas = new ObservableCollection<ProductResultEntity>();
            _productDataRepository = DbManager.Instance.GetRepository<ProductResultRepository>();

            StartTime = DateTime.Now.AddDays(-1);
            EndTime = DateTime.Now;
            MaxCount = 10;
        }

        /// <summary>
        /// 开始时间
        /// </summary>
        private DateTime _startTime;
        public DateTime StartTime
        {
            get { return _startTime; }
            set { _startTime = value; NotifyPropertyChanged("StartTime"); }
        }

        /// <summary>
        /// 结束时间
        /// </summary>
        private DateTime _endTime;
        public DateTime EndTime
        {
            get { return _endTime; }
            set { _endTime = value; NotifyPropertyChanged("EndTime"); }
        }

        private ObservableCollection<ProductResultEntity> _productDatas;
        public ObservableCollection<ProductResultEntity> ProductDatas
        {
            get { return _productDatas; }
            set { _productDatas = value; NotifyPropertyChanged("ProductDatas"); }
        }

        private double _totalCount;
        public double TotalCount
        {
            get { return _totalCount; }
            set { _totalCount = value; NotifyPropertyChanged("TotalCount"); }
        }

        private double _passCount;
        public double PassCount
        {
            get { return _passCount; }
            set { _passCount = value; NotifyPropertyChanged("PassCount"); }
        }      

        private double _uph;
        public double UPH
        {
            get { return _uph; }
            set { _uph = value; NotifyPropertyChanged("UPH"); }
        }


        private string _passRate;
        public string PassRate
        {
            get { return _passRate; }
            set { _passRate = value; NotifyPropertyChanged("PassRate"); }
        }

        private string _searchCode;
        public string SearchCode
        {
            get { return _searchCode; }
            set { _searchCode = value; NotifyPropertyChanged("SearchCode"); }
        }

        private double _maxCount;
        public double MaxCount
        {
            get { return _maxCount; }
            set { _maxCount = value; NotifyPropertyChanged("MaxCount"); }
        }
        public ICommand QueryCommand { get; }
        public ICommand ExportCommand { get; }

        private void ExecuteQueryCommand()
        {
            if (_productDataRepository.QueryHistory(StartTime, EndTime, SearchCode, out var data, out _))
            {
                ProductDatas.Clear();
                data.ForEach(item =>
                {
                    ProductDatas.Add(item);
                });
                TotalCount = ProductDatas.Count();
                PassCount = ProductDatas.Where(a=>a.Result == "Pass").Count();
                PassRate = "100.0";
                if (TotalCount != 0)
                {
                    PassRate = Math.Round(100 * PassCount / TotalCount, 1).ToString();
                }

                UPH = 0;
                if (ProductDatas!=null && ProductDatas.Count()>0) {
                    UPH = Math.Round(TotalCount / (ProductDatas.Select(a=>a.StartTime).Max() - ProductDatas.Select(a => a.StartTime).Min()).TotalHours, 1);
                }
                InitSeriesData();
            }
        }


        /// <summary>
        /// 报警类型饼状图
        /// </summary>
        private SeriesCollection _cartesianSeriesCollection;
        public SeriesCollection CartesianSeriesCollection
        {
            get { return _cartesianSeriesCollection; }
            set { _cartesianSeriesCollection = value; NotifyPropertyChanged("CartesianSeriesCollection"); }
        }


        private ObservableCollection<string> _timeLists { get; set; }
        public ObservableCollection<string> TimeLists
        {
            get { return _timeLists; }
            set { _timeLists = value; NotifyPropertyChanged("TimeLists"); }
        }


        protected void InitSeriesData()
        {
            if (ProductDatas == null) return;
            List<ProductDataPerHour> productDataPerHours = new List<ProductDataPerHour>();
            var startTime1 = StartTime;
            var endTime1 = EndTime;

            while (startTime1 < endTime1)
            {
                var tmeEndTime = startTime1.AddHours(1);
                var tmp = ProductDatas.Where(a => a.StartTime >= startTime1 && a.StartTime < tmeEndTime).ToList();
                productDataPerHours.Add(new ProductDataPerHour()
                {
                    StartTime = startTime1,
                    EndTime = tmeEndTime,
                    PassQuatity = tmp.Where(a => a.Result == "Pass").Count(),
                    TotalQuatity = tmp.Count()
                });
                startTime1 = tmeEndTime;
            }

            // Initialize AlarmCodes and AlarmSeries
            TimeLists = new ObservableCollection<string>(productDataPerHours.Select(g => g.StartTime.ToString("HH:mm:ss")));

            CartesianSeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "ProductData",
                    ColumnPadding = 1,
                    Values = new ChartValues<double>(productDataPerHours.Select(g => g.TotalQuatity))
                }
            };
            var maxCount = productDataPerHours.Max(g => g.TotalQuatity);
            MaxCount = maxCount * 1.5;
        }

        private void ExecuteExportCommand()
        {
            if (ProductDatas == null)
            {

                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",  // 只允许保存为CSV格式
                DefaultExt = ".csv",
                FileName = "ProductDatas.csv",  // 默认文件名
                Title = "Save ProductDatas as CSV"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                // 使用StreamWriter写入CSV文件
                using (var writer = new StreamWriter(filePath, false))
                {
                    // 写入CSV标题行
                    writer.WriteLine("StartTime,Code,Result");

                    foreach (var alarm in ProductDatas)
                    {
                        // 将每个字段按照逗号分隔，并写入CSV
                        writer.WriteLine($"{alarm.StartTime:yyyy-MM-dd HH:mm:ss},{alarm.Code},{alarm.Result}");
                    }
                }

            }
        }

    }

    public class ProductDataPerHour : NotifyPropertyChangedObject
    {
        private double _totalQuatity;
        public double TotalQuatity
        {
            get { return _totalQuatity; }
            set { _totalQuatity = value; NotifyPropertyChanged("TotalQuatity"); }
        }

        private double _passQuatity;
        public double PassQuatity
        {
            get { return _passQuatity; }
            set { _passQuatity = value; NotifyPropertyChanged("PassQuatity"); }
        }

        public double Yield
        {
            get
            {
                if (TotalQuatity != 0)
                {
                    return Math.Round(PassQuatity / TotalQuatity, 4);
                }
                else
                {
                    return 1;
                }
            }
        }

        private DateTime _startTime;
        public DateTime StartTime
        {
            get { return _startTime; }
            set { _startTime = value; NotifyPropertyChanged("StartTime"); }
        }

        private DateTime _endTime;
        public DateTime EndTime
        {
            get { return _endTime; }
            set { _endTime = value; NotifyPropertyChanged("EndTime"); }
        }
    }
}

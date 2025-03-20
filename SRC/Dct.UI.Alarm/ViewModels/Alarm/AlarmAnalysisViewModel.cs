using Dct.Models;
using Dct.Models.Repository;
using Dct.UI.Alarm.ViewModels.Product;
using Dct.UI.Alarm.Views.Product;
using LiveCharts;
using LiveCharts.Wpf;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Input;

namespace Dct.UI.Alarm.ViewModels.Alarm
{
    public class AlarmAnalysisViewModel : NotifyPropertyChangedObject
    {
        private readonly AlarmHistoryRepository _alarmHistoryRepository;

        public AlarmAnalysisViewModel()
        {

            QueryCommand = new RelayCommand(ExecuteQueryCommand);
            ExportCommand = new RelayCommand(ExecuteExportCommand);
            AlarmList = new ObservableCollection<AlarmHistory>();
            _alarmHistoryRepository = DbManager.Instance.GetRepository<AlarmHistoryRepository>();

            StartTime = DateTime.Now.AddDays(-1);
            EndTime = DateTime.Now;
            MaxAlarmCount = 10;

            TrendMaxAlarmCount = 10;
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

        private ObservableCollection<AlarmHistory> _alarmList;
        public ObservableCollection<AlarmHistory> AlarmList
        {
            get { return _alarmList; }
            set { _alarmList = value; NotifyPropertyChanged("AlarmList"); }
        }

        private double _totalAlarmCount;
        public double TotalAlarmCount
        {
            get { return _totalAlarmCount; }
            set { _totalAlarmCount = value; NotifyPropertyChanged("TotalAlarmCount"); }
        } 
        
        private double _mtba;
        public double MTBA
        {
            get { return _mtba; }
            set { _mtba = value; NotifyPropertyChanged("MTBA"); }
        }  
        private string _searchCode;
        public string SearchCode
        {
            get { return _searchCode; }
            set { _searchCode = value; NotifyPropertyChanged("SearchCode"); }
        }

        private double _maxAlarmCount;
        public double MaxAlarmCount
        {
            get { return _maxAlarmCount; }
            set { _maxAlarmCount = value; NotifyPropertyChanged("MaxAlarmCount"); }
        }


        private double _trendMaxAlarmCount;
        public double TrendMaxAlarmCount
        {
            get { return _trendMaxAlarmCount; }
            set { _trendMaxAlarmCount = value; NotifyPropertyChanged("TrendMaxAlarmCount"); }
        }


        public ICommand QueryCommand { get; }
        public ICommand ExportCommand { get; }

        private void ExecuteQueryCommand()
        {
            if (_alarmHistoryRepository.QueryAlarmHistory(StartTime, EndTime, SearchCode, out var data, out _))
            {
                AlarmList.Clear();
                data.ForEach(alarmHistory =>
                {
                    AlarmList.Add(new AlarmHistory()
                    {
                        Code = alarmHistory.Code,
                        Type = alarmHistory.Type,
                        StationID = alarmHistory.StationID,
                        Description = alarmHistory.Description,
                        StartTime = alarmHistory.StartTime,
                        Duration = alarmHistory.Duration
                    });
                });
                TotalAlarmCount = AlarmList.Count();

                if (TotalAlarmCount > 0)
                {
                    var totalhour = (EndTime - StartTime).TotalHours;
                    var totalAlarmhour = data.Sum(a => a.Duration.TotalHours);
                    MTBA = Math.Round((EndTime - StartTime).TotalHours, 2);
                }
                else
                {
                    MTBA = Math.Round((EndTime - StartTime).TotalHours, 2);
                }
                InitSeriesData();
                InitTrendSeriesData();
            }
        }

        private void InitTrendSeriesData()
        {
            if (AlarmList == null || AlarmList.Count == 0) return;
            List<AlarmDataPerHour> alarmTrendDataPerHours = new List<AlarmDataPerHour>();
            var startTime1 = StartTime;
            var endTime1 = EndTime;

            while (startTime1 < endTime1)
            {
                var tmeEndTime = startTime1.AddHours(1);
                var tmp = AlarmList.Where(a => a.StartTime >= startTime1 && a.StartTime < tmeEndTime).ToList();
                alarmTrendDataPerHours.Add(new AlarmDataPerHour()
                {
                    StartTime = startTime1,
                    EndTime = tmeEndTime,
                    TotalQuatity = tmp.Count()
                });
                startTime1 = tmeEndTime;
            }

            // Initialize AlarmCodes and AlarmSeries
            TrendTimeLists = new ObservableCollection<string>(alarmTrendDataPerHours.Select(g => g.StartTime.ToString("HH:mm:ss")));

            TrendCartesianSeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Alarm Trend",
                    ColumnPadding = 1,
                    Values = new ChartValues<double>(alarmTrendDataPerHours.Select(g => g.TotalQuatity))
                }
            };
            var maxCount = alarmTrendDataPerHours.Max(g => g.TotalQuatity);
            TrendMaxAlarmCount = maxCount * 1.5;
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
        
        /// <summary>
        /// 报警类型饼状图
        /// </summary>
        private SeriesCollection _trendAartesianSeriesCollection;
        public SeriesCollection TrendCartesianSeriesCollection
        {
            get { return _trendAartesianSeriesCollection; }
            set { _trendAartesianSeriesCollection = value; NotifyPropertyChanged("TrendCartesianSeriesCollection"); }
        }


        private ObservableCollection<string> _alarmCodes { get; set; }
        public ObservableCollection<string> AlarmCodes
        {
            get { return _alarmCodes; }
            set { _alarmCodes = value; NotifyPropertyChanged("AlarmCodes"); }
        }
        

        private ObservableCollection<string> _trendTimeLists { get; set; }
        public ObservableCollection<string> TrendTimeLists
        {
            get { return _trendTimeLists; }
            set { _trendTimeLists = value; NotifyPropertyChanged("TrendTimeLists"); }
        }


        protected void InitSeriesData()
        {
            if (AlarmList == null|| AlarmList.Count == 0) return;
            var groupedAlarms = AlarmList.GroupBy(a => a.Code)
                                 .Select(g => new { Code = g.Key, Count = g.Count() })
                                 .ToList();

            // Initialize AlarmCodes and AlarmSeries
            AlarmCodes = new ObservableCollection<string>(groupedAlarms.Select(g => g.Code));

            CartesianSeriesCollection = new SeriesCollection
            {
                new ColumnSeries
                {
                    Title = "Alarms",
                    ColumnPadding = 1,
                    Values = new ChartValues<int>(groupedAlarms.Select(g => g.Count))
                }
            };
            int maxCount = groupedAlarms.Max(g => g.Count);
            MaxAlarmCount = maxCount * 1.5;
        }

        private void ExecuteExportCommand()
        {
            if (AlarmList == null) {

                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",  // 只允许保存为CSV格式
                DefaultExt = ".csv",
                FileName = "alarms.csv",  // 默认文件名
                Title = "Save Alarms as CSV"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                // 使用StreamWriter写入CSV文件
                using (var writer = new StreamWriter(filePath))
                {
                    // 写入CSV标题行
                    writer.WriteLine("StartTime,StationID,AlarmCode,Type,AlarmContent");

                    // 写入每个Alarm的数据行
                    foreach (var alarm in AlarmList)
                    {
                        // 将每个字段按照逗号分隔，并写入CSV
                        writer.WriteLine($"{alarm.StartTime:yyyy-MM-dd HH:mm:ss},{alarm.StationID},{alarm.Code},{alarm.Type},{alarm.Description.Replace(",", ".")}");
                    }
                }
                
            }
        }

    }

    public class AlarmDataPerHour : NotifyPropertyChangedObject
    {
        private double _totalQuatity;
        public double TotalQuatity
        {
            get { return _totalQuatity; }
            set { _totalQuatity = value; NotifyPropertyChanged("TotalQuatity"); }
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

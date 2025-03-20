using LiveCharts.Wpf;
using LiveCharts;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Dct.Models.Entity;

namespace Dct.UI.Alarm.ViewModels
{

    public class AlarmGroupModel : NotifyPropertyChangedObject
    {
        /// <summary>
        /// 报警类型
        /// </summary>
        private string _code;
        public string Code
        {
            get { return _code; }
            set { _code = value;  NotifyPropertyChanged("Code"); }
        }

        /// <summary>
        /// 时间
        /// </summary>
        private double _time;
        public double Time
        {
            get { return _time; }
            set { _time = value; NotifyPropertyChanged("Time"); }
        }

        /// <summary>
        /// 次数
        /// </summary>
        private int _count;
        public int Count
        {
            get { return _count; }
            set { _count = value; NotifyPropertyChanged("Count"); }
        }

        /// <summary>
        /// 时间占比
        /// </summary>
        private double _percent;
        public double Percent
        {
            get { return _percent; }
            set { _percent = value; NotifyPropertyChanged("Percent"); }
        }
    }


    public class AlarmTimeStatisticViewModel: NotifyPropertyChangedObject
    {
        protected AlarmTimeStatisticViewModel()
        {
            CurrentDayAlarmVMs = new ObservableCollection<AlarmHistoryEntity>();
        }

        private ObservableCollection<AlarmHistoryEntity> _currentDayAlarmVMs;
        public ObservableCollection<AlarmHistoryEntity> CurrentDayAlarmVMs
        {
            get { return _currentDayAlarmVMs; }
            set { _currentDayAlarmVMs = value; NotifyPropertyChanged("CurrentDayAlarmVMs"); }
        }

        /// <summary>
        /// 报警类型饼状图
        /// </summary>
        private SeriesCollection _pieSeriesCollection;
        public SeriesCollection PieSeriesCollection
        {
            get { return _pieSeriesCollection; }
            set { _pieSeriesCollection = value;  NotifyPropertyChanged("PieSeriesCollection"); }
        }

        /// <summary>
        /// 报警按组统计
        /// </summary>
        private ObservableCollection<AlarmGroupModel> _alarmGroupList;
        public ObservableCollection<AlarmGroupModel> AlarmGroupList
        {
            get { return _alarmGroupList; }
            set { _alarmGroupList = value; NotifyPropertyChanged("AlarmGroupList"); }
        }


        protected void InitPieSeriesData()
        {
            if (CurrentDayAlarmVMs == null) return;
            var alarmgroupList = CurrentDayAlarmVMs.GroupBy(x => x.Code)
                .Select(x => new { x.Key, Count = x.Count(), Time = x.Sum(t => t.Duration.TotalSeconds), }).ToList();
            if (alarmgroupList == null) return;
            alarmgroupList = alarmgroupList.OrderByDescending(x => x.Time).Take(10).ToList();//排序取前10
            int m = 0;
            double sumTime = alarmgroupList.Sum(t => t.Time);
            AlarmGroupList = new ObservableCollection<AlarmGroupModel>();
            foreach (var alarmgroup in alarmgroupList)
            {
                var y = m % 10;
                var model = new AlarmGroupModel()
                {
                    Code = alarmgroup.Key,
                    Time = Math.Round((double)(alarmgroup.Time / 60), 1),
                    Count = alarmgroup.Count,
                    Percent = sumTime == 0 ? 0 : Math.Round((alarmgroup.Time / sumTime) * 100, 2),
                };
                m++;
                AlarmGroupList.Add(model);
            }
            PieSeriesCollection = new SeriesCollection();
            ChartValues<double> chartvalue = new ChartValues<double>();
            foreach (var item in AlarmGroupList)
            {
                chartvalue = new ChartValues<double>();
                chartvalue.Add(item.Percent);
                PieSeries series = new PieSeries();
                series.DataLabels = true;
                series.Values = chartvalue;
                series.Title = item.Code;
                PieSeriesCollection.Add(series);
            }
        }
    }
}

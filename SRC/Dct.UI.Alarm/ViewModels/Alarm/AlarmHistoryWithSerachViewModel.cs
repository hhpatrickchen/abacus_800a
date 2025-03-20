using Dct.Models;
using Dct.Models.Entity;
using Dct.Models.Repository;
using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Dct.UI.Alarm.ViewModels
{
    public class AlarmHistoryWithSerachViewModel: NotifyPropertyChangedObject
    {
        private readonly AlarmHistoryRepository _alarmHistoryRepository;

        public AlarmHistoryWithSerachViewModel()
        {
            AlarmList = new ObservableCollection<AlarmHistory>();
            QueryCommand = new RelayCommand(ExecuteQueryCommand);

            _alarmHistoryRepository = DbManager.Instance.GetRepository<AlarmHistoryRepository>();

            StartTime = DateTime.Now.AddDays(-1);
            EndTime = DateTime.Now;
        }

        private ObservableCollection<AlarmHistory> _alarmList;
        public ObservableCollection<AlarmHistory> AlarmList
        {
            get { return _alarmList; }
            set { _alarmList = value; NotifyPropertyChanged("AlarmList"); }
        }

        private string _title;
        public string Title
        {
            get { return _title; }
            set { _title = value; NotifyPropertyChanged("Title"); }
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

        public ICommand QueryCommand { get; }

        private void ExecuteQueryCommand()
        {
            AlarmList.Clear();
            if (StartTime > EndTime) {
                return;
            }
            if (_alarmHistoryRepository.QueryAlarmHistory(StartTime, EndTime, "", out var data, out _))
            {
                data.OrderBy(a=>a.StartTime).ToList().ForEach(alarm => { 
                    AlarmList.Add(new AlarmHistory() { 
                        Code = alarm.Code, 
                        Description = alarm.Description, 
                        StartTime = alarm.StartTime, 
                        Duration = alarm.Duration 
                    }); 
                });

                if (data.Count > 0)
                {
                    var totalSec = (EndTime - StartTime).TotalHours;
                    var totalAlarmSec = data.Sum(a => a.Duration.TotalHours);
                    var mtbf = Math.Round((totalSec - totalAlarmSec) / (data.Count), 2);
                    Title = $"MTBA: {mtbf} H";
                } else
                {
                    Title = $"MTBA: {(EndTime - StartTime).TotalHours} H";
                }
            }
        }
    }

    public class AlarmHistory: NotifyPropertyChangedObject
    {
        private string _code;
        public string Code
        {
            get { return _code; }
            set { _code = value; NotifyPropertyChanged("Code"); }
        }

        private string _type;
        public string Type
        {
            get { return _type; }
            set { _type = value; NotifyPropertyChanged("Type"); }
        }

        private string _stationId;
        public string StationID
        {
            get { return _stationId; }
            set { _stationId = value; NotifyPropertyChanged("StationID"); }
        }


        private string _description;

        public string Description
        {
            get { return _description; }
            set { _description = value; NotifyPropertyChanged("Description"); }
        }


        private DateTime _startTime;
        public DateTime StartTime
        {
            get { return _startTime; }
            set { _startTime = value; NotifyPropertyChanged("StartTime"); }
        }

        private TimeSpan _duration;
        public TimeSpan Duration
        {
            get { return _duration; }
            set { _duration = value; NotifyPropertyChanged("Duration"); }
        }



    }
}

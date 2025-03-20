using Dct.Models;
using Dct.Models.Repository;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace Dct.UI.Alarm.ViewModels
{
    public class AlarmTimeStatisticWithSearchViewModel: AlarmTimeStatisticViewModel
    {

        private readonly AlarmHistoryRepository _alarmHistoryRepository;

        public AlarmTimeStatisticWithSearchViewModel() 
        {
            QueryCommand = new RelayCommand(ExecuteQueryCommand);

            _alarmHistoryRepository = DbManager.Instance.GetRepository<AlarmHistoryRepository>();

            StartTime = DateTime.Now.AddDays(-1);
            EndTime = DateTime.Now;
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
            set { _endTime = value;  NotifyPropertyChanged("EndTime"); }
        }

        public ICommand QueryCommand { get; }

        private void ExecuteQueryCommand()
        {
            CurrentDayAlarmVMs.Clear();
            if (_alarmHistoryRepository.QueryAlarmHistory(StartTime, EndTime, "", out var data, out _))
            {
                data.ForEach(alarmHistory => {
                    CurrentDayAlarmVMs.Add(alarmHistory);
                });

                InitPieSeriesData();
            }
        }
    }
}

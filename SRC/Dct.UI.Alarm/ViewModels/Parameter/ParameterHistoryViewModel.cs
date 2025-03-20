using Dct.Models.Repository;
using Dct.Models;
using Dct.UI.Alarm.ViewModels.Product;
using LiveCharts.Wpf;
using LiveCharts;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;
using Dct.Models.Entity;

namespace Dct.UI.Alarm.ViewModels.Parameter
{
    public class ParameterHistoryViewModel: NotifyPropertyChangedObject
    {
        private readonly ParamterChangeHistoryRepository _repository;

        public ParameterHistoryViewModel()
        {

            QueryCommand = new RelayCommand(ExecuteQueryCommand);
            ExportCommand = new RelayCommand(ExecuteExportCommand);
            Datas = new ObservableCollection<ParameterChangeHistoryEntity>();
            _repository = DbManager.Instance.GetRepository<ParamterChangeHistoryRepository>();

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
            set { _endTime = value; NotifyPropertyChanged("EndTime"); }
        }

        private ObservableCollection<ParameterChangeHistoryEntity> _datas;
        public ObservableCollection<ParameterChangeHistoryEntity> Datas
        {
            get { return _datas; }
            set { _datas = value; NotifyPropertyChanged("Datas"); }
        }

        public ICommand QueryCommand { get; }
        public ICommand ExportCommand { get; }

        private void ExecuteQueryCommand()
        {
            if (_repository.QueryHistory(StartTime, EndTime, out var data, out _))
            {
                Datas.Clear();
                data.ForEach(item =>
                {
                    Datas.Add(item);
                });
               
            }
        }


        private void ExecuteExportCommand()
        {
            if (Datas == null)
            {

                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "CSV Files (*.csv)|*.csv",  // 只允许保存为CSV格式
                DefaultExt = ".csv",
                FileName = "ParameterHistory.csv",  // 默认文件名
                Title = "Save ParameterHistory as CSV"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                string filePath = saveFileDialog.FileName;

                // 使用StreamWriter写入CSV文件
                using (var writer = new StreamWriter(filePath, false))
                {
                    // 写入CSV标题行
                    writer.WriteLine("Time,StationID,Item,OldValue,NewValue,UserName");

                    foreach (var alarm in Datas)
                    {
                        // 将每个字段按照逗号分隔，并写入CSV
                        writer.WriteLine($"{alarm.ChangeTime:yyyy-MM-dd HH:mm:ss},{alarm.StationID},{alarm.Name},{alarm.OldValue},{alarm.NewValue},{alarm.UserName}");
                    }
                }

            }
        }

    }
}

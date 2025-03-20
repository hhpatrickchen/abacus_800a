using Dct.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models.Repository
{
    public class AlarmHistoryRepository : BaseRepository<AlarmHistoryEntity>
    {
        public AlarmHistoryRepository(IFreeSql fsql) : base(fsql)
        {
        }

        public bool QueryAlarmHistory(DateTime StartTime, DateTime EndTime, string SearchCode, out List<AlarmHistoryEntity> data, out string errorMsg)
        {
            data = new List<AlarmHistoryEntity>();
            errorMsg = string.Empty;

            try
            {
                var query = this.Select;
                if (!string.IsNullOrEmpty(SearchCode))
                {
                    query = query.Where(a => a.Code == SearchCode);
                }
                data = query.Where(a => a.StartTime < EndTime && a.StartTime >= StartTime).ToList();
                return true;
            }
            catch (Exception ex) 
            {
                ErrorLog(ex);
                errorMsg = ex.GetExceptionMessage();
                return false;
            }

        }

        public bool ClearAllAlarm(out string errorMsg) 
        {
            errorMsg = string.Empty;
            try
            {
               return this.Update(alarm => alarm.State == AlarmState.Set, alarm => new AlarmHistoryEntity { State = AlarmState.Clear, EndTime = DateTime.Now }) >= 0;
            }
            catch (Exception ex)
            {
                ErrorLog(ex);
                errorMsg = ex.GetExceptionMessage();
                return false;
            }
        }

        public bool SetAlarm(AlarmHistoryEntity newAlarm, out string errorMsg) 
        {
            errorMsg = string.Empty;
            try
            {
                newAlarm.State = AlarmState.Set;
                newAlarm.StartTime = DateTime.Now;

                newAlarm = this.Insert(newAlarm);

                return true;
            }
            catch (Exception ex)
            {
                ErrorLog(ex);
                errorMsg = ex.GetExceptionMessage();
                return false;
            }
        }

        public bool ClearAlarm(AlarmHistoryEntity oldAlarm, out string errorMsg)
        {
            errorMsg = string.Empty;
            try
            {
                this.Update(alarm => alarm.State == AlarmState.Set && alarm.Code == oldAlarm.Code, alarm => new AlarmHistoryEntity { State = AlarmState.Clear, EndTime = DateTime.Now });

                return true;
            }
            catch (Exception ex)
            {
                ErrorLog(ex);
                errorMsg = ex.GetExceptionMessage();
                return false;
            }
        }

    }
}

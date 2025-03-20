using Dct.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models.Repository
{
    public class FingerEngagementRepository : BaseRepository<FingerEngagementEntity>
    {
        public FingerEngagementRepository(IFreeSql fsql) : base(fsql)
        {
        }

        public bool Insert(FingerEngagementEntity entity, out string errMsg)
        {
            try
            {
                errMsg = string.Empty;

                entity = this.Insert(entity);
                return true;
            }
            catch (Exception ex) {
                errMsg = ex.Message;
                return false;
            }
        }

        public bool QueryHistory(DateTime StartTime, DateTime EndTime, string SearchShutterName, out List<FingerEngagementEntity> data, out string errorMsg)
        {
            data = new List<FingerEngagementEntity>();
            errorMsg = string.Empty;

            try
            {
                var query = this.Select;
                if (!string.IsNullOrEmpty(SearchShutterName))
                {
                    query = query.Where(a => a.ShutterName == SearchShutterName);
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

    }
}

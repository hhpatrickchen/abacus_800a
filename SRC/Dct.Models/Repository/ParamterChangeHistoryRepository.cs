using Dct.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models.Repository
{
    public class ParamterChangeHistoryRepository : BaseRepository<ParameterChangeHistoryEntity>
    {
        public ParamterChangeHistoryRepository(IFreeSql fsql) : base(fsql)
        {
        }

        public bool QueryHistory(DateTime StartTime, DateTime EndTime, out List<ParameterChangeHistoryEntity> data, out string errorMsg)
        {
            data = new List<ParameterChangeHistoryEntity>();
            errorMsg = string.Empty;

            try
            {
                var query = this.Select;

                data = query.Where(a => a.ChangeTime < EndTime && a.ChangeTime >= StartTime).ToList();
                return true;
            }
            catch (Exception ex)
            {
                ErrorLog(ex);
                errorMsg = ex.GetExceptionMessage();
                return false;
            }

        }

        public bool Insert(ParameterChangeHistoryEntity entity, out string errorMsg)
        {
            errorMsg = string.Empty;
            try
            {
                this.Insert(entity);

                return true;
            }
            catch (Exception ex) {
                errorMsg = ex.Message; 
                ErrorLog(ex);

                return false;
            }
        }
    }
}

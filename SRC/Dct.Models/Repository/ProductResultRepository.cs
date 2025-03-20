using Dct.Models.Entity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models.Repository
{
    public class ProductResultRepository : BaseRepository<ProductResultEntity>
    {
        public ProductResultRepository(IFreeSql fsql) : base(fsql)
        {
        }

        public bool QueryHistory(DateTime StartTime, DateTime EndTime, string SearchCode, out List<ProductResultEntity> data, out string errorMsg)
        {
            data = new List<ProductResultEntity>();
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

        public bool Insert(ProductResultEntity entity, out string errorMsg)
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

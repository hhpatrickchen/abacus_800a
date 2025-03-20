using FreeSql.Internal.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Dct.Models
{

    public interface IBaseRepository<TEntity, TKey> : FreeSql.IBaseRepository<TEntity, TKey>
            where TEntity : class
    {

    }

    public abstract class BaseRepository<TEntity> : FreeSql.BaseRepository<TEntity, int>, IBaseRepository<TEntity, int>
        where TEntity : BaseEntity
    {
        protected IFreeSql _fsql;
        public BaseRepository(IFreeSql fsql) : base(fsql, null, null)
        {
            _fsql = fsql;

        }

        public  int Update(Expression<Func<TEntity, bool>> predicate, Expression<Func<TEntity, TEntity>> updateExpression)
        {
            return _fsql.Update<TEntity>()
                .Set(updateExpression)
                .Where(predicate)
                .ExecuteAffrows();
        }

        public event SqlErrorLogEventHandler SqlErrorLog;

        protected void ErrorLog(Exception ex)
        {
            SqlErrorLog?.Invoke(this, new SqlExceptionLogModel(ex));
        }
    }

    public delegate void SqlErrorLogEventHandler(object sender, SqlExceptionLogModel e);

    public class SqlExceptionLogModel
    {
        public Exception Exception { get; set; }

        public string ExceptionMessage { get; set; }

        public SqlExceptionLogModel(Exception ex) 
        {
            Exception = ex;

            ExceptionMessage = $"操作数据库失败, 失败原因：{ex.GetExceptionMessage()}";
        }

    }
}

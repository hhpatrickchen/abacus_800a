using FreeSql;
using System;
using System.Reflection;

namespace Dct.Models
{
    public class DbManager
    {
        private static DbManager _instance;
        

        public static DbManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new DbManager();
                }

                return _instance;
            }
        }
        public IFreeSql fsql;
        private DbManager() { }

        public bool ConnectDB()
        {
            try
            {
                // 初始化 FreeSql 实例
                fsql = new FreeSqlBuilder()
                    .UseConnectionString(DataType.Sqlite, "Data Source=DB1.db; Pooling=true;Min Pool Size=2")
                    .UseAutoSyncStructure(true)
                    .Build();

                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        public T GetRepository<T>() where T: IBaseRepository
        {
            // 使用反射来创建带参数的实例
            var constructor = typeof(T).GetConstructor(new[] { typeof(IFreeSql) });
            if (constructor == null)
            {
                throw new InvalidOperationException($"No constructor found for {typeof(T).Name}");
            }
            return (T)constructor.Invoke(new object[] { fsql });
        }
    }
}

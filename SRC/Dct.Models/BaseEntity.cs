using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FreeSql;
using FreeSql.DataAnnotations;

namespace Dct.Models
{
    public interface IEntity
    {
    }

    public interface IEntity<TKey> : IEntity
    {
        TKey Id { get; set; }
    }

    public abstract class BaseEntity<TKey> : IEntity<TKey>
    {
        [Column(IsIdentity = true, IsPrimary = true)]
        public TKey Id { get; set; }
    }

    public abstract class BaseEntity : BaseEntity<int>
    {
    }

    public abstract class TimeManagerEntity : BaseEntity<int>
    {
        /// <summary>
        /// 创建时间
        /// </summary>
        [Column(IsNullable = false, ServerTime = DateTimeKind.Utc, CanUpdate = false, DbType = "DATETIME")]
        public DateTime CreatedTime { get; set; }

        /// <summary>
        /// 修改时间
        /// </summary>
        [Column(IsNullable = false, ServerTime = DateTimeKind.Utc, DbType = "DATETIME")]
        public DateTime UpdateTime { get; set; }
    }

}

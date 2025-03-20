using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models.Entity
{
    [Index("index_ParameterChangeName", "Name", false)]
    [Index("index_ParameterChangeChangeTime", "ChangeTime", false)]
    [Table(Name = "ParameterChangeHistory")]
    public class ParameterChangeHistoryEntity : BaseEntity
    {
        [Column(IsNullable = false, DbType = "DATETIME")]
        public DateTime ChangeTime { get; set; }

        public string Name { get; set; }
        public string StationID { get; set; }
        public string OldValue { get; set; }
        public string NewValue { get; set; }
        public string UserName { get; set; }
    }
}

using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models.Entity
{
    [Index("index_FingerEngagement_StartTime", "StartTime", false)]
    [Index("index_FingerEngagement_ShutterName", "ShutterName", false)]
    [Table(Name = "FingerEngagement")]
    public class FingerEngagementEntity : BaseEntity
    {
        [Column(IsNullable = false, StringLength = 50)]
        public string ShutterName { get; set; }


        [Column(IsNullable = false, StringLength = 50)]
        public string Name { get; set; }


        [Column(IsNullable = false, DbType = "DATETIME")]
        public DateTime StartTime { get; set; }
    }
}

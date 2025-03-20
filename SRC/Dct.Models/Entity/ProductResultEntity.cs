using FreeSql.DataAnnotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Dct.Models.Entity
{

    [Index("index_ProductResultCode", "Code", false)]
    [Index("index_ProductResultStartTime", "StartTime", false)]
    [Table(Name = "ProductResult")]
    public class ProductResultEntity : BaseEntity
    {
        [Column(IsNullable = false, DbType = "DATETIME")]
        public DateTime StartTime { get; set; }

        [Column(IsNullable = true, DbType = "DATETIME")]
        public DateTime EndTime { get; set; }

        [Column(IsNullable = false, StringLength = 255)]
        public string Recipe { get; set; }

        [Column(IsNullable = false, StringLength = 255)]
        public string Code { get; set; }

        [Column(IsNullable = false, StringLength = 255)]
        public string CoverTrayID { get; set; }

        [Column(IsNullable = false, StringLength = 255)]
        public string TrayType { get; set; }

        [Column(IsNullable = false, StringLength = 32)]
        public string Result { get; set; }
    }
}

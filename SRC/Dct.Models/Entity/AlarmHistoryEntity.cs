using FreeSql.DataAnnotations;
using System;

namespace Dct.Models.Entity
{
    public enum AlarmState
    {
        Set,
        Clear
    }

    [Index("index_Code", "Code", false)]
    [Index("index_StartTime", "StartTime", false)]
    [Table(Name = "AlarmHistory")]
    public class AlarmHistoryEntity : BaseEntity
    {
        [Column(IsNullable = false, StringLength = 255)]
        public string Code { get; set; }


        [Column(IsNullable = false, StringLength = 50)]
        public string Type { get; set; }

        [Column(IsNullable = false, StringLength = 50)]
        public string StationID { get; set; }

        public string Description { get; set; }
        [Column(IsNullable = false, DbType = "DATETIME")]
        public DateTime StartTime { get; set; }
        [Column(IsNullable = true, DbType = "DATETIME")]
        public DateTime? EndTime { get; set; }

        [Column(IsNullable = false)]
        public AlarmState State { get; set; }

        [Column(IsIgnore = true)]
        public TimeSpan Duration
        {
            get
            {
                if (EndTime != null && EndTime > StartTime && State == AlarmState.Clear)
                {
                    return EndTime.Value - StartTime;
                }
                else if (DateTime.Now>StartTime)
                {
                    return DateTime.Now - StartTime;
                } else
                {
                    return TimeSpan.Zero;
                }
            }
        }

    }
}

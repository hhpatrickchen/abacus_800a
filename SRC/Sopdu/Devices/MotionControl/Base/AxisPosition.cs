using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sopdu.Devices.MotionControl.Base
{
    public class PositionConfig
    {
        public string DisplayName { get; set; }
        public List<AxisPosition> PositionList { get; set; }
    }

    public class AxisPosition : NotifyPropertyChangedObject, ICloneable
    {
        private float _accTime;//acceleration time
        private long _coordinate;//position value
        private float _decTime;//deceleration
        private uint _inPositionRange;// in position range
        private bool _isRelativePosition;//position value is use for relative position
        private float _startVelocity;//start velocity
        private float _maxVelocity;//maximum velocity
        private string _name;//position name

        public float AccTime
        {
            get
            {
                return _accTime;
            }
            set
            {
                _accTime = value;
                NotifyPropertyChanged();
            }
        }

        public long Coordinate
        {
            get
            {
                return _coordinate;
            }
            set
            {
                _coordinate = value;
                NotifyPropertyChanged();
            }
        }

        public float DecTime
        {
            get
            {
                return _decTime;
            }
            set
            {
                _decTime = value;
                NotifyPropertyChanged();
            }
        }

        public uint InPositionRange
        {
            get
            {
                return _inPositionRange;
            }
            set
            {
                _inPositionRange = value;
                NotifyPropertyChanged();
            }
        }

        public bool IsRelativePosition
        {
            get
            {
                return _isRelativePosition;
            }
            set
            {
                _isRelativePosition = value;
                NotifyPropertyChanged();
            }
        }

        public float MaxVelocity
        {
            get
            {
                return _maxVelocity;
            }
            set
            {
                _maxVelocity = value;
                NotifyPropertyChanged();
            }
        }

        public string Name
        {
            get
            {
                return _name;
            }
            set
            {
                _name = value;
                NotifyPropertyChanged();
            }
        }

        public float StartVelocity
        {
            get
            {
                return _startVelocity;
            }
            set
            {
                _startVelocity = value;
                NotifyPropertyChanged();
            }
        }

        public object Clone()
        {
            AxisPosition position = new AxisPosition()
            {
                AccTime = this.AccTime,
                Coordinate = this.Coordinate,
                DecTime = this.DecTime,
                InPositionRange = this.InPositionRange,
                IsRelativePosition = this.IsRelativePosition,
                MaxVelocity = this.MaxVelocity,
                Name = this.Name,
                StartVelocity = this.StartVelocity,
            };
            return position;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("Position:");
            sb.Append(Name);
            sb.Append(";Coordinate:");
            sb.Append(Coordinate);
            sb.Append(";Relative:");
            sb.Append(IsRelativePosition);
            sb.Append(";StartVelocity:");
            sb.Append(StartVelocity);
            sb.Append(";MaxVelocity:");
            sb.Append(MaxVelocity);
            sb.Append(";AccTime:");
            sb.Append(AccTime);
            sb.Append(";DecTime:");
            sb.Append(DecTime);
            sb.Append(";InPosRange:");
            sb.Append(InPositionRange);
            return sb.ToString();
        }
    }
}
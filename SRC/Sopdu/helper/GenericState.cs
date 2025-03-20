using Sopdu.Devices;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Sopdu.helper
{
    #region Enum Module Type

    public class SecsGemAttr : Attribute
    {
        public SecsGemAttr(string name, string description)
        {
            Name = name;
            Descriptions = description;
        }

        public string Name { get; private set; }
        public string Descriptions { get; private set; }
    }

    public class EnumDescriptionTypeConverter : EnumConverter
    {
        public EnumDescriptionTypeConverter(Type type)
            : base(type)
        {
        }

        public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (destinationType == typeof(string))
            {
                if (value != null)
                {
                    FieldInfo fi = value.GetType().GetField(value.ToString());
                    if (fi != null)
                    {
                        var attributes = (DescriptionAttribute[])fi.GetCustomAttributes(typeof(DescriptionAttribute), false);
                        return ((attributes.Length > 0) && (!String.IsNullOrEmpty(attributes[0].Description))) ? attributes[0].Description : value.ToString();
                    }
                }

                return string.Empty;
            }

            return base.ConvertTo(context, culture, value, destinationType);
        }
    }

    [TypeConverter(typeof(EnumDescriptionTypeConverter))]
    public enum MachineState
    {
        [Description("System Not Initialized")]
        NotInit,

        [Description("Initialize")]
        Init,

        [Description("Initializing")]
        InitRun,

        [Description("Initialize Error")]
        InitRunErr,

        [Description("Initialize Complete")]
        InitComplete,

        [Description("Running")]
        Run,

        [Description("Run Time Error")]
        RunErr,

        [Description("Cycle Stop")]
        Stop,

        [Description("Pause")]
        Pause,

        [Description("Maintenance Mode")]
        Maintenance,

        [Description("EMO Detected")]
        EMO,

        [Description("EMO Released")]
        EMORelease,

        [Description("Error")]
        ERR,

        [Description("Error Recover")]
        ERRR_Recover,

        [Description("Eq Reset State")]
        EqResetState,

        [Description("EMO Wait Release")]
        EMOWaitRelease,

        [Description("Abort")]
        Abort,

        [Description("Stopping")]
        Stopping,

        Aborted,
        RunInitAbort,
        EnterMaintainance,
        InitRunStop,
        RunStop,
        WarnStop,
        Warning
    }

    # endregion

    public class GenericState : NotifyPropertyChangedObject
    {
        private MachineState _CurrentState;
        public MachineState CurrentState { get { return _CurrentState; } set { _CurrentState = value; NotifyPropertyChanged(); } }

        public MachineState GetState()
        {
            return _CurrentState;
        }

        public void SetState(MachineState state)
        {
            _CurrentState = state;
        }
    }
}
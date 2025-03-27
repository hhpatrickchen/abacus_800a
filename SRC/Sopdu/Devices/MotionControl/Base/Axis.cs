using Sopdu.helper;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Sopdu.Devices.MotionControl.Base
{
    public enum AxisCommand
    {
        None,
        ServoOn,
        ServoOff,
        HomeSearch,
        Move,
        JogPositive,
        JogNegative,
        Stop,
        DecelerationStop,
        AlarmReset_Start,
        AlarmReset_End,
        HomeSearchStart,
        HomeSearchEnd,
        ModBusOn,
        ModBusOff
    }

    public enum AxisStatus
    {
        Error,
        Alarm,
        EStopped,
        ServoOff,
        Uninitialized,
        Ready,
        Moving,
        Initializing,
    }

    public abstract class Axis : GenericDevice// should use axis with axis controller.. if only uses axis it will be putting the cart before the horse
    {
        public string DisplayName { get; set; }
        public string _PositionFilePath;
        private double _currentCoordinate;
        private string _errorcode;
        private AxisPosition _currentPosition;
        private AxisPosition _jogPosition;
        private AxisCommand _command;
        private AxisStatus _currentStatus;
        [XmlIgnore]
        public IOModule.DiscreteIO opBrake;
        public ManualResetEvent SkipCommandEvent;
        public ManualResetEvent SkipcommandCompleteEvent;

        public Axis()
        {
            this.PositionList = new ObservableCollection<AxisPosition>();
            SkipCommandEvent = new ManualResetEvent(false);
            SkipcommandCompleteEvent = new ManualResetEvent(false);
            this.CommandSetEvent = new ManualResetEvent(false);
            this.CommandDoneEvent = new ManualResetEvent(false);
            this.CommandCompleteEvent = new ManualResetEvent(false);
            this.StatusChangedEvent = new ManualResetEvent(false);
            this.Command = AxisCommand.None;
            this.CurrentStatus = AxisStatus.ServoOff;
            this.ForceStopped = false;
            this.IsInitialized = false;
        }

        public string PositionFilePath
        {
            get
            {
                return _PositionFilePath;
            }
            set
            {
                _PositionFilePath = value;
            }
        }

        private bool _bIsEnabled;

        [XmlIgnore]
        public bool bIsEnable
        {
            get { return _bIsEnabled; }
            set { _bIsEnabled = value; NotifyPropertyChanged(); }
        }

        [XmlIgnore]
        public AxisCommand Command
        {
            get
            {
                return _command;
            }
            set
            {
                _command = value;
                NotifyPropertyChanged();
            }
        }

        [XmlIgnore]
        public ManualResetEvent CommandDoneEvent
        {
            get;
            private set;
        }

        [XmlIgnore]
        public ManualResetEvent CommandSetEvent
        {
            get;
            private set;
        }

        [XmlIgnore]
        public ManualResetEvent CommandCompleteEvent//new add in
        {
            get;
            private set;
        }

        [XmlIgnore]
        public string sendmsg { get; set; }

        [XmlIgnore]
        public string Rxmsg { get; set; }

        [XmlIgnore]
        public double CurrentCoordinate
        {
            get
            {
                return _currentCoordinate;
            }
            set
            {
                _currentCoordinate = value;
                NotifyPropertyChanged();
            }
        }

        [XmlIgnore]
        public AxisPosition CurrentPosition
        {
            get
            {
                return _currentPosition;
            }
            set
            {
                _currentPosition = value;
                NotifyPropertyChanged();
            }
        }

        [XmlIgnore]
        public bool PositionEnd
        {
            get;
            set;
        }

        [XmlIgnore]
        public AxisStatus CurrentStatus
        {
            get
            {
                return _currentStatus;
            }
            set
            {
                _currentStatus = value;
                NotifyPropertyChanged();
                StatusChangedEvent.Set();
            }
        }

        [XmlIgnore]
        public bool ForceStopped
        {
            get;
            set;
        }

        [XmlIgnore]
        public bool IsInitialized
        {
            get;
            set;
        }

        [XmlIgnore]
        public string ErrorCode
        {
            get
            {
                return _errorcode;
            }
            set { _errorcode = value; NotifyPropertyChanged(); }
        }

        [XmlIgnore]
        public AxisPosition JogPosition
        {
            get
            {
                return _jogPosition;
            }
            set
            {
                _jogPosition = value;
                NotifyPropertyChanged();
            }
        }

        [XmlIgnore]
        public ObservableCollection<AxisPosition> JogPositionList
        {
            get;
            private set;
        }

        public ObservableCollection<AxisPosition> PositionList
        {
            get;
            private set;
        }

        [XmlIgnore]
        public ManualResetEvent StatusChangedEvent
        {
            get;
            private set;
        }

        public virtual void AlarmReset(bool isAutoOp = false)
        {
            bIsEnable = false;
            bool result = SetCommand(AxisCommand.AlarmReset_Start);
            result = WaitMsgRx(5000);//wait for the same command to appear...
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            //CommandCompleteEvent.Reset();
            Thread.Sleep(500);
            result = SetCommand(AxisCommand.AlarmReset_End);
            result = WaitMsgRx(5000);
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            ///CommandCompleteEvent.Reset();
            bIsEnable = true;
            if (!result)
            {
                throw new Exception("AlarmReset Failed");
            }
        }

        public void DecelerationStop()
        {
            ForceStopped = true;
            // No matter what is the current command, set command to DecelerationStop.
            Command = AxisCommand.DecelerationStop;
            CommandSetEvent.Set();
        }

        public bool IsReady()
        {
            return CurrentStatus == AxisStatus.Ready;
        }

        public void ResetCommand()
        {
            Command = AxisCommand.None;
            //  CommandSetEvent.Reset();
            CommandDoneEvent.Set();
        }

        public void SavePositionFile()
        { // cannnot save position directly when using ObservableCollection
            GenericRecipe<PositionConfig> recipe = new GenericRecipe<PositionConfig>(this.PositionFilePath);
            PositionConfig position = new PositionConfig();
            position.PositionList = new System.Collections.Generic.List<AxisPosition>();
            foreach (AxisPosition pos in this.PositionList)
            {
                position.PositionList.Add(pos);
            }
            recipe.Write(position);
        }

        public void ReadPositionFile()
        {
            GenericRecipe<PositionConfig> recipe = new GenericRecipe<PositionConfig>(this.PositionFilePath);
            PositionConfig position = recipe.Read();
            foreach (AxisPosition pos in position.PositionList)
            {
                PositionList.Add(pos);
            }
        }

        public virtual void ServoOff(bool isAutoOp = false)
        {
            bIsEnable = false;

            bool result = SetCommand(AxisCommand.ServoOff);

            result = WaitMsgRx(2000);
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            //CommandCompleteEvent.Reset();
            bIsEnable = true;

            if (!result)
            {
                throw new Exception("ServoOff Failed");
            }
            if (opBrake != null)
                opBrake.SetOutput(false);
            
        }

        public virtual void ServoOn(bool bEMOExit, bool isAutoOp = false)
        {
            bIsEnable = false;
            CommandDoneEvent.Reset();
            bool result = SetCommand(AxisCommand.ServoOn);
            result = WaitMsgRx(2000);
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            bIsEnable = true;
            if (!result)
            {
                throw new Exception("ServoOn Failed");
            }
            if (opBrake != null)
                opBrake.SetOutput(true);
        }

        public virtual void StartHomeSearch(bool bEMOExit, bool isAutoOp = false)
        {
            bIsEnable = false;
            bool result = SetCommand(AxisCommand.HomeSearchStart);
            result = WaitMsgRx(5000);//wait for the same command to appear...
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            Thread.Sleep(500); //from 100 to 500
            //CommandCompleteEvent.Reset();
            CurrentStatus = AxisStatus.Initializing;
            result = SetCommand(AxisCommand.HomeSearchEnd);
            result = WaitMsgRx(5000);
            CommandSetEvent.Reset();
            //CommandCompleteEvent.Reset();
            CommandDoneEvent.Reset();
            bIsEnable = true;
            //bool result = SetCommand(AxisCommand.HomeSearch);
            //CurrentStatus = AxisStatus.Initializing;
            //this.IsInitialized = false;
            //result = WaitMsgRx(2000);
            //CommandSetEvent.Reset();
            //CommandDoneEvent.Reset();
            if (!result)
            {
                throw new Exception("StartHomeSearch Failed");
            }
        }

        //public virtual void StartJogNegative(bool isAutoOp = false)
        //{
        //    bIsEnable = false;
        //    bool result = SetCommand(AxisCommand.JogNegative);
        //    result = WaitMsgRx(2000);
        //    CommandSetEvent.Reset();
        //    CommandDoneEvent.Reset();
        //    bIsEnable = true;
        //    if (!result)
        //    {
        //        throw new Exception("StartJogNegative Failed");
        //    }
        //}

        //public virtual void StartJogPositive(bool isAutoOp = false)
        //{
        //    bIsEnable = false;
        //    bool result = SetCommand(AxisCommand.JogPositive);
        //    result = WaitMsgRx(2000);
        //    CommandSetEvent.Reset();
        //    CommandDoneEvent.Reset();
        //    bIsEnable = true;
        //    if (!result)
        //    {
        //        throw new Exception("StartJogPositive Failed");
        //    }
        //}

        public virtual void StartMove(int positionNumber, bool isAutoOp = false)
        {
            bIsEnable = false;
            AxisPosition oldPosition = CurrentPosition;
            CurrentPosition = PositionList[positionNumber];
            bool result = SetCommand(AxisCommand.Move);
            CurrentStatus = AxisStatus.Moving;
            result = WaitMsgRx(2000);
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            //  CommandCompleteEvent.Reset();
            bIsEnable = true;
            if (!result)
            {
                CurrentPosition = oldPosition;
                throw new Exception("StartMove Failed");
            }
        }

        public virtual void StartMove(AxisPosition position, bool isAutoOp = false)
        {
            bIsEnable = false;
            AxisPosition oldPosition = CurrentPosition;
            CurrentPosition = position;
            bool result = SetCommand(AxisCommand.Move);
            result = WaitMsgRx(3000);
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            // CommandCompleteEvent.Reset();
            bIsEnable = true;
            if (!result)
            {
                CurrentPosition = oldPosition;                
                throw new Exception("StartMove Failed");
             
            }            
        }

        public virtual bool StartMove_(AxisPosition position, bool isAutoOp = false)
        {
            bIsEnable = false;
            AxisPosition oldPosition = CurrentPosition;
            CurrentPosition = position;
            bool result = SetCommand(AxisCommand.Move);
            result = WaitMsgRx_(500);
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            // CommandCompleteEvent.Reset();
            bIsEnable = true;
            if (!result)
            {
                return false;

            }
            return true;
        }

        public virtual void SetModBusOn()//internal function
        {
            bIsEnable = false;
            bool result = SetCommand(AxisCommand.ModBusOn);
            //  result = WaitMsgRx(2000);
            //Command = AxisCommand.None;
            //CommandSetEvent.Reset();
            // CommandDoneEvent.Reset();
            //  CommandCompleteEvent.Reset();
            bIsEnable = true;
        }

        public virtual void SetModBusOff()//internal function
        {
            bIsEnable = false;
            bool result = SetCommand(AxisCommand.ModBusOff);
            //  result = WaitMsgRx(2000);
            //Command = AxisCommand.None;
            //CommandSetEvent.Reset();
            // CommandDoneEvent.Reset();
            //  CommandCompleteEvent.Reset();
            bIsEnable = true;
        }
        public void SetModbusOnComplete()
        {
            Command = AxisCommand.None;
            CommandSetEvent.Reset();
            CommandDoneEvent.Reset();
            //  CommandCompleteEvent.Reset();
            bIsEnable = true;
        }

        public void Stop()
        {
            ForceStopped = true;
            // No matter what is the current command, set command to Stop.
            Command = AxisCommand.Stop;
            CommandSetEvent.Set();
        }

        public void WaitReady(int timeout)
        {
            Thread.Sleep(200);
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int waitLength = timeout; waitLength > 0; waitLength = (int)(timeout - sw.ElapsedMilliseconds))
            {
                bool result = StatusChangedEvent.WaitOne(waitLength);
                if (result)
                {
                    StatusChangedEvent.Reset();
                }
                else
                {
                    throw new TimeoutException();
                }
                switch (CurrentStatus)
                {
                    case AxisStatus.Alarm:
                    case AxisStatus.Error:
                    case AxisStatus.EStopped:
                    case AxisStatus.ServoOff:
                        throw new Exception("Axis Error: " + CurrentStatus.ToString());
                    case AxisStatus.Ready:
                        return;
                }
            }
            switch (CurrentStatus)
            {
                case AxisStatus.Alarm:
                case AxisStatus.Error:
                case AxisStatus.EStopped:
                case AxisStatus.ServoOff:
                    throw new Exception("Axis Error: " + CurrentStatus.ToString());
                case AxisStatus.Ready:
                    return;
            }
            throw new TimeoutException();
        }

        public virtual bool SetCommand(AxisCommand command)
        {
            if (Command == AxisCommand.None)
            {
                Command = command;
                CommandDoneEvent.Reset();
                CommandCompleteEvent.Reset();
                CommandSetEvent.Set();

                //CommandSetEvent.Set();
                //CommandDoneEvent.Reset();
                //CommandCompleteEvent.Reset();
                return true;
            }
            return false;
        }

        protected virtual bool WaitMsgRx(int timeout)
        {
            if (CommandDoneEvent.WaitOne(1000))//this is activated once command is tx to rs232 via pconcontroller
            {
                for (int waitLength = timeout; (waitLength > 0); waitLength = timeout - 100)
                {
                    //may be can use this to monitor is command have been executed correctly

                    if (sendmsg == Rxmsg)
                    {
                        CommandCompleteEvent.Set(); //Command = AxisCommand.None;
                        return true;
                    }
                    Thread.Sleep(100);
                }
            }
            Command = AxisCommand.None;
            throw new TimeoutException();
        }

        private bool WaitMsgRx_(int timeout)
        {
            if (CommandDoneEvent.WaitOne(500))//this is activated once command is tx to rs232 via pconcontroller
            {
                for (int waitLength = timeout; (waitLength > 0); waitLength = timeout - 100)
                {
                    //may be can use this to monitor is command have been executed correctly

                    if (sendmsg == Rxmsg)
                    {
                        CommandCompleteEvent.Set(); //Command = AxisCommand.None;
                        return true;
                    }
                    Thread.Sleep(100);
                }
            }
            Command = AxisCommand.None;
            return false;
        }
        private bool WaitCommandDone(int timeout)
        {
            Stopwatch sw = new Stopwatch();
            sw.Start();
            for (int waitLength = timeout; (waitLength > 0); waitLength = (int)(timeout - sw.ElapsedMilliseconds))
            {
                if (CommandDoneEvent.WaitOne(waitLength)) return true;
            }
            if (Command != AxisCommand.None)
            {
                throw new TimeoutException();
            }
            return false;
        }
    }
}
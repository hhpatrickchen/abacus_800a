using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using static System.Windows.Forms.VisualStyles.VisualStyleElement.TaskbarClock;

namespace Sopdu.Devices.IOModule
{
    public static class IOTool
    {
        public static bool CompareResult(this DiscreteIO io, DiscreteIO other, int time = 0)
        {
            if (io.Logic != other.Logic)
            {
                while (time <= 0)
                {
                    if (io.Logic == other.Logic)
                    {
                        return true;
                    }

                    time = time - 100;

                    Thread.Sleep(100);
                }

                return false;
            }

            return true;
        }
    }
}

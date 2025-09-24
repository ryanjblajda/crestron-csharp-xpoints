using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace Blajda.xPoints
{
    public class xPointEventArgs : EventArgs
    {
        public ushort OutputIndex { get; private set; }
        public ushort DigitalAnalogValue { get; private set; }
        public string StringValue { get; private set; }

        public xPointEventArgs()
        {
        }

        public xPointEventArgs(ushort index, object value, SIMPL.Signal type)
        {
            this.OutputIndex = index;

            if (value != null)
            {
                switch (type)
                {
                    case (SIMPL.Signal.Analog):
                        this.DigitalAnalogValue = (ushort)value;
                        break;
                    case (SIMPL.Signal.Digital):
                        this.DigitalAnalogValue = (ushort)value;
                        break;
                    case (SIMPL.Signal.Serial):
                        this.StringValue = (string)value;
                        break;
                }
            }
            else
            {
                this.StringValue = "";
                this.DigitalAnalogValue = 0;
            }
        }
    }
}
using System;
using System.Text;
using System.Collections.Generic;
using Crestron.SimplSharp;                          				// For Basic SIMPL# Classes

namespace SIMPL
{
    public enum DigitalSignal
    {
        Off = 0,
        On = 1
    }

    public enum Signal
    {
        Digital = 1,
        Analog = 2,
        Serial = 3
    }

    public static class Conversion
    {
        public static bool ConvertToBool(ushort value)
        {
            if ((DigitalSignal)value == DigitalSignal.On) return true;
            return false;
        }

        public static ushort ConvertToSignal(bool state)
        {
            if (state) return (ushort)DigitalSignal.On;
            return (ushort)DigitalSignal.Off;
        }
    }

    public class DigitalPayloadArgs : EventArgs
    {
        public ushort Payload { get; private set; }

        public DigitalPayloadArgs()
        {
        }

        public DigitalPayloadArgs(object payload)
        {
            this.Payload = (ushort)payload;
        }

        public DigitalPayloadArgs(ushort payload)
        {
            this.Payload = payload;
        }

        public DigitalPayloadArgs(bool payload)
        {
            this.Payload = SIMPL.Conversion.ConvertToSignal(payload);
        }

        public DigitalPayloadArgs(DigitalSignal payload)
        {
            this.Payload = (ushort)payload;
        }
    }

    public class AnalogPayloadArgs : EventArgs
    {
        public ushort Payload { get; private set; }

        public AnalogPayloadArgs()
        {
        }

        public AnalogPayloadArgs(object payload)
        {
            this.Payload = (ushort)payload;
        }
    }

    public class StringPayloadArgs : EventArgs
    {
        public string Payload { get; private set; }

        public StringPayloadArgs()
        {
        }

        public StringPayloadArgs(object payload)
        {
            this.Payload = (string)payload;
        }
    }

    public class StringArrayPayloadArgs : EventArgs
    {
        public string[] Payload { get; private set; }
        public ushort   PayloadCount { get; private set; }

        public StringArrayPayloadArgs()
        {
        }

        public StringArrayPayloadArgs(string[] payload)
        {
            this.Payload = (string[])payload;
        }

        public StringArrayPayloadArgs(List<string> payload)
        {
            this.Payload = payload.ToArray();
            this.PayloadCount = (ushort)payload.Count;
        }
    }

    public class AnalogArrayPayloadArgs : EventArgs
    {
        public ushort[] Payload { get; private set; }
        public ushort PayloadCount { get; private set; }

        public AnalogArrayPayloadArgs()
        {
        }

        public AnalogArrayPayloadArgs(List<int> payload)
        {
            this.Payload = payload.ConvertAll<ushort>(item => (ushort)item).ToArray();
            this.PayloadCount = (ushort)payload.Count;
        }

        public AnalogArrayPayloadArgs(List<ushort> payload)
        {
            this.Payload = payload.ToArray();
            this.PayloadCount = (ushort)payload.Count;
        }
    }

    public class DigitalArrayPayloadArgs : EventArgs
    {
        public ushort[] Payload { get; private set; }
        public ushort PayloadCount { get; private set; }

        public DigitalArrayPayloadArgs()
        {
        }

        public DigitalArrayPayloadArgs(List<ushort> payload)
        {
            this.Payload = payload.ToArray();
            this.PayloadCount = (ushort)payload.Count;
        }

        public DigitalArrayPayloadArgs(List<bool> payload)
        {
            this.Payload = payload.ConvertAll<ushort>(item => SIMPL.Conversion.ConvertToSignal(item)).ToArray();
        }

        public DigitalArrayPayloadArgs(List<DigitalSignal> payload)
        {
            this.Payload = payload.ConvertAll<ushort>(item => (ushort)item).ToArray();
            this.PayloadCount = (ushort)payload.Count;
        }
    }
}

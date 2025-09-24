using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;

namespace Blajda.xPoints
{
    internal class Tuple<T1, T2> {
        public T1 Index { get; set; }
        public T2 Value { get; set; }

        public Tuple()
        {
        }

        public Tuple(T1 t1, T2 t2)
        {
            Index = t1;
            Value = t2;
        }
    }

    public class xPoint 
    {
        private readonly Dictionary<string, Tuple<ushort, object>> Properties;
        private xPointBus Communications;

        public SIMPL.Signal Type { get; set; }
        public string Name { get; set; }

        internal string _oldfilter;
        private string _groupfilter;
        public string GroupFilter 
        {
            get 
            {
                return _groupfilter;
            }
            set 
            {
                if (value != "") {
                    this._oldfilter = this._groupfilter;
                    this._groupfilter = value;
                }
                else { this._groupfilter = null; }

                this.ClearAllOutputs(); //clear all outputs when the crosspoint has been set to an invalid group //moved here from else block 7/30/2024
                if (this.Communications != null) { this.Communications.SyncRequest(this); }
            }
        }

        public delegate void IncomingMessage(object sender, xPointEventArgs args);
        public event IncomingMessage MessageReceived;

        public delegate ushort GetStatus(ushort index);
        public GetStatus GetSignalState { get; set; }

        public xPoint()
        {
            this.Properties = new Dictionary<string, Tuple<ushort, object>>();
        }

        private void ClearAllOutputs()
        {
            //need to update
            List<KeyValuePair<string, Tuple<ushort, object>>> currentProperties = new List<KeyValuePair<string, Tuple<ushort, object>>>();
            //other threads could add properties while we are trying to access the list, so we need to lock it and make a copy
            lock (this.Properties)
            {
                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} | ACQUIRED LOCK ON PROPERTIES DICTIONARY", this.Name);
                currentProperties = this.Properties.Where(item => item.Value.Value == null).ToList();
                if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("XPOINT @ {0} | {1} PROPERTIES ARE OUTPUTS", this.Name, currentProperties.Count);
                if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("XPOINT @ {0} | PROPERTIES LIST: {1}", this.Name, String.Join(", ", currentProperties.Select(item => item.Key).ToArray()));
                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} | CREATE COPY OF PROPERTIES DICTIONARY", this.Name);
            }
            if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} | RELEASED LOCK ON PROPERTIES DICTIONARY", this.Name);
            //update the objects via the copy of the list
            if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} | RESET PROPERTIES OUTPUTS DICTIONARY VALUES", this.Name);
            currentProperties.ForEach(delegate(KeyValuePair<string, Tuple<ushort, object>> item) { this.MessageReceived(this, new xPointEventArgs(item.Value.Index, null, this.Type)); });
        }

        
        internal void OnSyncRequest(xPoint requestor)
        {
            if (this != requestor) //make sure that we cannot request sync status from ourselves
            {
                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} | OKAY TO BEGIN SYNC RESPONSE", this.Name);
                IEnumerable<string> Common = this.Properties.Where(item => item.Value != null).Select(item => item.Key).Intersect(requestor.Properties.Keys);
                if (Common != null)
                {
                    List<string> ToSync = Common.ToList<string>();
                    if (xPointUtilities.IsVerbose) { CrestronConsole.PrintLine("XPOINT @ {0} -> {2} COMMON PROPERTIES WITH XPOINT @ {1}", this.Name, requestor.Name, ToSync.Count); }
                    ToSync.ForEach(delegate(string item) //for each item get the status.
                    {
                        if (this.Properties[item].Value != null) //if the value is null we are assuming thats an output property
                        {
                            if (this.Type == SIMPL.Signal.Serial)
                            {
                                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} -> XPOINT @ {1} | {2} : {3}", this.Name, requestor.Name, this.Properties[item].Value, item);
                                requestor.OnCrosspointEvent(new xPointEvent(this.Properties[item].Value, item));
                            }
                            else
                            {
                                if (this.GetSignalState != null)
                                {
                                    if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} -> XPOINT @ {1} | {2} : {3}", this.Name, requestor.Name, this.GetSignalState(this.Properties[item].Index), item);
                                    requestor.OnCrosspointEvent(new xPointEvent(this.GetSignalState(this.Properties[item].Index), item));
                                }
                            }
                        }
                    });
                }
                else { if (xPointUtilities.IsVerbose) { CrestronConsole.PrintLine("XPOINT @ {0} -> NO COMMON PROPERTIES WITH XPOINT @ {1} [NULL]", this.Name, requestor.Name); } }
            }
        }
        

        internal void OnCrosspointEvent(xPointEvent msg)
        {
            if (this.Properties.Keys.Contains(msg.Property) && this.Properties[msg.Property].Value == null && this._groupfilter != null) //verify that the incoming message meets all our test conditions
            {
                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT @ {0} | NOTIFY | {1} @ OUTPUT INDEX: {2} | STATE {3}", this.Name, msg.Property, this.Properties[msg.Property].Index, msg.Value);
                if (this.MessageReceived != null) { this.MessageReceived(this, new xPointEventArgs(this.Properties[msg.Property].Index, msg.Value, this.Type)); }
            }
        }

        public void SetType(ushort type)
        {
            this.Type = (SIMPL.Signal)type;
            switch (this.Type)
            {
                case SIMPL.Signal.Analog:
                    this.Communications = Comm.AnalogBus;
                    break;
                case SIMPL.Signal.Digital:
                    this.Communications = Comm.DigitalBus;
                    break;
                case SIMPL.Signal.Serial:
                    this.Communications = Comm.SerialBus;
                    break;
            }
        }

        public void AddChangeProperty(string property, ushort index, object value)
        {
            lock (this.Properties)
            {
                if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("{0} | ACQUIRING LOCK TO UPDATE PROPERTY {1} | INDEX => {2}", this.Name, property, value);

                if (property != "" && property != null)
                {
                    Tuple<ushort, object> result = new Tuple<ushort, object>();
                    if (this.Properties.TryGetValue(property, out result)) //make sure that the key doesnt exist already
                    {
                        if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("{0} | UPDATING PROPERTY {1} | INDEX => {2} | VALUE => {3}", this.Name, property, index, value);
                        result.Value = value; //if it does, modify the value
                        result.Index = index;
                    }
                    else
                    {
                        if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("{0} | CREATE NEW PROPERTY {1} | INDEX => {2} | VALUE => {3}", this.Name, property, index, value);
                        this.Properties.Add(property, new Tuple<ushort, object>(index, value)); //if not make a new one.
                    }
                }

                if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("{0} | RELEASING LOCK AFTER UPDATING PROPERTY", this.Name);
            }
        }

        public void AddChangeProperty(string property, ushort index)
        {
            lock (this.Properties)
            {
                if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("{0} | ACQUIRING LOCK ON PROPERTIES", this.Name, property, index);

                if (property != "" && property != null)
                {
                    Tuple<ushort, object> result = new Tuple<ushort, object>();

                    //attempt to get an already existing key
                    if (this.Properties.TryGetValue(property, out result))
                    {
                        if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("{0} | UPDATING EXISTING PROPERTY {1} | INDEX => {2}", this.Name, property, index);
                        result.Index = index;
                    }
                    else
                    {
                        if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("{0} | CREATE NEW PROPERTY {1} | INDEX => {2}", this.Name, property, index);
                        this.Properties.Add(property, new Tuple<ushort, object>(index, null)); //if not make a new one.
                    }
                }
                else { if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("{0} | PROPERTY CONTENTS {1} == EMPTY / NULL | INDEX => {2} --> NOT CREATING PROPERTY", this.Name, property, index); }

                if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("{0} | RELEASING LOCK AFTER UPDATING PROPERTY", this.Name);
            }
        }

        public void SerialChange(string value, string property, ushort index)
        {
            this.AddChangeProperty(property, index, value);
            if (this.GroupFilter != null)
            {
                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("{0} | CHANGE | Type: {1} // State: {2} // PropertyName: {3} // GroupFilter: {4} // Index: {5}", this.Name, this.Type, value, property, this.GroupFilter, index);
                if (this.Communications != null) { this.Communications.Publish(this.Type, this.Properties[property].Value, property, this.GroupFilter); }
            }
        }

        public void AnalogChange(ushort value, string property, ushort index)
        {
            this.AddChangeProperty(property, index, SIMPL.Signal.Analog);
            if (this.GroupFilter != null)
            {
                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("{0} | CHANGE | Type: {1} // State: {2} // PropertyName: {3} // GroupFilter: {4} // Index: {5}", this.Name, this.Type, value, property, this.GroupFilter, index);
                if (this.Communications != null) { this.Communications.Publish(this.Type, value, property, this.GroupFilter); }
            }
        }

        public void DigitalChange(ushort value, string property, ushort index)
        {
            this.AddChangeProperty(property, index, (SIMPL.DigitalSignal)value);
            if (this.GroupFilter != null)
            {
                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("{0} | CHANGE | Type: {1} // State: {2} // PropertyName: {3} // GroupFilter: {4} // Index: {5}", this.Name, this.Type, value, property, this.GroupFilter, index);
                if (this.Communications != null) { this.Communications.Publish(this.Type, value, property, this.GroupFilter); }
            }
        }
    }
}
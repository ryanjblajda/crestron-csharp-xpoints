using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Crestron.SimplSharp;
using Blajda;

namespace Blajda.xPoints
{
    public class xPointEvent : IMessage
    {
        public object Value { get; private set; }
        public string Property { get; private set; }
        public string Description { get; set; }

        public xPointEvent(object val, string prop)
        {
            this.Value = val;
            this.Property = prop;
            this.Description = String.Format("{0} | {1}", val, prop);
        }
    }

    public static class Comm
    {
        public static xPointBus DigitalBus = new xPointBus();
        public static xPointBus AnalogBus  = new xPointBus();
        public static xPointBus SerialBus  = new xPointBus();
    }

    public class xPointBus
    {
        private readonly Dictionary<string, List<xPoint>> _observers = new Dictionary<string, List<xPoint>>();

        public void SyncRequest(xPoint requestor)
        {
            this.UnSubscribe(requestor);

            if (requestor.GroupFilter != null)
            {
                this.Subscribe(requestor);
            }      
        }

        private void Subscribe(xPoint x)
        {
                List<xPoint> pubs = new List<xPoint>();

                lock (_observers) //make other threads wait until complete to prevent ArgumentExceptions
                {
                    if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("XPOINT BUS | LOCK ACQUIRED", x.Name, x.GroupFilter);

                    try
                    {
                        List<xPoint> result = new List<xPoint>();

                        if (_observers.TryGetValue(x.GroupFilter, out result))
                        {
                            if (!result.Contains(x))
                            {
                                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT BUS | ADD {0} TO EXISTING GROUP {1}", x.Name, x.GroupFilter);
                                result.Add(x);
                            }
                            else
                            {
                                if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT BUS | {0} ALREADY MEMBER OF GROUP {1}", x.Name, x.GroupFilter);
                            }
                        }
                        else
                        {
                            if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT BUS | CREATE NEW GROUP {0}", x.GroupFilter);
                            List<xPoint> newGroup = new List<xPoint>();
                            newGroup.Add(x);
                            _observers.Add(x.GroupFilter, newGroup);
                            if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT BUS | ADD {0} TO NEW GROUP {1}", x.Name, x.GroupFilter);
                        }
                    }   
                    catch (Exception e)
                    {
                        CrestronConsole.PrintLine("XPOINT BUS | ERROR ADDING {0} TO GROUP {1} | {2} {3}", x.Name, x.GroupFilter, e.Message, e.InnerException);
                    }   
                    
                    pubs = _observers[x.GroupFilter].Where(xpoint => xpoint.Type == x.Type).ToList();

                    if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("XPOINT BUS | LOCK RELEASING", x.Name, x.GroupFilter);
                }

                if(pubs != null) pubs.ForEach(pub => pub.OnSyncRequest(x)); 
        }

        private int UnSubscribe(xPoint x)
        {
            var removed = 0;

            if (x._oldfilter != null && x._oldfilter != x.GroupFilter)
            {
                lock (_observers)
                {
                    try
                    {
                        if (this._observers.Keys.Contains(x._oldfilter))
                        {
                            removed = _observers[x._oldfilter].RemoveAll(xpoint => xpoint == x);
                            if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT BUS | REMOVED {0} OBJECTS FROM {1} GROUP", removed, x._oldfilter);
                        }
                        else { if (xPointUtilities.IsDebug) { CrestronConsole.PrintLine("XPOINT BUS | NO KEY EXISTS: {0}", x._oldfilter); } }
                    }
                    catch (ArgumentNullException e)
                    {
                        if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT BUS | ERR REMOVING XPOINTS: {0}", e.Message);
                    }
                }
            }

            return removed;
        }

        public void Publish(SIMPL.Signal type, object value, string property, string group)
        {
            if (group != null)
            {
                try
                {
                    List<xPoint> subs = new List<xPoint>();

                    lock (this._observers)
                    {
                        if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("XPOINT BUS | LOCK ACQUIRED FOR OBSERVERS OF GROUP {0}", group);
                        if (this._observers.Keys.Contains(group))
                        {
                            subs = this._observers[group].Where(xpoint => xpoint.Type == type).ToList();
                            if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("XPOINT BUS | SUBSCRIBERS ACQUIRED {0}", subs.Count);
                        }
                    }
                    if (xPointUtilities.IsVerbose) CrestronConsole.PrintLine("XPOINT BUS | LOCK RELEASING");

                    if (xPointUtilities.IsDebug) CrestronConsole.PrintLine("XPOINT BUS | PUBLISH | Type: {0} // State: {1} // PropertyName: {2} // GroupFilter: {3}", type, value, property, group);
                    if(subs != null) subs.ForEach(sub => sub.OnCrosspointEvent(new xPointEvent(value, property)));
                }
                catch (Exception e)
                {
                    CrestronConsole.PrintLine("XPOINT BUS | {0} {1}", e.Message, e.InnerException);
                }
            }
        }
    }
}
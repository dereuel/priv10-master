﻿using MiscHelpers;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.Net;
using System.Runtime.Serialization;

namespace WinFirewallAPI
{
    [Serializable()]
    [DataContract(Name = "FirewallEvent", Namespace = "http://schemas.datacontract.org/")]
    public class FirewallEvent : EventArgs
    {
        [DataMember()]
        public int ProcessId;
        [DataMember()]
        public string ProcessFileName;

        [DataMember()]
        public FirewallRule.Actions Action;

        [DataMember()]
        public UInt32 Protocol;
        [DataMember()]
        public FirewallRule.Directions Direction;
        [DataMember()]
        public IPAddress LocalAddress;
        [DataMember()]
        public UInt16 LocalPort;
        [DataMember()]
        public IPAddress RemoteAddress;
        [DataMember()]
        public UInt16 RemotePort;

        [DataMember()]
        public DateTime TimeStamp;
    }

    public class FirewallMonitor
    {
        /*
            {0CCE9226-69AE-11D9-BED3-505054503030}
            Identifies the Filtering Platform Connection audit subcategory.
            This subcategory audits connections that are allowed or blocked by WFP.

            {0CCE9225-69AE-11D9-BED3-505054503030}
            Identifies the Filtering Platform Packet Drop audit subcategory.
            This subcategory audits packets that are dropped by Windows Filtering Platform (WFP).

            {0CCE9233-69AE-11D9-BED3-505054503030}
            Identifies the Filtering Platform Policy Change audit subcategory.
            This subcategory audits events generated by changes to Windows Filtering Platform (WFP).         
         */
        const string FirewallEventPolicyID = "0CCE9226-69AE-11D9-BED3-505054503030";

        public enum Auditing : int
        {
            Off = 0,
            Blocked = 1,
            Allowed = 2,
            All = 3
        }

        public Auditing GetAuditPolicy()
        {
            try
            {
                AuditPolicy.AUDIT_POLICY_INFORMATION pol = AuditPolicy.GetSystemPolicy(FirewallEventPolicyID);
                if ((pol.AuditingInformation & AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Success) != 0 && (pol.AuditingInformation & AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Failure) != 0)
                    return Auditing.All;
                if ((pol.AuditingInformation & AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Success) != 0)
                    return Auditing.Allowed;
                if ((pol.AuditingInformation & AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Failure) != 0)
                    return Auditing.Blocked;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return Auditing.Off;
        }

        public bool SetAuditPolicy(Auditing audit)
        {
            try
            {
                AuditPolicy.AUDIT_POLICY_INFORMATION pol = AuditPolicy.GetSystemPolicy(FirewallEventPolicyID);
                switch (audit)
                {
                    case Auditing.All: pol.AuditingInformation = AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Success | AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Failure; break;
                    case Auditing.Blocked: pol.AuditingInformation = AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Failure; break;
                    case Auditing.Allowed: pol.AuditingInformation = AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.Success; break;
                    case Auditing.Off: pol.AuditingInformation = AuditPolicy.AUDIT_POLICY_INFORMATION_TYPE.None; break;
                }
                TokenManipulator.AddPrivilege(TokenManipulator.SE_SECURITY_NAME);
                // Note: without SeSecurityPrivilege this fails silently
                AuditPolicy.SetSystemPolicy(pol);
                TokenManipulator.RemovePrivilege(TokenManipulator.SE_SECURITY_NAME);
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
                return false;
            }
            return true;
        }

        EventLogWatcher mEventWatcher = null;

        private enum EventIDs
        {
            Blocked = 5157,
            Allowed = 5156
        }

        protected string GetQuery()
        {
            int LayerRTID = 44;
            // Note: 
            //			Alowed connections LayerRTID == 48 
            //			Blocked connections LayerRTID == 44
            //			Opening a TCP port for licening LayerRTID == 38 and 36 // Resource allocation
            //			Opening a UDP port LayerRTID == 38 and 36 // Resource allocation

            return "*[System[(Level=4 or Level=0) and (EventID=" + (int)EventIDs.Blocked + " or EventID=" + (int)EventIDs.Allowed + ")]] and *[EventData[Data[@Name='LayerRTID']>='" + LayerRTID + "']]";
        }

        public bool StartEventWatcher()
        {
            try
            {
                mEventWatcher = new EventLogWatcher(new EventLogQuery("Security", PathType.LogName, GetQuery()));
                mEventWatcher.EventRecordWritten += new EventHandler<EventRecordWrittenEventArgs>(OnConnection);
                mEventWatcher.Enabled = true;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
                return false;
            }
            return true;
        }

        public void StopEventWatcher()
        {
            if (mEventWatcher != null)
            {
                mEventWatcher.EventRecordWritten -= new EventHandler<EventRecordWrittenEventArgs>(OnConnection);
                mEventWatcher.Dispose();
                mEventWatcher = null;
            }
        }

        private void OnConnection(object obj, EventRecordWrittenEventArgs arg)
        {
            if (arg.EventRecord == null)
                return;

            FirewallEvent args = ReadFirewallEvent(arg.EventRecord);
            if(args != null)
                FirewallEvent?.Invoke(this, args);
        }

        enum EventProperties
        {
            PublisherName = 0,
            EventID,
            Level,
            Keywords,

            ProcessID,
            ProcessFileName,
            Direction,
            SourceAddress,
            SourcePort,
            DestAddress,
            DestPort,
            Protocol,
            LayerRTID
        };

        private EventLogPropertySelector eventPropertySelector = new EventLogPropertySelector(new[] {
            "Event/System/Provider/@Name",		            //  0
		    "Event/System/EventID",			                //  1
		    "Event/System/Level",				            //  2
		    "Event/System/Keywords",			            //  3

		    "Event/EventData/Data[@Name='ProcessID']",		//  4 - ProcessID
		    "Event/EventData/Data[@Name='Application']",	//  5 - ProcessFileName
		    "Event/EventData/Data[@Name='Direction']",		//  6 - Direction
		    "Event/EventData/Data[@Name='SourceAddress']",	//  7 - SourceAddress
		    "Event/EventData/Data[@Name='SourcePort']",	    //  8 - SourcePort
		    "Event/EventData/Data[@Name='DestAddress']",	//  9 - DestAddress
		    "Event/EventData/Data[@Name='DestPort']",		// 10 - DestPort
		    "Event/EventData/Data[@Name='Protocol']",		// 11 - Protocol
		    "Event/EventData/Data[@Name='LayerRTID']"		// 12 - LayerRTID

		    /*"Event/EventData/Data[@Name='FilterRTID']",		// 13
		    "Event/EventData/Data[@Name='LayerName']",			// 14
		    "Event/EventData/Data[@Name='RemoteUserID']",		// 15
		    "Event/EventData/Data[@Name='RemoteMachineID']"*/	// 16
        });

        protected FirewallEvent ReadFirewallEvent(EventRecord record)
        {
            try
            {
                var PropertyValues = ((EventLogRecord)record).GetPropertyValues(eventPropertySelector);

                FirewallEvent args = new FirewallEvent();

                args.ProcessId = (int)(UInt64)PropertyValues[(int)EventProperties.ProcessID];
                string fileName = PropertyValues[(int)EventProperties.ProcessFileName].ToString();
                args.ProcessFileName = fileName.Equals("System", StringComparison.OrdinalIgnoreCase) ? "System" : NtUtilities.parsePath(fileName);

                args.Action = FirewallRule.Actions.Undefined;

                switch ((UInt16)PropertyValues[(int)EventProperties.EventID])
                {
                    case (UInt16)EventIDs.Blocked: args.Action = FirewallRule.Actions.Block; break;
                    case (UInt16)EventIDs.Allowed: args.Action = FirewallRule.Actions.Allow; break;
                    default: return null;
                }

                args.Protocol = (UInt32)PropertyValues[(int)EventProperties.Protocol];
                args.Direction = FirewallRule.Directions.Unknown;
                if (PropertyValues[(int)EventProperties.Direction].ToString() == "%%14592")
                {
                    args.Direction = FirewallRule.Directions.Inbound;
                    args.LocalAddress = IPAddress.Parse(PropertyValues[(int)EventProperties.DestAddress].ToString());
                    args.LocalPort = (UInt16)MiscFunc.parseInt(PropertyValues[(int)EventProperties.DestPort].ToString());
                    args.RemoteAddress = IPAddress.Parse(PropertyValues[(int)EventProperties.SourceAddress].ToString());
                    args.RemotePort = (UInt16)MiscFunc.parseInt(PropertyValues[(int)EventProperties.SourcePort].ToString());
                }
                else if (PropertyValues[(int)EventProperties.Direction].ToString() == "%%14593")
                {
                    args.Direction = FirewallRule.Directions.Outbound;
                    args.LocalAddress = IPAddress.Parse(PropertyValues[(int)EventProperties.SourceAddress].ToString());
                    args.LocalPort = (UInt16)MiscFunc.parseInt(PropertyValues[(int)EventProperties.SourcePort].ToString());
                    args.RemoteAddress = IPAddress.Parse(PropertyValues[(int)EventProperties.DestAddress].ToString());
                    args.RemotePort = (UInt16)MiscFunc.parseInt(PropertyValues[(int)EventProperties.DestPort].ToString());
                }
                else
                    return null; // todo log error

                args.TimeStamp = record.TimeCreated != null ? (DateTime)record.TimeCreated : DateTime.Now;

                // for debug only
                //if(!FirewallRule.MatchAddress(args.RemoteAddress, "LocalSubnet") && !NetFunc.IsMultiCast(args.RemoteAddress))
                //    AppLog.Debug("Firewall Event: {0}({1}) -> {2}", args.ProcessFileName, args.ProcessId, args.RemoteAddress);

                return args;
            }
            catch (Exception err)
            {
                AppLog.Exception(err);
            }
            return null;
        }

        public List<FirewallEvent> LoadLog() // Note: this call takes some time to complete
        {
            List<FirewallEvent> Events = new List<FirewallEvent>();

            EventLogReader logReader = new EventLogReader(new EventLogQuery("Security", PathType.LogName, GetQuery()));
            for (EventRecord eventdetail = logReader.ReadEvent(); eventdetail != null; eventdetail = logReader.ReadEvent())
            {
                FirewallEvent args = ReadFirewallEvent(eventdetail);
                if (args != null)
                    Events.Add(args);
            }

            return Events;
        }

        public event EventHandler<FirewallEvent> FirewallEvent;
    }
}
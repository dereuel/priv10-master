﻿using MiscHelpers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using PrivateService;
using PrivateAPI;
using WinFirewallAPI;
using System.Diagnostics;

namespace PrivateWin10
{
    [Serializable()]
    [DataContract(Name = "Program", Namespace = "http://schemas.datacontract.org/")]
    public class Program
    {
        //public Guid guid;

        [DataMember()]
        public ProgramID ID;
        [DataMember()]
        public string Description;

        //[NonSerialized()] // Note: BinaryFormatter can handle circular references
        public ProgramSet ProgSet = null;

        [NonSerialized()]
        public Dictionary<string, FirewallRuleEx> Rules = new Dictionary<string, FirewallRuleEx>();

        [NonSerialized()]
        public Dictionary<Guid, NetworkSocket> Sockets = new Dictionary<Guid, NetworkSocket>();

        [NonSerialized()]
        public List<LogEntry> Log = new List<LogEntry>();

        [NonSerialized()]
        public Dictionary<string, DnsEntry> DnsLog = new Dictionary<string, DnsEntry>();

        [DataMember()]
        public int RuleCount = 0;
        [DataMember()]
        public int EnabledRules = 0;
        [DataMember()]
        public int DisabledRules = 0;
        [DataMember()]
        public int ChgedRules = 0;

        [DataMember()]
        public DateTime LastAllowed = DateTime.MinValue;
        [DataMember()]
        public int AllowedCount = 0;
        [DataMember()]
        public DateTime LastBlocked = DateTime.MinValue;
        [DataMember()]
        public int BlockedCount = 0;
        public DateTime LastActivity { get { return MiscFunc.Max(LastAllowed, LastBlocked); } }
        //private bool ActivityChanged = false;

        [DataMember()]
        public int SocketCount = 0;
        [DataMember()]
        public int SocketsWeb = 0;
        [DataMember()]
        public int SocketsTcp = 0;
        [DataMember()]
        public int SocketsSrv = 0;
        [DataMember()]
        public int SocketsUdp = 0;

        [DataMember()]
        public UInt64 UploadRate = 0;
        [DataMember()]
        public UInt64 DownloadRate = 0;
        [DataMember()]
        public UInt64 TotalUpload = 0;
        [DataMember()]
        public UInt64 TotalDownload = 0;

        // The old values keep the last total of all closed sockets
        internal UInt64 OldUpload = 0;
        internal UInt64 OldDownload = 0;

        public void AssignSet(ProgramSet progSet)
        {
            // unlink old config
            if (ProgSet != null)
                ProgSet.Programs.Remove(ID);

            // link program with its config
            ProgSet = progSet;
            ProgSet.Programs.Add(ID, this);
        }

        public Program()
        {
            //guid = Guid.NewGuid();
        }

        public Program(ProgramID progID)
        {
            //guid = Guid.NewGuid();

            ID = progID.Duplicate();

            Description = GetDescription();
        }
        
        protected string GetDescription()
        {
            string Name = "";
            string Info = null;

            switch (ID.Type)
            {
                case ProgramID.Types.System:
                    Name = "Windows NT-Kernel/System"; // Translate.fmt("name_system");
                    break;
                case ProgramID.Types.Global:
                    Name = "All Processes"; // Translate.fmt("name_global");
                    break;
                case ProgramID.Types.Program:
                    Name = System.IO.Path.GetFileName(ID.Path);
                    Info = NtUtilities.GetExeDescription(ID.Path);
                    break;
                case ProgramID.Types.Service:
                    Name = ID.GetServiceId();
                    Info = ProcessMonitor.GetServiceName(Name);
                    break;
                case ProgramID.Types.App:
                    var SID = ID.GetPackageSID();
                    var AppPkg = App.engine.FirewallManager.GetAppPkgBySid(SID);
                    if (AppPkg != null)
                    {
                        Name = AppPkg.ID;
                        Info = App.GetResourceStr(AppPkg.Name);
                    }
                    else
                        Name = SID;
                    break;
            }

            if (Info != null && Info.Length > 0)
                return Info + " (" + Name + ")";
            return Name;
        }

        public bool Update()
        {
            UInt64 uploadRate = 0;
            UInt64 downloadRate = 0;

            UInt64 totalUpload = OldUpload;
            UInt64 totalDownload = OldDownload;

            foreach (DnsEntry Entry in DnsLog.Values)
            {
                Entry.ConCounter = Entry.OldConCounter;
                Entry.TotalUpload = Entry.OldUpload;
                Entry.TotalDownload = Entry.OldDownload;
            }

            SocketsWeb = 0;
            SocketsTcp = 0;
            SocketsUdp = 0;
            SocketsSrv = 0;
            foreach (NetworkSocket Socket in Sockets.Values)
            {
                uploadRate += Socket.Stats.UploadRate.ByteRate;
                downloadRate += Socket.Stats.DownloadRate.ByteRate;

                totalUpload += Socket.Stats.SentBytes;
                totalDownload += Socket.Stats.ReceivedBytes;

                DnsEntry Entry = GetDnsEntry(Socket.RemoteHostName, Socket.RemoteAddress);
                if (Entry != null)
                {
                    Entry.ConCounter++;
                    Entry.TotalUpload += Socket.Stats.SentBytes;
                    Entry.TotalDownload += Socket.Stats.ReceivedBytes;
                }

                if ((Socket.ProtocolType & 0xFF) == (UInt32)IPHelper.AF_PROT.UDP)
                {
                    SocketsUdp++;
                }
                else if ((Socket.ProtocolType & 0xFF) == (UInt32)IPHelper.AF_PROT.TCP)
                {
                    SocketsTcp++;
                    if (Socket.RemotePort == 80 || Socket.RemotePort == 443)
                        SocketsWeb++;
                    if (Socket.State == (int)IPHelper.MIB_TCP_STATE.LISTENING)
                        SocketsSrv++;
                }
            }

            RuleCount = 0;
            EnabledRules = 0;
            DisabledRules = 0;
            ChgedRules = 0;            
            foreach (FirewallRuleEx rule in Rules.Values)
            {
                RuleCount++;
                if (rule.Enabled)
                    EnabledRules++;
                else
                    DisabledRules++;
                if (rule.State != FirewallRuleEx.States.Approved)
                    ChgedRules++;
            }

            if (UploadRate != uploadRate || DownloadRate != downloadRate 
             || TotalUpload != totalUpload || TotalDownload != totalDownload
             || SocketCount != Sockets.Count //|| ActivityChanged
             )
            {
                SocketCount = Sockets.Count;

                UploadRate = uploadRate;
                DownloadRate = downloadRate;

                TotalUpload = totalUpload;
                TotalDownload = totalDownload;

                //ActivityChanged = false;

                return true;
            }
            return false;
        }

        public bool IsSpecial()
        {
            if (ID.Type == ProgramID.Types.System || ID.Type == ProgramID.Types.Global)
                return true;
            return false;
        }

        public bool Exists()
        {
            bool PathMissing = (ID.Path != null && ID.Path.Length > 0 && !File.Exists(ID.Path));

            switch (ID.Type)
            {
                case ProgramID.Types.Program:   return !PathMissing;

                case ProgramID.Types.Service:   var State = ServiceHelper.GetServiceState(ID.GetServiceId());
                                                return (State != ServiceHelper.ServiceState.NotFound) && !PathMissing;

                case ProgramID.Types.App:       var package = App.engine.FirewallManager.GetAppPkgBySid(ID.GetPackageSID());
                                                return package != null && !PathMissing;

                default:                        return true;
            }
        }

        public void AddLogEntry(LogEntry logEntry)
        {
            switch (logEntry.FwEvent.Action)
            {
                case FirewallRule.Actions.Allow:
                    AllowedCount++; 
                    LastAllowed = logEntry.FwEvent.TimeStamp;
                    break;
                case FirewallRule.Actions.Block:
                    BlockedCount++;
                    LastBlocked = logEntry.FwEvent.TimeStamp;
                    break;
            }

            // add to log
            Log.Add(logEntry);
            while (Log.Count > ProgramList.MaxLogLength)
                Log.RemoveAt(0);
        }

        public FirewallRule.Actions LookupRuleAction(FirewallEvent FwEvent, NetworkMonitor.AdapterInfo NicInfo)
        {
            int BlockRules = 0;
            int AllowRules = 0;
            foreach (FirewallRuleEx rule in Rules.Values)
            {
                if (!rule.Enabled)
                    continue;
                if (rule.Direction != FwEvent.Direction)
                    continue;
                if (rule.Protocol != (int)NetFunc.KnownProtocols.Any && FwEvent.Protocol != rule.Protocol)
                    continue;
                if (((int)NicInfo.Profile & rule.Profile) == 0)
                    continue;
                if (rule.Interface != (int)FirewallRule.Interfaces.All && (int)NicInfo.Type != rule.Interface)
                    continue;
                if (!FirewallManager.MatchEndpoint(rule.RemoteAddresses, rule.RemotePorts, FwEvent.RemoteAddress, FwEvent.RemotePort, NicInfo))
                    continue;
                if (!FirewallManager.MatchEndpoint(rule.LocalAddresses, rule.LocalPorts, FwEvent.RemoteAddress, FwEvent.RemotePort, NicInfo))
                    continue;

                rule.HitCount++;

                if (rule.Action == FirewallRule.Actions.Allow)
                    AllowRules++;
                else if (rule.Action == FirewallRule.Actions.Block)
                    BlockRules++;
            }

            // Note: block rules take precedence
            if (BlockRules > 0)
                return FirewallRule.Actions.Block;
            if (AllowRules > 0)
                return FirewallRule.Actions.Allow;
            return FirewallRule.Actions.Undefined;
        }

        public Tuple<int, int> LookupRuleAccess(NetworkSocket Socket)
        {
            int AllowOutProfiles = 0;
            int BlockOutProfiles = 0;
            int AllowInProfiles = 0;
            int BlockInProfiles = 0;

            int Protocol = 0;
            if ((Socket.ProtocolType & 0xFF) == (UInt32)IPHelper.AF_PROT.TCP)
                Protocol = (int)IPHelper.AF_PROT.TCP;
            else if ((Socket.ProtocolType & 0xFF) == (UInt32)IPHelper.AF_PROT.UDP)
                Protocol = (int)IPHelper.AF_PROT.UDP;
            else
                return Tuple.Create(0, 0);

            foreach (FirewallRule rule in Rules.Values)
            {
                if (!rule.Enabled)
                    continue;

                if (rule.Protocol != (int)NetFunc.KnownProtocols.Any && Protocol != rule.Protocol)
                    continue;
                if (Protocol == (int)IPHelper.AF_PROT.TCP)
                {
                    if (!FirewallManager.MatchEndpoint(rule.RemoteAddresses, rule.RemotePorts, Socket.RemoteAddress, Socket.RemotePort))
                        continue;
                }
                if (!FirewallManager.MatchEndpoint(rule.LocalAddresses, rule.LocalPorts, Socket.LocalAddress, Socket.LocalPort))
                    continue;

                switch (rule.Direction)
                {
                    case FirewallRule.Directions.Outbound:
                    {
                        if (rule.Action == FirewallRule.Actions.Allow)
                            AllowOutProfiles |= rule.Profile;
                        else if (rule.Action == FirewallRule.Actions.Block)
                            BlockOutProfiles |= rule.Profile;
                        break;
                    }
                    case FirewallRule.Directions.Inbound:
                    {
                        if (rule.Action == FirewallRule.Actions.Allow)
                            AllowInProfiles |= rule.Profile;
                        else if (rule.Action == FirewallRule.Actions.Block)
                            BlockInProfiles |= rule.Profile;
                        break;
                    }
                }
            }

            for (int i = 0; i < FirewallManager.FwProfiles.Length; i++)
            {
                if ((AllowOutProfiles & (int)FirewallManager.FwProfiles[i]) == 0
                 && (BlockOutProfiles & (int)FirewallManager.FwProfiles[i]) == 0)
                {
                    if (App.engine.FirewallManager.GetDefaultOutboundAction(FirewallManager.FwProfiles[i]) == FirewallRule.Actions.Allow)
                        AllowOutProfiles |= (int)FirewallManager.FwProfiles[i];
                    else
                        BlockOutProfiles |= (int)FirewallManager.FwProfiles[i];
                }

                if ((AllowInProfiles & (int)FirewallManager.FwProfiles[i]) == 0
                 && (BlockInProfiles & (int)FirewallManager.FwProfiles[i]) == 0)
                {
                    if (App.engine.FirewallManager.GetDefaultInboundAction(FirewallManager.FwProfiles[i]) == FirewallRule.Actions.Allow)
                        AllowInProfiles |= (int)FirewallManager.FwProfiles[i];
                    else
                        BlockInProfiles |= (int)FirewallManager.FwProfiles[i];
                }
            }

            AllowOutProfiles &= ~BlockOutProfiles;
            AllowInProfiles &= ~BlockInProfiles;

            return Tuple.Create(AllowOutProfiles, AllowInProfiles);
        }

        [Serializable()]
        [DataContract(Name = "LogEntry", Namespace = "http://schemas.datacontract.org/")]
        public class LogEntry : WithHost
        {
            [DataMember()]
            public Guid guid;

            [DataMember()]
            public ProgramID ProgID;
            [DataMember()]
            public FirewallEvent FwEvent;

            public enum Realms
            {
                Undefined = 0,
                LocalHost,
                MultiCast,
                LocalArea,
                Internet
            }
            [DataMember()]
            public Realms Realm = Realms.Undefined; 

            public enum States
            {
                Undefined = 0,
                FromLog,
                UnRuled, // there was no rule found for this connection
                RuleAllowed,
                RuleBlocked,
                RuleError, // a rule was found but it appears it was not obeyed (!)
            }
            [DataMember()]
            public States State = States.Undefined;

            public void CheckAction(FirewallRule.Actions action)
            {
                switch (action)
                {
                    case FirewallRule.Actions.Undefined:
                        State = States.UnRuled;
                        break;
                    case FirewallRule.Actions.Allow:
                        if (FwEvent.Action == FirewallRule.Actions.Allow)
                            State = LogEntry.States.RuleAllowed;
                        else
                            State = LogEntry.States.RuleError;
                        break;
                    case FirewallRule.Actions.Block:
                        if (FwEvent.Action == FirewallRule.Actions.Block)
                            State = LogEntry.States.RuleBlocked;
                        else
                            State = LogEntry.States.RuleError;
                        break;
                }
            }

            public LogEntry()
            { 
            }

            public LogEntry(FirewallEvent Event, ProgramID progID)
            {
                guid = Guid.NewGuid();
                FwEvent = Event;
                ProgID = progID;

                if (NetFunc.IsLocalHost(FwEvent.RemoteAddress))
                    Realm = Realms.LocalHost;
                else if (NetFunc.IsMultiCast(FwEvent.RemoteAddress))
                    Realm = Realms.MultiCast;
                else if (FirewallManager.MatchAddress(FwEvent.RemoteAddress, FirewallRule.AddrKeywordLocalSubnet))
                    Realm = Realms.LocalArea;
                else
                    Realm = Realms.Internet;
            }
        }

        public void AddSocket(NetworkSocket socket)
        {
            Sockets.Add(socket.guid, socket);

            socket.HostNameChanged += OnHostChanged;
            OnHostChanged(socket, null);
        }

        private void OnHostChanged(object sender, WithHost.ChangeEventArgs e)
        {
            var socket = sender as NetworkSocket;

            // if we get a better host name re asign and if needed remove old entry
            if (e != null)
            {
                string OldName = e.oldName ?? socket.RemoteAddress?.ToString();
                DnsEntry OldEntry;
                if (OldName != null && DnsLog.TryGetValue(OldName, out OldEntry))
                {
                    OldEntry.ConCounter--;
                    if (OldEntry.ConCounter <= 0)
                        DnsLog.Remove(socket.RemoteAddress.ToString());
                }
            }

            DnsEntry Entry = GetDnsEntry(socket.RemoteHostName, socket.RemoteAddress);
            if (Entry != null)
            {
                Entry.ConCounter++;
            }
        }

        public void RemoveSocket(NetworkSocket socket)
        {
            OldUpload += socket.Stats.SentBytes;
            OldDownload += socket.Stats.ReceivedBytes;

            Sockets.Remove(socket.guid);

            DnsEntry Entry = GetDnsEntry(socket.RemoteHostName, socket.RemoteAddress);
            if (Entry != null)
            {
                Entry.OldUpload += socket.Stats.SentBytes;
                Entry.OldDownload += socket.Stats.ReceivedBytes;
                Entry.OldConCounter++;
            }
        }


        [Serializable()]
        [DataContract(Name = "DnsEntry", Namespace = "http://schemas.datacontract.org/")]
        public class DnsEntry
        {
            [DataMember()]
            public Guid guid;
            [DataMember()]
            public ProgramID ProgID;
            [DataMember()]
            public string HostName;
            public bool Unresolved = false;
            //public IPAddress LastSeenIP;
            [DataMember()]
            public DateTime LastSeen;
            [DataMember()]
            public int SeenCounter = 0;

            [DataMember()]
            public int ConCounter = 0;
            [DataMember()]
            public UInt64 TotalUpload = 0;
            [DataMember()]
            public UInt64 TotalDownload = 0;
            // The old values keep the last total of all closed sockets
            public int OldConCounter = 0;
            internal UInt64 OldUpload = 0;
            internal UInt64 OldDownload = 0;

            public DnsEntry()
            {
            }

            public DnsEntry(ProgramID progID)
            {
                guid = Guid.NewGuid();
                ProgID = progID;
            }

            public void Store(XmlWriter writer, bool WithId = false)
            {
                writer.WriteStartElement("Entry");

                if(WithId)
                    ProgID.Store(writer);

                writer.WriteElementString("HostName", HostName);
                writer.WriteElementString("LastSeen", LastSeen.ToString());
                writer.WriteElementString("SeenCounter", SeenCounter.ToString());
                writer.WriteElementString("ConCounter", ConCounter.ToString());

                writer.WriteElementString("ReceivedBytes", TotalDownload.ToString());
                writer.WriteElementString("SentBytes", TotalUpload.ToString());

                writer.WriteEndElement();
            }

            public bool Load(XmlNode entryNode)
            {
                foreach (XmlNode node in entryNode.ChildNodes)
                {
                    if (node.Name == "ID")
                    {
                        ProgID = new ProgramID();
                        ProgID.Load(node);
                    }
                    else if (node.Name == "HostName")
                        HostName = node.InnerText;
                    else if (node.Name == "LastSeen")
                        DateTime.TryParse(node.InnerText, out LastSeen);
                    else if (node.Name == "SeenCounter")
                        int.TryParse(node.InnerText, out SeenCounter);
                    else if (node.Name == "ConCounter")
                        int.TryParse(node.InnerText, out OldConCounter);
                    else if (node.Name == "ReceivedBytes")
                        UInt64.TryParse(node.InnerText, out OldDownload);
                    else if (node.Name == "SentBytes")
                        UInt64.TryParse(node.InnerText, out OldUpload);
                }
                return HostName != null;
            }
        }

        public void LogDomain(string HostName, DateTime TimeStamp)
        {
            DnsEntry Entry = null;
            if (!DnsLog.TryGetValue(HostName, out Entry))
            {
                Entry = new DnsEntry(ID);
                Entry.HostName = HostName;
                DnsLog.Add(HostName, Entry);
            }
            else if (Entry.LastSeen == TimeStamp)
                return; // dont count duplicates

            Entry.LastSeen = TimeStamp;
            //Entry.LastSeenIP = IP;
            Entry.SeenCounter++;
        }

        public DnsEntry GetDnsEntry(string HostName, IPAddress Address)
        {
            if (HostName == null && (Address == null || Address.Equals(IPAddress.Any) || Address.Equals(IPAddress.IPv6Any)))
                return null;

            bool Unresolved = HostName == null;
            if (Unresolved)
                HostName = Address.ToString();

            if (HostName.Length == 0)
                return null;

            DnsEntry Entry;
            if (!DnsLog.TryGetValue(HostName, out Entry))
            {
                Entry = new DnsEntry(ID);
                Entry.Unresolved = Unresolved;
                Entry.HostName = HostName;
                Entry.LastSeen = DateTime.Now;
                Entry.SeenCounter++;
                DnsLog.Add(HostName, Entry);
            }
            return Entry;
        }

        public void Store(XmlWriter writer)
        {
            writer.WriteStartElement("Program");

            // Note: ID must be first!!!
            ID.Store(writer);

            writer.WriteElementString("Description", Description);

            writer.WriteElementString("ReceivedBytes", TotalDownload.ToString());
            writer.WriteElementString("SentBytes", TotalUpload.ToString());

            writer.WriteStartElement("FwRules");
            foreach (FirewallRuleEx rule in Rules.Values)
                rule.Store(writer);
            writer.WriteEndElement();

            writer.WriteStartElement("DnsLog");
            foreach (DnsEntry Entry in DnsLog.Values)
                Entry.Store(writer);
            writer.WriteEndElement();

            writer.WriteEndElement();
        }

        public bool Load(XmlNode entryNode)
        {
            foreach (XmlNode node in entryNode.ChildNodes)
            {
                if (node.Name == "ID")
                {
                    ProgramID id = new ProgramID();
                    if (id.Load(node))
                    {
                        // COMPAT: remove service tag
                        ID = FirewallRuleEx.AdjustProgID(id);
                    }
                }
                else if (node.Name == "Description")
                    Description = node.InnerText;
                else if (node.Name == "ReceivedBytes")
                    UInt64.TryParse(node.InnerText, out OldDownload);
                else if (node.Name == "SentBytes")
                    UInt64.TryParse(node.InnerText, out OldUpload);
                else if (node.Name == "FwRules")
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        FirewallRuleEx rule = new FirewallRuleEx();
                        rule.ProgID = ID; // todo: remove later, load loads this amyways
                        if (rule.Load(childNode) && !Rules.ContainsKey(rule.guid))
                        {
                            // COMPAT: update entry, old version did not save these data separatly
                            //if (ID.Type != ProgramID.Types.Global && (rule.BinaryPath == null && rule.ServiceTag == null && rule.AppSID == null))
                            //    rule.SetProgID(ID);
                            
                            Rules.Add(rule.guid, rule);
                        }
                        else
                            Priv10Logger.LogError("Failed to load Firewall RuleEx {0} in {1}", rule.Name != null ? rule.Name : "[un named]", this.Description);
                    }
                }
                else if (node.Name == "DnsLog")
                {
                    foreach (XmlNode childNode in node.ChildNodes)
                    {
                        DnsEntry Entry = new DnsEntry(ID);
                        if (Entry.Load(childNode) && !DnsLog.ContainsKey(Entry.HostName))
                            DnsLog.Add(Entry.HostName, Entry);
                        else
                            Priv10Logger.LogError("Failed to load DnsLog Entry in {0}", this.Description);
                    }
                }
                else
                    AppLog.Debug("Unknown Program Value, '{0}':{1}", node.Name, node.InnerText);
            }

            if(Description == null || Description.Substring(0,2) == "@{")
                Description = GetDescription();

            return ID != null;
        }
    }
}

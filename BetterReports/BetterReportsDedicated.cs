using LiteDB;
using NetworkedPlugins.API;
using NetworkedPlugins.API.Enums;
using NetworkedPlugins.API.Events.Player;
using NetworkedPlugins.API.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BetterReports
{
    public class BetterReportsDedicated : NPAddonDedicated<BetterReportsDedicatedConfig, BetterReportsRemoteConfig>
    {
        public override string AddonId { get; } = "sdAyWFypb3n4J75Wxpns";
        public override string AddonAuthor { get; } = "Killers0992";
        public override string AddonName { get; } = "BetterReports";
        public override Version AddonVersion { get; } = new Version(1,0,1);

        public override NPPermissions Permissions { get; } = new NPPermissions()
        {
            ReceivePermissions = new List<AddonSendPermissionTypes>()
            {
                AddonSendPermissionTypes.PlayerNickname,
            },
            SendPermissions = new List<AddonReceivePermissionTypes>()
            {
                AddonReceivePermissionTypes.HintMessages,
                AddonReceivePermissionTypes.RedirectPlayer,
                AddonReceivePermissionTypes.ReportMessages,
                AddonReceivePermissionTypes.RemoteAdminNewCommands,
                AddonReceivePermissionTypes.RemoteAdminMessages,
            }
        };

        public string path = "";
        public LiteDatabase db
        {
            get
            {
                return BetterReportsHandler.Handler.GetDatabase(Server, path);
            }
        }

        public static Dictionary<NPServer, BetterReportsDedicated> Servers { get; } = new Dictionary<NPServer, BetterReportsDedicated>();

        public override void OnEnable()
        {
            Servers.Add(Server, this);
            if (string.IsNullOrEmpty(Server.ServerConfig.LinkToken))
            {
                path = Path.Combine(ServerPath, "Reports.db");
            }
            else
            {
                if (!Directory.Exists(Path.Combine("serverlinks")))
                    Directory.CreateDirectory(Path.Combine("serverlinks"));
                if (!Directory.Exists(Path.Combine("serverlinks", Server.ServerConfig.LinkToken)))
                    Directory.CreateDirectory(Path.Combine("serverlinks", Server.ServerConfig.LinkToken));

                path = Path.Combine("serverlinks", Server.ServerConfig.LinkToken, "Reports.db");
            }

            this.PlayerLocalReport += OnLocalPlayerReport;
            base.OnEnable();
        }

        private void OnLocalPlayerReport(PlayerLocalReportEvent ev)
        {
            var reportCol = db.GetCollection<ReportModel>("reports");
            int freeID = 0;
            List<int> ids = reportCol.FindAll().Select(p => p.TicketID).ToList();
            for (int i = 1; i < int.MaxValue; i++)
            {
                if (ids.Any(p => p == i))
                    continue;

                freeID = i;
                break;
            }

            string outMessage = RemoteConfig.Messages.new_ticket.
                Replace("%id%", freeID.ToString()).
                Replace("%issuer_id%", ev.Player.UserID).
                Replace("%issuer_nick%", ev.Player.Nickname).
                Replace("%target_id%", ev.TargetPlayer.UserID).
                Replace("%target_nick%", ev.TargetPlayer.Nickname).
                Replace("%reason%", ev.Reason).
                Replace("%server_ip%", Server.ServerAddress).
                Replace("%server_port%", Server.ServerPort.ToString());

            foreach (var sv in GetServers())
            {
                foreach (var plr in sv.Players)
                {
                    if (plr.RemoteAdminAccess)
                        plr.SendHint(outMessage, RemoteConfig.HintDuration);
                }
            }
            var mod = new ReportModel()
            {
                TicketID = freeID,
                Status = (byte)0,
                IssuerID = ev.Player.UserID,
                IssuerNICK = ev.Player.Nickname,
                IssueTime = DateTime.Now,
                Reason = ev.Reason,
                TargetID = ev.TargetPlayer.UserID,
                TargetNICK = ev.TargetPlayer.Nickname,
                ClosedbyID = "",
                ClosedbyNICK = "",
                ClosedTime = DateTime.Now,
                Response = "",
                ServerIP = Server.ServerAddress,
                ServertPort = Server.ServerPort
            };
            reportCol.Insert(freeID, mod);
        }

        public override void OnDisable()
        {
            Servers.Remove(Server);
            BetterReportsHandler.Handler.UnregisterDB(Server);
            this.PlayerLocalReport -= OnLocalPlayerReport;
            base.OnDisable();
        }

        public List<ReportModel> GetReports()
        {
            var reportCol = db.GetCollection<ReportModel>("reports").FindAll();
            return reportCol.ToList();
        }

        public void DenyTicket(int ticketId, string userid, string nick, string response)
        {
            var col3 = db.GetCollection<ReportModel>("reports");
            var ticket = col3.FindById(ticketId);
            if (ticket != null)
            {
                if (ticket.Status == (byte)0)
                {
                    ticket.Status = (byte)2;
                    ticket.ClosedbyID = userid;
                    ticket.ClosedbyNICK = nick;
                    ticket.ClosedTime = DateTime.Now;
                    ticket.Response = response;
                    col3.Update(ticket.TicketID, ticket);
                    string messageAdmin = RemoteConfig.Messages.ticket_declined.
                        Replace("%response%", response).
                        Replace("%id%", ticket.TicketID.ToString()).
                        Replace("%issuer_id%", userid).
                        Replace("%issuer_nick%", nick);
                    string messageClient = RemoteConfig.Messages.ticket_declined_response.
                        Replace("%response%", response).
                        Replace("%id%", ticket.TicketID.ToString()).
                        Replace("%issuer_id%", userid).
                        Replace("%issuer_nick%", nick);

                    foreach (var sv in GetServers())
                    {
                        var plr2 = sv.GetPlayer(ticket.IssuerID);
                        if (plr2 != null)
                        {
                            plr2.SendReportMessage(messageClient);
                        }
                        foreach (var plr in sv.Players)
                        {
                            if (plr.RemoteAdminAccess)
                                plr.SendHint(messageAdmin, RemoteConfig.HintDuration);
                        }
                    }
                }
            }

    }

        public void AcceptTicket(int ticketId, string userid, string nick, string response)
        {
            var col3 = db.GetCollection<ReportModel>("reports");
            var ticket = col3.FindById(ticketId);
            if (ticket != null)
            {
                if (ticket.Status == (byte)0)
                {
                    ticket.Status = (byte)1;
                    ticket.ClosedbyID = userid;
                    ticket.ClosedbyNICK = nick;
                    ticket.ClosedTime = DateTime.Now;
                    ticket.Response = response;
                    col3.Update(ticket.TicketID, ticket);
                    string messageAdmin = RemoteConfig.Messages.ticket_accepted.
                        Replace("%response%", response).
                        Replace("%id%", ticket.TicketID.ToString()).
                        Replace("%issuer_id%", userid).
                        Replace("%issuer_nick%", nick);
                    string messageClient = RemoteConfig.Messages.ticket_accepted_response.
                        Replace("%response%", response).
                        Replace("%id%", ticket.TicketID.ToString()).
                        Replace("%issuer_id%", userid).
                        Replace("%issuer_nick%", nick);

                    foreach (var sv in GetServers())
                    {
                        var plr2 = sv.GetPlayer(ticket.IssuerID);
                        if (plr2 != null)
                        {
                            plr2.SendReportMessage(messageClient);
                        }
                        foreach (var plr in sv.Players)
                        {
                            if (plr.RemoteAdminAccess)
                                plr.SendHint(messageAdmin, RemoteConfig.HintDuration);
                        }
                    }
                }
            }
        }
    }
}

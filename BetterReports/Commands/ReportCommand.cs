using NetworkedPlugins.API;
using NetworkedPlugins.API.Enums;
using NetworkedPlugins.API.Interfaces;
using NetworkedPlugins.API.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterReports.Commands
{
    public class ReportCommand : ICommand
    {
        public string CommandName { get; } = "REPORT";

        public string Description { get; } = "Command for management of server reports.";

        public string Permission { get; } = "betterreports.report";

        public CommandType Type { get; } = CommandType.RemoteAdmin;

        public void Invoke(NPPlayer player, string[] arguments)
        {
            var brd = BetterReportsDedicated.Servers[player.Server];
            if (brd == null)
                return;

            if (arguments.Length == 0)
            {
                player.SendRAMessage(string.Concat(" Commands: ",
                    Environment.NewLine,
                    " - REPORT deny <id> <response> - Decline report.",
                    Environment.NewLine,
                    " - REPORT accept <id> <response> - Accept report.",
                    Environment.NewLine,
                    " - REPORT list - List of avaliable reports.",
                    Environment.NewLine,
                    " - REPORT goto <id> - Change current server to issuers server of report."));
            }
            else
            {
                switch (arguments[0].ToUpper())
                {
                    case "LIST":
                        var col = brd.db.GetCollection<ReportModel>("reports");
                        player.SendRAMessage(brd.RemoteConfig.Messages.avaliable_reports);
                        var reports = col.FindAll();
                        if (reports.Count() == 0)
                        {
                            player.SendRAMessage(brd.RemoteConfig.Messages.no_reports_avaliable);
                        }
                        foreach (var rep in reports)
                        {
                            if (rep.Status == (byte)0)
                                player.SendRAMessage(brd.RemoteConfig.Messages.report_list_item
                                    .Replace("%report_id%", $"{rep.TicketID}")
                                    .Replace("%issuer_nick%", rep.IssuerNICK)
                                    .Replace("%target_nick%", rep.TargetNICK)
                                    .Replace("%issuer_id%", rep.IssuerID)
                                    .Replace("%target_id%", rep.TargetID)
                                    .Replace("%reason%", rep.Reason)
                                    .Replace("%server_ip%", rep.ServerIP)
                                    .Replace("%server_port%", $"{rep.ServertPort}"));
                        }
                        break;
                    case "GOTO":
                        if (arguments.Length == 2)
                        {
                            if (int.TryParse(arguments[1], out int repID))
                            {
                                var col2 = brd.db.GetCollection<ReportModel>("reports");
                                var ticket = col2.FindById(repID);
                                if (ticket != null)
                                {
                                    foreach (var srv in brd.GetServers())
                                    {
                                        var srvPlr = srv.GetPlayer(ticket.TargetID);
                                        if (srvPlr != null)
                                        {
                                            string mem = brd.RemoteConfig.Messages.ticket_admin_onway.Replace("%admin_name%", player.Nickname);
                                            srvPlr.SendHint(mem, 5f);
                                            player.SendRAMessage(brd.RemoteConfig.Messages.redirecting_to_server
                                                .Replace("%server_ip%", srv.ServerAddress)
                                                .Replace("%server_port%", $"{srv.ServerPort}"));
                                            player.Redirect(srv.ServerPort);
                                            return;
                                        }
                                    }
                                    foreach (var srv in brd.GetServers())
                                    {
                                        var srvPlr = srv.GetPlayer(ticket.IssuerID);
                                        if (srvPlr != null)
                                        {
                                            string mem = brd.RemoteConfig.Messages.ticket_admin_onway.Replace("%admin_name%", player.Nickname);
                                            srvPlr.SendHint(mem, 5f);
                                            player.SendRAMessage(brd.RemoteConfig.Messages.redirecting_to_server
                                                .Replace("%server_ip%", srv.ServerAddress)
                                                .Replace("%server_port%", $"{srv.ServerPort}"));
                                            player.Redirect(srv.ServerPort);
                                            return;
                                        }
                                    }
                                    player.SendRAMessage(brd.RemoteConfig.Messages.issued_player_not_found);
                                }
                                else
                                {
                                    player.SendRAMessage(brd.RemoteConfig.Messages.report_not_found.Replace("%report_id%", $"{repID}"));
                                }
                            }
                        }
                        else
                        {
                            player.SendRAMessage($"Syntax: REPORT goto <id>");
                        }
                        break;
                    case "DENY":
                        if (arguments.Length > 2)
                        {
                            if (int.TryParse(arguments[1], out int repID))
                            {
                                string message = string.Join(" ", arguments.Skip(2));
                                var col3 = brd.db.GetCollection<ReportModel>("reports");
                                var ticket = col3.FindById(repID);
                                if (ticket != null)
                                {
                                    if (ticket.Status == (byte)0)
                                    {
                                        ticket.Status = (byte)2;
                                        ticket.ClosedbyID = player.UserID;
                                        ticket.ClosedbyNICK = player.Nickname;
                                        ticket.ClosedTime = DateTime.Now;
                                        ticket.Response = message;
                                        col3.Update(ticket.TicketID, ticket);
                                        string messageAdmin = brd.RemoteConfig.Messages.ticket_declined.
                                            Replace("%response%", message).
                                            Replace("%id%", ticket.TicketID.ToString()).
                                            Replace("%issuer_id%", player.UserID).
                                            Replace("%issuer_nick%", player.Nickname);
                                        string messageClient = brd.RemoteConfig.Messages.ticket_declined_response.
                                            Replace("%response%", message).
                                            Replace("%id%", ticket.TicketID.ToString()).
                                            Replace("%issuer_id%", player.UserID).
                                            Replace("%issuer_nick%", player.Nickname);

                                        foreach (var sv in brd.GetServers())
                                        {
                                            var plr2 = sv.GetPlayer(ticket.IssuerID);
                                            if (plr2 != null)
                                            {
                                                plr2.SendReportMessage(messageClient);
                                            }
                                            foreach (var plr in sv.Players)
                                            {
                                                if (plr.RemoteAdminAccess)
                                                    plr.SendHint(messageAdmin, brd.RemoteConfig.HintDuration);
                                            }
                                        }
                                        player.SendRAMessage(brd.RemoteConfig.Messages.report_declined
                                            .Replace("%report_id%", $"{repID}")
                                            .Replace("%response%", message));
                                    }
                                    else
                                    {
                                        player.SendRAMessage(brd.RemoteConfig.Messages.report_already
                                            .Replace("%state%", ticket.Status == 1 ? brd.RemoteConfig.Messages.state_accepted : brd.RemoteConfig.Messages.report_declined)
                                            .Replace("%closedby_nick%", ticket.ClosedbyNICK)
                                            .Replace("%closedby_id%", $"{ticket.ClosedbyID}"));
                                    }
                                }
                                else
                                {
                                    player.SendRAMessage(brd.RemoteConfig.Messages.report_not_found.Replace("%report_id%", $"{repID}"));
                                }
                            }
                        }
                        else
                        {
                            player.SendRAMessage($"Syntax: REPORT accept <id> <response>");
                        }
                        break;
                    case "ACCEPT":
                        if (arguments.Length > 2)
                        {
                            if (int.TryParse(arguments[1], out int repID))
                            {
                                string message = string.Join(" ", arguments.Skip(2));
                                var col4 = brd.db.GetCollection<ReportModel>("reports");
                                var ticket = col4.FindById(repID);
                                if (ticket != null)
                                {
                                    if (ticket.Status == (byte)0)
                                    {
                                        ticket.Status = (byte)1;
                                        ticket.ClosedbyID = player.UserID;
                                        ticket.ClosedbyNICK = player.Nickname;
                                        ticket.ClosedTime = DateTime.Now;
                                        ticket.Response = message;
                                        col4.Update(ticket.TicketID, ticket);
                                        string messageAdmin = brd.RemoteConfig.Messages.ticket_accepted_response.
                                            Replace("%response%", message).
                                            Replace("%id%", ticket.TicketID.ToString()).
                                            Replace("%issuer_id%", player.UserID).
                                            Replace("%issuer_nick%", player.Nickname);
                                        string messageClient = brd.RemoteConfig.Messages.ticket_accepted_response.
                                            Replace("%response%", message).
                                            Replace("%id%", ticket.TicketID.ToString()).
                                            Replace("%issuer_id%", player.UserID).
                                            Replace("%issuer_nick%", player.Nickname);


                                        foreach (var sv in brd.GetServers())
                                        {
                                            var plr2 = sv.GetPlayer(ticket.IssuerID);
                                            if (plr2 != null)
                                            {
                                                plr2.SendReportMessage(messageClient);
                                            }
                                            foreach (var plr in sv.Players)
                                            {
                                                if (plr.RemoteAdminAccess)
                                                    plr.SendHint(messageAdmin, brd.RemoteConfig.HintDuration);
                                            }
                                        }
                                        player.SendRAMessage(brd.RemoteConfig.Messages.state_accepted
                                            .Replace("%report_id%", $"{repID}")
                                            .Replace("%response%", message));
                                    }
                                    else
                                    {
                                        player.SendRAMessage(brd.RemoteConfig.Messages.report_already
                                            .Replace("%state%", ticket.Status == 1 ? brd.RemoteConfig.Messages.state_accepted : brd.RemoteConfig.Messages.report_declined)
                                            .Replace("%closedby_nick%", ticket.ClosedbyNICK)
                                            .Replace("%closedby_id%", $"{ticket.ClosedbyID}"));
                                    }
                                }
                                else
                                {
                                    player.SendRAMessage(brd.RemoteConfig.Messages.report_not_found.Replace("%report_id%", $"{repID}"));
                                }
                            }
                        }
                        else
                        {
                            player.SendRAMessage($"Syntax: REPORT accept <id> <response>");
                        }
                        break;
                }
            }
        }
    }
}

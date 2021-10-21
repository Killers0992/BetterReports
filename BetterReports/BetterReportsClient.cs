using Exiled.Events.EventArgs;
using LiteNetLib.Utils;
using NetworkedPlugins.API;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BetterReports
{
    public class BetterReportsClient : NPAddonClient<BetterReportsClientConfig>
    {
        public override string AddonId { get; } = "BP032DxpREPORTS";
        public override string AddonAuthor { get; } = "Killers0992";
        public override string AddonName { get; } = "BetterReports";
        public override Version AddonVersion { get; } = new Version(1, 0, 0);


        public override void OnEnable()
        {
            Exiled.Events.Handlers.Server.LocalReporting += OnLocalReport;
        }

        private void OnLocalReport(LocalReportingEventArgs ev)
        {
            NetDataWriter writer = new NetDataWriter();
            writer.Put((byte)0);
            writer.Put(ev.Issuer.UserId);
            writer.Put(ev.Target.UserId);
            writer.Put(ev.Reason);
            SendData(writer);
        }
    }
}

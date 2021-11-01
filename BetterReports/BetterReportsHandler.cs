using LiteDB;
using NetworkedPlugins.API;
using NetworkedPlugins.API.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BetterReports
{
    public class BetterReportsHandler : NPAddonHandler<BetterReportsDedicatedConfig>
    {
        public static Dictionary<string, LiteDatabase> ReportsHandlers { get; } = new Dictionary<string, LiteDatabase>();

        public static Dictionary<NPServer, string> Servers { get; } = new Dictionary<NPServer, string>();

        public LiteDatabase GetDatabase(NPServer server, string path)
        {
            if (Servers.TryGetValue(server, out string targetDB))
                return ReportsHandlers[targetDB];
            else
            {
                string id = server.FullAddress;
                if (!string.IsNullOrEmpty(server.ServerConfig.LinkToken))
                    id = server.ServerConfig.LinkToken;

                if (!ReportsHandlers.ContainsKey(id))
                    ReportsHandlers.Add(id, new LiteDatabase(path));

                Servers.Add(server, id);
                return ReportsHandlers[id];
            }
        }

        public void UnregisterDB(NPServer server)
        {
            if (Servers.TryGetValue(server, out string db))
            {
                if (Servers.Where(p => p.Value == db).Count() == 1)
                {
                    if (ReportsHandlers.TryGetValue(db, out LiteDatabase database))
                    {
                        database.Dispose();
                    }
                    ReportsHandlers.Remove(db);
                }
                Servers.Remove(server);
            }
        }

        public static BetterReportsHandler Handler;

        public override void OnEnable()
        {
            Handler = this;
            base.OnEnable();
        }

        public override void OnDisable()
        {
            Handler = null;
            base.OnDisable();
        }
    }
}

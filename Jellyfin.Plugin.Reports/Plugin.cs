using System;
using System.Collections.Generic;
using Jellyfin.Plugin.Reports.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Common.Plugins;
using MediaBrowser.Model.Plugins;
using MediaBrowser.Model.Serialization;

namespace Jellyfin.Plugin.Reports
{
    public class Plugin : BasePlugin<PluginConfiguration>, IHasWebPages
    {
        public Plugin(IApplicationPaths appPaths, IXmlSerializer xmlSerializer)
            : base(appPaths, xmlSerializer)
        {
        }

        public override string Name => "Reports";

        public override string Description => "Generate Reports";

        public PluginConfiguration PluginConfiguration => Configuration;

        public override Guid Id => new Guid("d4312cd9-5c90-4f38-82e8-51da566790e8");

        public IEnumerable<PluginPageInfo> GetPages()
        {
            return new PluginPageInfo[]
            {
                new PluginPageInfo
                {
                    Name = "reports",
                    EmbeddedResourcePath = GetType().Namespace + ".Web.reports.html",
                    EnableInMainMenu = true
                },
                new PluginPageInfo
                {
                    Name = "reportsjs",
                    EmbeddedResourcePath = GetType().Namespace + ".Web.reports.js"
                }
            };
        }
    }
}

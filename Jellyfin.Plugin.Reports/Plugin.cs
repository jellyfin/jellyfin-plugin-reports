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


        public override string Description
            => "Generate Reports";

        public PluginConfiguration PluginConfiguration => Configuration;

        private Guid _id = new Guid("2FE79C34-C9DC-4D94-9DF2-2F3F36764414");
        public override Guid Id
        {
            get { return _id; }
        }

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

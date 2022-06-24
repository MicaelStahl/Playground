using System.Web.Mvc;
using System.Web.Routing;
using EPiServer.Framework;
using EPiServer.Framework.Initialization;

namespace Playground.Features.Plugins.GetPagesOfTypePlugin.Initialization
{
    [InitializableModule]
    public class PluginRouteInitialization : IInitializableModule
    {
        public void Initialize(InitializationEngine context)
        {
            RouteTable.Routes.MapRoute(
                "GetPagesOfTypePlugin",
                "custom-plugins/getpagesoftype",
                new { controller = "GetPagesOfTypePlugin", action = "Index" });
        }

        public void Uninitialize(InitializationEngine context)
        {
        }
    }
}
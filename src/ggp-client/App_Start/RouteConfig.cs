using System.Web.Mvc;
using System.Web.Routing;
using ggp_client.Helpers;

namespace ggp_client
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            routes.MapMvcAttributeRoutes(new InheritedDirectRouteProvider());
        }
    }
}

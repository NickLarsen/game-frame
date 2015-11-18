using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Mvc.Routing;

namespace ggp_client.Helpers
{
    public class InheritedDirectRouteProvider : DefaultDirectRouteProvider
    {
        protected override IReadOnlyList<IDirectRouteFactory>
            GetActionRouteFactories(ActionDescriptor actionDescriptor)
        {
            return actionDescriptor.GetCustomAttributes(typeof(IDirectRouteFactory), inherit: true).Cast<IDirectRouteFactory>().ToArray();
        }
    }
}
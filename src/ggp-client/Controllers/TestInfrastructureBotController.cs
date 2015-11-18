using System.Web.Mvc;
using ggp_client.Helpers;
using ggp_client.Models;

namespace ggp_client.Controllers
{
    [RoutePrefix("test-infrastructure")]
    public class TestInfrastructureBotController : GameManagerController
    {
        protected override string HandleRequest(InfoMessage message)
        {
            return "ready";
        }

        protected override string HandleRequest(StartMessage message)
        {
            return "ready";
        }

        protected override string HandleRequest(PlayMessage message)
        {
            return "(mark 1 1)";
        }

        protected override string HandleRequest(StopMessage message)
        {
            return "done";
        }

        protected override string HandleRequest(AbortMessage message)
        {
            return "done";
        }
    }
}
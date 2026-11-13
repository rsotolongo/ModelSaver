using System.Web.Mvc;
using System.Web.Routing;

namespace ProQualEx.ModelSaver.Mvc5.Sample;

public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start()
    {
        AreaRegistration.RegisterAllAreas();
        RouteConfig.RegisterRoutes(RouteTable.Routes);
    }
}

using System.Web.Mvc;
using System.Web.Routing;
using ProQualEx.ModelSaver.Mvc5.Sample.Routing;

namespace ProQualEx.ModelSaver.Mvc5.Sample;

public class MvcApplication : System.Web.HttpApplication
{
    protected void Application_Start(object sender, EventArgs e)
    {
        AreaRegistration.RegisterAllAreas();
        RouteConfig.RegisterRoutes(RouteTable.Routes);
    }
}

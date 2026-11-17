using System.Web.Mvc;
using System.Web.Routing;
using ProQualEx.ModelSaver.Mvc5.Sample.Routing;

namespace ProQualEx.ModelSaver.Mvc5.Sample;

/// <summary>
/// The ASP.NET application host for the sample.
/// </summary>
public class MvcApplication : System.Web.HttpApplication
{
    /// <summary>
    /// Registers areas and routes when the application starts.
    /// </summary>
    /// <param name="sender">The event source.</param>
    /// <param name="e">The event data.</param>
    protected void Application_Start(object sender, EventArgs e)
    {
        AreaRegistration.RegisterAllAreas();
        RouteConfig.RegisterRoutes(RouteTable.Routes);
    }
}

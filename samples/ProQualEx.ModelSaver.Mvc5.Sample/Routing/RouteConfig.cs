using System.Web.Mvc;
using System.Web.Routing;

namespace ProQualEx.ModelSaver.Mvc5.Sample.Routing;

/// <summary>
/// Registers the sample's routes.
/// </summary>
public static class RouteConfig
{
    /// <summary>
    /// Adds the default route, which opens the order edit page.
    /// </summary>
    /// <param name="routes">The route collection to add to.</param>
    public static void RegisterRoutes(RouteCollection routes)
    {
        routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

        routes.MapRoute(
            name: "Default",
            url: "{controller}/{action}/{id}",
            defaults: new { controller = "Order", action = "Edit", id = UrlParameter.Optional });
    }
}

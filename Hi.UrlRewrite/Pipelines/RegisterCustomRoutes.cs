using System.Web.Mvc;
using System.Web.Routing;
using Sitecore.Mvc.Pipelines.Loader;
using Sitecore.Pipelines;

namespace Hi.UrlRewrite.Pipelines
{
  public class RegisterCustomRoutes : InitializeRoutes
  {
    public override void Process(PipelineArgs args)
    {
      Sitecore.Diagnostics.Log.Info("Sitecore is registering custom routes", this);
      RouteTable.Routes.MapRoute("HiUrlRewrite-RedirectExport", "api/urlrewrite/{controller}/{action}");
    }
  }
}
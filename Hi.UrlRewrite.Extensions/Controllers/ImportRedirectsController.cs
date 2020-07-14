using System;
using System.IO;
using System.Web.Mvc;
using Hi.UrlRewrite.Extensions.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Mvc.Controllers;
using Sitecore.Sites;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  public class ImportRedirectsController : SitecoreController
  {
    public ActionResult ImportRedirects(string csvItemId, string rootFolderId = "")
    {
      using (new SiteContextSwitcher(SiteContextFactory.GetSiteContext("master")))
      {
        var rootFolder = Sitecore.Context.Database.GetItem(ID.Parse(Guid.Parse(rootFolderId)));
        var csvItem = Sitecore.Context.Database.GetItem(ID.Parse(Guid.Parse(csvItemId)));
        var csvMediaItem = new MediaItem(csvItem);

        var stream = csvMediaItem.GetMediaStream();

        RedirectImportService importService = new RedirectImportService();
        importService.GenerateRedirectsFromCsv(stream, rootFolder);

        throw new NotImplementedException();
      }
    }
  }
}
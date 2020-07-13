using System;
using System.IO;
using System.Web.Mvc;
using Hi.UrlRewrite.Extensions.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Mvc.Controllers;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  public class ImportRedirectsController : SitecoreController
  {
    public ActionResult ImportRedirects(string csvItemId, string rootFolderId = "")
    {
      // since this route is only mapped for CM and Standalone, the master database can be accessed directly
      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      //var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));
      var csvItem = db.GetItem(ID.Parse(Guid.Parse(csvItemId)));
      var csvMediaItem = new Sitecore.Data.Items.MediaItem(csvItem);

      var stream = csvMediaItem.GetMediaStream();

      RedirectImportService.GenerateRedirectsFromCsv(stream);

      throw new NotImplementedException();
    }
  }
}
using System;
using System.IO;
using System.Web.Mvc;
using Hi.UrlRewrite.Extensions.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;
using Sitecore.Sites;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  public class ImportRedirectsController : SitecoreController
  {
    public ActionResult ImportRedirects(string csvItemId, string rootFolderId)
    {
      Assert.IsNotNullOrEmpty(csvItemId, "CSV item Id must not be empty");
      Assert.IsNotNullOrEmpty(rootFolderId, "Root folder item Id must not be empty");

      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));
      var csvItem = db.GetItem(ID.Parse(Guid.Parse(csvItemId)));
      var csvMediaItem = new MediaItem(csvItem);

      var stream = csvMediaItem.GetMediaStream();

      // since this route is only mapped for CM instances, we can use the master database.
      RedirectImportService importService = new RedirectImportService(db);
      importService.GenerateRedirectsFromCsv(stream, rootFolder);

      throw new NotImplementedException();
    }
  }
}
using System;
using System.Web.Mvc;
using Hi.UrlRewrite.Extensions.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  public class ImportRedirectsController : SitecoreController
  {
    public ActionResult ImportRedirects(string csvItemId, string rootFolderId)
    {
      Assert.IsNotNullOrEmpty(csvItemId, "CSV item Id must not be empty");
      Assert.IsNotNullOrEmpty(rootFolderId, "Root folder item Id must not be empty");

      // since this route is only mapped for CM instances, we can use the master database.
      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));

      if (!IsValidRootFolder(rootFolder))
      {
        return Content("Please select a Redirect Root folder for importing.");
      }

      var csvItem = db.GetItem(ID.Parse(Guid.Parse(csvItemId)));
      var csvMediaItem = new MediaItem(csvItem);

      var stream = csvMediaItem.GetMediaStream();

      var importService = new RedirectImportService(db);
      importService.GenerateRedirectsFromCsv(stream, rootFolder);

      var logFile = MediaItemWriter.WriteFile(importService.Warnings, db, Constants.LogPath, MediaItemWriter.GetFileName(rootFolder, "Import"), ".log");

      return Content(logFile.ToString());
    }

    private static bool IsValidRootFolder(Item rootFolder)
    {
      return rootFolder != null && rootFolder.TemplateID.ToString() == Templates.Folders.RedirectFolderItem.TemplateId;
    }
  }
}
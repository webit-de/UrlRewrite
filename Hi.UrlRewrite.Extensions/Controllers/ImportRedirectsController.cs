using System;
using System.IO;
using System.Web.Mvc;
using Hi.UrlRewrite.Extensions.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.Mvc.Controllers;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  [Authorize]
  public class ImportRedirectsController : SitecoreController
  {
    /// <summary>
    /// Import redirects from a csv media item
    /// </summary>
    /// <param name="csvItemId">The item id</param>
    /// <param name="rootFolderId">The id of the root folder to import into.</param>
    /// <returns>The warning log as csv.</returns>
    public ActionResult ImportRedirects(string csvItemId, string rootFolderId)
    {
      Assert.IsNotNullOrEmpty(csvItemId, "CSV item Id must not be empty");
      Assert.IsNotNullOrEmpty(rootFolderId, "Root folder item Id must not be empty");

      // since this route is only mapped for CM instances, the master database can be used.
      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));

      if (!IsValidRootFolder(rootFolder))
      {
        return Content("Please select a Redirect Root folder for importing.");
      }

      var csvStream = CsvService.GetCsvStream(db, csvItemId);
      if (csvStream == null)
      {
        return Content("Could not create a csv stream from the provided item.");
      }

      var importService = new RedirectImportService(db);
      var warningLog = importService.GenerateRedirectsFromCsv(csvStream, rootFolder);
      
      return Content(warningLog);
    }

    /// <summary>
    /// Check if the root folder id points to a redirect folder item
    /// </summary>
    /// <param name="rootFolder">The root folder id</param>
    /// <returns>True, if the id points to a RedirectFolderItem</returns>
    private static bool IsValidRootFolder(Item rootFolder)
    {
      return rootFolder != null && rootFolder.TemplateID.ToString() == Templates.Folders.RedirectFolderItem.TemplateId;
    }
  }
}
using System;
using System.IO;
using System.Web.Mvc;
using Hi.UrlRewrite.Extensions.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Mvc.Controllers;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  public class ExportRedirectsController : SitecoreController
  {
    public ActionResult ExportRedirects(string rootFolderId, bool recursive)
    {
      // since this route is only mapped 
      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));

      if (!IsValidRootFolder(rootFolder))
      {
        return Content("Please select a Redirectfolder or -subfolder for exporting.");
      }

      var redirectExportService = new RedirectExportService(db, rootFolder);

      var csv = redirectExportService.ExportRedirects(recursive);

      MediaItemWriter.WriteFile(new MemoryStream(csv), db, Constants.ExportPath, MediaItemWriter.GetFileName(rootFolder), ".csv");

      return Content(csv.ToString(), "text/csv");
    }

    private static bool IsValidRootFolder(Item rootFolder)
    {
      if (rootFolder == null)
      {
        return false;
      }

      var templateIdString = rootFolder.TemplateID.ToString();
      return templateIdString == Templates.Folders.RedirectFolderItem.TemplateId ||
             templateIdString == Templates.Folders.RedirectSubFolderItem.TemplateId;
    }
  }
}
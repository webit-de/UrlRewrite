using System;
using System.IO;
using System.Linq;
using System.Web.Mvc;
using Hi.UrlRewrite.Extensions.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Mvc.Controllers;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  public class ExportRedirectsController : SitecoreController
  {
    public ActionResult ExportRedirects(string rootFolderId, bool recursive = true, bool returnCsv = true)
    {
      // since this route is only mapped 
      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));

      if (!IsValidRootFolder(rootFolder))
      {
        return Content("Please select a Redirectfolder or -subfolder for exporting.");
      }

      var exportService = new RedirectExportService(db, rootFolder);

      var csv = exportService.ExportRedirects(recursive);
      
      var fileId = FileWriter.WriteFile(new MemoryStream(csv), db, Constants.ExportPath, FileWriter.GetFileName(rootFolder), ".csv");

      return returnCsv ? Content(csv.ToString(), "text/csv") : Content(fileId.ToString());
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
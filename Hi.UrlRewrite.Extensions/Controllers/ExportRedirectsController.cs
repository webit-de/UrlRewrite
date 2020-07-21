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
    public ActionResult ExportRedirects(string rootFolderId, bool recursive = true, bool isApiCall = true)
    {
      // since this route is only mapped 
      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));

      if (!IsValidRootFolder(rootFolder))
      {
        return Content("Please select a Redirectfolder or -subfolder for exporting.");
      }

      var exportService = new RedirectExportService(db, rootFolder);

      var csv = exportService.ExportRedirects(recursive, out var logId);
      
      var fileId = FileWriter.WriteFile(new MemoryStream(csv), db, Constants.ExportPath, FileWriter.GetFileName(rootFolder), ".csv");

      // in an api call, return the csv file
      if (isApiCall)
      {
        return Content(csv.ToString(), "text/csv");
      }
      // return ids of generated csv and log files otherwise
      var idResult = "{\"resultId\":\"" + fileId + "\",\"logId\":\"" + logId + "\"} ";
      return Content(idResult, "text/json");
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
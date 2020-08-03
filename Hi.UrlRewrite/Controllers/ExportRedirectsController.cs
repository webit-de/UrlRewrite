﻿using System;
using System.IO;
using System.Web.Mvc;
using Hi.UrlRewrite.Services;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Mvc.Controllers;

namespace Hi.UrlRewrite.Controllers
{
  [Authorize]
  public class ExportRedirectsController : SitecoreController
  {
    /// <summary>
    /// Export the redirects in the selected root folder
    /// </summary>
    /// <param name="rootFolderId">The id of the root folder</param>
    /// <param name="recursive">Whether to export only immediate children or all descendants</param>
    /// <param name="isApiCall">Whether the request is called via api or in UI. This controls how the results are returned</param>
    /// <returns>The export result as CSV if called from API. The IDs of the result and report items if called from UI</returns>
    public ActionResult ExportRedirects(string rootFolderId, bool recursive = true, bool isApiCall = true)
    {
      // since this route is only mapped for CM instances, the master database can be used.
      var db = Sitecore.Configuration.Factory.GetDatabase("master");

      var rootFolder = GetRootFolder(db, rootFolderId);
      if (rootFolder == null)
      {
        return Content("Please select a Redirectfolder or -subfolder for exporting.");
      }

      var exportService = new RedirectExportService(db, rootFolder);
      var csv = exportService.ExportRedirects(recursive, out var reportId);

      // in an api call, return the csv
      if (isApiCall)
      {
        var csvStream = new MemoryStream(csv);
        return File(csvStream, "text/csv", FileService.GetFileName(rootFolder));
      }

      var resultIds = GetResultsAsJson(csv, db, rootFolder, reportId.ToString());
      return Content(resultIds, "text/json");
    }

    /// <summary>
    /// Get the root folder item
    /// </summary>
    /// <param name="db">The database</param>
    /// <param name="rootFolderId">The id of the root folder</param>
    /// <returns>The Root folder item if the rootFolderId points to a valid item, null otherwise.</returns>
    private static Item GetRootFolder(Database db, string rootFolderId)
    {
      if (!Guid.TryParse(rootFolderId, out var parsedId))
      {
        return null;
      }

      var item = db.GetItem(ID.Parse(parsedId));
      return IsValidRoot(item) ? item : null;
    }

    /// <summary>
    /// Check if the root folder id points to a valid item
    /// </summary>
    /// <param name="rootFolder">The root folder id</param>
    /// <returns>True, if the id points to a RedirectFolderItem or RedirectSubFolderItem</returns>
    private static bool IsValidRoot(Item rootFolder)
    {
      if (rootFolder == null)
      {
        return false;
      }

      var templateIdString = rootFolder.TemplateID.ToString();

      return rootFolder.IsRedirectFolderItem() || rootFolder.IsRedirectSubFolderItem();
    }

    /// <summary>
    /// Get the result ids formatted in Json
    /// </summary>
    /// <param name="csv">The result csv</param>
    /// <param name="db">The database</param>
    /// <param name="rootFolder">The root Folder</param>
    /// <param name="reportId">The id of the report</param>
    /// <returns></returns>
    private string GetResultsAsJson(byte[] csv, Database db, Item rootFolder, string reportId)
    {
      // write the file to the master database for UI access
      var fileId = FileService.WriteFile(new MemoryStream(csv), db, Constants.ExportPath, FileService.GetFileName(rootFolder), ".csv");
      return "{\"resultId\":\"" + fileId + "\",\"reportId\":\"" + reportId + "\"} ";
    }
  }
}
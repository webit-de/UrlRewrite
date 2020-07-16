using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
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

      var db = Sitecore.Configuration.Factory.GetDatabase("master");
      var rootFolder = db.GetItem(ID.Parse(Guid.Parse(rootFolderId)));

      if (rootFolder.TemplateID.ToString() != Templates.Folders.RedirectFolderItem.TemplateId)
      {
        return Content("Please select a Redirect Root folder for importing.");
      }

      var csvItem = db.GetItem(ID.Parse(Guid.Parse(csvItemId)));
      var csvMediaItem = new MediaItem(csvItem);

      var stream = csvMediaItem.GetMediaStream();

      // since this route is only mapped for CM instances, we can use the master database.
      RedirectImportService importService = new RedirectImportService(db);
      importService.GenerateRedirectsFromCsv(stream, rootFolder);

      var logFile = WriteLogFile(importService.Warnings, db, csvItem.Name);

      return Content(logFile);
    }

    private string WriteLogFile(List<string> messages, Database db, string name)
    {
      // don't write epmty log files
      if (!messages.Any())
      {
        return string.Empty;
      }

      //var logFolder = db.GetItem(Constants.LogPath);
      //var fileTemplate = db.GetTemplate("/sitecore/templates/System/Media/Unversioned/File");
      //logFolder.Add("Import_" + name + DateTime.Now, fileTemplate);

      // Create the options
      Sitecore.Resources.Media.MediaCreatorOptions options = new Sitecore.Resources.Media.MediaCreatorOptions();
      options.FileBased = true;
      options.IncludeExtensionInItemName = false;
      options.Versioned = false;
      options.Destination = Constants.LogPath + "Import_" + name + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm");
      options.Database = db;

      // Create the file
      Sitecore.Resources.Media.MediaCreator creator = new Sitecore.Resources.Media.MediaCreator();
      MediaItem mediaItem = creator.CreateFromStream(GenerateLogStream(messages), ".log", options);

      return mediaItem.ID.ToString();
    }

    private Stream GenerateLogStream(IEnumerable<string> logEntries)
    {
      var builder = new StringBuilder();

      foreach (var entry in logEntries)
      {
        builder.AppendLine(entry);
      }

      return new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
    }
  }
}
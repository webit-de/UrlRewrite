using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hi.UrlRewrite.Extensions.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Hi.UrlRewrite.Extensions.Services
{
  public class RedirectExportService
  {
    private readonly Database _db;

    private readonly List<ImportExportLogEntry> Warnings = new List<ImportExportLogEntry>();

    private int RedirectFolderPathLength
    {
      get
      {
        // initialize folder path lazily
        if (_redirectFolderPath == 0)
        {
          if (_rootItem.TemplateID.ToString() == Templates.Folders.RedirectFolderItem.TemplateId)
          {
            _redirectFolderPath = _rootItem.Paths.FullPath.Length;
          }
          else
          {
            var redirectFolder = _rootItem.Axes.GetAncestors()
              .First(x => x.TemplateID.ToString() == Templates.Folders.RedirectFolderItem.TemplateId);
            _redirectFolderPath = redirectFolder.Paths.FullPath.Length;
          }
        }

        return _redirectFolderPath;
      }
    }

    private int _redirectFolderPath;

    private readonly Item _rootItem;

    private List<Item> _redirectsToExport = new List<Item>();

    public RedirectExportService(Database database, Item rootItem)
    {
      _db = database;
      _rootItem = rootItem;
    }

    public byte[] ExportRedirects(bool recursive, out ID logId)
    {
      var exportedRedirects = new HashSet<RedirectCsvEntry>();

      GetExportCandidates(recursive, _rootItem);

      foreach (var redirect in _redirectsToExport)
      {
        exportedRedirects.Add(CreateRedirectEntry(redirect));
      }

      logId = WriteLog();

      return CsvService.GenerateCsv(exportedRedirects);
    }

    private ID WriteLog()
    {
      if (Warnings.Any())
      {
        var warningsStream = new MemoryStream(CsvService.GenerateCsv(Warnings));
        return FileWriter.WriteFile(warningsStream, _db, Constants.LogPath, FileWriter.GetFileName(_rootItem, "Export"), ".csv");
      }
      return ID.Null;
    }

    private void GetExportCandidates(bool recursive, Item currentFolderItem)
    {
      foreach (var exportableItem in currentFolderItem.Children
        .Where(IsExportableItem)
        .Where(HasValidData))
      {
        _redirectsToExport.Add(exportableItem);
      }

      if (!recursive)
      {
        return;
      }

      foreach (var subfolder in currentFolderItem.Children.Where(x => x.TemplateID.ToString() == Templates.Folders.RedirectSubFolderItem.TemplateId))
      {
        GetExportCandidates(true, subfolder);
      }
    }

    private static bool IsExportableItem(Item item)
    {
      Assert.IsNotNull(item, "The item must not be null.");

      var templateIdString = item.TemplateID.ToString();
      return templateIdString == Templates.Inbound.SimpleRedirectItem.TemplateId ||
             templateIdString == Templates.Inbound.ShortUrlItem.TemplateId;
    }

    private bool HasValidData(Item redirect)
    {
      var templateIdString = redirect.TemplateID.ToString();
      if (templateIdString == Templates.Inbound.SimpleRedirectItem.TemplateId)
      {
        return HasSimpleRedirectValidData(redirect);
      }

      if (templateIdString == Templates.Inbound.ShortUrlItem.TemplateId)
      {
        return HasShortUrlValidData(redirect);
      }

      Warnings.Add(new ImportExportLogEntry()
      {
        ItemName = redirect.Name,
        ItemId = redirect.ID.ToString(),
        Created = false,
        Message = "The item has an invalid type and could not be exported."
      });

      return false;
    }

    private bool HasShortUrlValidData(Item shortUrl)
    {
      var hasInvalidData = shortUrl["Target"].IsNullOrEmpty() ||
                           shortUrl["Short Url"].IsNullOrEmpty() ||
                           shortUrl["Short Url Settings"].IsNullOrEmpty();

      if (hasInvalidData)
      {
        Warnings.Add(new ImportExportLogEntry()
        {
          ItemName = shortUrl.Name,
          ItemId = shortUrl.ID.ToString(),
          Created = false,
          Message = "The Short URL has invalid data and was not exported."
        });
      }

      return !hasInvalidData;
    }

    private bool HasSimpleRedirectValidData(Item simpleRedirect)
    {
      var hasInvalidData = simpleRedirect["Target"].IsNullOrEmpty() ||
                           simpleRedirect["Path"].IsNullOrEmpty();

      if (hasInvalidData)
      {
        Warnings.Add(new ImportExportLogEntry()
        {
          ItemName = simpleRedirect.Name,
          ItemId = simpleRedirect.ID.ToString(),
          Created = false,
          Message = "The Simple Redirect has invalid data and was not exported."
        });
      }

      return !hasInvalidData;
    }

    private RedirectCsvEntry CreateRedirectEntry(Item redirect)
    {
      var templateIdString = redirect.TemplateID.ToString();
      if (templateIdString == Templates.Inbound.SimpleRedirectItem.TemplateId)
      {
        return CreateSimpleRedirectEntry(redirect);
      }

      if (templateIdString == Templates.Inbound.ShortUrlItem.TemplateId)
      {
        return CreateShortUrlEntry(redirect);
      }

      Warnings.Add(new ImportExportLogEntry()
      {
        ItemName = redirect.Name,
        ItemId = redirect.ID.ToString(),
        Created = false,
        Message = "The item has an invalid type and was not exported."
      });

      return null;
    }

    private RedirectCsvEntry CreateSimpleRedirectEntry(Item simpleRedirect)
    {
      return new RedirectCsvEntry()
      {
        ItemId = simpleRedirect.ID.ToString(),
        Name = simpleRedirect.Name,
        Path = GetRelativePath(simpleRedirect),
        PathToken = simpleRedirect["Path"],
        ShortUrlPrefix = string.Empty,
        Status = GetStatus(simpleRedirect),
        Target = simpleRedirect["Target"],
        Type = Constants.RedirectType.SIMPLEREDIRECT.ToString()
      };
    }

    private RedirectCsvEntry CreateShortUrlEntry(Item shortUrl)
    {
      return new RedirectCsvEntry()
      {
        ItemId = shortUrl.ID.ToString(),
        Name = shortUrl.Name,
        Path = GetRelativePath(shortUrl),
        PathToken = shortUrl["Short Url"],
        ShortUrlPrefix = GetShortUrlPrefix(shortUrl),
        Status = GetStatus(shortUrl),
        Target = shortUrl["Target"],
        Type = Constants.RedirectType.SHORTURL.ToString()
      };
    }

    private static string GetStatus(Item redirect)
    {
      var enabled = redirect["Enabled"] == "1";
      return enabled ? Constants.ImportStatus.ENABLED.ToString() : Constants.ImportStatus.DISABLED.ToString();
    }

    private string GetShortUrlPrefix(Item shortUrl)
    {
      var settingsItem = _db.GetItem(shortUrl["Short Url Settings"]);

      return settingsItem["Prefix"];
    }

    private string GetRelativePath(Item redirect)
    {
      return redirect.Paths.ParentPath.Remove(0, RedirectFolderPathLength);
    }
  }
}
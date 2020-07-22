using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hi.UrlRewrite.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;
using Sitecore.StringExtensions;

namespace Hi.UrlRewrite.Services
{
  public class RedirectExportService
  {
    /// <summary>
    /// The database
    /// </summary>
    private readonly Database _db;

    /// <summary>
    /// The report service
    /// </summary>
    private readonly ReportService _reportService = new ReportService();

    /// <summary>
    /// The length of the item path of the redirect folder.
    /// Refers to the _rootItem if it's a <see cref="Templates.Folders.RedirectFolderItem"/>, otherwise to the first ancestor of type see cref="Templates.Folders.RedirectFolderItem"/>
    /// </summary>
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

    /// <summary>
    /// The selected root item
    /// </summary>
    private readonly Item _rootItem;

    /// <summary>
    /// The export candidates. Check for validity before adding to this list.
    /// </summary>
    private List<Item> _redirectsToExport = new List<Item>();

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="database"></param>
    /// <param name="rootItem"></param>
    public RedirectExportService(Database database, Item rootItem)
    {
      _db = database;
      _rootItem = rootItem;
    }

    /// <summary>
    /// Export the redirects descending from the selected _rootItem
    /// </summary>
    /// <param name="recursive">Whether to export all descendants or just immediate children</param>
    /// <param name="reportId">The out parameter for the report</param>
    /// <returns></returns>
    public byte[] ExportRedirects(bool recursive, out ID reportId)
    {
      var exportedRedirects = new HashSet<RedirectCsvEntry>();

      CreateExportCandidateList(recursive, _rootItem);

      foreach (var redirect in _redirectsToExport)
      {
        exportedRedirects.Add(CreateRedirectEntry(redirect));
      }

      reportId = _reportService.WriteReport(_rootItem, "ExportReport");

      return CsvService.GenerateCsv(exportedRedirects);
    }
    
    /// <summary>
    /// Creates the list of all Items in the folder Item which are valid for export
    /// </summary>
    /// <param name="recursive">Whether to export all descendants or just immediate children</param>
    /// <param name="currentFolderItem">The current folder item</param>
    private void CreateExportCandidateList(bool recursive, Item currentFolderItem)
    {
      foreach (var exportableItem in currentFolderItem.Children
        .Where(IsExportableItem)
        .Where(HasCompleteData))
      {
        _redirectsToExport.Add(exportableItem);
      }

      if (!recursive)
      {
        return;
      }

      foreach (var subfolder in currentFolderItem.Children.Where(x => x.TemplateID.ToString() == Templates.Folders.RedirectSubFolderItem.TemplateId))
      {
        CreateExportCandidateList(true, subfolder);
      }
    }

    /// <summary>
    /// Check if the item has an exportable type
    /// </summary>
    /// <param name="item">The checked item</param>
    /// <returns>True, if the item has an exportable type</returns>
    private static bool IsExportableItem(Item item)
    {
      Assert.IsNotNull(item, "The item must not be null.");

      var templateIdString = item.TemplateID.ToString();
      return templateIdString == Templates.Inbound.SimpleRedirectItem.TemplateId ||
             templateIdString == Templates.Inbound.ShortUrlItem.TemplateId;
    }

    /// <summary>
    /// Check if the data of the redirect item is complete
    /// </summary>
    /// <param name="redirect">The redirect item to check</param>
    /// <returns>True, if all required data are available</returns>
    private bool HasCompleteData(Item redirect)
    {
      var templateIdString = redirect.TemplateID.ToString();
      if (templateIdString == Templates.Inbound.SimpleRedirectItem.TemplateId)
      {
        return HasSimpleRedirectCompleteData(redirect);
      }

      if (templateIdString == Templates.Inbound.ShortUrlItem.TemplateId)
      {
        return HasShortUrlCompleteData(redirect);
      }

      _reportService.AddWarning("The item has an invalid type and could not be exported.", redirect);
      return false;
    }

    /// <summary>
    /// Check if the data of the Short URL item is complete
    /// </summary>
    /// <param name="shortUrl">The Short URL item to check</param>
    /// <returns>True, if all required data are available</returns>
    private bool HasShortUrlCompleteData(Item shortUrl)
    {
      var hasInvalidData = shortUrl["Target"].IsNullOrEmpty() ||
                           shortUrl["Short Url"].IsNullOrEmpty() ||
                           shortUrl["Short Url Settings"].IsNullOrEmpty();

      if (hasInvalidData)
      {
        _reportService.AddWarning("The Short URL has invalid data and was not exported.", shortUrl);
      }

      return !hasInvalidData;
    }

    /// <summary>
    /// Check if the data of the Simple Redirect item is complete
    /// </summary>
    /// <param name="simpleRedirect">The Simple Redirect item to check</param>
    /// <returns>True, if all required data are available.</returns>
    private bool HasSimpleRedirectCompleteData(Item simpleRedirect)
    {
      var hasInvalidData = simpleRedirect["Target"].IsNullOrEmpty() ||
                           simpleRedirect["Path"].IsNullOrEmpty();

      if (hasInvalidData)
      {
        _reportService.AddWarning("The Simple Redirect has invalid data and was not exported.", simpleRedirect);
      }

      return !hasInvalidData;
    }

    /// <summary>
    /// Create a CSV Model <see cref="RedirectCsvEntry"/> from an Item
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <returns>The CSV Model</returns>
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

      _reportService.AddWarning("The item has an invalid type and was not exported.", redirect);
      return null;
    }


    /// <summary>
    /// Create a CSV Model <see cref="RedirectCsvEntry"/> from a Simple Redirect item
    /// </summary>
    /// <param name="simpleRedirect">The Simple Redirect item</param>
    /// <returns>The CSV Model</returns>
    private RedirectCsvEntry CreateSimpleRedirectEntry(Item simpleRedirect)
    {
      return new RedirectCsvEntry()
      {
        ItemId = simpleRedirect.ID.ToString(),
        ItemName = simpleRedirect.Name,
        RelativeItemPath = GetRelativePath(simpleRedirect),
        RedirectedUrl = simpleRedirect["Path"],
        ShortUrlPrefix = string.Empty,
        Status = GetStatus(simpleRedirect),
        RedirectTarget = simpleRedirect["Target"],
        Type = Constants.RedirectType.SIMPLEREDIRECT.ToString()
      };
    }


    /// <summary>
    /// Create a CSV Model <see cref="RedirectCsvEntry"/> from a Short URL item
    /// </summary>
    /// <param name="shortUrl">The Short URL item</param>
    /// <returns>The CSV Model</returns>
    private RedirectCsvEntry CreateShortUrlEntry(Item shortUrl)
    {
      return new RedirectCsvEntry()
      {
        ItemId = shortUrl.ID.ToString(),
        ItemName = shortUrl.Name,
        RelativeItemPath = GetRelativePath(shortUrl),
        RedirectedUrl = shortUrl["Short Url"],
        ShortUrlPrefix = GetShortUrlPrefix(shortUrl),
        Status = GetStatus(shortUrl),
        RedirectTarget = shortUrl["Target"],
        Type = Constants.RedirectType.SHORTURL.ToString()
      };
    }

    /// <summary>
    /// Get the status of the redirect Item
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <returns></returns>
    private static string GetStatus(Item redirect)
    {
      var enabled = redirect["Enabled"] == "1";
      return enabled ? Constants.ImportStatus.ENABLED.ToString() : Constants.ImportStatus.DISABLED.ToString();
    }

    /// <summary>
    /// Get the Short URL prefix
    /// </summary>
    /// <param name="shortUrl">The Short URL Item</param>
    /// <returns></returns>
    private string GetShortUrlPrefix(Item shortUrl)
    {
      var settingsItem = _db.GetItem(shortUrl["Short Url Settings"]);

      return settingsItem["Prefix"];
    }

    /// <summary>
    /// Get the path relative to the <see cref="Templates.Folders.RedirectFolderItem"/> ancestor
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <returns>The relative path to the item</returns>
    private string GetRelativePath(Item redirect)
    {
      return redirect.Paths.ParentPath.Remove(0, RedirectFolderPathLength);
    }
  }
}
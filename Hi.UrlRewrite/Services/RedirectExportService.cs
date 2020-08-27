using System.Collections.Generic;
using System.Linq;
using Hi.UrlRewrite.Extensions;
using Hi.UrlRewrite.Models;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

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
    public ReportService ReportService { get; }

    /// <summary>
    /// The selected root item
    /// </summary>
    public Item RootItem { get; }

    /// <summary>
    /// The length of the item path of the redirect folder.
    /// </summary>
    private int _redirectFolderPath;

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
          if (RootItem.TemplateID.ToString() == Templates.Folders.RedirectFolderItem.TemplateId)
          {
            _redirectFolderPath = RootItem.Paths.FullPath.Length;
          }
          else
          {
            var redirectFolder = RootItem.Axes.GetAncestors()
              .First(x => x.TemplateID.ToString() == Templates.Folders.RedirectFolderItem.TemplateId);
            _redirectFolderPath = redirectFolder.Paths.FullPath.Length;
          }
        }

        return _redirectFolderPath;
      }
    }

    /// <summary>
    /// The export candidates. Check for validity before adding to this list.
    /// </summary>
    private List<Item> RedirectsToExport { get; set; }

    /// <summary>
    /// Constructor
    /// </summary>
    /// <param name="database"></param>
    /// <param name="rootItem"></param>
    public RedirectExportService(Database database, Item rootItem)
    {
      _db = database;
      RootItem = rootItem;
      ReportService = new ReportService();
      RedirectsToExport = new List<Item>();
    }

    /// <summary>
    /// Export the redirects descending from the selected _rootItem
    /// </summary>
    /// <param name="exportAllDescendants">Whether to export all descendants or just immediate children</param>
    /// <param name="reportId">The out parameter for the report</param>
    /// <returns></returns>
    public byte[] ExportRedirects(bool exportAllDescendants, out ID reportId)
    {
      var exportedRedirects = new HashSet<RedirectCsvEntry>();

      CreateExportCandidateList(exportAllDescendants, RootItem);

      foreach (var redirect in RedirectsToExport)
      {
        exportedRedirects.Add(CreateRedirectEntry(redirect));
      }

      reportId = ReportService.WriteReport(RootItem, "ExportReport");

      return CsvService.GenerateCsv(exportedRedirects);
    }

    /// <summary>
    /// Creates the list of all Items in the folder Item which are valid for export
    /// </summary>
    /// <param name="exportAllDescendants">Whether to export all descendants or just immediate children</param>
    /// <param name="currentFolderItem">The current folder item</param>
    private void CreateExportCandidateList(bool exportAllDescendants, Item currentFolderItem)
    {
      AddImmediateChildrenToExport(currentFolderItem);

      if (exportAllDescendants)
        AddDescendantsToExport(currentFolderItem);
    }

    /// <summary>
    /// Add immediate children of <see cref="currentFolderItem"/> to the export data.
    /// </summary>
    /// <param name="currentFolderItem">The current folder item</param>
    private void AddImmediateChildrenToExport(Item currentFolderItem)
    {
      foreach (var exportableItem in currentFolderItem.Children
        .Where(IsExportableItem)
        .Where(HasCompleteData))
      {
        RedirectsToExport.Add(exportableItem);
      }
    }

    /// <summary>
    /// Add all relevant descendants of <see cref="currentFolderItem"/> to the export data.
    /// </summary>
    /// <param name="currentFolderItem">The current folder item.</param>
    private void AddDescendantsToExport(Item currentFolderItem)
    {
      foreach (var subfolder in currentFolderItem.Children.Where(x =>
        x.TemplateID.ToString() == Templates.Folders.RedirectSubFolderItem.TemplateId))
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

      return item.IsSimpleRedirectItem() || item.IsShortUrlItem();
    }

    /// <summary>
    /// Check if the data of the redirect item is complete
    /// </summary>
    /// <param name="redirect">The redirect item to check</param>
    /// <returns>True, if all required data are available</returns>
    private bool HasCompleteData(Item redirect)
    {
      if (redirect.IsSimpleRedirectItem())
        return HasSimpleRedirectCompleteData(redirect);

      if (redirect.IsShortUrlItem())
        return HasShortUrlCompleteData(redirect);

      ReportService.AddWarning("The item has an invalid type and could not be exported.", redirect);
      return false;
    }

    /// <summary>
    /// Check if the data of the Short URL item is complete
    /// </summary>
    /// <param name="shortUrl">The Short URL item to check</param>
    /// <returns>>Returns <code>true</code>, if all required data are available.</returns>
    private bool HasShortUrlCompleteData(Item shortUrl)
    {
      var hasValidData = shortUrl["Target"].HasValue() &&
                         shortUrl["Short Url"].HasValue() &&
                         shortUrl["Short Url Settings"].HasValue();

      if (hasValidData)
        return true;

      ReportService.AddWarning("The Short URL has invalid data and was not exported.", shortUrl);
      return false;
    }

    /// <summary>
    /// Check if the data of the Simple Redirect item is complete
    /// </summary>
    /// <param name="simpleRedirect">The Simple Redirect item to check</param>
    /// <returns>Returns <code>true</code>, if all required data are available.</returns>
    private bool HasSimpleRedirectCompleteData(Item simpleRedirect)
    {
      var hasValidData = simpleRedirect["Target"].HasValue() &&
                           simpleRedirect["Path"].HasValue();

      if (hasValidData)
        return true;

      ReportService.AddWarning("The Simple Redirect has invalid data and was not exported.", simpleRedirect);
      return false;
    }

    /// <summary>
    /// Create a CSV Model <see cref="RedirectCsvEntry"/> from the specified <see cref="redirect"/> <see cref="Item"/>.
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <returns>The CSV Model</returns>
    private RedirectCsvEntry CreateRedirectEntry(Item redirect)
    {
      if (redirect.IsSimpleRedirectItem())
        return CreateSimpleRedirectEntry(redirect);

      if (redirect.IsShortUrlItem())
        return CreateShortUrlEntry(redirect);

      ReportService.AddWarning("The item has an invalid type and was not exported.", redirect);
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
        TargetUrl = GetUrl(simpleRedirect),
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
        TargetUrl = GetUrl(shortUrl),
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

    /// <summary>
    /// Get the URL to the redirect Item
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <returns></returns>
    private static string GetUrl(Item redirect)
    {
      LinkField linkField = redirect.Fields["Target"];
      var targetItem = linkField?.TargetItem;

      if (targetItem != null && linkField.IsInternal)
      {
        var urlOptions = new Sitecore.Links.UrlOptions
        {
          AlwaysIncludeServerUrl = true
        };

        // add a space to indicate the end of the url for url highlighting in text editors
        return Sitecore.Links.LinkManager.GetItemUrl(targetItem, urlOptions) + " ";
      }

      return string.Empty;
    }
  }
}
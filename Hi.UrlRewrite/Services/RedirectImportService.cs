
using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using CsvHelper;
using Hi.UrlRewrite.Extensions;
using Hi.UrlRewrite.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;

namespace Hi.UrlRewrite.Services
{
  public class RedirectImportService
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
    /// Constructor
    /// </summary>
    /// <param name="database"></param>
    public RedirectImportService(Database database)
    {
      _db = database;
      ReportService = new ReportService();
    }

    /// <summary>
    /// Import redirect items from the provided csv stream as descendants of the root item
    /// </summary>
    /// <param name="csvStream">The csv stream</param>
    /// <param name="rootItem">The root item</param>
    /// <returns>The import report</returns>
    public string ImportRedirectsFromCsv(Stream csvStream, Item rootItem)
    {
      try
      {
        using (var reader = new StreamReader(csvStream))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
          csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.Trim().ToLower();

          foreach (var redirect in csv.GetRecords<RedirectCsvEntry>())
          {
            ProcessRedirect(redirect, rootItem);
          }

          return ReportService.GetReport(rootItem, "ImportReport");
        }
      }
      catch (Exception e)
      {
        ReportService.AddWarning("There has been an error parsing the CSV file:\n" + e.Message);
        throw;
      }
    }
    
    /// <summary>
    /// Process a single Redirect CSV Model
    /// </summary>
    /// <param name="redirect">The redirect model</param>
    /// <param name="rootItem">The root item</param>
    private void ProcessRedirect(RedirectCsvEntry redirect, Item rootItem)
    {
      if (RedirectDataIsValid(redirect, out var existingRedirect))
      {
        // if DELETE status is set, delete the item
        if (redirect.Status.ToUpper() == Constants.ImportStatus.DELETE.ToString())
        {
          DeleteRedirect(redirect);
          return;
        }

        // update redirect or create a new one
        Enum.TryParse(redirect.Type, true, out Constants.RedirectType typeEnum);
        switch (typeEnum)
        {
          case Constants.RedirectType.SIMPLEREDIRECT:
            ProcessSimpleRedirect(redirect, rootItem, existingRedirect);
            return;
          case Constants.RedirectType.SHORTURL:
            ProcessShortUrlItem(redirect, rootItem, existingRedirect);
            return;
          default:
            ReportService.AddWarning("Redirect has an invalid or not supported redirect type and can not be imported.", redirect, false);
            return;
        }
      }
    }

    /// <summary>
    /// Delete the redirect item
    /// </summary>
    /// <param name="redirect">The data of the redirect item to delete</param>
    private void DeleteRedirect(RedirectCsvEntry redirect)
    {
      var item = _db.GetItem(ID.Parse(Guid.Parse(redirect.ItemId)));
      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        item?.Recycle();
      }
    }

    /// <summary>
    /// Process the redirect as Simple Redirect
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <param name="rootItem">The root item</param>
    /// <param name="existingRedirect">The existing item with the same id</param>
    private void ProcessSimpleRedirect(RedirectCsvEntry redirect, Item rootItem, Item existingRedirect = null)
    {
      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        try
        {
          Item simpleRedirect = CreateRedirectItem(redirect, rootItem, existingRedirect, Templates.Inbound.SimpleRedirectItem.TemplateId);
          PopulateSimpleRedirect(redirect, simpleRedirect);
        }
        catch (Exception e)
        {
          ReportService.AddWarning("There has been an error processing the Simple Redirect:\n" + e.Message, redirect, false);
        }
      }
    }

    /// <summary>
    /// Populate the a Simple Redirect item with the provided data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="simpleRedirect">The Simple Redirect item</param>
    private void PopulateSimpleRedirect(RedirectCsvEntry data, Item simpleRedirect)
    {
      var path = GetRedirectedPath(data, simpleRedirect);

      simpleRedirect.Editing.BeginEdit();
      simpleRedirect["Path"] = path;
      simpleRedirect["Target"] = GetRedirectTarget(data);
      // if the path is empty, disable the redirect regardless of status field to avoid ambiguous paths.
      simpleRedirect["Enabled"] = path == string.Empty ? "0" : GetRedirectStatus(data);
      simpleRedirect.Editing.EndEdit();
    }

    /// <summary>
    /// Process the redirect as Short URL
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <param name="rootItem">The root item</param>
    /// <param name="existingRedirect">The existing item with the same id</param>
    private void ProcessShortUrlItem(RedirectCsvEntry redirect, Item rootItem, Item existingRedirect = null)
    {
      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        try
        {
          var shortUrl = CreateRedirectItem(redirect, rootItem, existingRedirect, Templates.Inbound.ShortUrlItem.TemplateId);
          PopulateShortUrl(redirect, shortUrl);
        }
        catch (Exception e)
        {
          ReportService.AddWarning("There has been an error processing the Short URL:\n" + e.Message, redirect, false);
        }
      }
    }
    
    /// <summary>
    /// Populate the a Short URL item with the provided data
    /// </summary>
    /// <param name="data">The data</param>
    /// <param name="shortUrl">The Short URL item</param>
    private void PopulateShortUrl(RedirectCsvEntry data, Item shortUrl)
    {
      var token = GetToken(data, shortUrl);

      shortUrl.Editing.BeginEdit();
      shortUrl["Short Url"] = token;
      shortUrl["Target"] = GetRedirectTarget(data);
      // if the token is empty, disable the redirect regardless of status field to avoid ambiguous paths.
      shortUrl["Enabled"] = token == string.Empty ? "0" : GetRedirectStatus(data);
      shortUrl["Short Url Settings"] = FindShortUrlSettings(data);
      shortUrl.Editing.EndEdit();
    }

    /// <summary>
    /// Creates an unpopulated <see cref="Item"/> for the specified <see cref="redirect"/>.
    /// </summary>
    /// <param name="redirect">The redirect csv data item.</param>
    /// <param name="rootItem">The redirect root.</param>
    /// <param name="existingRedirect">The existing item with the same id.</param>
    /// <param name="templateId">Specfies the template used to create the redirect item.</param>
    /// <returns></returns>
    private Item CreateRedirectItem(RedirectCsvEntry redirect, Item rootItem, Item existingRedirect, string templateId)
    {
      Item result;
      var parentFolder = CreateFolderStructure(redirect, rootItem);

      // if no redirect with the id is existing, create a new one
      if (existingRedirect == null)
      {
        var template = _db.GetTemplate(ID.Parse(Guid.Parse(templateId)));
        result = parentFolder.Add(redirect.ItemName, template);
      }
      // modify the existing one otherwise
      else
      {
        result = existingRedirect;
        result.MoveTo(parentFolder);
      }

      return result;
    }

    private static string GetRedirectStatus(RedirectCsvEntry redirect)
    {
      return redirect.Status.ToUpper() == Constants.ImportStatus.ENABLED.ToString() ? "1" : "0";
    }

    /// <summary>
    /// Get the target value. Triggers a warning if the link is internal and the target can't be found.
    /// </summary>
    /// <param name="redirect">The imported redirect</param>
    /// <returns>The target field value</returns>
    private string GetRedirectTarget(RedirectCsvEntry redirect)
    {
      var startIndex = redirect.RedirectTarget.IndexOf('{');
      var id = redirect.RedirectTarget.Substring(startIndex, 38);

      if (HasExternalLink(redirect))
      {
        return redirect.RedirectTarget;
      }

      if (!Guid.TryParse(id, out var guid))
      {
        ReportService.AddWarning("The target for the redirect is invalid. The Link will be broken.", redirect, true);
        return redirect.RedirectTarget;
      }

      var targetItem = _db.GetItem(ID.Parse(guid));
      if (targetItem != null)
      {
        return redirect.RedirectTarget;
      }

      ReportService.AddWarning("The target for the redirect does not exist. The Link will be broken.", redirect, true);
      return redirect.RedirectTarget;
    }

    /// <summary>
    /// Check if the redirect target is an external link
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <returns>True if the redirect target is external</returns>
    private bool HasExternalLink(RedirectCsvEntry redirect)
    {
      return redirect.RedirectTarget.Contains("linktype=\"external\"");
    }

    /// <summary>
    /// Get the redirected path
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <param name="createdItem">The created item</param>
    /// <returns>The redirected path if it's unique, otherwise an empty string</returns>
    private string GetRedirectedPath(RedirectCsvEntry redirect, Item createdItem)
    {
      // do NOT use caching here!
      var query = "/sitecore/content//*[@@templateid='" + Templates.Inbound.SimpleRedirectItem.TemplateId + "']";

      // exclude the created item, since simple redirects have their name as standard value for the path field.
      var isPathUnique = _db.SelectItems(query).Where(x => x.ID != createdItem.ID).All(x => x["Path"] != redirect.RedirectedUrl);

      if (isPathUnique)
      {
        return redirect.RedirectedUrl;
      }

      ReportService.AddWarning("The path for the Simple Redirect is not unique and has been cleared. The redirect has been disabled.", redirect, true);
      return string.Empty;
    }

    /// <summary>
    /// Get the Short URL token
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <param name="createdItem">The created item</param>
    /// <returns>The Short URL token if it's unique, otherwise an empty string</returns>
    private string GetToken(RedirectCsvEntry redirect, Item createdItem)
    {
      // do NOT use caching here!
      var query = "/sitecore/content//*[@@templateid='" + Templates.Inbound.ShortUrlItem.TemplateId + "']";
      var isTokenUnique = _db.SelectItems(query).Where(x => x.ID != createdItem.ID).All(x => x["Short Url"] != redirect.RedirectedUrl);

      if (isTokenUnique)
      {
        return redirect.RedirectedUrl;
      }

      ReportService.AddWarning("The token for the Short Url is not unique and has been cleared. The redirect has been disabled.", redirect, true);
      return string.Empty;
    }

    /// <summary>
    /// Find the Short URL Settings based on the prefix
    /// </summary>
    /// <param name="redirect">The redirect item</param>
    /// <returns>A Short Url Settings item ID with matching prefix</returns>
    private string FindShortUrlSettings(RedirectCsvEntry redirect)
    {
      var query = "/sitecore/content//*[@@templateid='" + Templates.Settings.ShortUrlSetting.TemplateId + "']";
      var shortUrlSettings = _db.SelectItems(query).FirstOrDefault(x => x["Prefix"] == redirect.ShortUrlPrefix);

      if (shortUrlSettings == null)
      {
        ReportService.AddWarning("Could not find matching Short URL Setting. The field has been left empty.", redirect, true);
        return string.Empty;
      }

      return shortUrlSettings.ID.ToString();
    }

    /// <summary>
    /// Check validity of a redirect CSV Model
    /// </summary>
    /// <param name="redirect">The redirect model</param>
    /// <param name="existingItem">Out parameter for existing Item with the same id</param>
    /// <returns>True, if the CSV Model contains only valid data</returns>
    private bool RedirectDataIsValid(RedirectCsvEntry redirect, out Item existingItem)
    {
      bool result = true;

      // check validity independent from existing items
      if (!CheckValidId(redirect, out existingItem) || !CheckEmptyFields(redirect))
      {
        result = false;
      }

      if (existingItem == null)
      {
        return result;
      }

      // check validity dependent from existing items
      if (ExistingItemHasSameRedirectType(redirect, existingItem))
      {
        return result;
      }

      return false;
    }

    /// <summary>
    /// Check if the redirect item has the same type as the imported one.
    /// </summary>
    /// <param name="redirect">The imported redirect</param>
    /// <param name="existingItem">The existing redirect item</param>
    /// <returns>Returns <code>true</code> if the imported redirect item has the same type as the existing item; otherwise <code>false</code>.</returns>
    private bool ExistingItemHasSameRedirectType(RedirectCsvEntry redirect, Item existingItem)
    {
      bool CheckExistingItemTemplateType(string templateId)
      {
        if (existingItem.TemplateID.ToString() == templateId)
        {
          return true;
        }

        ReportService.AddWarning("The imported redirect has a different type than the existing item and can not be imported.", redirect, false);
        return false;
      }

      Enum.TryParse(redirect.Type, true, out Constants.RedirectType redirectType);

      switch (redirectType)
      {
        case Constants.RedirectType.SIMPLEREDIRECT:
          return CheckExistingItemTemplateType(Templates.Inbound.SimpleRedirectItem.TemplateId);

        case Constants.RedirectType.SHORTURL:
          return CheckExistingItemTemplateType(Templates.Inbound.ShortUrlItem.TemplateId);

        default:
          ReportService.AddWarning("The redirect has an invalid redirect type and can not be imported.", redirect, false);
          return false;
      }
    }
    
    /// <summary>
    /// Check if a mandatory field is empty.
    /// </summary>
    /// <param name="redirect">The imported redirect</param>
    /// <returns>True if all mandatory fields have a value</returns>
    private bool CheckEmptyFields(RedirectCsvEntry redirect)
    {
      // if the item should be deleted, don't check field values
      if (redirect.Status.ToUpper() == Constants.ImportStatus.DELETE.ToString())
      {
        return true;
      }

      if (redirect.Type.IsNullOrEmpty() ||
          redirect.ItemName.IsNullOrEmpty() ||
          redirect.RelativeItemPath.IsNullOrEmpty() ||
          redirect.RedirectedUrl.IsNullOrEmpty() ||
          redirect.Type == Constants.RedirectType.SHORTURL.ToString() && redirect.ShortUrlPrefix.IsNullOrEmpty() ||
          redirect.Status.IsNullOrEmpty() ||
          redirect.RedirectTarget.IsNullOrEmpty())
      {
        ReportService.AddWarning("The redirect has at least one empty field and can not be imported.", redirect, false);
        return false;
      }

      return true;
    }

    /// <summary>
    /// Check if the id is valid. Write the item in the out parameter if valid and an item with the id exists.
    /// </summary>
    /// <param name="redirect">The imported redirect</param>
    /// <param name="existingItem">The existing item</param>
    /// <returns>True, if the id is valid or empty</returns>
    private bool CheckValidId(RedirectCsvEntry redirect, out Item existingItem)
    {
      existingItem = null;

      // redirect item id is valid
      if (Guid.TryParse(redirect.ItemId, out var redirectGuid))
      {
        existingItem = _db.GetItem(ID.Parse(redirectGuid));
        return true;
      }

      // no id available
      if (redirect.ItemId.IsNullOrEmpty())
        return true;

      // invalid item id
      ReportService.AddWarning("The redirect has an invalid id and can not be imported.", redirect, false);
      return false;
    }

    /// <summary>
    /// Create the folder structure based on the path.
    /// </summary>
    /// <param name="redirect">The imported redirect</param>
    /// <param name="rootItem">The root item</param>
    /// <returns>The parent item for the redirect</returns>
    private Item CreateFolderStructure(RedirectCsvEntry redirect, Item rootItem)
    {
      var currentFolder = rootItem;

      var foldersInPath = redirect.RelativeItemPath.Split('/').Where(x => x.HasValue()).ToArray();

      // go through every folder in the hierarchy and add missing ones
      foreach (var folder in foldersInPath)
      {
        var nextFolder = currentFolder.Children.FirstOrDefault(x => x.Name == folder) ??
                         CreateChildFolder(currentFolder, folder);

        currentFolder = nextFolder;
      }

      return currentFolder;
    }

    /// <summary>
    /// Create a redirect subfolder.
    /// </summary>
    /// <param name="parent">The parent item for the new folder</param>
    /// <param name="name">The name of the new folder</param>
    /// <returns>The folder item</returns>
    private Item CreateChildFolder(Item parent, string name)
    {
      var template = _db.GetTemplate(ID.Parse(Guid.Parse(Templates.Folders.RedirectSubFolderItem.TemplateId)));

      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        return parent.Add(name, template);
      }
    }
  }
}
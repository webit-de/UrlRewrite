
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using Hi.UrlRewrite.Extensions.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;

namespace Hi.UrlRewrite.Extensions.Services
{
  public class RedirectImportService
  {
    public List<string> Warnings = new List<string>();

    private readonly Database _db;

    public RedirectImportService(Database database)
    {
      _db = database;
    }

    public void GenerateRedirectsFromCsv(Stream csvStream, Item rootItem)
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
        }
      }
      catch (Exception e)
      {
        Warnings.Add("There has been an error parsing the CSV file:\n" + e.Message);
        throw;
      }
    }

    private void ProcessRedirect(RedirectCsvEntry redirect, Item rootItem)
    {
      if (!CheckValidity(redirect, out var existingRedirect))
      {
        return;
      }

      // if DELETE status is set, delete the item
      if (redirect.Status.ToUpper() == Constants.ImportStatus.DELETE.ToString())
      {
        rootItem?.Recycle();
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
          Warnings.Add("Redirect " + redirect.Name + " has an invalid redirect type and can not be imported.");
          return;
      }
    }

    private void ProcessSimpleRedirect(RedirectCsvEntry redirect, Item rootItem, Item existingRedirect = null)
    {
      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        try
        {
          Item simpleRedirect;
          var parentFolder = CreateFolderStructure(redirect, rootItem);

          // if no redirect with the id is existing, create a new one
          if (existingRedirect == null)
          {
            var template = _db.GetTemplate(ID.Parse(Guid.Parse(Templates.Inbound.SimpleRedirectItem.TemplateId)));
            simpleRedirect = CreateFolderStructure(redirect, rootItem).Add(redirect.Name, template);
          }
          // modify the existing one otherwise
          else
          {
            simpleRedirect = existingRedirect;
            simpleRedirect.MoveTo(parentFolder);
          }
          simpleRedirect.Editing.BeginEdit();
          simpleRedirect["Path"] = GetPath(redirect);
          simpleRedirect["Target"] = GetRedirectTarget(redirect);
          simpleRedirect["Enabled"] = GetRedirectStatus(redirect);
          simpleRedirect.Editing.EndEdit();
        }
        catch (Exception e)
        {
          Warnings.Add("There has been an error processing the item for Simple Redirect '" + redirect.Name + "':\n" + e.Message);
        }
      }
    }

    private void ProcessShortUrlItem(RedirectCsvEntry redirect, Item rootItem, Item existingRedirect = null)
    {
      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        try
        {
          Item shortUrl;
          var parentFolder = CreateFolderStructure(redirect, rootItem);

          // if no redirect with the id is existing, create a new one
          if (existingRedirect == null)
          {
            var template = _db.GetTemplate(ID.Parse(Guid.Parse(Templates.Inbound.ShortUrlItem.TemplateId)));
            shortUrl = parentFolder.Add(redirect.Name, template);
          }
          // modify the existing one otherwise
          else
          {
            shortUrl = existingRedirect;
            shortUrl.MoveTo(parentFolder);
          }

          shortUrl.Editing.BeginEdit();
          shortUrl["ShortUrl"] = GetToken(redirect);
          shortUrl["Target"] = GetRedirectTarget(redirect);
          shortUrl["Enabled"] = GetRedirectStatus(redirect);
          shortUrl["Short Url Settings"] = FindShortUrlSettings(redirect.ShortUrlPrefix, rootItem);
          shortUrl.Editing.EndEdit();
        }
        catch (Exception e)
        {
          Warnings.Add("There has been an error processing the item for Short URL '" + redirect.Name + "':\n" + e.Message);
        }
      }
    }

    private static string GetRedirectStatus(RedirectCsvEntry redirect)
    {
      return redirect.Status.ToUpper() == Constants.ImportStatus.ENABLED.ToString() ? "1" : "0";
    }

    private static string GetRedirectTarget(RedirectCsvEntry redirect)
    {
      // TODO: warn if target does not exist
      return redirect.Target;
    }

    private static string GetPath(RedirectCsvEntry redirect)
    {
      // TODO: check uniqueness
      return redirect.PathToken;
    }

    private static string GetToken(RedirectCsvEntry redirect)
    {
      //TODO: check uniqueness
      return redirect.PathToken;
    }

    private string FindShortUrlSettings(string prefix, Item rootItem)
    {
      var query = "/sitecore/content//*[@@templateid='" + Templates.Settings.ShortUrlSetting.TemplateId + "']";
      var shortUrlSettings = _db.SelectItems(query).FirstOrDefault(x => x["Prefix"] == prefix);

      if (shortUrlSettings == null)
      {
        Warnings.Add("Could not find matching Short URL Settings for '" + rootItem.Name + "'. The field has been left empty.");
        return string.Empty;
      }

      return shortUrlSettings.ID.ToString();
    }

    private bool CheckValidity(RedirectCsvEntry redirect, out Item existingItem)
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
      if (!CheckEqualType(redirect, existingItem))
      {
        result = false;
      }

      return result;
    }

    /// <summary>
    /// Check if the redirect item has the same type as the imported one.
    /// </summary>
    /// <param name="redirect">The imported redirect</param>
    /// <param name="existingItem">The existing redirect item</param>
    /// <returns>True if the imported redirect item has the same type as the existing item.</returns>
    private bool CheckEqualType(RedirectCsvEntry redirect, Item existingItem)
    {

      Enum.TryParse(redirect.Type, true, out Constants.RedirectType typeEnum);

      switch (typeEnum)
      {
        case Constants.RedirectType.SHORTURL:
          if (existingItem.TemplateID.ToString() == Templates.Inbound.SimpleRedirectItem.TemplateId)
          {
            return true;
          }
          Warnings.Add("The imported redirect '" + redirect.Name + "' has a different type than the existing item and can not be imported.");
          return false;

        case Constants.RedirectType.SIMPLEREDIRECT:
          if (existingItem.TemplateID.ToString() == Templates.Inbound.ShortUrlItem.TemplateId)
          {
            return true;
          }
          Warnings.Add("The imported redirect '" + redirect.Name + "' has a different type than the existing item and can not be imported.");
          return false;

        default:
          Warnings.Add("Redirect '" + redirect.Name + "' has an invalid redirect type and can not be imported.");
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
          redirect.Name.IsNullOrEmpty() ||
          redirect.Path.IsNullOrEmpty() ||
          redirect.PathToken.IsNullOrEmpty() ||
          redirect.Type == Constants.RedirectType.SHORTURL.ToString() && redirect.ShortUrlPrefix.IsNullOrEmpty() ||
          redirect.Status.IsNullOrEmpty() ||
          redirect.Target.IsNullOrEmpty())
      {
        Warnings.Add("Redirect '" + redirect.Name + "' has at least one empty field and can not be imported.");
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

      // check the id is valid
      if (!Guid.TryParse(redirect.ItemId, out var redirectGuid))
      {
        if (redirect.ItemId != string.Empty)
        {
          Warnings.Add("Redirect '" + redirect.Name + "' has an invalid id and can not be imported.");
          return false;
        }
        return true;
      }

      existingItem = _db.GetItem(ID.Parse(redirectGuid));
      return true;
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

      string[] foldersInPath = redirect.Path.Split('/');

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
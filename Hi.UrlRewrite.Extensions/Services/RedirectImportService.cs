﻿
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Hi.UrlRewrite.Entities.Rules;
using Hi.UrlRewrite.Extensions.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.StringExtensions;

namespace Hi.UrlRewrite.Extensions.Services
{
  public class RedirectImportService
  {
    public List<string> Warnings = new List<string>();

    /// <summary>
    /// Generates a csv file for the selected items
    /// </summary>
    /// <param name="redirects">The redirect items to generate i csv file from</param>
    public void GenerateCsv(IEnumerable<InboundRule> redirects)
    {

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
            ProcessRedirectItem(redirect, rootItem);
          }
        }
      }
      catch (Exception e)
      {
        throw;
      }
    }

    private void ProcessRedirectItem(RedirectCsvEntry redirect, Item rootItem)
    {
      if (!CheckValidity(redirect, out var existingItem))
      {
        return;
      }

      Enum.TryParse(redirect.Type, true, out Constants.RedirectType typeEnum);
      switch (typeEnum)
      {
        case Constants.RedirectType.SIMPLEREDIRECT:
          CreateSimpleRedirectItem(redirect, rootItem);
          return;
        case Constants.RedirectType.SHORTURL:
          CreateShortUrlItem(redirect, rootItem);
          return;
        default:
          Warnings.Add("Redirect " + redirect.Name + " has an invalid redirect type and can not be imported.");
          return;
      }
    }

    private void CreateSimpleRedirectItem(RedirectCsvEntry redirect, Item rootItem)
    {
      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        string enabled = redirect.Status.ToLower() == "enabled" ? "1" : "0";

        try
        {
          var template = Sitecore.Context.Database.GetTemplate(ID.Parse(Guid.Parse(Templates.Inbound.SimpleRedirectItem.TemplateId)));
          Item simpleRedirect = CreateFolderStructure(redirect, rootItem).Add(redirect.Name, template);
          simpleRedirect.Editing.BeginEdit();
          simpleRedirect["Path"] = redirect.PathToken;
          simpleRedirect["Target"] = redirect.Target;
          simpleRedirect["Enabled"] = enabled;
          simpleRedirect.Editing.EndEdit();
        }
        catch (Exception e)
        {
          Warnings.Add("There has been an error creating the item for '" + redirect.Name + "': \n" + e.Message);
        }
      }
    }

    private void CreateShortUrlItem(RedirectCsvEntry redirect, Item rootItem)
    {
      using (new Sitecore.SecurityModel.SecurityDisabler())
      {
        string enabled = redirect.Status.ToLower() == "enabled" ? "1" : "0";

        try
        {
          var template = Sitecore.Context.Database.GetTemplate(ID.Parse(Guid.Parse(Templates.Inbound.ShortUrlItem.TemplateId)));
          Item simpleRedirect = CreateFolderStructure(redirect, rootItem).Add(redirect.Name, template);
          simpleRedirect.Editing.BeginEdit();
          simpleRedirect["ShortUrl"] = redirect.PathToken;
          simpleRedirect["Target"] = redirect.Target;
          simpleRedirect["Enabled"] = enabled;
          simpleRedirect["Short Url Settings"] = FindShortUrlSettings(redirect.ShortUrlPrefix);
          simpleRedirect.Editing.EndEdit();
        }
        catch (Exception e)
        {
          Warnings.Add("There has been an error creating the item for '" + redirect.Name + "': \n" + e.Message);
        }
      }
    }

    private string FindShortUrlSettings(string prefix)
    {
      throw new NotImplementedException();
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
    /// <returns>True if all mandatory fields have a </returns>
    private bool CheckEmptyFields(RedirectCsvEntry redirect)
    {
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

      existingItem = Sitecore.Context.Database.GetItem(ID.Parse(redirectGuid));
      return true;
    }

    /// <summary>
    /// Gets the for the imported redirect based on the provided path.
    /// </summary>
    /// <param name="redirect">The imported redirect</param>
    /// <returns>The parent item</returns>
    private Item CreateFolderStructure(RedirectCsvEntry redirect, Item rootItem)
    {
      throw new NotImplementedException();
    }
  }
}
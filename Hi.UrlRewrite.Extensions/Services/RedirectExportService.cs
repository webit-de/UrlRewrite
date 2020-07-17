using System;
using System.Collections.Generic;
using System.Linq;
using Hi.UrlRewrite.Extensions.Models;
using Sitecore.Data;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Hi.UrlRewrite.Extensions.Services
{
  public class RedirectExportService
  {
    private readonly Database _db;

    private List<string> Warnings = new List<string>();

    public RedirectExportService(Database database)
    {
      _db = database;
    }

    public void ExportRedirects(Item rootItem, bool recursive)
    {
      var exportedRedirects = new HashSet<RedirectCsvEntry>();
      foreach (var redirect in rootItem.Children.Where(IsExportableItem))
      {
        exportedRedirects.Add(CreateRedirectEntry(redirect));
      }
    }

    private static bool IsExportableItem(Item item)
    {
      Assert.IsNotNull(item, "The item must not be null.");

      var templateIdString = item.TemplateID.ToString();
      return templateIdString == Templates.Inbound.SimpleRedirectItem.TemplateId ||
             templateIdString == Templates.Inbound.ShortUrlItem.TemplateId;
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

      Warnings.Add("The item '" +redirect.Name+ "' has an invalid type and could not be exported.");
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
        ShortUrlPrefix = GetShortUrlPrefix(simpleRedirect),
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

    private string GetStatus(Item shortUrl)
    {
      throw new NotImplementedException();
    }

    private string GetShortUrlPrefix(Item shortUrl)
    {
      throw new NotImplementedException();
    }

    private string GetRelativePath(Item redirect)
    {
      throw new NotImplementedException();
    }
  }
}
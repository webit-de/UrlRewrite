
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Hi.UrlRewrite.Entities.Rules;
using Hi.UrlRewrite.Extensions.Models;
using Sitecore.Data.Items;
using Sitecore.Diagnostics;

namespace Hi.UrlRewrite.Extensions.Services
{
  public static class RedirectImportService
  {
    /// <summary>
    /// Generates a csv file for the selected items
    /// </summary>
    /// <param name="redirects">The redirect items to generate i csv file from</param>
    public static void GenerateCsv(IEnumerable<InboundRule> redirects)
    {

    }


    public static void GenerateRedirectsFromCsv(Stream csvStream)
    {
      try
      {
        var warnings = new Dictionary<RedirectCsvEntry, string>();
        using (var reader = new StreamReader(csvStream))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
          csv.Configuration.PrepareHeaderForMatch = (string header, int index) => header.ToLower();
          foreach (var redirect in csv.GetRecords<RedirectCsvEntry>())
          {
            if (redirect.Type == Constants.SimpleRedirectType)
            {
              GenerateSimpleRedirectItem(redirect);
              continue;
            }

            if (redirect.Type == Constants.ShortUrlType)
            {
              GenerateShortUrlItem(redirect);
              continue;
            }

            warnings.Add(redirect, "Invalid redirect type.");
          }
          Sitecore.Diagnostics.Log.Debug("hallo");
        }
      }
      catch (Exception e)
      {
        throw;
      }
    }

    private static void GenerateSimpleRedirectItem(RedirectCsvEntry redirect)
    {
    }
    private static void GenerateShortUrlItem(RedirectCsvEntry redirect)
    {
    }
  }
}
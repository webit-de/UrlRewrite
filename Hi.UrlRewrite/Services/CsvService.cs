
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Hi.UrlRewrite.Models;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Hi.UrlRewrite.Services
{
  public static class CsvService
  {
    /// <summary>
    /// Generate a CSV from an enumerable of <see cref="ICsvModel"/>.
    /// </summary>
    /// <typeparam name="T">The model Type</typeparam>
    /// <param name="entries">The entries.</param>
    /// <returns>CSV as byte[]</returns>
    public static byte[] GenerateCsv<T>(IEnumerable<T> entries) where T : ICsvModel
    {
      var csvStream = new MemoryStream();
      using (var writer = new StreamWriter(csvStream))
      using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
      {
        csv.WriteRecords(entries);
        writer.Flush();
        return csvStream.ToArray();
      }
    }

    /// <summary>
    /// Get the csv stream from a CSV Media file
    /// </summary>
    /// <param name="db"></param>
    /// <param name="csvItemId"></param>
    /// <returns></returns>
    public static Stream GetCsvStream(Database db, string csvItemId)
    {
      var csvItem = db.GetItem(ID.Parse(Guid.Parse(csvItemId)));

      if (csvItem == null)
      {
        return null;
      }

      var csvMediaItem = new MediaItem(csvItem);

      return csvMediaItem.Extension.ToLower() != "csv" ? null : csvMediaItem.GetMediaStream();
    }
  }
}
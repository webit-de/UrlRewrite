
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using Hi.UrlRewrite.Extensions.Models;

namespace Hi.UrlRewrite.Extensions.Services
{
  public class CsvService
  {
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
  }
}
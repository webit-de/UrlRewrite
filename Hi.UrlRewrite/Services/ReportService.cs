
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Hi.UrlRewrite.Models;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Hi.UrlRewrite.Services
{
  public class ReportService
  {
    /// <summary>
    /// The report entries
    /// </summary>
    private readonly List<ImportExportReportEntry> _reportEntries = new List<ImportExportReportEntry>();

    /// <summary>
    /// Add a warning to the report
    /// </summary>
    /// <param name="redirect">The current redirect model</param>
    /// <param name="message">The message</param>
    /// <param name="created">Whether an item was created</param>
    public void AddWarning(string message, RedirectCsvEntry redirect, bool created = false)
    {
      _reportEntries.Add(new ImportExportReportEntry()
      {
        ItemName = redirect?.ItemName,
        ItemId = redirect?.ItemId,
        Message = message,
        Created = created
      });
    }

    /// <summary>
    /// Add a warning to the report
    /// </summary>
    /// <param name="redirect">The current redirect item</param>
    /// <param name="message">The message</param>
    /// <param name="created">Whether an item was created</param>
    public void AddWarning(string message, Item redirect, bool created = false)
    {
      _reportEntries.Add(new ImportExportReportEntry()
      {
        ItemName = redirect?.Name,
        ItemId = redirect?.ID.ToString(),
        Message = message,
        Created = created
      });
    }

    /// <summary>
    /// Add a warning to the report
    /// </summary>
    /// <param name="message">The message</param>
    public void AddWarning(string message)
    {
      _reportEntries.Add(new ImportExportReportEntry()
      {
        Message = message
      });
    }

    /// <summary>
    /// Get the report and write it as a media item
    /// </summary>
    /// <param name="rootItem">The root item</param>
    /// <returns>The report as csv string</returns>
    public string GetReport(Item rootItem, string nameAddition = "")
    {
      if (!_reportEntries.Any())
      {
        return string.Empty;
      }

      var csvResult = CsvService.GenerateCsv(_reportEntries);
      FileService.WriteFile(new MemoryStream(csvResult), rootItem.Database, Constants.ReportPath, FileService.GetFileName(rootItem, nameAddition), ".csv");

      return System.Text.Encoding.UTF8.GetString(csvResult);
    }

    /// <summary>
    /// Write the report as a Media Item
    /// </summary>
    /// <returns>The ID of the Media Item</returns>
    public ID WriteReport(Item rootItem, string nameAddition = "")
    {
      if (_reportEntries.Any())
      {
        var warningsStream = new MemoryStream(CsvService.GenerateCsv(_reportEntries));
        return FileService.WriteFile(warningsStream, rootItem.Database, Constants.ReportPath, FileService.GetFileName(rootItem, nameAddition), ".csv");
      }
      return ID.Null;
    }
  }
}
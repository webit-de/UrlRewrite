
using CsvHelper.Configuration.Attributes;

namespace Hi.UrlRewrite.Models
{
  /// <summary>
  /// CSV Model for the import / export report
  /// </summary>
  public class ImportExportReportEntry : ICsvModel
  {
    [Name("ItemName")]
    public string ItemName { get; set; }
    [Name("Message")]
    public string Message { get; set; }
    [Name("Created")]
    public bool Created { get; set; }
    [Name("ItemId")]
    public string ItemId { get; set; }
  }
}
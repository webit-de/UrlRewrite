
using CsvHelper.Configuration.Attributes;

namespace Hi.UrlRewrite.Models
{
  /// <summary>
  /// CSV Model for redirect entries
  /// </summary>
  public class RedirectCsvEntry : ICsvModel
  {
    [Name("RedirectedUrl")]
    public string RedirectedUrl { get; set; }
    [Name("ItemName")]
    public string ItemName { get; set; }
    [Name("Type")]
    public string Type { get; set; }
    [Name("ShortUrlPrefix")]
    public string ShortUrlPrefix { get; set; }
    [Name("Status")]
    public string Status { get; set; }
    [Name("RelativeItemPath")]
    public string RelativeItemPath { get; set; }
    [Name("RedirectTarget")]
    public string RedirectTarget { get; set; }
    [Name("ItemId")]
    public string ItemId { get; set; }
  }
}
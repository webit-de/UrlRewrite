
using CsvHelper.Configuration.Attributes;

namespace Hi.UrlRewrite.Extensions.Models
{
  public class RedirectCsvEntry : ICsvModel
  {
    [Name("Redirected Url")]
    public string RedirectedUrl { get; set; }
    [Name("Item Name")]
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
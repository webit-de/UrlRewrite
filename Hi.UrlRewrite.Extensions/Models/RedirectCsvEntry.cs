
using CsvHelper.Configuration.Attributes;

namespace Hi.UrlRewrite.Extensions.Models
{
  public class RedirectCsvEntry
  {
    [Name("Path/Token")]
    public string PathToken { get; set; }
    [Name("Name")]
    public string Name { get; set; }
    [Name("Type")]
    public string Type { get; set; }
    [Name("ShortUrlPrefix")]
    public string ShortUrlPrefix { get; set; }
    [Name("Status")]
    public string Status { get; set; }
    [Name("Path")]
    public string Path { get; set; }
    [Name("Target")]
    public string Target { get; set; }
    [Name("ItemId")]
    public string ItemId { get; set; }
  }
}
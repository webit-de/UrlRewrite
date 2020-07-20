
using System.Text;
using CsvHelper.Configuration.Attributes;

namespace Hi.UrlRewrite.Extensions.Models
{
  public class ImportExportLogEntry : ICsvModel
  {
    [Name("Item Name")]
    public string ItemName { get; set; }
    [Name("Message")]
    public string Message { get; set; }
    [Name("Created")]
    public bool Created { get; set; }
    [Name("Item Id")]
    public string ItemId { get; set; }
  }
}
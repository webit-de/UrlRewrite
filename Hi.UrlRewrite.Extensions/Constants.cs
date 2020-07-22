
namespace Hi.UrlRewrite.Extensions
{
  public class Constants
  {
    /// <summary>
    /// The path for storing import / export reports
    /// </summary>
    public static readonly string ReportPath = "/sitecore/media library/Files/Url Rewrite/Reports/";

    /// <summary>
    /// The path for storing exported csv files
    /// </summary>
    public static readonly string ExportPath = "/sitecore/media library/Files/Url Rewrite/Redirect Exports/";

    /// <summary>
    /// Enum for exportable redirect types
    /// </summary>
    public enum RedirectType
    {
      SHORTURL,
      SIMPLEREDIRECT
    }

    /// <summary>
    /// Enum for states handled in imports
    /// </summary>
    public enum ImportStatus
    {
      ENABLED,
      DISABLED,
      DELETE
    }
  }
}
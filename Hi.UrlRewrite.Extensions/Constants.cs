using System;
using Sitecore.Data;

namespace Hi.UrlRewrite.Extensions
{
  public class Constants
  {
    public static readonly ID RedirectExportDialogueScript =
      ID.Parse(Guid.Parse("{5A3D2D3B-C56F-4AEF-84E8-08FCB9598BFE}"));

    public static readonly ID RedirectImportDialogueScript =
      ID.Parse(Guid.Parse("{B4D24E93-23D2-44C8-BFFF-8F01B6EE8FA3}"));

    public static readonly string LogPath = "/sitecore/media library/Files/Url Rewrite/Logs/";
    public static readonly string ExportPath = "/sitecore/media library/Files/Url Rewrite/Redirect Exports/";

    public enum RedirectType
    {
      SHORTURL,
      SIMPLEREDIRECT
    }

    public enum ImportStatus
    {
      ENABLED,
      DISABLED,
      DELETE
    }
  }
}
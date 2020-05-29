using System;
using System.Linq;
using System.Threading;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Web;

namespace Hi.UrlRewrite.Processing
{
  public class ShortUrlRewriteProcessor : HttpRequestProcessor
  {
    public override void Process(HttpRequestArgs args)
    {
      var db = Context.Database;
      var language = Context.Language;

      // TODO: get prefix
      if (db == null || language == null || !args.Url.FilePathWithQueryString.Contains("goto"))
      {
        return;
      }
      var token = ExtractShortUrlToken(args.Url.FilePathWithQueryString);

      try
      {
        // TODO: use caching
        var query = "/sitecore/content//*[@@templateid='{EA7922DB-83AD-49BA-AD53-F30F058CEE74}']";
        var shortUrlItem = db.SelectItems(query)
          .FirstOrDefault(x => x.Fields[ID.Parse(Guid.Parse(Constants.ShortUrl_FieldId))].Value.Equals(token));

        if (shortUrlItem == null)
        {
          return;
        }

        LinkField targetField = shortUrlItem.Fields[ID.Parse(Guid.Parse(Constants.ShortUrlTarget_FieldId))];
        WebUtil.RewriteUrl(targetField.Url);
        Context.Item = targetField.TargetItem;
      }
      catch (ThreadAbortException)
      {
        // swallow this exception because we may have called Response.End
      }
      catch (Exception ex)
      {
        if (ex is ThreadAbortException) return;

        Log.Error(this, ex, db, "Exception occured");
      }
    }

    /// <summary>
    /// Extracts the shourt url token from the provided url
    /// </summary>
    /// <param name="url">The url</param>
    /// <returns>The short url token</returns>
    private static string ExtractShortUrlToken(string url)
    {
      var urlSegments = url.Split('/');
      var tokenIndex = Array.IndexOf(urlSegments, "goto") + 1;

      return urlSegments[tokenIndex];
    }
  }
}
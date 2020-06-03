using System;
using System.Linq;
using System.Threading;
using Hi.UrlRewrite.Templates.Folders;
using Sitecore;
using Sitecore.Data;
using Sitecore.Data.Fields;
using Sitecore.Pipelines.HttpRequest;
using Sitecore.Web;

namespace Hi.UrlRewrite.Processing
{
  /// <summary>
  /// Processor for short url requests
  /// </summary>
  public class ShortUrlRewriteProcessor : HttpRequestProcessor
  {
    public override void Process(HttpRequestArgs args)
    {
      var db = Context.Database;
      var language = Context.Language;

      if (db == null || language == null)
      {
        return;
      }

      try
      {
        var shortUrlPrefix = GetShortUrlPrefix(db);

        // don't redirect, if no prefix is configured, or the url does not start with the short url prefix
        if (shortUrlPrefix.Equals(string.Empty) 
            || !args.Url.FilePathWithQueryString.StartsWith("/" + shortUrlPrefix) 
            || args.Url.FilePathWithQueryString.Trim('/').EndsWith(shortUrlPrefix)) // ensure that there is a token Present
        {
          return;
        }


        // get the short url item for the provided token
        var token = ExtractShortUrlToken(args.Url.FilePathWithQueryString,shortUrlPrefix);
        var query = Context.Site.ContentStartPath + "//*[@@templateid='" + Constants.ShortUrl_ItemId + "']";
        var shortUrlItem = db.SelectItems(query)
          .FirstOrDefault(x => x.Fields[ID.Parse(Guid.Parse(Constants.ShortUrl_FieldId))].Value.Equals(token));

        if (shortUrlItem == null)
        {
          return;
        }

        // perform the redirect
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
    /// Extracts the short url token from the provided url
    /// </summary>
    /// <param name="url">The url</param>
    /// <returns>The short url token</returns>
    private static string ExtractShortUrlToken(string url, string shortUrlPrefix)
    {
      var urlSegments = url.Split('/');
      var tokenIndex = Array.IndexOf(urlSegments, shortUrlPrefix) + 1;

      return urlSegments[tokenIndex];
    }

    /// <summary>
    /// Get the short url prefix for the current context
    /// </summary>
    /// <param name="db">The database of the current context</param>
    /// <returns>The prefix for short urls</returns>
    private static string GetShortUrlPrefix(Database db)
    {
      // find prefix in site context
      var query = Context.Site.ContentStartPath + "//*[@@templateid='" + RedirectFolderItem.TemplateId + "']";
      var rewriteFolderItem = db.SelectItems(query).FirstOrDefault();
      if (rewriteFolderItem != null)
      {
        return rewriteFolderItem.Fields[ID.Parse(Guid.Parse(Constants.ShortUrlPrefix_FieldId))].Value;
      }

      // find global prefix
      query = "sitecore/content//*[@@templateid='" + RedirectFolderItem.TemplateId + "']";
      rewriteFolderItem = db.SelectItems(query).FirstOrDefault();

      return rewriteFolderItem == null ? string.Empty : rewriteFolderItem.Fields[ID.Parse(Guid.Parse(Constants.ShortUrlPrefix_FieldId))].Value;
    }
  }
}

using System;
using Hi.UrlRewrite.Templates.Inbound;
using Sitecore.Data;
using Sitecore.SecurityModel;
using Sitecore.Shell.Framework.Commands;

namespace Hi.UrlRewrite.Tasks
{
  public class GenerateShortUrl : Command
  {
    /// <summary>
    /// Generate a unique short url
    /// </summary>
    /// <param name="context">The command context</param>
    public override void Execute(CommandContext context)
    {
      if (context.Items.Length != 1 || context.Items[0] == null)
        return;

      var shortUrlItem = new ShortUrlItem(context.Items[0]);
      var shortUrl = Helpers.ShortUrlHelpers.GenerateShortUrl(shortUrlItem);
      using (new SecurityDisabler())
      {
        context.Items[0].Editing.BeginEdit();
        context.Items[0].Fields[ID.Parse(Guid.Parse(Constants.ShortUrl_FieldId))].Value = shortUrl;
        context.Items[0].Editing.EndEdit();
      }
    }
  }
}
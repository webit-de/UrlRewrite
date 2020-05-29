
using System;
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

      var shortUrl = Helpers.ShortUrlHelpers.GenerateShortUrl(context.Items[0]);
      using (new SecurityDisabler())
      {
        context.Items[0].Editing.BeginEdit();
        context.Items[0].Fields[ID.Parse(Guid.Parse(Constants.ShortUrl_FieldId))].Value = shortUrl;
        context.Items[0].Editing.EndEdit();
      }
    }
  }
}
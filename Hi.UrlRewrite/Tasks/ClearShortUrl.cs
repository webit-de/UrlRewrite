using System;
using Sitecore.Data;
using Sitecore.SecurityModel;
using Sitecore.Shell.Framework.Commands;

namespace Hi.UrlRewrite.Tasks
{
  public class ClearShortUrl : Command
  {
    /// <summary>
    /// Clears the short url field
    /// </summary>
    /// <param name="context">The command context</param>
    public override void Execute(CommandContext context)
    {
      if (context.Items.Length != 1 || context.Items[0] == null)
        return;

      using (new SecurityDisabler())
      {
        context.Items[0].Editing.BeginEdit();
        context.Items[0].Fields[ID.Parse(Guid.Parse(Constants.ShortUrl_FieldId))].Value = string.Empty;
        context.Items[0].Editing.EndEdit();
      }
    }
  }
}
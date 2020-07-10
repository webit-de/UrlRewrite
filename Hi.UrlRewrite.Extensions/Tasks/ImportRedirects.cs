using System;
using Sitecore.Diagnostics;
using Sitecore.Shell.Framework.Commands;
using Sitecore.StringExtensions;
using Spe.Core.Host;

namespace Hi.UrlRewrite.Extensions.Tasks
{
  public class ImportRedirects : Command
  {
    /// <summary>
    /// Starts the import redirects dialogue
    /// </summary>
    /// <param name="context">The command context</param>
    public override void Execute(CommandContext context)
    {
      using (ScriptSession session = ScriptSessionManager.NewSession("Defailt", false))
      {
        var scriptitem = Sitecore.Context.Database.GetItem(Constants.RedirectImportDialogueScript);
        var script = scriptitem["Script"];
        if (script.IsNullOrEmpty())
        {
          return;
        }

        try
        {
          session.ExecuteScriptPart(script);
        }
        catch (Exception e)
        {
          Console.WriteLine("There has been an error executing the redirect import script: \n" + e.Message);
          throw;
        }
      }
    }

    /// <summary>
    /// Check if the command is active
    /// </summary>
    /// <param name="context">The command context</param>
    /// <returns>CommandState.Enabled if the selected item is a redirect folder item. CommandState.Disabled otherwise.</returns>
    public override CommandState QueryState(CommandContext context)
    {
      Assert.ArgumentNotNull((object)context, nameof(context));

      if (context.Items.Length != 1 || context.Items[0] == null)
      {
        return CommandState.Disabled;
      }

      var itemTemplate = context.Items[0].TemplateID.ToString();
      return itemTemplate.Equals(Templates.Folders.RedirectFolderItem.TemplateId) ? CommandState.Enabled : CommandState.Disabled;
    }
  }
}
using System.Web.Mvc;
using Sitecore.Data.Items;
using Sitecore.Mvc.Controllers;

namespace Hi.UrlRewrite.Extensions.Controllers
{
  public class ExportRedirectsController : SitecoreController
  {
    public ActionResult ExportRedirects(string rootFolderId = "")
    {
      return new EmptyResult();
    }
    
    ///// <summary>
    ///// Starts the export redirects dialogue
    ///// </summary>
    ///// <param name="context">The command context</param>
    //public override void Execute(CommandContext context)
    //{
    //  using (ScriptSession session = ScriptSessionManager.NewSession("Defailt", false))
    //  {
    //    var scriptitem = Sitecore.Context.Database.GetItem(Constants.RedirectExportDialogueScript);
    //    var script = scriptitem["Script"];
    //    if (script.IsNullOrEmpty())
    //    {
    //      return;
    //    }

    //    try
    //    {
    //      session.ExecuteScriptPart(script);
    //    }
    //    catch (Exception e)
    //    {
    //      Console.WriteLine("There has been an error executing the redirect export script: \n" + e.Message);
    //      throw;
    //    }
    //  }
    //}

    ///// <summary>
    ///// Check if the command is active
    ///// </summary>
    ///// <param name="context">The command context</param>
    ///// <returns>CommandState.Enabled if the selected item is a redirect folder or redirect subfolder item. CommandState.Disabled otherwise.</returns>
    //public override CommandState QueryState(CommandContext context)
    //{
    //  Assert.ArgumentNotNull((object)context, nameof(context));

    //  if (context.Items.Length != 1 || context.Items[0] == null)
    //  {
    //    return CommandState.Disabled;
    //  }

    //  var itemTemplate = context.Items[0].TemplateID.ToString();
    //  if (itemTemplate.Equals(Templates.Folders.RedirectFolderItem.TemplateId)
    //  || itemTemplate.Equals(Templates.Folders.RedirectSubFolderItem.TemplateId))
    //  {
    //    return CommandState.Enabled;
    //  }

    //  return CommandState.Disabled;
    //}
  }
}
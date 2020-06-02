using System.Linq;
using System.Web.UI;
using Hi.UrlRewrite.Helpers;
using Sitecore.Configuration;
using Sitecore.Diagnostics;
using Control = Sitecore.Web.UI.HtmlControls.Control;

namespace Hi.UrlRewrite.Fields
{
  public class ShortUrl : Control
  {

    public ShortUrl()
    {
      Class = "scContentControl";
    }

    protected override void DoRender(HtmlTextWriter output)
    {
      // display the full url instead of only the token
      var owningItem = Sitecore.Data.Database.GetDatabase("master").GetItem(ItemID);

      var redirectFolderItem = ShortUrlHelpers.GetRedirectFolderItem(owningItem);

      var valueString = string.Empty;

      // only display a value, if a token is assigned
      if (Value != string.Empty)
      {
        valueString = GetHostname(owningItem) + "/" + redirectFolderItem.ShortUrlPrefix + "/" + Value;
      }

      output.Write("<input" + GetControlAttributes() + "value='" + valueString + "' readonly >");
    }

    public string ItemID
    {
      get
      {
        return base.GetViewStateString("ItemID");
      }
      set
      {
        Assert.ArgumentNotNullOrEmpty(value, "value");
        base.SetViewStateString("ItemID", value);
      }
    }

    string GetHostname(Sitecore.Data.Items.Item item)
    {
      var siteInfos = Factory.GetSiteInfoList().Where(x => x.RootPath.ToLower().StartsWith("/sitecore/content/"));

      return siteInfos?.FirstOrDefault(x => item.Paths.FullPath.StartsWith(x.ContentStartItem))?.TargetHostName;

    }
  }
}

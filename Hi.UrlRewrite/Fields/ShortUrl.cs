using System.Linq;
using System.Web.UI;
using Hi.UrlRewrite.Templates.Inbound;
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
      var owningItem = new ShortUrlItem(Sitecore.Data.Database.GetDatabase("master").GetItem(ItemID));
      
      string valueString;

      // only display a value, if a token is assigned
      if (Value != string.Empty && owningItem.UrlSetting != null)
      {
        valueString = GetHostname(owningItem) + "/" + owningItem.UrlSetting.ShortUrlPrefix + "/" + Value;
      }
      else
      {
        {
          valueString = owningItem.UrlSetting == null
            ? "Please select a Short Url Settings item."
            : "No Short Url has been generated yet.";
        }
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

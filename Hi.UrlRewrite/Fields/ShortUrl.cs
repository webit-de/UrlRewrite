using System.Web.UI;
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
      string err = null;
      output.Write("<input" + GetControlAttributes() + "value='" + Value + "' readonly >");
      
    }
  }
}

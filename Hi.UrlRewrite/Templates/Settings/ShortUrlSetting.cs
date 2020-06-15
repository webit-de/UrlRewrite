using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Hi.UrlRewrite.Templates.Settings
{
  public partial class ShortUrlSetting : CustomItem
  {

    public static readonly string TemplateId = "{C6EE5060-3D99-463F-9E7B-978A14E0FA5A}";


    #region Boilerplate CustomItem Code

    public ShortUrlSetting(Item innerItem)
      : base(innerItem)
    {

    }

    public static implicit operator ShortUrlSetting(Item innerItem)
    {
      return innerItem != null ? new ShortUrlSetting(innerItem) : null;
    }

    public static implicit operator Item(ShortUrlSetting customItem)
    {
      return customItem != null ? customItem.InnerItem : null;
    }

    #endregion //Boilerplate CustomItem Code


    #region Field Instance Methods

    public string ShortUrlPrefix => InnerItem.Fields["Prefix"].Value;

    public int TokenLength => int.Parse(InnerItem.Fields["Token Length"].Value);

    #endregion //Field Instance Methods
  }
}
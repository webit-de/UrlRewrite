using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Hi.UrlRewrite.Templates.Settings;
using Sitecore.Data.Fields;
using Sitecore.Data.Items;

namespace Hi.UrlRewrite.Templates.Inbound
{
  public class ShortUrlItem : CustomItem
  {
    public static readonly string TemplateId = "{407C94A5-869D-49BB-A57B-4551AF040B75}";

    #region Inherited Base Templates

    private readonly BaseUrlRewriteItem _BaseUrlRewriteItem;
    public BaseUrlRewriteItem BaseUrlRewriteItem { get { return _BaseUrlRewriteItem; } }

    private readonly BaseEnabledItem _BaseEnabledItem;
    public BaseEnabledItem BaseEnabledItem { get { return _BaseEnabledItem; } }

    #endregion

    #region Boilerplate CustomItem Code

    public ShortUrlItem(Item innerItem)
      : base(innerItem)
    {
      _BaseUrlRewriteItem = new BaseUrlRewriteItem(innerItem);
      _BaseEnabledItem = new BaseEnabledItem(innerItem);
    }

    public static implicit operator ShortUrlItem(Item innerItem)
    {
      return innerItem != null ? new ShortUrlItem(innerItem) : null;
    }

    public static implicit operator Item(ShortUrlItem customItem)
    {
      return customItem != null ? customItem.InnerItem : null;
    }

    #endregion //Boilerplate CustomItem Code


    #region Field Instance Methods


    public string ShortUrl
    {
      get
      {
        var urlSetting = new ShortUrlSetting(UrlSetting.TargetItem);
        return urlSetting.ShortUrlPrefix.Value + InnerItem.Fields["Short Url"];
      }
    }


    public LinkField Target
    {
      get
      {
        return new LinkField(InnerItem.Fields["Target"]);
      }
    }

    public LinkField UrlSetting
    {
      get
      {
        return new LinkField(InnerItem.Fields["Short Url Settings"]);
      }
    }
    public int SortOrder
    {
      get
      {
        return this.InnerItem.Appearance.Sortorder;
      }
    }

    #endregion //Field Instance Methods
  }
}
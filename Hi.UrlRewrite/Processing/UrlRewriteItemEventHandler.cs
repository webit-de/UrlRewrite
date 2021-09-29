using System;
using System.Linq;
using Hi.UrlRewrite.Templates.Folders;
using Hi.UrlRewrite.Templates.Inbound;
using Sitecore.Data;
using Sitecore.Data.Events;
using Sitecore.Data.Items;
using Sitecore.Events;
using Sitecore.SecurityModel;

namespace Hi.UrlRewrite.Processing
{
  public class UrlRewriteItemEventHandler
  {

    #region ItemSaved

    public void OnItemSaved(object sender, EventArgs args)
    {
      var item = Event.ExtractParameter(args, 0) as Item;
      var itemChanges = Event.ExtractParameter(args, 1) as ItemChanges;

      if (item == null)
      {
        return;
      }

      RunItemSaved(item, itemChanges);
    }

    public void OnItemSavedRemote(object sender, EventArgs args)
    {
      var itemSavedRemoteEventArg = args as ItemSavedRemoteEventArgs;
      if (itemSavedRemoteEventArg == null)
      {
        return;
      }

      var itemId = itemSavedRemoteEventArg.Item.ID;
      var db = itemSavedRemoteEventArg.Item.Database;
      Item item;

      using (new SecurityDisabler())
      {
        item = db.GetItem(itemId, itemSavedRemoteEventArg.Item.Language);
      }

      RunItemSaved(item, itemSavedRemoteEventArg.Changes);
    }

    private void RunItemSaved(Item item, ItemChanges itemChanges)
    {
      var db = item.Database;
      var language = item.Language;
      var rulesEngine = new RulesEngine(db, language);

      try
      {
        using (new SecurityDisabler())
        {
          var redirectFolderItem = GetRedirectFolderItem(item);

          if (redirectFolderItem == null) return;

          if (item.IsRedirectFolderItem())
          {
            Log.Info(this, db, "Clearing cached inbound rules (reason: Redirect Folder [{0}] saves)",
              item.Paths.FullPath);

            rulesEngine.ClearInboundRuleCache();
          }
          else if (item.IsOutboundRuleItem())
          {
            Log.Info(this, db, "Refreshing Outbound Rule [{0}] after save event", item.Paths.FullPath);

            rulesEngine.RefreshRule(item, redirectFolderItem);
          }
          else if (item.IsSimpleRedirectItem())
          {
            if (rulesEngine.CanRefreshInboundRule(item, redirectFolderItem))
            {
              Log.Info(this, db, "Refreshing Simple Redirect [{0}] after save event", item.Paths.FullPath);

              rulesEngine.RefreshRule(item, redirectFolderItem);
            }
            else
            {
              Log.Info(this, db,
                "Simple Redirect [{0}] cannot be individually refreshed after save event. Clearing inbound rule cache.",
                item.Paths.FullPath);

              rulesEngine.ClearInboundRuleCache();
            }
          }
          else if (item.IsInboundRuleItem())
          {
            if (rulesEngine.CanRefreshInboundRule(item, redirectFolderItem))
            {
              Log.Info(this, db, "Refreshing Inbound Rule [{0}] after save event", item.Paths.FullPath);

              rulesEngine.RefreshRule(item, redirectFolderItem);
            }
            else
            {
              Log.Info(this, db,
                "Inbound Rule [{0}] cannot be individually refreshed after save event. Clearing inbound rule cache.",
                item.Paths.FullPath);

              rulesEngine.ClearInboundRuleCache();
            }
          }
          else if (item.IsRedirectType() && item.IsInboundRuleItemChild() &&
                   db.Name.Equals("master", StringComparison.CurrentCultureIgnoreCase))
          {
            var inboundRuleItem = item.Parent;
            var inboundRule = new InboundRuleItem(inboundRuleItem);

            inboundRule.BeginEdit();
            inboundRule.Action.InnerField.SetValue(item.ID.ToString(), false);
            inboundRule.EndEdit();
          }
          else if (item.IsInboundRuleItemChild())
          {
            if (rulesEngine.CanRefreshInboundRule(item.Parent, redirectFolderItem))
            {
              Log.Info(this, db, "Refreshing Inbound Rule [{0}] after save event", item.Parent.Paths.FullPath);

              rulesEngine.RefreshRule(item.Parent, redirectFolderItem);
            }
            else
            {
              Log.Info(this, db,
                "Inbound Rule [{0}] cannot be individually refreshed after save event. Clearing inbound rule cache.",
                item.Parent.Paths.FullPath);

              rulesEngine.ClearInboundRuleCache();
            }
          }
          else if (item.IsOutboundRuleItemChild())
          {
            Log.Info(this, db, "Refreshing Outbound Rule [{0}] after save event", item.Parent.Paths.FullPath);

            rulesEngine.RefreshRule(item.Parent, redirectFolderItem);
          }
        }
      }
      catch (Exception ex)
      {
        Log.Error(this, ex, db, "Exception occured when saving item after save - Item ID: {0} Item Path: {1}", item.ID,
          item.Paths.FullPath);
      }

    }

    private static Item GetRedirectFolderItem(Item item)
    {
      return item.Axes.GetAncestors().FirstOrDefault(a => a.TemplateID.Equals(new ID(RedirectFolderItem.TemplateId)));
    }

    #endregion

    #region ItemDeleting

    public void OnItemDeleting(object sender, EventArgs args)
    {
      var item = Event.ExtractParameter(args, 0) as Item;
      if (item == null)
      {
        return;
      }

      RunItemDeleted(item, item.ParentID);
    }

    public void OnItemDeletingRemote(object sender, EventArgs args)
    {
      var item = Event.ExtractParameter(args, 0) as Item;
      if (item == null)
      {
        return;
      }

      RunItemDeleted(item, item.ParentID);
    }

    private void RunItemDeleted(Item item, ID formerParentId)
    {

      var rulesEngine = new RulesEngine(item.Database, item.Language);

      try
      {

        using (new SecurityDisabler())
        {

          if (item.IsInboundRuleItem() || item.IsSimpleRedirectItem())
          {
            Log.Info(this, item.Database, "Clearing inbound rules cache after delete event");

            rulesEngine.DeleteRule(item, null);
            //rulesEngine.ClearInboundRuleCache();
          }
          else if (item.IsInboundRuleItemChild(formerParentId))
          {
            Item itemParent = item.Parent;
            if (itemParent == null && formerParentId != (ID)null)
            {
              itemParent = item.Database.GetItem(formerParentId);
            }

            Log.Info(this, item.Database, "Updating parent inbound rule ({0}) after child delete event",
              itemParent.Paths.Path);

            rulesEngine.RefreshRule(itemParent, GetRedirectFolderItem(itemParent));
          }
          else if (item.IsOutboundRuleItem() || item.IsOutboundRuleItemChild())
          {
            Log.Info(this, item.Database, "Clearing outbound rules cache after delete event",
              item.Parent.Paths.FullPath);

            rulesEngine.ClearOutboundRuleCache();
          }

        }
      }
      catch (Exception ex)
      {
        Log.Error(this, ex, item.Database,
          "Exception occured which deleting item after publish Item ID: {0} Item Path: {1}", item.ID,
          item.Paths.FullPath);
      }
    }

    #endregion

  }
}

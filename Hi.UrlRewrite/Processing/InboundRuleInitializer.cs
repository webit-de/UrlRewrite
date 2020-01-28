﻿using Sitecore.Configuration;
using Sitecore.Data;
using Sitecore.Pipelines;
using Sitecore.SecurityModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Managers;
using Sitecore.Globalization;
using Sitecore.Sites;

namespace Hi.UrlRewrite.Processing
{
    public class InboundRuleInitializer
    {
        private string masterDatabaseName = "master";

        public void Process(PipelineArgs args)
        {
            Log.Info(this, "Initializing URL Rewrite.");

            try
            {
              using (new SecurityDisabler())
              using(new SiteContextSwitcher(SiteContext.GetSite("shell")))
              {
                // cache all of the rules
                foreach (var db in Factory.GetDatabases().Where(e => e.HasContentItem))
                {
                  foreach (var language in LanguageManager.GetLanguages(db))
                  {
                    var rulesEngine = new RulesEngine(db, language);
                    rulesEngine.GetCachedInboundRules();
                  }
                }

                // make sure that the page event has been deployed
                DeployEventIfNecessary();
              }
            }
            catch (Exception ex)
            {
                Hi.UrlRewrite.Log.Error(this, ex, "Exception during initialization.");
            }
        }

        private void DeployEventIfNecessary()
        {
            if (!Sitecore.Configuration.Factory.GetDatabaseNames().Contains(masterDatabaseName, StringComparer.InvariantCultureIgnoreCase))
            {
                Log.Info(this, "Skipping DeployEventIfNecessary() as '{0}' database not present", masterDatabaseName);
                return;
            }

            var database = Sitecore.Data.Database.GetDatabase(masterDatabaseName);
            if (database == null)
            {
                return;
            }

            var eventItem = database.GetItem(new ID(Constants.RedirectEventItemId));
            if (eventItem == null)
            {
                return;
            }

            var workflow = database.WorkflowProvider.GetWorkflow(eventItem);
            if (workflow == null)
            {
                return;
            }

            var workflowState = workflow.GetState(eventItem);
            if (workflowState == null)
            {
                return;
            }

            const string analyticsDraftStateWorkflowId = "{39156DC0-21C6-4F64-B641-31E85C8F5DFE}";

            if (!workflowState.StateID.Equals(analyticsDraftStateWorkflowId))
            {
                return;
            }

            const string analyticsDeployCommandId = "{4044A9C4-B583-4B57-B5FF-2791CB0351DF}";
            var workflowResult = workflow.Execute(analyticsDeployCommandId, eventItem, "Deploying UrlRewrite Redirect event during initialization", false, new object[0]);

        }



    }



}
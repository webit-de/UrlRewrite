﻿
namespace Hi.UrlRewrite
{
    public static class Constants
    {
        //public const string RedirectFolderItemsQuery = "fast:{0}//*[@@templateid = '{1}']";
        public const string RewriteFolderSearchQueryDefault = "{0}/descendant-or-self::*[@@templateid = '{1}']";
        public const string RewriteFolderSearchRootDefault = "/sitecore/content";
        public const string TwoTemplateQuery = "*[@@templateid = '{0}' or @@templateid = '{1}']";
        public const string SingleTemplateQuery = "*[@@templateid = '{0}']";

        public const string MatchType_MatchesThePattern_ItemId = "{2C94D94E-6FDA-465B-BCA1-4C18EF249EAB}";
        public const string MatchType_DoesNotMatchThePattern_ItemId = "{35479F72-B51C-4878-8BE1-53373D66633A}";

        public const string MatchScope_Response_ItemId = "{2ACBEB4A-DCA3-4D96-BA9C-CB592789BBF3}";
        public const string MatchScope_ServerVariables_ItemId = "{BB9BA60D-CAA4-4D74-88A4-B0553BA67401}";

        public const string UsingType_RegularExpressions_ItemId = "{75BFA469-AE7D-47FD-9A2F-DD8B3AF0865C}";
        public const string UsingType_Wildcards_ItemId = "{E936A17D-0014-4848-9779-1D9BE9095A7D}";
        public const string UsingType_ExactMatch_ItemId = "{3323E74E-2BC8-4055-8A2B-B95656B2E786}";

        public const string LogicalGroupingType_MatchAll_ItemId = "{3FCBE882-C812-4C3C-B89F-4E98A2596C97}";
        public const string LogicalGroupingType_MatchAny_ItemId = "{8E8F11D0-5401-417B-B9E7-46264A2B1D7C}";

        public const string RedirectType_Permanent_ItemId = "{C194D441-47EB-4C89-A336-4FDE6A2DC6B3}";
        public const string RedirectType_Found_ItemId = "{D80D36EB-F98A-419B-B4C4-497E31FBA8A0}";
        public const string RedirectType_SeeOther_ItemId = "{6AC362BB-AFFD-4FE7-AB23-C2B2B6E33105}";
        public const string RedirectType_Temporary_ItemId = "{5A6BE6F1-9D9A-460F-B990-C40BBF78FC6E}";

        public const string HttpCacheabilityType_NoCache_ItemId = "{9FF1B0F9-D0B5-417C-9DD6-5228931F23BF}";
        public const string HttpCacheabilityType_Private_ItemId = "{E0450AB3-1573-4FBB-BC8F-BDFCBDDACE17}";
        public const string HttpCacheabilityType_Server_ItemId = "{E9F6A9D3-11A5-4CB1-B1B6-2C017C0506E0}";
        public const string HttpCacheabilityType_ServerAndNoCache_ItemId = "{2C41C9F2-7CCC-494D-843C-20EE7CA236C5}";
        public const string HttpCacheabilityType_Public_ItemId = "{6BCFC758-B190-44D4-88DF-B34E977D9202}";
        public const string HttpCacheabilityType_ServerAndPrivate_ItemId = "{E2BF5631-ACC2-4906-8B66-FC23A65163EE}";


        public const string CheckIfInputStringType_IsAFile_ItemId = "{B8D9255F-03CF-4331-AE54-B771E9815A55}";
        public const string CheckIfInputStringType_IsNotAFile_ItemId = "{4F431A8F-3DA5-439B-9640-B5B3D2FDF643}";
        public const string CheckIfInputStringType_IsADirectory_ItemId = "{60952AD2-8862-4B24-AD28-27B69678C6BC}";
        public const string CheckIfInputStringType_IsNotADirectory_ItemId = "{7D4064C9-68D3-4D49-9F47-345CA7675DAB}";
        public const string CheckIfInputStringType_MatchesThePattern_ItemId = "{B30DC355-4122-4260-8AC6-0F9E93205556}";
        public const string CheckIfInputStringType_DoesNotMatchThePattern_ItemId = "{2F8AEB84-DE5C-4102-85B6-AB8059F7CB85}";

        public const string ConditionInputType_QueryString_ItemId = "{DBD45014-AA3C-4F63-92B6-D72C23DD5C26}";
        public const string ConditionInputType_HttpHost_ItemId = "{CE714D26-9BC2-44AB-91CB-5D98F7BF7DE4}";
        public const string ConditionInputType_Https_ItemId = "{F9D6EA61-3C0B-41FA-8DA1-8405BED83BAD}";

        public const string UrlRewriteModuleFolder_ItemId = "{76C853FC-AA0B-4412-BA5A-BCF44BF0194C}";
        public const string UrlRewriteTemplatesFolder_ItemId = "{5D183D73-5F71-48D5-B381-47D96E9687B9}";
        public const string UrlRewriteSettings_ItemId = "{CD887CD4-9837-4929-B68E-366E3A7D1FEA}";

        public const string UrlRewriter_ItemId = "{3CF68609-B1F2-4ADE-B7E3-91B5CF74F5B8}";
        public const string RedirectEventItemId = "{1d668f23-eeba-4bd3-93b3-94861ed42060}";

        public const string ShortUrlTokenCharacters = "abcdefghijkmnopqrstuvwxyzABCDEFGHJKLMNPQRSTUVWXYZ123456789";
        
        public const string ShortUrl_FieldId = "{410EAE33-FD47-415C-AFDC-61DEA4BEAF8F}";
        public const string ShortUrlTarget_FieldId = "{7ACDD68F-CBEA-41D2-818D-BC777CD5D799}";


        /// <summary>
        /// The path for storing import / export reports
        /// </summary>
        public static readonly string ReportPath = "/sitecore/media library/Files/Url Rewrite/Reports/";

        /// <summary>
        /// The path for storing exported csv files
        /// </summary>
        public static readonly string ExportPath = "/sitecore/media library/Files/Url Rewrite/Redirect Exports/";

        /// <summary>
        /// Enum for exportable redirect types
        /// </summary>
        public enum RedirectType
        {
          SHORTURL,
          SIMPLEREDIRECT
        }

        /// <summary>
        /// Enum for states handled in imports
        /// </summary>
        public enum ImportStatus
        {
          ENABLED,
          DISABLED,
          DELETE
        }
    }
}

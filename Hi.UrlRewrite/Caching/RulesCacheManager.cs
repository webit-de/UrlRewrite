using Sitecore.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Data.Managers;
using Sitecore.Diagnostics;
using Sitecore.Globalization;

namespace Hi.UrlRewrite.Caching
{
    public static class RulesCacheManager
    {
        private static Dictionary<string, RulesCache> Caches = new Dictionary<string, RulesCache>();

        public static RulesCache GetCache(Database db, Language language)
        {
            Assert.IsNotNull(db, "Database (db) cannot be null.");

            var cacheKey = GetCacheKey(db, language);

            if (Caches.ContainsKey(cacheKey))
            {
                return Caches[cacheKey];
            }
            else
            {
                var cache = new RulesCache(db, language);
                Caches.Add(cacheKey, cache);

                return cache;
            }
        }

        private static string GetCacheKey(Database db, Language language)
        {
          return db.Name + "_" + GetRegionalIsoCode(language, db);
        }

        private static string GetRegionalIsoCode(Language language, Database database = null)
        {
          if (language == null) return string.Empty;

          var langItem = LanguageManager.GetLanguageItem(language, database ?? Sitecore.Context.Database);
          if (langItem == null) return string.Empty;

          var iso = langItem.Fields["Regional Iso Code"].ToString();

          return string.IsNullOrEmpty(iso)
            ? langItem.DisplayName
            : iso;
        }

  }
}
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Sitecore.Shell.Framework.Commands.System;
using Sitecore.StringExtensions;

namespace Hi.UrlRewrite.Extensions
{
  /// <summary>
  /// String extension class
  /// </summary>
  public static class StringExtension
  {
    /// <summary>
    /// Indicates whether the specified string has a value, i.e. Not null or empty
    /// </summary>
    /// <param name="target">The string to extended.</param>
    /// <returns>Returns <code>true</code> if the <see cref="target"/> string has as value; otherwise <code>false</code>.</returns>
    public static bool HasValue(this string target)
    {
      return !target.IsNullOrEmpty();
    }
  }
}
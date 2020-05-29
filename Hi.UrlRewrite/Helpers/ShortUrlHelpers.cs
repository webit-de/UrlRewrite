using System;
using System.Linq;
using Hi.UrlRewrite.Templates.Folders;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Hi.UrlRewrite.Helpers
{
  public static class ShortUrlHelpers
  {
    /// <summary>
    /// Check if the provided token does already exist
    /// </summary>
    /// <param name="token">The token to check</param>
    /// <returns>True if the token exists, False otherwise</returns>
    public static bool DoesTokenExist(string token, RedirectFolderItem redirectFolderItem)
    {
      var query = "/sitecore/content//*[@@templateid='{EA7922DB-83AD-49BA-AD53-F30F058CEE74}']";
      var shortUrlItems = redirectFolderItem.Database.SelectItems(query);
      return shortUrlItems.Any(x => x.Fields[ID.Parse(Guid.Parse(Constants.ShortUrlTarget_FieldId))].Value.Equals(redirectFolderItem.ShortUrlPrefix + "/" + token));
    }

    /// <summary>
    /// Generate a random unique short url
    /// </summary>
    /// <param name="item">The Short Url Item</param>
    /// <returns>Random unique short url</returns>
    public static string GenerateShortUrl(Item item)
    {
      string resultToken;

      var redirectFolderItem = GetRedirectFolderItem(item);
      // generate random tokens until a non existing token is found
      var numTries = 10;
      do
      {
        // do not generate a short Url if the try amount has been exceeded, to avoid an infinite loop
        if (numTries < 1)
        {
          return string.Empty;
        }

        resultToken = GenerateToken(redirectFolderItem.ShortUrlTokenLength);
        numTries--;
      } while (DoesTokenExist(resultToken, redirectFolderItem));


      return resultToken;
    }

    /// <summary>
    /// Generate a random token
    /// </summary>
    /// <param name="length">The length of the token</param>
    /// <returns>Random token with the provided length</returns>
    private static string GenerateToken(int length)
    {
      // if no length is provided use a length of 4
      length = length == 0 ? 4 : length;

      var resultToken = string.Empty;

      // add random characters to the token until it has the correct length
      var random = new Random();
      for (int i = 0; i < length; i++)
      {
        var randomIndex = random.Next(Constants.ShortUrlTokenCharacters.Length);
        resultToken += Constants.ShortUrlTokenCharacters[randomIndex];
      }

      return resultToken;
    }

    /// <summary>
    /// Get the parent RedirectFolderItem
    /// </summary>
    /// <param name="item"></param>
    /// <returns>Th</returns>
    private static RedirectFolderItem GetRedirectFolderItem(Item item)
    {
      var redirectFolderItem = item.Axes.GetAncestors().FirstOrDefault(a => a.TemplateID.Equals(new ID(RedirectFolderItem.TemplateId)));

      return redirectFolderItem == null
        ? null
        : new RedirectFolderItem(redirectFolderItem);
    }
  }
}
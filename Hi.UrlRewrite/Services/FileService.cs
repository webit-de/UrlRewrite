
using System;
using System.IO;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Hi.UrlRewrite.Services
{
  public static class FileService
  {
    /// <summary>
    /// Get the file name based on the root item
    /// </summary>
    /// <param name="rootItem">The root item.</param>
    /// <param name="nameAddition">The name addition</param>
    /// <returns>The name of the file</returns>
    public static string GetFileName(Item rootItem, string nameAddition = "")
    {
      var siteInfo = Sitecore.Links.LinkManager.ResolveTargetSite(rootItem);
      var result = DateTime.Now.ToString("yyyy-MM-dd_HH-mm") + "_" + siteInfo.Name + "_" + rootItem.Name;
      if (nameAddition == string.Empty)
      {
        return result;
      }

      return result + "_" + nameAddition;
    }
    
    /// <summary>
    /// Write a media file with the provided file stream
    /// </summary>
    /// <param name="fileStream">The file stream</param>
    /// <param name="db">The database</param>
    /// <param name="filePath">The file Path without the File</param>
    /// <param name="fileName">The file Name</param>
    /// <param name="fileExtension">The file extension </param>
    /// <returns></returns>
    public static ID WriteFile(Stream fileStream, Database db, string filePath, string fileName, string fileExtension)
    {
      // Create the options
      var options = new Sitecore.Resources.Media.MediaCreatorOptions
      {
        FileBased = true,
        IncludeExtensionInItemName = false,
        Versioned = false,
        Destination = filePath + fileName,
        Database = db
      };

      // Create the file
      var creator = new Sitecore.Resources.Media.MediaCreator();
      MediaItem mediaItem = creator.CreateFromStream(fileStream, fileExtension, options);

      return mediaItem.ID;
    }
  }
}
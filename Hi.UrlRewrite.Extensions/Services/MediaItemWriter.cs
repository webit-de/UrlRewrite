
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Sitecore.Data;
using Sitecore.Data.Items;

namespace Hi.UrlRewrite.Extensions.Services
{
  public static class MediaItemWriter
  {

    public static ID WriteFile(List<string> messages, Database db, string filePath, string fileExtension)
    {
      // don't write empty files
      if (!messages.Any())
      {
        return ID.Null;
      }

      var fileStream = GenerateStreamFromStringCollection(messages);

      return WriteFile(fileStream, db, filePath, fileExtension);
    }

    public static ID WriteFile(Stream fileStream, Database db, string filePath, string fileExtension)
    {
      // Create the options
      var options = new Sitecore.Resources.Media.MediaCreatorOptions
      {
        FileBased = true,
        IncludeExtensionInItemName = false,
        Versioned = false,
        Destination = filePath + "_" + DateTime.Now.ToString("yyyy-MM-dd_HH-mm"),
        Database = db
      };

      // Create the file
      var creator = new Sitecore.Resources.Media.MediaCreator();
      MediaItem mediaItem = creator.CreateFromStream(fileStream, fileExtension, options);

      return mediaItem.ID;
    }

    private static Stream GenerateStreamFromStringCollection(IEnumerable<string> logEntries)
    {
      var builder = new StringBuilder();

      foreach (var entry in logEntries)
      {
        builder.AppendLine(entry);
      }

      return new MemoryStream(Encoding.UTF8.GetBytes(builder.ToString()));
    }
  }
}
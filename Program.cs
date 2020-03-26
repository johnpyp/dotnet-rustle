using System.Threading.Tasks;
using System;
using System.IO;
using System.Linq;
using System.Globalization;
using System.Collections.Generic;

namespace dotnet_rustle
{
  class Program
  {
    static async Task Main(string[] args)
    {
      var date = DateTime.Parse("2020-03-24", CultureInfo.InvariantCulture);
      var elasticStuff = new ElasticStuff();
      var myEnum = await MessageEnumerable(date, 2, elasticStuff);
      elasticStuff.IndexMessages(myEnum);

    }
    static async Task<IEnumerable<LogMessage>> MessageEnumerable(DateTime date, int daysBack, ElasticStuff elasticStuff)
    {
      await elasticStuff.InitElastic();
      var dates = Util.GenerateDates(date, daysBack);
      var missingCacheLines = new HashSet<string>(await FileStuff.ReadMissingCache());
      var indexCacheLines = new HashSet<string>(await FileStuff.ReadIndexCache());
      var channels = await FileStuff.DownloadChannels();
      IEnumerable<LogMessage> LocalMeme()
      {
        foreach (var day in dates)
        {
          var shortDateString = date.ToString("yyyy-MM-dd");
          foreach (var channel in channels)
          {
            var testStr = $"{channel}::{shortDateString}";
            if (missingCacheLines.Contains(testStr) || indexCacheLines.Contains(testStr))
            {
              continue;
            }
            var lines = FileStuff.ReadLog(channel, day);
            if (lines == null)
            {
              continue;
            }
            var messages = lines.Where(x => x != "").Select(msg => ElasticStuff.ConstructLogMessage(channel, msg));
            foreach (var message in messages)
            {
              yield return message;
            }
            File.AppendAllTextAsync("data/index-cache.txt", $"{channel}::{shortDateString}\n");
            Console.WriteLine($"{shortDateString}::{channel}");
          }
        }
      }
      return LocalMeme();
    }
  }
}
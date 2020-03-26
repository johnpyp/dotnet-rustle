using System;
using System.Linq;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using Nest;
using System.Net.Http;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace dotnet_rustle
{
  public class ElasticStuff
  {
    public ElasticClient Client { get; }
    public ElasticStuff()
    {
      var settings = new ConnectionSettings(new Uri("http://localhost:9200")).DefaultIndex("rustlesearch");

      Client = new ElasticClient(settings);

    }
    public void IndexMessages(IEnumerable<LogMessage> messages)
    {
      Client.BulkAll<LogMessage>(messages, b => b
       .Index("rustlesearch")
       .Pipeline("rustlesearch-pipeline")
       .BackOffTime("30s")
       .BackOffRetries(2)
       .MaxDegreeOfParallelism(Environment.ProcessorCount)
       .Size(2000))
       .Wait(TimeSpan.FromDays(1), next =>
       {
         Console.WriteLine("boop");
       });

    }
  public static LogMessage ConstructLogMessage(string channel, string line)
    {
      // Console.WriteLine(line);
      var bracketIdx = line.IndexOf("]");
      var ts = ParseDateToISO(line.Slice(1, bracketIdx));
      var afterBracket = line.Slice(bracketIdx + 2);
      var colonIdx = afterBracket.IndexOf(":");
      var username = afterBracket.Slice(0, colonIdx);
      var text = afterBracket.Slice(colonIdx + 2);

      return new LogMessage
      {
        Channel = FirstCharToUpper(channel),
        Ts = ts,
        Username = username.ToLower(),
        Text = text,
      };

    }
    public static string ParseDateToISO(string date)
    {
      var year = date.Slice(0, 4);
      var month = date.Slice(5, 7);
      var day = date.Slice(8, 10);
      var hour = date.Slice(11, 13);
      var minute = date.Slice(14, 16);
      var second = date.Slice(17, 19);

      return $"{year}-{month}-{day}T{hour}:{minute}:{second}.000Z";

    }
    public static string FirstCharToUpper(string input)
    {

      return input switch
      {
        null => throw new ArgumentNullException(nameof(input)),
        "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
        _ => input.First().ToString().ToUpper() + input.Substring(1)
      };
    }

    public async Task InitElastic()
    {
      var templateJson = JObject.FromObject(
        new
        {
          index_patterns = "rustlesearch*",
          mappings = new
          {
            properties = new
            {
              channel = new { type = "keyword" },
              text = new { type = "text" },
              ts = new { type = "date" },
              username = new { type = "keyword" }
            }
          },
          settings = new
          {
            number_of_replicas = 0,
            number_of_shards = 1,
            refresh_interval = "1s",
            sort = new
            {
              field = new[] { "ts", "ts" },
              order = new[] { "desc", "asc" }
            },
            codec = "best_compression"
          }
        }
      );
      HttpClient client = new HttpClient();
      await client.PutAsync("http://localhost:9200/_template/rustlesearch-template",
        new StringContent(templateJson.ToString(), Encoding.UTF8, "application/json"));
      var pipelineJson = JObject.Parse(@"{
        description: 'Monthly date-time index mapping',
        processors: [
          {
            date_index_name: {
              date_rounding: 'M',
              field: 'ts',
              index_name_prefix: 'rustlesearch-'
            }
          },
          {
            set: {
              field: '_id',
              value: '{{channel}}-{{username}}-{{ts}}'
            }
          }
        ]
       }
        ");
      await client.PutAsync("http://localhost:9200/_ingest/pipeline/rustlesearch-pipeline",
        new StringContent(pipelineJson.ToString(), Encoding.UTF8, "application/json"));
    }
  }
  public class LogMessage
  {
    public string Channel { get; set; }
    public string Username { get; set; }
    public string Ts { get; set; }
    public string Text { get; set; }
  }
  public static class MyStringExtensions
  {

    public static string Slice(this string s, int startIdx, int endIdx)
    {
      return s.Substring(startIdx, endIdx - startIdx);
    }
    public static string Slice(this string s, int startIdx)

    {
      return s.Substring(startIdx);
    }
  }

}
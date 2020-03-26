using System.Threading.Tasks;
using System.IO;
using System;
using System.Globalization;
using Newtonsoft.Json;

namespace dotnet_rustle
{
  public class FileStuff
  {
    public static string[] ReadMonths(string channel)
    {
      var path = $"data/months/{channel}.json";
      if (File.Exists(path))
      {
        return JsonConvert.DeserializeObject<string[]>(File.ReadAllText(path));

      }
      System.IO.Directory.CreateDirectory("data/months");
      var webClient = new System.Net.WebClient();
      var url = $"https://overrustlelogs.net/api/v1/{channel}/months.json";
      Console.WriteLine(url);
      var text = webClient.DownloadString(url);
      File.WriteAllText(path, text);
      return JsonConvert.DeserializeObject<string[]>(text);
    }

    public static DateTime ReadMinDate(string channel)
    {
      var arr = ReadMonths(channel);
      var last = arr[arr.Length - 1];
      var date = DateTime.ParseExact(last, "MMMM yyyy", CultureInfo.InvariantCulture);
      return date;
    }
    public static string[] ReadLog(string channel, DateTime date)
    {
      var shortDateString = date.ToString("yyyy-MM-dd");
      var path = $"data/orl/{channel}::{shortDateString}.txt";
      if (File.Exists(path))
      {
        return File.ReadAllLines(path);
      }
      var minDate = ReadMinDate(channel);
      if (minDate.CompareTo(date) > 0)
      {
        return new string[0];
      }
      System.IO.Directory.CreateDirectory("data/orl");
      var longDateString = date.ToString("MMMM yyyy/yyyy-MM-dd");
      var webClient = new System.Net.WebClient();
      var url = $"https://overrustlelogs.net/{channel} chatlog/{longDateString}.txt";
      Console.WriteLine(url);
      try
      {

        var text = webClient.DownloadString(url);
        File.WriteAllText(path, text);
        return text.Split("\n");
      }
      catch (System.Net.WebException err)
      {
        Console.WriteLine($"{channel}::{shortDateString} not found");
        File.AppendAllText("data/missing-cache.txt", $"{channel}::{shortDateString}\n");
        return null;
      }
    }

    public static async Task<string[]> DownloadChannels()
    {
      var webClient = new System.Net.WebClient();
      var text = await webClient.DownloadStringTaskAsync(new System.Uri("https://overrustlelogs.net/api/v1/channels.json"));
      return JsonConvert.DeserializeObject<string[]>(text);
    }

    public static async Task<string[]> ReadMissingCache()
    {
      if (!File.Exists("data/missing-cache.txt"))
      {
        return new string[] { };
      }
      return await File.ReadAllLinesAsync("data/missing-cache.txt");

    }

    public static async Task<string[]> ReadIndexCache()
    {
      if (!File.Exists("data/index-cache.txt"))
      {
        return new string[] { };
      }
      return await File.ReadAllLinesAsync("data/index-cache.txt");

    }

  }

}
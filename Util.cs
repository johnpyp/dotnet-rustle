using System;
using System.Collections.Generic;

namespace dotnet_rustle
{
  public class Util
  {
    public static List<DateTime> GenerateDates(DateTime startDate, int daysBack)
    {
      var list = new List<DateTime>();
      list.Add(startDate);
      for (int i = 0; i < daysBack; i++)
      {
        startDate = startDate.Subtract(TimeSpan.FromDays(1));
        list.Add(startDate);
      }
      return list;
    }
  }
}
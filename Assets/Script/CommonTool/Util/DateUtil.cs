using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DateUtil
{
    public static long Current()
    {
        return GetTimestamp(DateTime.Now);
    }

    public static long GetTimestamp(DateTime datetime)
    {
        DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
        long timeStamp = (long)(datetime - startTime).TotalMilliseconds; // 相差毫秒数
        return timeStamp / 1000;
    }

    /// <summary>
    /// 日期格式化
    /// "" : 2022/7/22 11:18:14
    /// "d" : 2022/7/22
    /// "g" : 2022/7/22 11:23
    /// "G" : 2022/7/22 11:23:05
    /// "T" : 11:23:05
    /// "u" : 2022-07-22 11:23:05Z
    /// "s" : 2022-07-22T11:23:05
    /// </summary>
    /// <param name="time"></param>
    /// <param name="format"></param>
    /// <returns></returns>
    public static string DateTimeFormat(DateTime dt, string format)
    {
        return dt.ToString(format);
    }

    public static DateTime SecondsToDateTime(long seconds)
    {
        DateTime startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)); // 当地时区
        return startTime.AddSeconds(seconds);
    }

    /// <summary>
    /// 秒转化为 时:分:秒 格式
    /// </summary>
    /// <param name="totalTime"></param>
    /// <param name="connector"></param>
    /// <returns></returns>
    public static string SecondsFormat(long totalTime, string connector = ":")
    {
        int seconds = Mathf.Max((int)(totalTime % 60), 0);
        int minutes = Mathf.Max((int)(totalTime / 60) % 60, 0);
        int hours = Mathf.Max((int)(totalTime / 3600), 0);

        string hoursStr = hours >= 10 ? hours.ToString() : "0" + hours;
        string minutesStr = minutes >= 10 ? minutes.ToString() : "0" + minutes;
        string secondsStr = seconds >= 10 ? seconds.ToString() : "0" + seconds;

        return hours == 0 ? minutesStr + connector + secondsStr : hoursStr + connector + minutesStr + connector + secondsStr;
    }

    /// <summary>
    /// 秒转化为 2d15h 或 12h36m 格式
    /// </summary>
    /// <param name="totalTime"></param>
    /// <returns></returns>
    public static string SecondsFormat2(long totalTime)
    {
        int seconds = Mathf.Max((int)(totalTime % 60), 0);
        int minutes = Mathf.Max((int)(totalTime / 60) % 60, 0);
        int hours = Mathf.Max((int)(totalTime / 3600) % 24, 0);
        int days = Mathf.Max((int)(totalTime / 86400));

        string daysStr = days + "d";
        string hoursStr = hours + "h";
        string minutesStr = minutes + "m";
        string secondsStr = seconds + "s";

        List<string> res = new();
        if (days > 0) res.Add(daysStr);
        if (hours > 0) res.Add(hoursStr);
        res.Add(minutesStr);
        res.Add(secondsStr);

        return res[0] + res[1];
    }
}

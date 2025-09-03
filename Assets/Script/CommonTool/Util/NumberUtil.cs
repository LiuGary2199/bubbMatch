using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using UnityEngine;

public class NumberUtil
{
    public static string DoubleToStr(double a)
    {
        return Math.Round(a, CommonConfig.RoundDigits).ToString();
    }
    public static string DoubleToStr(double a, int digits)
    {
        return Math.Round(a, digits).ToString();
    }
      public static double ConvertWithDecimalFix(object obj)
  {
      if (obj is string str)
      {
          // 替换逗号为点
          str = str.Replace(',', '.');
          return double.Parse(str, CultureInfo.InvariantCulture);
      }

      return Convert.ToDouble(obj);
  }
       public static double SafeConvertToDouble(object obj, double defaultValue = 0)
   {
       if (obj == null) return defaultValue;

       // 直接是double类型
       if (obj is double d) return d;

       // 处理字符串（关键！）
       if (obj is string str)
       {
           // 方法1：使用不变文化（确保用.作为小数点）
           if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
           {
               return d;
           }

           // 方法2：尝试替换逗号为点（兼容欧洲格式）
           str = str.Replace(',', '.');
           if (double.TryParse(str, NumberStyles.Any, CultureInfo.InvariantCulture, out d))
           {
               return d;
           }

           return defaultValue;
       }

       // 处理其他数值类型（int、float等）
       try
       {
           return Convert.ToDouble(obj);
       }
       catch
       {
           return defaultValue;
       }
   }

    public static double Round(double a)
    {
        return Math.Round(a, CommonConfig.RoundDigits);
    }

}

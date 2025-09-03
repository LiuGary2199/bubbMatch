using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class StringUtil
{
    /// <summary>
    /// 字符串转为枚举类型
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="strType"></param>
    /// <returns></returns>
    public static T ToEnum<T>(string strType)
    {
        T t = (T)Enum.Parse(typeof(T), strType);
        return t;
    }
}

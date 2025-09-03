using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CustomPrint
{
    public static void Print(params object[] args)
    {
        if (args == null)
        {
            Debug.Log("Array is Null");
        }
        if (args.Length == 0)
        {
            Debug.Log("Array is Empty");
        }

        string[] strArgs = new string[args.Length];
        for (int i = 0; i < args.Length; i++)
        {
            strArgs[i] = args[i].ToString();
        }
        Debug.Log(String.Join("  ", strArgs));
    }

    /// <summary>
    /// 更高辨识度的log
    /// </summary>
    /// <param name="log">log内容</param>
    /// <param name="color">颜色 0黄 1青 2绿 3蓝</param>
    public static void Print(object log, int color = 0)
    {
        if (color == 0)
            MonoBehaviour.print("<color=yellow><b>+++++   " + log + "</b></color>");
        else if (color == 1)
            MonoBehaviour.print("<color=cyan><b>+++++   " + log + "</b></color>");
        else if (color == 2)
            MonoBehaviour.print("<color=green><b>+++++   " + log + "</b></color>");
        else if (color == 3)
            MonoBehaviour.print("<color=blue><b>+++++   " + log + "</b></color>");
    }
}

using System;

public class ProgressCalculator
{
    // 预计算分母值以提高性能
    private static readonly double Denominator;
    
    static ProgressCalculator()
    {
        double exponent = Math.Pow(7200, 0.83);
        double expValue = Math.Exp(-0.004 * exponent);
        Denominator = 1 - expValue;
    }
    
    /// <summary>
    /// 计算百分比进度
    /// </summary>
    /// <param name="x">输入值 (0-7200)</param>
    /// <returns>百分比进度 (0-100)</returns>
    public static double CalculateProgress(int x)
    {
        if (x < 0) return 0;
        if (x > 7200) return 100;
        
        if (x == 0) return 0;
        
        double exponent = Math.Pow(x, 0.83);
        double numerator = 1 - Math.Exp(-0.004 * exponent);
        
        return numerator / Denominator;
    }
    
    /// <summary>
    /// 计算百分比进度（带小数位数控制）
    /// </summary>
    /// <param name="x">输入值 (0-7200)</param>
    /// <param name="decimalPlaces">小数位数</param>
    /// <returns>格式化后的百分比字符串</returns>
    public static string CalculateProgressFormatted(int x, int decimalPlaces = 2)
    {
        double progress = CalculateProgress(x);
        return progress.ToString($"F{decimalPlaces}") + "%";
    }
}

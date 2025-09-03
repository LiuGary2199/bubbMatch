using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 日历管理器 - 用于打开和管理日历界面
/// </summary>
public class DailyCalendarManager : MonoBehaviour
{
    [Header("测试按钮")]
    public Button OpenCalendarBtn; // 打开日历的按钮（用于测试）
    
    void Start()
    {
        // 绑定测试按钮
        if (OpenCalendarBtn != null)
        {
            OpenCalendarBtn.onClick.AddListener(OpenDailyCalendar);
        }
    }
    
    /// <summary>
    /// 打开每日日历界面
    /// </summary>
    public void OpenDailyCalendar()
    {
        Debug.Log("打开每日挑战日历");
        
        try
        {
            // 使用UIManager打开日历界面
            UIManager.GetInstance().ShowUIForms(nameof(DailyPopPanel));
            
            Debug.Log("日历界面打开成功");
        }
        catch (Exception e)
        {
            Debug.LogError($"打开日历界面失败: {e.Message}");
        }
    }
    
    /// <summary>
    /// 检查今天是否已完成每日挑战
    /// </summary>
    /// <returns>true表示已完成，false表示未完成</returns>
    public bool IsTodayCompleted()
    {
        string todayKey = $"DailyChallenge_{DateTime.Now:yyyy_MM_dd}";
        return PlayerPrefs.GetInt(todayKey, 0) == 1;
    }
    
    /// <summary>
    /// 标记今天的每日挑战为已完成
    /// </summary>
    public void MarkTodayCompleted()
    {
        string todayKey = $"DailyChallenge_{DateTime.Now:yyyy_MM_dd}";
        PlayerPrefs.SetInt(todayKey, 1);
        PlayerPrefs.Save();
        
        Debug.Log($"标记今天({DateTime.Now:yyyy-MM-dd})的每日挑战为已完成");
    }
    
    /// <summary>
    /// 检查指定日期是否已完成每日挑战
    /// </summary>
    /// <param name="date">要检查的日期</param>
    /// <returns>true表示已完成，false表示未完成</returns>
    public bool IsDateCompleted(DateTime date)
    {
        string dateKey = $"DailyChallenge_{date:yyyy_MM_dd}";
        return PlayerPrefs.GetInt(dateKey, 0) == 1;
    }
    
    /// <summary>
    /// 获取本月已完成的天数
    /// </summary>
    /// <returns>已完成的天数</returns>
    public int GetMonthCompletedDays()
    {
        DateTime now = DateTime.Now;
        DateTime firstDay = new DateTime(now.Year, now.Month, 1);
        DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
        
        int completedDays = 0;
        
        for (DateTime date = firstDay; date <= lastDay; date = date.AddDays(1))
        {
            if (IsDateCompleted(date))
            {
                completedDays++;
            }
        }
        
        return completedDays;
    }
    
    /// <summary>
    /// 重置所有每日挑战记录（仅用于测试）
    /// </summary>
    [ContextMenu("重置所有每日挑战记录")]
    public void ResetAllDailyRecords()
    {
        // 警告：这会删除所有每日挑战记录
        DateTime now = DateTime.Now;
        DateTime firstDay = new DateTime(now.Year, now.Month, 1);
        DateTime lastDay = firstDay.AddMonths(1).AddDays(-1);
        
        for (DateTime date = firstDay; date <= lastDay; date = date.AddDays(1))
        {
            string dateKey = $"DailyChallenge_{date:yyyy_MM_dd}";
            PlayerPrefs.DeleteKey(dateKey);
        }
        
        PlayerPrefs.Save();
        Debug.Log("已重置本月所有每日挑战记录");
    }
    
    /// <summary>
    /// 模拟完成几天的挑战（仅用于测试）
    /// </summary>
    [ContextMenu("模拟完成前几天的挑战")]
    public void SimulateCompletedDays()
    {
        DateTime now = DateTime.Now;
        
        // 模拟完成前5天的挑战
        for (int i = 1; i <= 5; i++)
        {
            DateTime date = now.AddDays(-i);
            string dateKey = $"DailyChallenge_{date:yyyy_MM_dd}";
            PlayerPrefs.SetInt(dateKey, 1);
        }
        
        PlayerPrefs.Save();
        Debug.Log("已模拟完成前5天的每日挑战");
    }
}


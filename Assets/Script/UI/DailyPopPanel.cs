using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyPopPanel : BaseUIForms
{
    [Header("日历配置")]
    public List<DailyPopItem> DailyPopItems; // 日历格子（至少42个，6行7列）
    public Text YearMonthText; // 显示年月的文本（如：2025/08）
    
    [Header("按钮")]
    public Button DailyChallengeBtn; // 每日挑战按钮
    public Button CloseBtn; // 关闭按钮
    
    private DateTime currentDate; // 当前显示的日期
    private DateTime today; // 今天的日期
    
    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
        
        // 初始化日期
        today = DateTime.Now;
        currentDate = today;
        
        // 显示当前月份的日历
        ShowCalendar();
        
        // 绑定按钮事件
        BindButtonEvents();
    }
    
    public override void Hidding()
    {
        base.Hidding();
        
        // 解绑按钮事件
        UnbindButtonEvents();
    }
    
    /// <summary>
    /// 显示日历
    /// </summary>
    private void ShowCalendar()
    {
        // 更新年月显示
        if (YearMonthText != null)
        {
            YearMonthText.text = currentDate.ToString("yyyy/MM");
        }
        
        // 获取当月信息
        int year = currentDate.Year;
        int month = currentDate.Month;
        
        // 当月第一天
        DateTime firstDayOfMonth = new DateTime(year, month, 1);
        // 当月最后一天
        DateTime lastDayOfMonth = firstDayOfMonth.AddMonths(1).AddDays(-1);
        // 当月天数
        int daysInMonth = lastDayOfMonth.Day;
        // 第一天是星期几，转换为周一开始的索引（0=周一，1=周二...6=周日）
        int firstDayOfWeek = (int)firstDayOfMonth.DayOfWeek;
        // .NET的DayOfWeek: 0=周日，1=周一...6=周六
        // 转换为周一开始: 周日=6，周一=0，周二=1...周六=5
        firstDayOfWeek = (firstDayOfWeek == 0) ? 6 : firstDayOfWeek - 1;
        
        // 清空所有日历格子
        for (int i = 0; i < DailyPopItems.Count; i++)
        {
            if (DailyPopItems[i] != null)
            {
                DailyPopItems[i].SetEmpty();
            }
        }
        
        // 填充日历
        for (int day = 1; day <= daysInMonth; day++)
        {
            int itemIndex = firstDayOfWeek + day - 1;
            
            // 确保不超出数组范围
            if (itemIndex < DailyPopItems.Count && DailyPopItems[itemIndex] != null)
            {
                // 检查是否为今天
                DateTime dateToCheck = new DateTime(year, month, day);
                bool isToday = dateToCheck.Date == today.Date;
                
                // 设置日期
                DailyPopItems[itemIndex].SetDate(day, isToday);
            }
        }
        
        Debug.Log($"显示日历：{year}年{month}月，共{daysInMonth}天，第一天是星期{firstDayOfWeek}");
    }
    
    /// <summary>
    /// 绑定按钮事件
    /// </summary>
    private void BindButtonEvents()
    {
        if (DailyChallengeBtn != null)
        {
            DailyChallengeBtn.onClick.RemoveAllListeners();
            DailyChallengeBtn.onClick.AddListener(OnDailyChallengeClick);
        }
        
        if (CloseBtn != null)
        {
            CloseBtn.onClick.RemoveAllListeners();
            CloseBtn.onClick.AddListener(OnCloseClick);
        }
    }
    
    /// <summary>
    /// 解绑按钮事件
    /// </summary>
    private void UnbindButtonEvents()
    {
        if (DailyChallengeBtn != null)
        {
            DailyChallengeBtn.onClick.RemoveAllListeners();
        }
        
        if (CloseBtn != null)
        {
            CloseBtn.onClick.RemoveAllListeners();
        }
    }
    
    /// <summary>
    /// 每日挑战按钮点击
    /// </summary>
    private void OnDailyChallengeClick()
    {
        Debug.Log("点击每日挑战按钮");
        
        // TODO: 在这里添加每日挑战的逻辑
        // 例如：开始每日挑战关卡
        
        // 播放点击音效
        // AudioManager.PlayClickSound();
        
        // 关闭面板
        OnCloseClick();
    }
    
    /// <summary>
    /// 关闭按钮点击
    /// </summary>
    private void OnCloseClick()
    {
        Debug.Log("关闭日历面板");
        
        // 播放点击音效
        // AudioManager.PlayClickSound();
        
        // 关闭UI面板
        UIManager.GetInstance().CloseOrReturnUIForms(nameof(DailyPopPanel));
    }
    
    /// <summary>
    /// 切换到上个月（如果需要的话）
    /// </summary>
    public void ShowPreviousMonth()
    {
        currentDate = currentDate.AddMonths(-1);
        ShowCalendar();
    }
    
    /// <summary>
    /// 切换到下个月（如果需要的话）
    /// </summary>
    public void ShowNextMonth()
    {
        currentDate = currentDate.AddMonths(1);
        ShowCalendar();
    }
    
    /// <summary>
    /// 回到当前月
    /// </summary>
    public void ShowCurrentMonth()
    {
        currentDate = today;
        ShowCalendar();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class DailyPopItem : MonoBehaviour
{
    [Header("UI组件")]
    public Text DayText; // 显示日期数字的文本
    public GameObject ToDayObj; // 今天的钻石标识
    
    [Header("可选组件")]
    public Button DayButton; // 日期按钮（可选，用于点击事件）
    public GameObject CompletedObj; // 已完成标识（可选）
    
    private int dayNumber; // 当前日期数字
    private bool isToday; // 是否为今天
    private bool isEmpty; // 是否为空（不属于当月）
    
    #region 公共方法
    
    /// <summary>
    /// 设置日期
    /// </summary>
    /// <param name="day">日期数字</param>
    /// <param name="isTodayFlag">是否为今天</param>
    public void SetDate(int day, bool isTodayFlag)
    {
        dayNumber = day;
        isToday = isTodayFlag;
        isEmpty = false;
        
        // 设置日期文本和今天标识（钻石）
        if (isToday)
        {
            // 今天：只显示钻石，隐藏文字
            if (DayText != null)
            {
                DayText.gameObject.SetActive(false);
            }
            if (ToDayObj != null)
            {
                ToDayObj.SetActive(true);
            }
        }
        else
        {
            // 其他日期：显示文字，隐藏钻石
            if (DayText != null)
            {
                DayText.text = day.ToString();
                DayText.gameObject.SetActive(true);
            }
            if (ToDayObj != null)
            {
                ToDayObj.SetActive(false);
            }
        }
        
        // 设置日期按钮可交互
        if (DayButton != null)
        {
            DayButton.interactable = true;
            DayButton.onClick.RemoveAllListeners();
            DayButton.onClick.AddListener(() => OnDayClick(day, isToday));
        }
        
        // 今天显示钻石，其他样式在UI中已设置好
        
        // 显示此格子
        gameObject.SetActive(true);
    }
    
    /// <summary>
    /// 设置为空（不属于当月）
    /// </summary>
    public void SetEmpty()
    {
        dayNumber = 0;
        isToday = false;
        isEmpty = true;
        
        // 隐藏日期文本
        if (DayText != null)
        {
            DayText.gameObject.SetActive(false);
        }
        
        // 隐藏今天标识
        if (ToDayObj != null)
        {
            ToDayObj.SetActive(false);
        }
        
        // 隐藏已完成标识
        if (CompletedObj != null)
        {
            CompletedObj.SetActive(false);
        }
        
        // 禁用按钮
        if (DayButton != null)
        {
            DayButton.interactable = false;
            DayButton.onClick.RemoveAllListeners();
        }
        
        // 可以选择隐藏整个格子，或者保持显示但为空
        // gameObject.SetActive(false);
    }
    
    /// <summary>
    /// 设置已完成状态
    /// </summary>
    /// <param name="completed">是否已完成</param>
    public void SetCompleted(bool completed)
    {
        if (CompletedObj != null)
        {
            CompletedObj.SetActive(completed && !isEmpty);
        }
    }
    
    /// <summary>
    /// 日期点击事件
    /// </summary>
    /// <param name="day">点击的日期</param>
    /// <param name="isTodayFlag">是否为今天</param>
    private void OnDayClick(int day, bool isTodayFlag)
    {
        if (isEmpty) return;
        
        Debug.Log($"点击了日期：{day}日，今天：{isTodayFlag}");
        
        // 播放点击音效
        // AudioManager.PlayClickSound();
        
        if (isTodayFlag)
        {
            Debug.Log("点击了今天，可以开始每日挑战！");
            // TODO: 触发每日挑战逻辑
            // 例如：GameManager.StartDailyChallenge();
        }
        else
        {
            Debug.Log($"点击了{day}日，可能显示历史记录或其他信息");
            // TODO: 显示该日期的历史记录或其他信息
        }
    }
    
    #endregion
    
    #region 属性访问器
    
    /// <summary>
    /// 获取日期数字
    /// </summary>
    public int DayNumber => dayNumber;
    
    /// <summary>
    /// 是否为今天
    /// </summary>
    public bool IsToday => isToday;
    
    /// <summary>
    /// 是否为空
    /// </summary>
    public bool IsEmpty => isEmpty;
    
    #endregion
}

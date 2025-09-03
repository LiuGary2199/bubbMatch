using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 签到红点提示脚本
/// 挂载在签到入口按钮上，自动检测是否可以签到并显示红点
/// </summary>
public class SignInRedDot : MonoBehaviour
{
    [Header("红点设置")]
    [SerializeField] private GameObject redDotObject; // 红点GameObject
    [SerializeField] private bool autoFindRedDot = true; // 是否自动查找红点对象
    [SerializeField] private string redDotName = "RedDot"; // 红点对象名称（用于自动查找）
    
    [Header("检测设置")]
    [SerializeField] private float checkInterval = 1f; // 检测间隔（秒）
    [SerializeField] private bool enableAutoCheck = true; // 是否启用自动检测
    
    [Header("调试")]
    [SerializeField] private bool showDebugLog = false; // 是否显示调试日志
    
    private const int MAX_SIGN_DAYS = 7;
    private float lastCheckTime = 0f;
    private bool lastRedDotState = false;
    
    void Start()
    {
        InitializeRedDot();
        CheckRedDotState();
    }
    
    void Update()
    {
        if (enableAutoCheck && Time.time - lastCheckTime >= checkInterval)
        {
            CheckRedDotState();
            lastCheckTime = Time.time;
        }
    }
    
    /// <summary>
    /// 初始化红点对象
    /// </summary>
    private void InitializeRedDot()
    {
        if (redDotObject == null && autoFindRedDot)
        {
            // 自动查找红点对象
            Transform redDotTransform = transform.Find(redDotName);
            if (redDotTransform != null)
            {
                redDotObject = redDotTransform.gameObject;
                if (showDebugLog)
                    Debug.Log($"SignInRedDot: 自动找到红点对象: {redDotObject.name}");
            }
            else
            {
                Debug.LogWarning($"SignInRedDot: 未找到名为 '{redDotName}' 的红点对象！");
            }
        }
        
        if (redDotObject == null)
        {
            Debug.LogError("SignInRedDot: 红点对象未设置！请在Inspector中设置redDotObject或确保子对象中有名为'RedDot'的对象。");
        }
    }
    
    /// <summary>
    /// 检查红点状态
    /// </summary>
    public void CheckRedDotState()
    {
        bool shouldShowRedDot = CanSignIn();
        
        if (shouldShowRedDot != lastRedDotState)
        {
            SetRedDotActive(shouldShowRedDot);
            lastRedDotState = shouldShowRedDot;
            
            if (showDebugLog)
            {
                Debug.Log($"SignInRedDot: 红点状态更新 - 显示: {shouldShowRedDot}");
            }
        }
    }
    
    /// <summary>
    /// 设置红点显示状态
    /// </summary>
    /// <param name="active">是否显示红点</param>
    public void SetRedDotActive(bool active)
    {
        if (redDotObject != null)
        {
            redDotObject.SetActive(active);
        }
    }
    
    /// <summary>
    /// 检查是否可以签到
    /// </summary>
    /// <returns>true表示可以签到，false表示不能签到</returns>
    private bool CanSignIn()
    {
        try
        {
            // 获取签到数据
            int[] checkDay = SaveDataManager.GetIntArray("CheckDay");
            int checkNums = SaveDataManager.GetInt("CheckNum");
            
            // 检查是否已达到最大签到天数
            if (checkNums >= MAX_SIGN_DAYS)
            {
                if (showDebugLog)
                    Debug.Log("SignInRedDot: 已达到最大签到天数，不显示红点");
                return false;
            }
            
            // 检查今天是否已经签到
            DateTime currentDate = DateTime.Now;
            bool canSignToday = false;
            
            if (checkDay == null || checkDay.Length == 0)
            {
                // 第一次签到
                canSignToday = true;
                if (showDebugLog)
                    Debug.Log("SignInRedDot: 第一次签到，显示红点");
            }
            else
            {
                // 检查上次签到日期
                DateTime lastSignDate = new DateTime(checkDay[0], checkDay[1], checkDay[2]);
                DateTime today = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day);
                TimeSpan timeSpan = today.Subtract(lastSignDate);
                
                canSignToday = timeSpan.Days > 0;
                
                if (showDebugLog)
                {
                    Debug.Log($"SignInRedDot: 上次签到: {lastSignDate}, 今天: {today}, 间隔: {timeSpan.Days}天, 可签到: {canSignToday}");
                }
            }
            
            return canSignToday;
        }
        catch (Exception e)
        {
            Debug.LogError($"SignInRedDot: 检查签到状态时出错: {e.Message}");
            return false;
        }
    }
    
    /// <summary>
    /// 手动刷新红点状态（外部调用）
    /// </summary>
    public void RefreshRedDot()
    {
        CheckRedDotState();
    }
    
    /// <summary>
    /// 强制显示红点
    /// </summary>
    public void ForceShowRedDot()
    {
        SetRedDotActive(true);
        lastRedDotState = true;
    }
    
    /// <summary>
    /// 强制隐藏红点
    /// </summary>
    public void ForceHideRedDot()
    {
        SetRedDotActive(false);
        lastRedDotState = false;
    }
    
    /// <summary>
    /// 设置自动检测间隔
    /// </summary>
    /// <param name="interval">检测间隔（秒）</param>
    public void SetCheckInterval(float interval)
    {
        checkInterval = Mathf.Max(0.1f, interval);
    }
    
    /// <summary>
    /// 启用/禁用自动检测
    /// </summary>
    /// <param name="enable">是否启用</param>
    public void SetAutoCheckEnabled(bool enable)
    {
        enableAutoCheck = enable;
        if (enable)
        {
            lastCheckTime = Time.time;
        }
    }
    
    /// <summary>
    /// 获取当前红点状态
    /// </summary>
    /// <returns>true表示红点显示，false表示红点隐藏</returns>
    public bool IsRedDotActive()
    {
        return redDotObject != null && redDotObject.activeInHierarchy;
    }
    
    /// <summary>
    /// 获取是否可以签到
    /// </summary>
    /// <returns>true表示可以签到</returns>
    public bool GetCanSignIn()
    {
        return CanSignIn();
    }
    
    void OnDestroy()
    {
        // 清理资源
        redDotObject = null;
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 编辑器中的调试按钮
    /// </summary>
    [ContextMenu("测试红点显示")]
    private void TestShowRedDot()
    {
        ForceShowRedDot();
    }
    
    [ContextMenu("测试红点隐藏")]
    private void TestHideRedDot()
    {
        ForceHideRedDot();
    }
    
    [ContextMenu("检查签到状态")]
    private void TestCheckSignIn()
    {
        bool canSign = CanSignIn();
        Debug.Log($"SignInRedDot: 当前签到状态 - 可签到: {canSign}");
    }
    #endif
}

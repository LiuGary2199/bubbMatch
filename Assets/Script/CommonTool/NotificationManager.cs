using System.Collections;
using System.Collections.Generic;
using UnityEngine;
#if UNITY_ANDROID
using Unity.Notifications.Android;
#elif UNITY_IOS
using Unity.Notifications.iOS;
#endif

/// <summary>
/// 游戏通知管理系统
/// 负责处理跨平台的通知功能
/// </summary>
public class NotificationManager : MonoBehaviour
{
    public static NotificationManager Instance { get; private set; }
    
    // 通知渠道ID
    private const string CHANNEL_ID = "game_notifications";
    
    // 可配置的URL Scheme，用于iOS唤起应用
    private string urlScheme = "yourgame://";

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);

        StartCoroutine(RequestNotificationPermission());
    }

    IEnumerator RequestNotificationPermission()
    {
        // 仅在移动平台请求通知权限
        #if UNITY_ANDROID || UNITY_IOS
        Debug.Log("请求通知权限...");
        
        #if UNITY_ANDROID
        var request = new PermissionRequest();
        while (request.Status == PermissionStatus.RequestPending)
            yield return null;
            
        Debug.Log($"Android通知权限状态: {request.Status}");
        #elif UNITY_IOS
        var authorizationOption = AuthorizationOption.Alert | AuthorizationOption.Badge | AuthorizationOption.Sound;
        using (var req = new AuthorizationRequest(authorizationOption, true))
        {
            while (!req.IsFinished)
                yield return null;
                
            string result = req.Granted ? "已授权" : "被拒绝";
            Debug.Log($"iOS通知权限状态: {result}, 错误: {req.Error}");
        }
        #endif
        
        InitializeNotifications();
        #else
        Debug.LogWarning("通知系统仅支持Android和iOS平台");
        #endif
    }

    /// <summary>
    /// 初始化通知系统
    /// </summary>
    private void InitializeNotifications()
    {
        ClearNotification();
        CreateNotificationChannel();
    }

    public void ClearNotification()
    {
        #if UNITY_ANDROID
        AndroidNotificationCenter.CancelAllScheduledNotifications();
        AndroidNotificationCenter.CancelAllNotifications();
        #elif UNITY_IOS
        iOSNotificationCenter.RemoveAllScheduledNotifications();
        iOSNotificationCenter.RemoveAllDeliveredNotifications();
        #endif
    }

    /// <summary>
    /// 创建Android通知渠道
    /// </summary>
    private void CreateNotificationChannel()
    {
        #if UNITY_ANDROID
        var channel = new AndroidNotificationChannel()
        {
            Id = CHANNEL_ID,
            Name = "游戏通知",
            Description = "接收游戏内的各种通知",
            Importance = Importance.High
        };

        AndroidNotificationCenter.RegisterNotificationChannel(channel);
        Debug.Log("Android通知渠道已创建");
        #endif
    }

    /// <summary>
    /// 发送即时通知
    /// </summary>
    public void SendImmediateNotification(string title, string message)
    {
        #if UNITY_ANDROID || UNITY_IOS
        #if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = message,
            FireTime = System.DateTime.Now
        };
        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
        #elif UNITY_IOS
        var notification = new iOSNotification
        {
            Title = title,
            Body = message,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound
        };
        iOSNotificationCenter.ScheduleNotification(notification);
        #endif
        Debug.Log($"发送即时通知: {title} - {message}");
        #else
        Debug.LogWarning($"在当前平台({Application.platform})上不支持发送通知");
        #endif
    }

    /// <summary>
    /// 发送定时通知
    /// </summary>
    public void ScheduleNotification(string title, string message, int secondsFromNow)
    {
        #if UNITY_ANDROID || UNITY_IOS
        #if UNITY_ANDROID
        var notification = new AndroidNotification
        {
            Title = title,
            Text = message,
            FireTime = System.DateTime.Now.AddSeconds(secondsFromNow)
        };
        AndroidNotificationCenter.SendNotification(notification, CHANNEL_ID);
        #elif UNITY_IOS
        var timeTrigger = new iOSNotificationTimeIntervalTrigger
        {
            TimeInterval = new System.TimeSpan(0, 0, secondsFromNow),
            Repeats = false
        };

        var notification = new iOSNotification
        {
            Title = title,
            Body = message,
            ShowInForeground = true,
            ForegroundPresentationOption = PresentationOption.Alert | PresentationOption.Sound,
            Trigger = timeTrigger
        };
        iOSNotificationCenter.ScheduleNotification(notification);
        #endif
        Debug.Log($"计划通知: {title} - {message}, 将在{secondsFromNow}秒后触发");
        #else
        Debug.LogWarning($"在当前平台({Application.platform})上不支持发送通知");
        #endif
    }

    void OnEnable()
    {
        #if UNITY_ANDROID
        AndroidNotificationCenter.OnNotificationReceived += OnAndroidNotificationReceived;
        #elif UNITY_IOS
        iOSNotificationCenter.OnNotificationReceived += OniOSNotificationReceived;
        #endif
    }

    void OnDisable()
    {
        #if UNITY_ANDROID
        AndroidNotificationCenter.OnNotificationReceived -= OnAndroidNotificationReceived;
        #elif UNITY_IOS
        iOSNotificationCenter.OnNotificationReceived -= OniOSNotificationReceived;
        #endif
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }

    #if UNITY_ANDROID
    private void OnAndroidNotificationReceived(AndroidNotificationIntentData data)
    {
        Debug.Log($"Android通知已接收: ID={data.Id}, Title={data.Notification.Title}");
        
        // 确保应用在前台运行
        if (!Application.isFocused)
        {
            using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            using (var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity"))
            {
                var packageManager = currentActivity.Call<AndroidJavaObject>("getPackageManager");
                var launchIntent = packageManager.Call<AndroidJavaObject>(
                    "getLaunchIntentForPackage",
                    Application.identifier
                );

                launchIntent.Call<AndroidJavaObject>("addFlags", 0x10000000); // FLAG_ACTIVITY_REORDER_TO_FRONT
                currentActivity.Call("startActivity", launchIntent);
            }
        }
    }
    #elif UNITY_IOS
    private void OniOSNotificationReceived(iOSNotification notification)
    {
        Debug.Log($"iOS通知已接收: ID={notification.Identifier}, Title={notification.Title}");
        
        var lastNotification = iOSNotificationCenter.GetLastRespondedNotification();
        if (lastNotification != null)
        {
            Debug.Log($"用户点击了通知: {lastNotification.Identifier}");
            
            // 确保应用在前台运行
            if (!Application.isFocused && !string.IsNullOrEmpty(urlScheme))
            {
                Application.OpenURL(urlScheme);
                Debug.Log($"通过URL scheme({urlScheme})唤起游戏");
            }
        }
    }
    #endif
}
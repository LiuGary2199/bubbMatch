using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using com.adjust.sdk;
using LitJson;
using DG.Tweening;
public class ADManager : MonoBehaviour
{
    public string MAX_SDK_KEY = "";
    public string MAX_REWARD_ID = "";
    public string MAX_INTER_ID = "";

    public bool isTest = false;
    public static ADManager Instance { get; private set; }

    private int retryAttempt;   // 广告加载失败后，重新加载广告次数
    private bool isShowingAd;     // 是否正在播放广告，用于判断切换前后台时是否增加计数

    public int lastPlayTimeCounter { get; private set; }   // 距离上次广告的时间间隔
    public int counter101 { get; private set; }     // 定时插屏(101)计数器
    public int counter102 { get; private set; }     // NoThanks插屏(102)计数器
    public int counter103 { get; private set; }     // 后台回前台插屏(103)计数器

    private string rewardNetworkName;
    private Action<bool> rewardCallBackAction;    // 激励视频回调
    private bool rewardSuccess;     // 激励视频是否成功收到奖励
    private string rewardIndex;     // 激励视频的打点

    private string interstitialNetworkName;
    private int interstitialType;      // 当前播放的插屏类型，101/102/103
    private string interstitialIndex;     // 插屏广告的的打点
    public bool pauseTimeInterstitial { get; private set; } // 定时插屏暂停播放

    private List<Action<ADType>> adPlayingCallbacks;    // 广告播放完成回调列表，用于其他系统广告计数（例如商店看广告任务）

    private long applicationPauseTimestamp;     // 切后台时的时间戳
    private Ad_CustomData RewardAdCustomData; //激励视频自定义数据
    private Ad_CustomData InterstitialAdCustomData; //插屏自定义数据
    private double InterstitialRewardnum = 0;
    private void Awake()
    {
        Instance = this;
    }

    private void OnEnable()
    {
        pauseTimeInterstitial = false;
        isShowingAd = false;
        lastPlayTimeCounter = 999;  // 初始时设置一个较大的值，不阻塞插屏广告
        rewardSuccess = false;

        // Android平台将Adjust的adid传给Max；iOS将randomKey传给Max
#if UNITY_ANDROID
        MaxSdk.SetSdkKey(GetSystemData.DecryptDES(MAX_SDK_KEY));
        // 将adjust id 传给Max
        string adjustId = SaveDataManager.GetString(CConfig.sv_AdjustAdid);
        if (string.IsNullOrEmpty(adjustId))
        {
            adjustId = Adjust.getAdid();
        }
        if (!string.IsNullOrEmpty(adjustId))
        {
            MaxSdk.SetUserId(adjustId);
            MaxSdk.InitializeSdk();
            SaveDataManager.SetString(CConfig.sv_AdjustAdid, adjustId);
        }
        else
        {
            StartCoroutine(setAdjustAdid());
        }
#else
        MaxSdk.SetSdkKey(GetSystemData.DecryptDES(MAX_SDK_KEY));
        MaxSdk.SetUserId(SaveDataManager.GetString(CConfig.sv_LocalUserId));
        MaxSdk.InitializeSdk();
#endif

        MaxSdkCallbacks.OnSdkInitializedEvent += (MaxSdkBase.SdkConfiguration sdkConfiguration) =>
        {
            // 打开调试模式
            //MaxSdk.ShowMediationDebugger();

            InitializeRewardedAds();
            MaxSdk.SetCreativeDebuggerEnabled(true);

            // 每秒执行一次计数
            InvokeRepeating(nameof(RepeatMethod), 1, 1);
        };
    }

    IEnumerator setAdjustAdid()
    {
        int i = 0;
        while (i < 5)
        {
            yield return new WaitForSeconds(1);
            if (CommonUtil.IsEditor())
            {
                MaxSdk.SetUserId(SaveDataManager.GetString(CConfig.sv_LocalUserId));
                MaxSdk.InitializeSdk();
                yield break;
            }
            else
            {
                string adjustId = Adjust.getAdid();
                if (!string.IsNullOrEmpty(adjustId))
                {
                    MaxSdk.SetUserId(adjustId);
                    MaxSdk.InitializeSdk();
                    SaveDataManager.SetString(CConfig.sv_AdjustAdid, adjustId);
                    yield break;
                }
            }
            i++;
        }
        if (i == 5)
        {
            MaxSdk.SetUserId(SaveDataManager.GetString(CConfig.sv_LocalUserId));
            MaxSdk.InitializeSdk();
        }
    }

    public void InitializeRewardedAds()
    {
        // Attach callback
        MaxSdkCallbacks.Rewarded.OnAdLoadedEvent += OnRewardedAdLoadedEvent;
        MaxSdkCallbacks.Rewarded.OnAdLoadFailedEvent += OnRewardedAdLoadFailedEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayedEvent += OnRewardedAdDisplayedEvent;
        MaxSdkCallbacks.Rewarded.OnAdClickedEvent += OnRewardedAdClickedEvent;
        MaxSdkCallbacks.Rewarded.OnAdRevenuePaidEvent += OnRewardedAdRevenuePaidEvent;
        MaxSdkCallbacks.Rewarded.OnAdHiddenEvent += OnRewardedAdHiddenEvent;
        MaxSdkCallbacks.Rewarded.OnAdDisplayFailedEvent += OnRewardedAdFailedToDisplayEvent;
        MaxSdkCallbacks.Rewarded.OnAdReceivedRewardEvent += OnRewardedAdReceivedRewardEvent;

        MaxSdkCallbacks.Interstitial.OnAdLoadedEvent += OnInterstitialLoadedEvent;
        MaxSdkCallbacks.Interstitial.OnAdLoadFailedEvent += OnInterstitialLoadFailedEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayedEvent += OnInterstitialDisplayedEvent;
        MaxSdkCallbacks.Interstitial.OnAdClickedEvent += OnInterstitialClickedEvent;
        MaxSdkCallbacks.Interstitial.OnAdRevenuePaidEvent += OnInterstitialRevenuePaidEvent;
        MaxSdkCallbacks.Interstitial.OnAdHiddenEvent += OnInterstitialHiddenEvent;
        MaxSdkCallbacks.Interstitial.OnAdDisplayFailedEvent += OnInterstitialAdFailedToDisplayEvent;

        // Load the first rewarded ad
        LoadRewardedAd();

        // Load the first interstitial
        LoadInterstitial();
    }

    private void LoadRewardedAd()
    {
        MaxSdk.LoadRewardedAd(MAX_REWARD_ID);
    }

    private void LoadInterstitial()
    {
        MaxSdk.LoadInterstitial(MAX_INTER_ID);
    }

    private void OnRewardedAdLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is ready for you to show. MaxSdk.IsRewardedAdReady(adUnitId) now returns 'true'.

        // Reset retry attempt
        retryAttempt = 0;
        rewardNetworkName = adInfo.NetworkName;

        RewardAdCustomData = new Ad_CustomData();
        RewardAdCustomData.user_id = CashOutManager.GetInstance().Data.UserID;
        RewardAdCustomData.version = Application.version;
        RewardAdCustomData.request_id = CashOutManager.GetInstance().EcpmRequestID();
        RewardAdCustomData.vendor = adInfo.NetworkName;
    }

    private void OnRewardedAdLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Rewarded ad failed to load 
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds).

        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));

        Invoke(nameof(LoadRewardedAd), (float)retryDelay);
    }

    private void OnRewardedAdDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_IOS
        MusicMgr.GetInstance().BgMusicSwitch = !MusicMgr.GetInstance().BgMusicSwitch;
        Time.timeScale = 0;
#endif
    }

    private void OnRewardedAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad failed to display. AppLovin recommends that you load the next ad.
        LoadRewardedAd();
        isShowingAd = false;
    }

    private void OnRewardedAdClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {

    }

    private void OnRewardedAdHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Rewarded ad is hidden. Pre-load the next ad
#if UNITY_IOS
        Time.timeScale = 1;
        MusicMgr.GetInstance().BgMusicSwitch = !MusicMgr.GetInstance().BgMusicSwitch;
#endif

        isShowingAd = false;
        LoadRewardedAd();
        if (rewardSuccess)
        {
            rewardSuccess = false;
            rewardCallBackAction?.Invoke(true);

            AfterAdPlaySuccess(ADType.Rewarded);
            PostEventScript.GetInstance().SendEvent("9007", rewardIndex);
        }
        else
        {
            rewardCallBackAction?.Invoke(false);
        }

        // 上报ecpm
        CashOutManager.GetInstance().ReportEcpm(adInfo, RewardAdCustomData.request_id, "REWARD");
    }

    private void OnRewardedAdReceivedRewardEvent(string adUnitId, MaxSdk.Reward reward, MaxSdkBase.AdInfo adInfo)
    {
        // The rewarded ad displayed and the user should receive the reward.
        rewardSuccess = true;
    }

    private void OnRewardedAdRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo info)
    {
        // Ad revenue paid. Use this callback to track user revenue.
        //从MAX获取收入数据
        var adRevenue = new AdjustAdRevenue(AdjustConfig.AdjustAdRevenueSourceAppLovinMAX);
        adRevenue.setRevenue(info.Revenue, "USD");
        adRevenue.setAdRevenueNetwork(info.NetworkName);
        adRevenue.setAdRevenueUnit(info.AdUnitIdentifier);
        adRevenue.setAdRevenuePlacement(info.Placement);

        //发回收入数据给自己后台
        string countryCodeByMAX = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"
        PostEventScript.GetInstance().SendEvent("9008", info.Revenue.ToString(), countryCodeByMAX);

        //带广告收入的漏传策略
        AdjustInitManager.Instance.AddAdCount(countryCodeByMAX, info.Revenue);

        string adjustAdid = AdjustInitManager.Instance.GetAdjustAdid();
        //发回收入数据给Adjust
        if (!string.IsNullOrEmpty(adjustAdid))
        {
            Adjust.trackAdRevenue(adRevenue);
            UnityEngine.Debug.Log("Max to Adjust (rewarded), adUnitId:" + adUnitId + ", revenue:" + info.Revenue + ", network:" + info.NetworkName + ", unit:" + info.AdUnitIdentifier + ", placement:" + info.Placement);
        }

        // 发回收入数据给Mintegral
        if (!string.IsNullOrEmpty(adjustAdid))
        {
#if UNITY_ANDROID || UNITY_IOS
            MBridgeRevenueParamsEntity mBridgeRevenueParamsEntity = new MBridgeRevenueParamsEntity(MBridgeRevenueParamsEntity.ATTRIBUTION_PLATFORM_ADJUST, adjustAdid);
            ///MaxSdkBase.AdInfo类型的adInfo
            mBridgeRevenueParamsEntity.SetMaxAdInfo(info);
            MBridgeRevenueManager.Track(mBridgeRevenueParamsEntity);
            UnityEngine.Debug.Log(nameof(MBridgeRevenueManager) + "~Rewarded revenue:" + info.Revenue);
#endif
        }
    }

    private void OnInterstitialLoadedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is ready for you to show. MaxSdk.IsInterstitialReady(adUnitId) now returns 'true'

        // Reset retry attempt
        retryAttempt = 0;
        interstitialNetworkName = adInfo.NetworkName;

        InterstitialAdCustomData = new Ad_CustomData();
        InterstitialAdCustomData.user_id = CashOutManager.GetInstance().Data.UserID;
        InterstitialAdCustomData.version = Application.version;
        InterstitialAdCustomData.request_id = CashOutManager.GetInstance().EcpmRequestID();
        InterstitialAdCustomData.vendor = adInfo.NetworkName;
    }

    private void OnInterstitialLoadFailedEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo)
    {
        // Interstitial ad failed to load 
        // AppLovin recommends that you retry with exponentially higher delays, up to a maximum delay (in this case 64 seconds)

        retryAttempt++;
        double retryDelay = Math.Pow(2, Math.Min(6, retryAttempt));

        Invoke(nameof(LoadInterstitial), (float)retryDelay);
    }

    private void OnInterstitialDisplayedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
#if UNITY_IOS
        MusicMgr.GetInstance().BgMusicSwitch = !MusicMgr.GetInstance().BgMusicSwitch;
        Time.timeScale = 0;
#endif
    }

    private void OnInterstitialAdFailedToDisplayEvent(string adUnitId, MaxSdkBase.ErrorInfo errorInfo, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad failed to display. AppLovin recommends that you load the next ad.
        LoadInterstitial();
        isShowingAd = false;
    }

    private void OnInterstitialClickedEvent(string adUnitId, MaxSdkBase.AdInfo adInfo) { }

    private void OnInterstitialRevenuePaidEvent(string adUnitId, MaxSdkBase.AdInfo info)
    {
        //从MAX获取收入数据
        var adRevenue = new AdjustAdRevenue(AdjustConfig.AdjustAdRevenueSourceAppLovinMAX);
        adRevenue.setRevenue(info.Revenue, "USD");
        adRevenue.setAdRevenueNetwork(info.NetworkName);
        adRevenue.setAdRevenueUnit(info.AdUnitIdentifier);
        adRevenue.setAdRevenuePlacement(info.Placement);

        //发回收入数据给自己后台
        string countryCodeByMAX = MaxSdk.GetSdkConfiguration().CountryCode; // "US" for the United States, etc - Note: Do not confuse this with currency code which is "USD"
        PostEventScript.GetInstance().SendEvent("9108", info.Revenue.ToString(), countryCodeByMAX);

        //带广告收入的漏传策略
        AdjustInitManager.Instance.AddAdCount(countryCodeByMAX, info.Revenue);

        //发回收入数据给Adjust
        if (!string.IsNullOrEmpty(AdjustInitManager.Instance.GetAdjustAdid()))
        {
            Adjust.trackAdRevenue(adRevenue);
            UnityEngine.Debug.Log("Max to Adjust (interstitial), adUnitId:" + adUnitId + ", revenue:" + info.Revenue + ", network:" + info.NetworkName + ", unit:" + info.AdUnitIdentifier + ", placement:" + info.Placement);
        }

        // 发回收入数据给Mintegral
        string adjustAdid = AdjustInitManager.Instance.GetAdjustAdid();
        if (!string.IsNullOrEmpty(adjustAdid))
        {
#if UNITY_ANDROID || UNITY_IOS
            MBridgeRevenueParamsEntity mBridgeRevenueParamsEntity = new MBridgeRevenueParamsEntity(MBridgeRevenueParamsEntity.ATTRIBUTION_PLATFORM_ADJUST, adjustAdid);
            ///MaxSdkBase.AdInfo类型的adInfo
            mBridgeRevenueParamsEntity.SetMaxAdInfo(info);
            MBridgeRevenueManager.Track(mBridgeRevenueParamsEntity);
            UnityEngine.Debug.Log(nameof(MBridgeRevenueManager) + "~Interstitial revenue:" + info.Revenue);
#endif
        }
    }

    private void OnInterstitialHiddenEvent(string adUnitId, MaxSdkBase.AdInfo adInfo)
    {
        // Interstitial ad is hidden. Pre-load the next ad.
#if UNITY_IOS
        Time.timeScale = 1;
        MusicMgr.GetInstance().BgMusicSwitch = !MusicMgr.GetInstance().BgMusicSwitch;
#endif
        LoadInterstitial();

        AfterAdPlaySuccess(ADType.Interstitial);
        PostEventScript.GetInstance().SendEvent("9107", interstitialIndex);
        // 上报ecpm
        CashOutManager.GetInstance().ReportEcpm(adInfo, InterstitialAdCustomData.request_id, "INTER");
    }


    /// <summary>
    /// 播放激励视频广告
    /// </summary>
    /// <param name="callBack"></param>
    /// <param name="index"></param>
    public void playRewardVideo(Action<bool> callBack, string index)
    {
        if (isTest)
        {
            callBack(true);
            AfterAdPlaySuccess(ADType.Rewarded);
            return;
        }

        bool rewardVideoReady = MaxSdk.IsRewardedAdReady(MAX_REWARD_ID);
        rewardCallBackAction = callBack;
        if (rewardVideoReady)
        {
            // 打点
            rewardIndex = index;
            PostEventScript.GetInstance().SendEvent("9002", index);
            isShowingAd = true;
            rewardSuccess = false;
            string placement = index + "_" + rewardNetworkName;
            RewardAdCustomData.placement_id = placement;
            MaxSdk.ShowRewardedAd(MAX_REWARD_ID, placement, JsonMapper.ToJson(RewardAdCustomData));
        }
        else
        {
            ToastManager.GetInstance().ShowToast("No ads right now, please try it later.");
            rewardCallBackAction(false);
        }
    }

    /// <summary>
    /// 播放插屏广告
    /// </summary>
    /// <param name="index"></param>
    public void playInterstitialAd(int index)
    {
        if (index == 101 || index == 102 || index == 103)
        {
            UnityEngine.Debug.LogError("广告点位不允许为101、102、103");
            return;
        }

        playInterstitial(index);
    }

    /// <summary>
    /// 播放插屏广告
    /// </summary>
    /// <param name="index">101/102/103</param>
    /// <param name="customIndex">用户自定义点位</param>
    private void playInterstitial(int index, int customIndex = 0)
    {
        interstitialType = index;

        if (isShowingAd)
        {
            return;
        }

        //这个参数很少有游戏会用 需要的时候自己再打开
        // 当用户过关数 < trial_MaxNum时，不弹插屏广告
         int sv_trialNum = SaveDataManager.GetInt(CConfig.sv_ad_trial_num);
         int trial_MaxNum = int.Parse(NetInfoMgr.instance.ConfigData.trial_MaxNum);
         if (sv_trialNum < trial_MaxNum)
         {
             return;
         }

        // 时间间隔低于阈值，不播放广告
        if (lastPlayTimeCounter < int.Parse(NetInfoMgr.instance.ConfigData.inter_freq))
        {
            return;
        }

        if (isTest)
        {
            AfterAdPlaySuccess(ADType.Interstitial);
            return;
        }

        bool interstitialVideoReady = MaxSdk.IsInterstitialReady(MAX_INTER_ID);
        if (interstitialVideoReady)
        {
           
            if (interstitialType == 101)
            {
                isShowingAd = true;
                DOVirtual.DelayedCall(0.1f, () => //停顿
                {
                    UIManager.GetInstance().CloseOrReturnUIForms(nameof(InsertTips));
                    string point = index.ToString();
                    if (customIndex > 0)
                    {
                        point += customIndex.ToString().PadLeft(2, '0');
                    }
                    interstitialIndex = point;
                    PostEventScript.GetInstance().SendEvent("9102", point);
                    string placement = point + "_" + interstitialNetworkName;
                    InterstitialAdCustomData.placement_id = placement;
                    MaxSdk.ShowInterstitial(MAX_INTER_ID, placement, JsonMapper.ToJson(InterstitialAdCustomData));
                });
            }
            else
            {
                isShowingAd = true;
                // 打点
                string point = index.ToString();
                if (customIndex > 0)
                {
                    point += customIndex.ToString().PadLeft(2, '0');
                }
                interstitialIndex = point;
                PostEventScript.GetInstance().SendEvent("9102", point);
                string placement = point + "_" + interstitialNetworkName;
                InterstitialAdCustomData.placement_id = placement;
                MaxSdk.ShowInterstitial(MAX_INTER_ID, placement, JsonMapper.ToJson(InterstitialAdCustomData));
            }
        }
    }

    /// <summary>
    /// 每秒更新一次计数器 - 101计数器 和 时间间隔计数器
    /// </summary>
    private void RepeatMethod()
    {
        lastPlayTimeCounter++;

        int relax_interval = int.Parse(NetInfoMgr.instance.ConfigData.relax_interval);
        // 计时器阈值设置为0或负数时，关闭广告101；
        // 播放广告期间不计数；
        if (relax_interval <= 0 || isShowingAd)
        {
            return;
        }
        else
        {
            counter101++;
            if (counter101 >= relax_interval && !pauseTimeInterstitial)
            {
                playInterstitial(101);
            }
            if (counter101 + 2 >= relax_interval && !pauseTimeInterstitial)
            {
                if (isShowingAd)
                {
                    return;
                }
                int sv_trialNum = SaveDataManager.GetInt(CConfig.sv_ad_trial_num);
                int trial_MaxNum = int.Parse(NetInfoMgr.instance.ConfigData.trial_MaxNum);
                if (sv_trialNum < trial_MaxNum)
                {
                    return;
                }
                if (lastPlayTimeCounter < int.Parse(NetInfoMgr.instance.ConfigData.inter_freq))
                {
                    return;
                }
                if (isTest)
                {
                    AfterAdPlaySuccess(ADType.Interstitial);
                    return;
                }
                bool interstitialVideoReady = MaxSdk.IsInterstitialReady(MAX_INTER_ID);
                if (interstitialVideoReady)
                {
                    InterstitialRewardnum = GameUtil.GetInterstitialData();
                    UIManager.GetInstance().ShowUIForms(nameof(InsertTips));
                    InsertTips.Instance.InitData(InterstitialRewardnum);
                }
            }
        }
    }

    /// <summary>
    /// NoThanks插屏 - 102
    /// </summary>
    public void NoThanksAddCount(int customIndex = 0)
    {
        // 用户行为累计次数计数器阈值设置为0或负数时，关闭广告102
        int nextlevel_interval = int.Parse(NetInfoMgr.instance.ConfigData.nextlevel_interval);
        if (nextlevel_interval <= 0)
        {
            return;
        }
        else
        {
            counter102 = SaveDataManager.GetInt("NoThanksCount") + 1;
            SaveDataManager.SetInt("NoThanksCount", counter102);
            if (counter102 >= nextlevel_interval)
            {
                playInterstitial(102, customIndex);
            }
        }
    }

    /// <summary>
    /// 前后台切换计数器 - 103
    /// </summary>
    /// <param name="pause"></param>
    private void OnApplicationPause(bool pause)
    {
        if (!pause)
        {
            // 切回前台
            if (!isShowingAd)
            {
                // 前后台切换时，播放间隔计数器需要累加切到后台的时间
                if (applicationPauseTimestamp > 0)
                {
                    lastPlayTimeCounter += (int)(DateUtil.Current() - applicationPauseTimestamp);
                    applicationPauseTimestamp = 0;
                }
                // 后台切回前台累计次数，后台配置为0或负数，关闭该广告
                int inter_b2f_count = int.Parse(NetInfoMgr.instance.ConfigData.inter_b2f_count);
                if (inter_b2f_count <= 0)
                {
                    return;
                }
                else
                {
                    counter103++;
                    if (counter103 >= inter_b2f_count)
                    {
                        playInterstitial(103);
                    }
                }
            }
        }
        else
        {
            // 切到后台
            applicationPauseTimestamp = DateUtil.Current();
        }
    }

    /// <summary>
    /// 暂停定时插屏播放 - 101
    /// </summary>
    public void PauseTimeInterstitial()
    {
        pauseTimeInterstitial = true;
    }

    /// <summary>
    /// 恢复定时插屏播放 - 101
    /// </summary>
    public void ResumeTimeInterstitial()
    {
        pauseTimeInterstitial = false;
    }

    /// <summary>
    /// 更新游戏的TrialNum
    /// </summary>
    /// <param name="num"></param>
    public void UpdateTrialNum(int num)
    {
        SaveDataManager.SetInt(CConfig.sv_ad_trial_num, num);
    }

    /// <summary>
    /// 注册看广告的回调事件
    /// </summary>
    /// <param name="callback"></param>
    public void RegisterPlayCallback(Action<ADType> callback)
    {
        if (adPlayingCallbacks == null)
        {
            adPlayingCallbacks = new List<Action<ADType>>();
        }

        if (!adPlayingCallbacks.Contains(callback))
        {
            adPlayingCallbacks.Add(callback);
        }
    }

    /// <summary>
    /// 广告播放成功后，执行看广告回调事件
    /// </summary>
    private void AfterAdPlaySuccess(ADType adType)
    {
        isShowingAd = false;
        // 播放间隔计数器清零
        lastPlayTimeCounter = 0;
        // 插屏计数器清零
        if (adType == ADType.Interstitial)
        {
            // 计数器清零
            if (interstitialType == 101)
            {
                HomePanel.Instance.AddCash(InterstitialRewardnum,null);
                InterstitialRewardnum =0;
                counter101 = 0;
            }
            else if (interstitialType == 102)
            {
                counter102 = 0;
                SaveDataManager.SetInt("NoThanksCount", 0);
            }
            else if (interstitialType == 103)
            {
                counter103 = 0;
            }
        }

        // 看广告总数+1
        SaveDataManager.SetInt(CConfig.sv_total_ad_num + adType.ToString(), SaveDataManager.GetInt(CConfig.sv_total_ad_num + adType.ToString()) + 1);
        // 真提现任务 
        if (adType == ADType.Rewarded)
            CashOutManager.GetInstance().AddTaskValue("Ad",1);

        // 回调
        if (adPlayingCallbacks != null && adPlayingCallbacks.Count > 0)
        {
            foreach (Action<ADType> callback in adPlayingCallbacks)
            {
                callback?.Invoke(adType);
            }
        }
    }

    /// <summary>
    /// 获取总的看广告次数
    /// </summary>
    /// <returns></returns>
    public int GetTotalAdNum(ADType adType)
    {
        return SaveDataManager.GetInt(CConfig.sv_total_ad_num + adType.ToString());
    }
}

public enum ADType { Interstitial, Rewarded }

[System.Serializable]
public class Ad_CustomData //广告自定义数据
{
    public string user_id; //用户id
    public string version; //版本号
    public string request_id; //请求id
    public string vendor; //渠道
    public string placement_id; //广告位id
}
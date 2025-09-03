using System;
using System.Collections;
using com.adjust.sdk;
using LitJson;
using UnityEngine;
using Random = UnityEngine.Random;

public class AdjustInitManager : MonoBehaviour
{
    public static AdjustInitManager Instance;

    public string adjustID;     // 由遇总的打包工具统一修改，无需手动配置

    //用户adjust 状态KEY
    private string sv_ADJustInitType = "sv_ADJustInitType";

    //adjust 时间戳
    private string sv_ADJustTime = "sv_ADJustTime";

    //adjust行为计数器
    public int _currentCount { get; private set; }

    public double _currentRevenue { get; private set; }

    double adjustInitAdRevenue = 0;


    private void Awake()
    {
        Instance = this;
        SaveDataManager.SetString(sv_ADJustTime, DateUtil.Current().ToString());

#if UNITY_IOS
        SaveDataManager.SetString(sv_ADJustInitType, AdjustStatus.OpenAsAct.ToString());
        AdjustInit();
#endif
    }

    private void Start()
    {
        _currentCount = 0;
    }


    void AdjustInit()
    {
#if UNITY_EDITOR
        return;
#endif
        AdjustConfig adjustConfig = new AdjustConfig(adjustID, AdjustEnvironment.Production, false);
        adjustConfig.setLogLevel(AdjustLogLevel.Verbose);
        adjustConfig.setSendInBackground(false);
        adjustConfig.setEventBufferingEnabled(false);
        adjustConfig.setLaunchDeferredDeeplink(true);
        Adjust.start(adjustConfig);

        StartCoroutine(SaveAdjustAdid());
    }

    private IEnumerator SaveAdjustAdid()
    {
        while (true)
        {
            string adjustAdid = Adjust.getAdid();
            if (string.IsNullOrEmpty(adjustAdid))
            {
                yield return new WaitForSeconds(5);
            }
            else
            {
                SaveDataManager.SetString(CConfig.sv_AdjustAdid, adjustAdid);
                NetInfoMgr.instance.SendAdjustAdid();
                yield break;
            }
        }
    }

    public string GetAdjustAdid()
    {
        return SaveDataManager.GetString(CConfig.sv_AdjustAdid);
    }

    /// <summary>
    /// 获取adjust初始化状态
    /// </summary>
    /// <returns></returns>
    public string GetAdjustStatus()
    {
        return SaveDataManager.GetString(sv_ADJustInitType);
    }

    /*
     *  API
     *  Adjust 初始化
     */
    public void InitAdjustData(bool isOldUser = false)
    {
        #if UNITY_IOS
            return;
        #endif
        // 如果后台配置的adjust_init_act_position <= 0，直接初始化
        if (string.IsNullOrEmpty(NetInfoMgr.instance.ConfigData.adjust_init_act_position) || int.Parse(NetInfoMgr.instance.ConfigData.adjust_init_act_position) <= 0)
        {
            SaveDataManager.SetString(sv_ADJustInitType, AdjustStatus.OpenAsAct.ToString());
        }
        print(" user init adjust by status :" + SaveDataManager.GetString(sv_ADJustInitType));
        //用户二次登录 根据标签初始化
        if (SaveDataManager.GetString(sv_ADJustInitType) == AdjustStatus.OldUser.ToString() || SaveDataManager.GetString(sv_ADJustInitType) == AdjustStatus.OpenAsAct.ToString())
        {
            print("second login  and  init adjust");
            AdjustInit();
        }
    }



    /*
     * API
     *  记录行为累计次数
     *  @param2 打点参数
     */
    public void AddActCount(string param2 = "")
    {
#if UNITY_IOS
            return;
#endif
        if (SaveDataManager.GetString(sv_ADJustInitType) != "") return;
        _currentCount++;
        print(" add up to :" + _currentCount);
        if (string.IsNullOrEmpty(NetInfoMgr.instance.ConfigData.adjust_init_act_position) || _currentCount == int.Parse(NetInfoMgr.instance.ConfigData.adjust_init_act_position))
        {
            LoadAdjustOnAct(param2);
        }
    }

    /// <summary>
    /// 记录广告行为累计次数，带广告收入
    /// </summary>
    /// <param name="countryCode"></param>
    /// <param name="revenue"></param>
    public void AddAdCount(string countryCode, double revenue)
    {
#if UNITY_IOS
            return;
#endif
        //if (SaveDataManager.GetString(sv_ADJustInitType) != "") return;

        _currentCount++;
        _currentRevenue += revenue;
        print(" Ads count: " + _currentCount + ", Revenue sum: " + _currentRevenue);

        //如果后台有adjust_init_adrevenue数据 且 能找到匹配的countryCode，初始化adjustInitAdRevenue
        if (!string.IsNullOrEmpty(NetInfoMgr.instance.ConfigData.adjust_init_adrevenue))
        {
            JsonData jd = JsonMapper.ToObject(NetInfoMgr.instance.ConfigData.adjust_init_adrevenue);
            if (jd.ContainsKey(countryCode))
            {
                adjustInitAdRevenue = double.Parse(jd[countryCode].ToString(), new System.Globalization.CultureInfo("en-US"));
            }
        }

        if (
            string.IsNullOrEmpty(NetInfoMgr.instance.ConfigData.adjust_init_act_position)                   //后台没有配置限制条件，直接走LoadAdjust
            || (_currentCount == int.Parse(NetInfoMgr.instance.ConfigData.adjust_init_act_position)         //累计广告次数满足adjust_init_act_position条件，且累计广告收入满足adjust_init_adrevenue条件，走LoadAdjust
                && _currentRevenue >= adjustInitAdRevenue)
        )
        {
            LoadAdjustOnAct();
        }
    }

    /*
     * API
     * 根据行为 初始化 adjust
     *  @param2 打点参数 
     */
    public void LoadAdjustOnAct(string param2 = "")
    {
        if (SaveDataManager.GetString(sv_ADJustInitType) != "") return;

        // 根据比例分流   adjust_init_rate_act  行为比例
        if (string.IsNullOrEmpty(NetInfoMgr.instance.ConfigData.adjust_init_rate_act) || int.Parse(NetInfoMgr.instance.ConfigData.adjust_init_rate_act) > Random.Range(0, 100))
        {
            print("user finish  act  and  init adjust");
            SaveDataManager.SetString(sv_ADJustInitType, AdjustStatus.OpenAsAct.ToString());
            AdjustInit();

            // 上报点位 新用户达成 且 初始化
            PostEventScript.GetInstance().SendEvent("1091", GetAdjustTime(), param2);
        }
        else
        {
            print("user finish  act  and  not init adjust");
            SaveDataManager.SetString(sv_ADJustInitType, AdjustStatus.CloseAsAct.ToString());
            // 上报点位 新用户达成 且  不初始化
            PostEventScript.GetInstance().SendEvent("1092", GetAdjustTime(), param2);
        }
    }

    
    /*
     * API
     *  重置当前次数
     */
    public void ResetActCount()
    {
        print("clear current ");
        _currentCount = 0;
    }


    // 获取启动时间
    private string GetAdjustTime()
    {
        return DateUtil.Current() - long.Parse(SaveDataManager.GetString(sv_ADJustTime)) + "";
    }
}


/*
 *@param
 *  OldUser     老用户
 *  OpenAsAct   行为触发且初始化
 *  CloseAsAct  行为触发不初始化
 */
public enum AdjustStatus
{
    OldUser,
    OpenAsAct,
    CloseAsAct
}
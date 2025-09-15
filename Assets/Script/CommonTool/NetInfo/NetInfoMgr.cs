/***
 * 
 * 
 * 网络信息控制
 * 
 * **/
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using LitJson;
using System.IO;
using System.Runtime.InteropServices;
//using MoreMountains.NiceVibrations;

public class NetInfoMgr : MonoBehaviour
{

    public static NetInfoMgr instance;
    //请求超时时间
    private static float TIMEOUT = 3f;
    //base
    public string BaseUrl;
    //登录url
    public string BaseLoginUrl;
    //配置url
    public string BaseConfigUrl;
    //时间戳url
    public string BaseTimeUrl;
    //更新AdjustId url
    public string BaseAdjustUrl;
    //后台gamecode
    public string GameCode = "20000";

    //channel渠道平台
#if UNITY_IOS
    public string Channel = "AppStore";
#elif UNITY_ANDROID
    public string Channel = "GooglePlay";
#else
    public string Channel = "Other";
#endif
    //工程包名
    private string PackageName { get { return Application.identifier; } }
    //登录url
    private string LoginUrl = "";
    //配置url
    private string ConfigUrl = "";
    //更新AdjustId url
    private string AdjustUrl = "";
    //国家
    public string country = "";
    //服务器Config数据
    public ServerData ConfigData;
    public GameData GameData;
    //游戏内数据
    public Init InitData;
    //提现相关后台数据
    public CashOutData CashOut_Data;
    //ADManager
    public GameObject adManager;
    [HideInInspector]
    public string gaid;
    [HideInInspector]
    public string aid;
    [HideInInspector]
    public string idfa;
    int ready_count = 0;
    public bool ready = false;
    //ios 获取idfa函数声明
#if UNITY_IOS
    [DllImport("__Internal")]
    internal extern static void getIDFA();
#endif
    public BlockRuleData BlockRule;
    [HideInInspector] public string DataFrom; //数据来源 打点用

    //签到奖励
    public List<List<RewardData>> List_SignInData = new List<List<RewardData>>();
    void Awake()
    {
        instance = this;
        LoginUrl = BaseLoginUrl + GameCode + "&channel=" + Channel + "&version=" + Application.version;
        ConfigUrl = BaseConfigUrl + GameCode + "&channel=" + Channel + "&version=" + Application.version;
        AdjustUrl = BaseAdjustUrl + GameCode;
    }
    private void Start()
    {
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass aj = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject p = aj.GetStatic<AndroidJavaObject>("currentActivity");
            p.Call("getGaid");
            p.Call("getAid");
        }
        else if (Application.platform == RuntimePlatform.IPhonePlayer)
        {
#if UNITY_IOS
            getIDFA();
            string idfv = UnityEngine.iOS.Device.vendorIdentifier;
            SaveDataManager.SetString("idfv", idfv);
#endif
        }
        else
        {
            Login();           //编辑器登录
        }
        //获取config数据
        GetConfigData();

        //提现登录
        CashOutManager.GetInstance().Login();
    }

    /// <summary>
    /// 获取gaid回调
    /// </summary>
    /// <param name="gaid_str"></param>
    public void gaidAction(string gaid_str)
    {
        Debug.Log("unity收到gaid：" + gaid_str);
        gaid = gaid_str;
        if (gaid == null || gaid == "")
        {
            gaid = SaveDataManager.GetString("gaid");
        }
        else
        {
            SaveDataManager.SetString("gaid", gaid);
        }
        ready_count++;
        if (ready_count == 2)
        {
            Login();
        }
    }
    /// <summary>
    /// 获取aid回调
    /// </summary>
    /// <param name="aid_str"></param>
    public void aidAction(string aid_str)
    {
        Debug.Log("unity收到aid：" + aid_str);
        aid = aid_str;
        if (aid == null || aid == "")
        {
            aid = SaveDataManager.GetString("aid");
        }
        else
        {
            SaveDataManager.SetString("aid", aid);
        }
        ready_count++;
        if (ready_count == 2)
        {
            Login();
        }
    }
    /// <summary>
    /// 获取idfa成功
    /// </summary>
    /// <param name="message"></param>
    public void idfaSuccess(string message)
    {
        Debug.Log("idfa success:" + message);
        idfa = message;
        SaveDataManager.SetString("idfa", idfa);
        Login();
    }
    /// <summary>
    /// 获取idfa失败
    /// </summary>
    /// <param name="message"></param>
    public void idfaFail(string message)
    {
        Debug.Log("idfa fail");
        idfa = SaveDataManager.GetString("idfa");
        Login();
    }
    /// <summary>
    /// 登录
    /// </summary>
    public void Login()
    {
        //获取本地缓存的Local用户ID
        string localId = SaveDataManager.GetString(CConfig.sv_LocalUserId);

        //没有用户ID，视为新用户，生成用户ID
        if (localId == "" || localId.Length == 0)
        {
            //生成用户随机id
            TimeSpan st = DateTime.UtcNow - new DateTime(1970, 1, 1, 0, 0, 0);
            string timeStr = Convert.ToInt64(st.TotalSeconds).ToString() + UnityEngine.Random.Range(0, 10).ToString() + UnityEngine.Random.Range(1, 10).ToString() + UnityEngine.Random.Range(1, 10).ToString() + UnityEngine.Random.Range(1, 10).ToString();
            localId = timeStr;
            SaveDataManager.SetString(CConfig.sv_LocalUserId, localId);
        }

        //拼接登录接口参数
        string url = "";
        if (Application.platform == RuntimePlatform.IPhonePlayer)       //一个参数 - iOS
        {
            url = LoginUrl + "&" + "randomKey" + "=" + localId + "&idfa=" + idfa + "&packageName=" + PackageName;
        }
        else if (Application.platform == RuntimePlatform.Android)  //两个参数 - Android
        {
            url = LoginUrl + "&" + "randomKey" + "=" + localId + "&gaid=" + gaid + "&androidId=" + aid + "&packageName=" + PackageName;
        }
        else //编辑器
        {
            url = LoginUrl + "&" + "randomKey" + "=" + localId + "&packageName=" + PackageName;
        }

        //获取国家信息
        getCountry(() =>
        {
            url += "&country=" + country;
            //登录请求
            NetWorkManager.GetInstance().HttpGet(url,
                (data) =>
                {
                    Debug.Log("Login 成功" + data.downloadHandler.text);
                    SaveDataManager.SetString("init_time", DateTime.Now.ToString());
                    ServerUserData serverUserData = JsonMapper.ToObject<ServerUserData>(data.downloadHandler.text);
                    SaveDataManager.SetString(CConfig.sv_LocalServerId, serverUserData.data.ToString());

                    SendAdjustAdid();

                    if (PlayerPrefs.GetInt("SendedEvent") != 1 && !String.IsNullOrEmpty(CommonUtil.StepLog))
                        CommonUtil.SendEvent();
                },
                () =>
                {
                    Debug.Log("Login 失败");
                });
        });
    }
    /// <summary>
    /// 获取国家
    /// </summary>
    /// <param name="cb"></param>
    private void getCountry(Action cb)
    {
        bool callBackReady = false;
        if (String.IsNullOrEmpty(country))
        {
            NetWorkManager.GetInstance().HttpGet("https://a.mafiagameglobal.com/event/country/", (data) =>
            {
                country = JsonMapper.ToObject<Dictionary<string, string>>(data.downloadHandler.text)["country"];
                Debug.Log("获取国家 成功:" + country);
                if (!callBackReady)
                {
                    callBackReady = true;
                    cb?.Invoke();
                }
            },
            () =>
            {
                Debug.Log("获取国家 失败");
                if (!callBackReady)
                {
                    country = "";
                    callBackReady = true;
                    cb?.Invoke();
                }
            });
        }
        else
        {
            if (!callBackReady)
            {
                callBackReady = true;
                cb?.Invoke();
            }
        }
    }

    /// <summary>
    /// 获取服务器Config数据
    /// </summary>
    private void GetConfigData()
    {
        Debug.Log("GetConfigData:" + ConfigUrl);
        //获取并存入Config
        NetWorkManager.GetInstance().HttpGet(ConfigUrl,
        (data) =>
        {
            DataFrom = "OnlineData";
            Debug.Log("ConfigData 成功" + data.downloadHandler.text);
            SaveDataManager.SetString("OnlineData", data.downloadHandler.text);
            SetConfigData(data.downloadHandler.text);
        },
        () =>
        {
            Debug.Log("ConfigData 失败");
            GetLoactionData();
        });
    }

    /// <summary>
    /// 获取本地Config数据
    /// </summary>
    private void GetLoactionData()
    {
        //是否有缓存
        if (SaveDataManager.GetString("OnlineData") == "" || SaveDataManager.GetString("OnlineData").Length == 0)
        {
            DataFrom = "LocalData_Updated"; //已联网更新过的数据
            Debug.Log("本地数据");
            TextAsset json = Resources.Load<TextAsset>("LocationJson/LocationData");
            SetConfigData(json.text);
        }
        else
        {
            DataFrom = "LocalData_Original"; //原始数据
            Debug.Log("服务器缓存数据");
            SetConfigData(SaveDataManager.GetString("OnlineData"));
        }
    }

    /// <summary>
    /// 解析config数据
    /// </summary>
    /// <param name="configJson"></param>
    void SetConfigData(string configJson)
    {
        //如果已经获得了数据则不再处理
        if (ConfigData == null)
        {
            RootData rootData = JsonMapper.ToObject<RootData>(configJson);
            ConfigData = rootData.data;
            InitData = JsonMapper.ToObject<Init>(ConfigData.init);
            if (!string.IsNullOrEmpty(ConfigData.game_data))
                GameData = JsonMapper.ToObject<GameData>(ConfigData.game_data);
            if (!string.IsNullOrEmpty(ConfigData.BlockRule))
                BlockRule = JsonMapper.ToObject<BlockRuleData>(ConfigData.BlockRule);
            if (!string.IsNullOrEmpty(ConfigData.CashOut_Data))
                CashOut_Data = JsonMapper.ToObject<CashOutData>(ConfigData.CashOut_Data);

            //GameReady();
            GetUserInfo();
        }
    }
    /// <summary>
    /// 进入游戏
    /// </summary>
    void GameReady()
    {
        //打开admanager
        adManager.SetActive(true);
        //进度条可以继续
        ready = true;
    }

    /// <summary>
    /// 向后台发送adjustId
    /// </summary>
    public void SendAdjustAdid()
    {
        string serverId = SaveDataManager.GetString(CConfig.sv_LocalServerId);
        string adjustId = AdjustInitManager.Instance.GetAdjustAdid();
        if (string.IsNullOrEmpty(serverId) || string.IsNullOrEmpty(adjustId))
        {
            return;
        }

        string url = AdjustUrl + "&serverId=" + serverId + "&adid=" + adjustId;
        NetWorkManager.GetInstance().HttpGet(url,
            (data) =>
            {
                Debug.Log("服务器更新adjust adid 成功" + data.downloadHandler.text);
            },
            () =>
            {
                Debug.Log("服务器更新adjust adid 失败");
            });
        CashOutManager.GetInstance().ReportAdjustID();
    }


    //获取用户信息
    public string UserDataStr = "";
    public UserInfoData UserData;
    int GetUserInfoCount = 0;
    void GetUserInfo()
    {
        //还有进入正常模式的可能
        if (PlayerPrefs.HasKey("OtherChance") && PlayerPrefs.GetString("OtherChance") == "YES")
            PlayerPrefs.DeleteKey("Save_AP");
        //已经记录过用户信息 跳过检查
        if (PlayerPrefs.HasKey("OtherChance") && PlayerPrefs.GetString("OtherChance") == "NO")
        {
            GameReady();
            return;
        }

        //检查归因渠道信息
        //CheckAdjustNetwork();
        //获取用户信息
        string CheckUrl = BaseUrl + "/api/client/user/checkUser";
        NetWorkManager.GetInstance().HttpGet(CheckUrl,
        (data) =>
        {
            UserDataStr = data.downloadHandler.text;
            print("+++++ 获取用户数据 成功" + UserDataStr);
            UserRootData rootData = JsonMapper.ToObject<UserRootData>(UserDataStr);
            UserData = JsonMapper.ToObject<UserInfoData>(rootData.data);
            if (UserDataStr.Contains("apple")
            || UserDataStr.Contains("Apple")
            || UserDataStr.Contains("APPLE"))
                UserData.IsHaveApple = true;
            GameReady();
        }, () => { });
        Invoke(nameof(ReGetUserInfo), 1);
    }
    void ReGetUserInfo()
    {
        if (!ready)
        {
            GetUserInfoCount++;
            if (GetUserInfoCount < 10)
            {
                print("+++++ 获取用户数据失败 重试： " + GetUserInfoCount);
                GetUserInfo();
            }
            else
            {
                print("+++++ 获取用户数据 失败次数过多，放弃");
                GameReady();
            }
        }
    }
    public void InitSignInData()
    {
        List_SignInData.Clear();
        for (int i = 0; i < GameData.dailydatelist.Count; i++)
        {
            List<RewardData> list = new List<RewardData>();
            for (int j = 0; j < GameData.dailydatelist[i].Count; j++)
            {
                int num = GameData.dailydatelist[i][j].reward_num;
                //num *= (int)InitData.gold_group[0].multi;
                var data = new RewardData("Cash", num);
                list.Add(data);
            }
            List_SignInData.Add(list);
        }
    }
}

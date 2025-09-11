using System;
using System.Collections;
using System.Collections.Generic;
using LitJson;
using UnityEngine;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.Linq;

public enum LoginPlatform { Android, IOS }

/// <summary> 提现功能管理 </summary>
public class CashOutManager : MonoSingleton<CashOutManager>
{
    [Header("登录平台")]
    public LoginPlatform _LoginPlatform = LoginPlatform.Android;
    [Header("短剧后台的产品id")]
    public string AppInfo = "4";
    string WithdrawPlatform = "PAYPAL";
    string BaseUrl = "https://us.nicedramatv.com";
    [HideInInspector] public string Account;
    [HideInInspector] public CashOutResponseData Data;
    [HideInInspector] public long LeftTime; // 剩余时间
    [HideInInspector] public CashOutPanel _CashOutPanel;
    [HideInInspector] public CashOutEnter _CashOutEnter;
    [HideInInspector] public float Money;
    [HideInInspector] public bool Ready;
    float MinWithdrawCount; //最小提现金额
    int Event_1304Time;
    string ClientIP;
    string RealIP;
    [HideInInspector] public long StartTime;


    #region 游戏逻辑
    private void Start()
    {
        Account = SaveDataManager.GetString("CashOut_Account");
        Money = SaveDataManager.GetFloat("CashOut_Money");
    }

    private void OnApplicationPause(bool pauseStatus)
    {
        long Seconds = LeftTime / 10000000;
        if (pauseStatus && Seconds > 0)
        {
            string title = "Your reward is ready!";
            string info = $"All {NetInfoMgr.instance.CashOut_Data.MoneyName} have been converted,Please check your rewards!";
            NotificationManager.Instance.ClearNotification();
            NotificationManager.Instance.ScheduleNotification(title, info, (int)Seconds);
            for (int i = 0; i < 10; i++) // 10次延时 10800秒 3小时
                NotificationManager.Instance.ScheduleNotification(title, info, (int)Seconds + (i * 10800));
        }

        if (pauseStatus)
            ReportEvent(1005);
        else
            ReportEvent(1006);
    }

    void TimeCount() //计时
    {
        if (Data != null)
        {
            //转化时间倒计时
            long nowTime = System.DateTime.UtcNow.Ticks;
            LeftTime = Data.ConvertTime - nowTime;
            //倒计时结束 更新用户信息 
            if (LeftTime <= 0)
            {
                LeftTime = 0;
                if (_CashOutPanel != null && _CashOutPanel.gameObject.activeSelf)
                    _CashOutPanel?.UpdateUserInfo(); //因为界面需要显示加载动画所以此处由_CashOutPanel调用
                else
                    UpdateUserInfo();
            }
            //更新剩余时间ui 
            string timeStr = "";
            long Seconds = LeftTime / 10000000;
            if (Seconds <= 0)
                timeStr = "00:00:00";
            else
            {
                int hour = (int)(Seconds / 3600);
                int minute = (int)((Seconds - hour * 3600) / 60);
                int second = (int)(Seconds - hour * 3600 - minute * 60);
                timeStr = string.Format("{0:D2}:{1:D2}:{2:D2}", hour, minute, second);
            }
            _CashOutPanel?.UpdateTime(timeStr);
            _CashOutEnter?.UpdateTime(timeStr);


            //任务
            if (NetInfoMgr.instance.CashOut_Data != null && NetInfoMgr.instance.CashOut_Data.TaskList.Count > 0)
            {
                //记录第一次登录日期utc
                if (!PlayerPrefs.HasKey("CashOut_FirstLoginTime"))
                {
                    DateTime firstLaunchUTC = DateTime.UtcNow.Date;
                    PlayerPrefs.SetString("CashOut_FirstLoginTime", firstLaunchUTC.ToString());
                }
                // 判断是否进入新的一天
                bool isNewDay = false;
                if (PlayerPrefs.HasKey("CashOut_LastCheckDate"))
                {
                    if (DateTime.TryParse(PlayerPrefs.GetString("CashOut_LastCheckDate"), out DateTime lastDate))
                        isNewDay = !DateTime.UtcNow.Date.Equals(lastDate);
                }
                else
                    isNewDay = true;
                PlayerPrefs.SetString("CashOut_LastCheckDate", DateTime.UtcNow.Date.ToString());
                if (isNewDay)
                {
                    PlayerPrefs.SetInt("TaskEventReported", 0);
                    PlayerPrefs.SetFloat("CashOut_TaskValue", 0);
                    _CashOutPanel?.UpdateTask();
                }
                //计算今天距离第一次登录过了几天
                int Day = (DateTime.UtcNow.Date - DateTime.Parse(PlayerPrefs.GetString("CashOut_FirstLoginTime"))).Days;
                if (Day >= NetInfoMgr.instance.CashOut_Data.TaskList.Count) //一天一任务 天数超出任务数量显示默认任务
                    Data.TaskData = NetInfoMgr.instance.CashOut_Data.TaskList.FirstOrDefault(t => t.IsDefault);
                else
                    Data.TaskData = NetInfoMgr.instance.CashOut_Data.TaskList[Day];
                Data.TaskData.NowValue = PlayerPrefs.GetFloat("CashOut_TaskValue");
            }
        }
    }

    public void AddMoney(float Value)
    {
        Money += Value;
        SaveDataManager.SetFloat("CashOut_Money", Money);
        _CashOutPanel?.UpdateMoney();
        _CashOutEnter?.UpdateMoney();
    }

    public void WaitToSendEvent1304() //等待 发送关闭商店后行为1304事件
    {
        InvokeRepeating(nameof(Count1304Time), 0, 1);
    }
    void Count1304Time() //计时器
    {
        Event_1304Time++;
    }
    public void SendEvent1304() ////打点 关闭商店后行为
    {
        CancelInvoke(nameof(Count1304Time));
        if (Event_1304Time <= 0)
            return;
        PostEventScript.GetInstance().SendEvent("1304", Event_1304Time.ToString());
        Event_1304Time = 0;
    }

    void CashOutLog(string log, bool IsError = false, bool IsOk = false) //提现相关功能日志
    {
        if (IsError)
            Debug.LogError("<color=red><b>+++++   " + log + "</b></color>");
        else
        {
            if (IsOk)
                print("<color=cyan><b>+++++   " + log + "</b></color>");
            else
                print("<color=yellow><b>+++++   " + log + "</b></color>");
        }
    }

    // //测试
    // void Update()
    // {
    //     if (Input.GetKeyDown(KeyCode.Space))
    //     {
    //         _CashOutPanel?.MoneyToCashAnim();
    //         _CashOutEnter?.MoneyToCashAnim(true);
    //     }
    // }
    #endregion

    #region 短剧后台各类接口
    Dictionary<string, string> Headers() // 请求头
    {
#if UNITY_EDITOR //编译器不传设备信息
        return new Dictionary<string, string>
        {
            {"app-version", Application.version},
            {"lang", I2.Loc.LocalizationManager.CurrentLanguageCode},
            {"Authorization", SaveDataManager.GetString("CashOut_Token")},
            {"platform", WithdrawPlatform},
            {"os-version", ""},
            {"device-name", ""},
        };
#endif

        return new Dictionary<string, string>
        {
            {"app-version", Application.version},
            {"lang", I2.Loc.LocalizationManager.CurrentLanguageCode},
            {"Authorization", SaveDataManager.GetString("CashOut_Token")},
            {"platform", WithdrawPlatform},
            {"os-version", GetOperatingSystem()},
            {"device-name", SystemInfo.deviceName},
        };
    }
    string GetOperatingSystem()
    {
        string os = SystemInfo.operatingSystem;
        if (!string.IsNullOrEmpty(os))
        {
            if (Application.platform == RuntimePlatform.Android)
            {
                // 安卓格式示例: "Android OS 13 / API-33 (TQ2A.230305.008.C1/9619669)"
                // 提取OS版本和API等级并拼接为"13_33"
                var parts = os.Split(new[] { ' ', '/', '-' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 4)
                {
                    string osVersion = parts[2]; // 提取"13"
                    string apiLevel = parts[4];  // 提取"33"
                    os = $"{osVersion}_{apiLevel}";
                }
            }
            else if (Application.platform == RuntimePlatform.IPhonePlayer)
            {
                // iOS格式示例: "iPhone OS 15.3.1"
                // 提取版本号"15.3.1"
                var parts = os.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length >= 3)
                {
                    os = parts[2]; // 提取版本号部分
                }
            }
        }
        return os;
    }

    public void Login() // 登录
    {
        string Platform = "Editor";
        string Manufacturer = "Editor";
        string DeviceAdId = "";
        if (_LoginPlatform == LoginPlatform.Android)
        {
            Platform = "Android";
            DeviceAdId = SaveDataManager.GetString("gaid");
            if (Application.platform == RuntimePlatform.Android)
            {
                AndroidJavaClass aj = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
                AndroidJavaObject p = aj.GetStatic<AndroidJavaObject>("currentActivity");
                Manufacturer = p.CallStatic<string>("getManufacturer");
            }
        }
        else
        {
            Platform = "iOS";
            Manufacturer = "Apple";
            DeviceAdId = SaveDataManager.GetString("idfv");
        }
        StringBuilder uuidsb = new StringBuilder();
        uuidsb.Append(SystemInfo.deviceUniqueIdentifier);
#if UNITY_ANDROID || UNITY_EDITOR //安卓UUID存在不同应用相同ID的情况 用SystemInfo.deviceUniqueIdentifier+AppInfo 
        bool isNewPlayer = !PlayerPrefs.HasKey(CConfig.sv_IsNewPlayer + "Bool") || SaveDataManager.GetBool(CConfig.sv_IsNewPlayer);
        bool hasuuidAndAppid = SaveDataManager.GetBool("UuidAndAPPid");
        if (isNewPlayer || hasuuidAndAppid) //新老用户兼容
            uuidsb.Append(AppInfo);
#endif
        var loginRequest = new Request_Login
        {
            platform = Platform,
            bundle_id = Application.identifier,
            uuid = uuidsb.ToString(),
            device_ad_id = DeviceAdId,
            device_lang = CultureInfo.CurrentCulture.Name,
            model = SystemInfo.deviceModel,
            manufacturer = Manufacturer,
            screen_size = Screen.width + "x" + Screen.height,
            screen_pixel = Screen.currentResolution.width + "x" + Screen.currentResolution.height,
        };

        string jsonBody = JsonMapper.ToJson(loginRequest);
        string loginUrl = $"{BaseUrl}/login";
        CashOutLog($"请求登录  请求体: {jsonBody}", false);

        NetWorkManager.GetInstance().HttpPostJson(
            url: loginUrl,
            jsonData: jsonBody,
            success: (result) =>
            {
                try
                {
                    var response = JsonMapper.ToObject<Response_User>(result.downloadHandler.text);
                    if (response.code == 0)
                    {
                        CashOutLog("登录成功 数据： " + result.downloadHandler.text, false, true);
#if UNITY_ANDROID || UNITY_EDITOR //安卓UUID 新老用户兼容
                        bool isNewPlayer = !PlayerPrefs.HasKey(CConfig.sv_IsNewPlayer + "Bool") || SaveDataManager.GetBool(CConfig.sv_IsNewPlayer);
                        if (isNewPlayer)
                            SaveDataManager.SetBool("UuidAndAPPid", true);
#endif

                        //刷新token 获取提现规则
                        SaveDataManager.SetString("CashOut_Token", response.data.token);
                        GetWithdrawRule();
                        //整理数据
                        Data = new CashOutResponseData();
                        Data.UserID = response.data.id.ToString();
                        Data.Cash = float.Parse(response.data.cash, CultureInfo.InvariantCulture);
                        DateTime ConvertTime = DateTime.Parse(response.data.convert_time);
                        if (PlayerPrefs.HasKey("CashOut_ConvertTime"))
                            Data.ConvertTime = long.Parse(SaveDataManager.GetString("CashOut_ConvertTime"));
                        if (Data.ConvertTime < ConvertTime.Ticks)
                        {
                            Money = 0;
                            SaveDataManager.SetFloat("CashOut_Money", Money);
                        }
                        Data.ConvertTime = ConvertTime.Ticks;
                        SaveDataManager.SetString("CashOut_ConvertTime", Data.ConvertTime.ToString());
                        InvokeRepeating(nameof(TimeCount), 1, 1);

                        // 更新UI
                        _CashOutPanel?.UpdateMoney();
                        _CashOutEnter?.UpdateMoney();

                        Ready = true;

                        //上报ip
                        GetClientIP();
                        GetRealIP_Step1();

                        //游戏启动打点 需要先登录成功才能打点 这里的时间戳参数由ReportEvent方法内部特殊处理
                        ReportEvent(1000);

                        //上报设备信息
                        ReportEvent_DeviceInfo();
                    }
                    else
                    {
                        CashOutLog($"登录失败: {response.msg}", true);
                        CashOutLog("如果报错是app not found，有可能是登陆平台选错了，有可能是包名和短剧后台ID对不上", true);
                        ToastManager.GetInstance().ShowToast("Login fail :" + response.msg);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析登录响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("登录请求失败", true);
                ToastManager.GetInstance().ShowToast("Login fail");
                Ready = false;
            },
            timeout: 3f,
            headers: Headers()
        );
    }

    public void UpdateUserInfo() // 更新用户信息
    {
        CancelInvoke(nameof(TimeCount));
        string url = $"{BaseUrl}/user";
        NetWorkManager.GetInstance().HttpGet(
            url: url,
            success: (result) =>
            {
                try
                {
                    var response = JsonMapper.ToObject<Response_User>(result.downloadHandler.text);
                    if (response.code == 0)
                    {
                        CashOutLog("用户信息数据： " + result.downloadHandler.text, false, true);
                        string Event_Money = Money.ToString();

                        double OldCash = SaveDataManager.GetDouble("CashOut_Cash");
                        float NewCash = float.Parse(response.data.cash, CultureInfo.InvariantCulture);
                        DateTime ConvertTime = DateTime.Parse(response.data.convert_time);
                        //当前时间小于后台时间 代表新一轮转换开始 清空Money
                        if (Data.ConvertTime < ConvertTime.Ticks)
                        {
                            Money = 0;
                            SaveDataManager.SetFloat("CashOut_Money", Money);
                        }
                        if (Money == 0)
                        {
                            //如果转换后Cash增加 播飞钱动画 否则播进度归零动画
                            bool IsIconFly = NewCash > 0 && NewCash > OldCash;
                            _CashOutPanel?.MoneyToCashAnim();
                            _CashOutEnter?.MoneyToCashAnim(IsIconFly);

                            //打点 如果钱转化了 上报转化信息
                            if (IsIconFly)
                                PostEventScript.GetInstance().SendEvent("1302", Event_Money, NewCash.ToString());
                        }
                        else
                        {
                            Data.Cash = NewCash;
                            _CashOutPanel?.UpdateMoney();
                            _CashOutEnter?.UpdateMoney();
                        }
                        Data.ConvertTime = ConvertTime.Ticks;
                        SaveDataManager.SetString("CashOut_ConvertTime", Data.ConvertTime.ToString());
                        Data.Cash = NewCash;

                        InvokeRepeating(nameof(TimeCount), 0, 1);
                        SaveDataManager.SetDouble("CashOut_Cash", Data.Cash);
                        _CashOutPanel?.CloseLoading_UpdateUI();
                    }
                    else
                    {
                        CashOutLog($"获取用户信息失败: {response.msg}", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析用户信息响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("获取用户信息请求失败", true);
            },
            timeout: 3f,
            headers: Headers()
        );
    }

    public void GetWithdrawRule() // 获取提现规则（规则获取不到不影响流程）
    {
        string url = $"{BaseUrl}/withdraw/rule?platform={WithdrawPlatform}";

        NetWorkManager.GetInstance().HttpGet(
            url: url,
            success: (result) =>
            {
                try
                {
                    var response = JsonMapper.ToObject<Response_WithdrawRule>(result.downloadHandler.text);
                    if (response.code == 0) // 成功状态码
                    {
                        CashOutLog("提现规则数据： " + result.downloadHandler.text, false, true);
                        MinWithdrawCount = float.Parse(response.data.min_amount, CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        CashOutLog($"提现规则获取失败: {response.msg}", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析提现规则响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("获取最小提现金额请求失败", true);
            },
            timeout: 3f,
            headers: Headers()
        );
    }

    public void Withdraw() // 提现
    {
        if (Data.Cash < MinWithdrawCount)
        {
            ToastManager.GetInstance().ShowToast($"Minimum withdrawal amount {MinWithdrawCount}");
            _CashOutPanel?.CloseLoading_Withdraw(true);
            string Amount = Data.Cash.ToString("F2", System.Globalization.CultureInfo.InvariantCulture);
            SendWithdrawEvent(Amount, false);
            return;
        }

        var withdrawRequest = new RequestData_Withdraw
        {
            //测试
            //amount = MinWithdrawCount.ToString("F2",System.Globalization.CultureInfo.InvariantCulture),

            amount = Data.Cash.ToString("F2", System.Globalization.CultureInfo.InvariantCulture),
            receiver_value = Account
        };

        string jsonBody = JsonMapper.ToJson(withdrawRequest);
        string url = $"{BaseUrl}/withdraw";

        NetWorkManager.GetInstance().HttpPostJson(
            url: url,
            jsonData: jsonBody,
            success: (result) =>
            {
                try
                {
                    var response = JsonMapper.ToObject<Response_Withdraw>(result.downloadHandler.text);
                    if (response.code == 0)
                    {
                        CashOutLog("提现数据： " + result.downloadHandler.text, false, true);
                        _CashOutPanel?.CloseLoading_Withdraw(true);
                        _CashOutPanel?.UpdateUserInfo();

                        SendWithdrawEvent(withdrawRequest.amount, true);
                    }
                    else
                    {
                        CashOutLog($"提现失败: {response.msg}", true);
                        _CashOutPanel?.CloseLoading_Withdraw();

                        SendWithdrawEvent(withdrawRequest.amount, false);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析提现响应数据失败: {e.Message}", true);
                    _CashOutPanel?.CloseLoading_Withdraw();
                    ToastManager.GetInstance().ShowToast("Withdraw fail :" + e.Message);

                    SendWithdrawEvent(withdrawRequest.amount, false);
                }
            },
            fail: () =>
            {
                CashOutLog("提现请求失败", true);
                _CashOutPanel?.CloseLoading_Withdraw();

                SendWithdrawEvent(withdrawRequest.amount, false);
            },
            timeout: 3f,
            headers: Headers()
        );
    }
    void SendWithdrawEvent(string Event_Cash, bool IsSuccess) //打点 提现成功或失败
    {
        PostEventScript.GetInstance().SendEvent("1303", Event_Cash, IsSuccess ? "1" : "0");
    }

    public void GetWithdrawRecord() // 获取提现记录
    {
        string url = $"{BaseUrl}/withdraw";
        NetWorkManager.GetInstance().HttpGet(
            url: url,
            success: (result) =>
            {
                try
                {
                    var response = JsonMapper.ToObject<Response_WithdrawRecord>(result.downloadHandler.text);
                    if (response.code == 0)
                    {
                        CashOutLog("提现记录数据： " + result.downloadHandler.text, false, true);
                        Data.Record = response.data.data;
                        _CashOutPanel?.CloseLoading_Record();
                        _CashOutPanel?.UpdateRecord();
                    }
                    else
                    {
                        CashOutLog($"提现记录获取失败: {response.msg}", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析提现记录响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("提现记录请求失败", true);
            },
            timeout: 3f,
            headers: Headers()
        );
    }

    public void ReportEcpm(MaxSdkBase.AdInfo info, string RequestID, string AdFormat) // 上报ecpm
    {
        return; //暂时不需要上报ecpm

        if (Application.isEditor)
        {
            CashOutLog("假装上报ecpm，当前为编辑器模式 RequestID： " + RequestID);
            return;
        }
        string url = $"{BaseUrl}/ecpm";
        RequestData_ReportEcpm requestData = new RequestData_ReportEcpm();
        requestData.type = AdFormat;
        requestData.request_id = RequestID;
        requestData.amount = info.Revenue.ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
        requestData.vendor = info.NetworkName;
        requestData.placement_id = info.Placement;
        requestData.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        requestData.signature = EcpmSignatureMD5(requestData.request_id, requestData.amount, Data.UserID, requestData.timestamp);
        string jsonBody = JsonMapper.ToJson(requestData);
        CashOutLog($"上报ecpmURL: {url}  请求体: {jsonBody}", false);

        NetWorkManager.GetInstance().HttpPostJson(
            url: url,
            jsonData: jsonBody,
            success: (result) =>
            {
                CashOutLog("上报ecpm数据： " + result.downloadHandler.text, false);
                try
                {
                    var response = JsonMapper.ToObject<Response_Ecpm>(result.downloadHandler.text);
                    if (response.code == 0) // 成功状态码
                    {
                        CashOutLog("上报ecpm成功", false);
                    }
                    else
                    {
                        CashOutLog($"上报ecpm失败: {response.msg}", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析上报ecpm响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("上报ecpm请求失败", true);
            },
            timeout: 3f,
            headers: Headers()
        );
    }
    public string EcpmRequestID() // 获取上报ecpm的request_id 广告加载时生成
    {
        string uuid = Guid.NewGuid().ToString();
        string formattedUuid = uuid.ToLowerInvariant().Replace("-", "");
        return formattedUuid;
    }
    string EcpmSignatureMD5(string requestId, string amount, string userId, long timestamp) // 上报ecpm生成签名
    {
        string Input = $"{requestId}-{amount}-{userId}-{timestamp}";
        using (MD5 md5 = MD5.Create())
        {
            byte[] inputBytes = Encoding.UTF8.GetBytes(Input);
            byte[] hashBytes = md5.ComputeHash(inputBytes);
            return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
        }
    }

    public void ReportAdjustID() // 上报adjust_id
    {
        string url = $"{BaseUrl}/user/ad";
        RequestData_ReportAdjustID requestData = new RequestData_ReportAdjustID();
        requestData.id = AdjustInitManager.Instance.GetAdjustAdid();
        string jsonBody = JsonMapper.ToJson(requestData);
        CashOutLog($"上报adjust_idURL: {url}  请求体: {jsonBody}", false);
        NetWorkManager.GetInstance().HttpPostJson(
            url: url,
            jsonData: jsonBody,
            success: (result) =>
            {
                CashOutLog("上报adjust_id数据： " + result.downloadHandler.text, false);
                try
                {
                    var response = JsonMapper.ToObject<BaseResponse>(result.downloadHandler.text);
                    if (response.code == 0) // 成功状态码
                    {
                        CashOutLog("上报adjust_id成功", false);
                    }
                    else
                    {
                        CashOutLog($"上报adjust_id失败: {response.msg}", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析上报adjust_id响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("上报adjust_id请求失败", true);
            },
            timeout: 3f,
            headers: Headers()
        );
    }

    void GetClientIP() // 获取客户端IP
    {
        string url = "http://ip-api.com/json/?key=NN3ExblXQt2Esoy";
        NetWorkManager.GetInstance().HttpGet(
            url: url,
            success: (result) =>
            {
                //CashOutLog("获取客户端IP数据： " + result.downloadHandler.text, false);
                try
                {
                    string json = result.downloadHandler.text;
                    JsonData data = JsonMapper.ToObject(json);
                    if (data.ContainsKey("query"))
                    {
                        ClientIP = data["query"].ToString();
                        ReportIDs();
                    }
                    else
                    {
                        CashOutLog("未找到IP地址", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析获取客户端IP响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("获取客户端IP请求失败", true);
            },
            timeout: 3f,
            headers: null
        );
    }
    void GetRealIP_Step1() // 获取真实IP网址
    {
        string url = "https://nstool.netease.com/";
        NetWorkManager.GetInstance().HttpGet(
            url: url,
            success: (result) =>
            {
                //CashOutLog("获取真实IP网址： " + result.downloadHandler.text, false);
                try
                {
                    // 使用正则表达式匹配iframe的src属性
                    string ipPattern = @"(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)\.(?:25[0-5]|2[0-4][0-9]|[01]?[0-9][0-9]?)";
                    var match = System.Text.RegularExpressions.Regex.Match(result.downloadHandler.text, ipPattern);
                    if (match.Success)
                    {
                        GetRealIP_Step2(match.Groups[1].Value);
                        //RealIP = match.Groups[1].Value;
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析获取真实IP网址响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("获取真实IP网址请求失败", true);
            },
            timeout: 3f,
            headers: null
        );
    }
    void GetRealIP_Step2(string url) // 获取真实IP
    {
        NetWorkManager.GetInstance().HttpGet(
           url: url,
           success: (result) =>
           {
               //CashOutLog("获取真实IP数据： " + result.downloadHandler.text, false);
               try
               {
                   var match = System.Text.RegularExpressions.Regex.Match(result.downloadHandler.text, @"IP地址信息:\s*(\d{1,3}\.\d{1,3}\.\d{1,3}\.\d{1,3})", System.Text.RegularExpressions.RegexOptions.IgnoreCase);
                   if (match.Success)
                   {
                       RealIP = match.Groups[1].Value;
                       ReportIDs();
                   }
               }
               catch (Exception e)
               {
                   CashOutLog($"解析获取真实IP响应数据失败: {e.Message}", true);
               }
           },
           fail: () =>
           {
               CashOutLog("获取真实IP请求失败", true);
           },
           timeout: 3f,
           headers: null
       );
    }
    void ReportIDs() // 上报各种ID
    {
        CashOutLog("上报ID  客户端Ip：" + ClientIP + "  真实Ip：" + RealIP);
        string url = $"{BaseUrl}/user/meta";
        NetWorkManager.GetInstance().HttpPostJson(
            url: url,
            jsonData: GetReportIDsBody(),
            success: (result) =>
            {
                try
                {
                    var response = JsonMapper.ToObject<BaseResponse>(result.downloadHandler.text);
                    if (response.code == 0) // 成功状态码
                    {
                        CashOutLog("上报ID成功 数据： " + result.downloadHandler.text, false, true);
                    }
                    else
                    {
                        CashOutLog($"上报各种ID失败: {response.msg}", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析上报各种ID响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("上报各种ID请求失败", true);
            },
            timeout: 3f,
            headers: Headers()
        );
    }
    string GetReportIDsBody() // 获取上报各种ID的请求体(处理某个ip为空的情况)
    {
        if (string.IsNullOrEmpty(ClientIP))
            return JsonMapper.ToJson(new { REAL_IP = RealIP });
        if (string.IsNullOrEmpty(RealIP))
            return JsonMapper.ToJson(new { CLIENT_IP = ClientIP });
        return JsonMapper.ToJson(new { CLIENT_IP = ClientIP, REAL_IP = RealIP });
    }

    public void ReportEvent(int type, string string_0 = null, string string_1 = null, int? big_int_0 = null) // 上报事件
    {
        if (string.IsNullOrEmpty(SaveDataManager.GetString("CashOut_Token")))
        {
            CashOutLog($"没Token不上报事件{type}", true);
            return;
        }

        RequestData_ReportEvent EventRequest = new RequestData_ReportEvent();
        EventRequest.network = GetNetworkInt();
        EventRequest.time_zone = GetTimeZone();
        EventRequest.events = new List<RequestData_ReportEvent_Event>();
        RequestData_ReportEvent_Event Event = new RequestData_ReportEvent_Event();
        Event.type = type;
        Event.timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); //毫秒时间戳
        if (type == 1000) //游戏启动时间戳 由于要先等登录成功获取token 所以特殊处理
            Event.timestamp = StartTime;
        Event.string_0 = string_0;
        Event.string_1 = string_1;
        Event.big_int_0 = big_int_0;
        EventRequest.events.Add(Event);

        string url = $"{BaseUrl}/event";
        CashOutLog($"上报事件{type}  请求体: {JsonMapper.ToJson(EventRequest)}", false);
        NetWorkManager.GetInstance().HttpPostJson(
            url: url,
            jsonData: JsonMapper.ToJson(EventRequest),
            success: (result) =>
            {
                //CashOutLog("上报事件数据： " + result.downloadHandler.text, false);
                try
                {
                    var response = JsonMapper.ToObject<BaseResponse>(result.downloadHandler.text);
                    if (response.code == 0) // 成功状态码
                    {
                        CashOutLog($"上报事件{type}成功", false, true);
                    }
                    else
                    {
                        CashOutLog($"上报事件{type}失败: {response.msg}", true);
                    }
                }
                catch (Exception e)
                {
                    CashOutLog($"解析上报事件响应数据失败: {e.Message}", true);
                }
            },
            fail: () =>
            {
                CashOutLog("上报事件请求失败", true);
            },
            timeout: 3f,
            headers: Headers()
        );
    }
    int GetNetworkInt() //根据网络类型获取对应的int值 
    {
        //安卓调原生方法 获取更详细的网络类型
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass aj = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject p = aj.GetStatic<AndroidJavaObject>("currentActivity");
            return p.CallStatic<int>("getNetwork");
        }

        //苹果端暂时这样处理
        NetworkReachability reachability = Application.internetReachability;
        if (reachability == NetworkReachability.ReachableViaLocalAreaNetwork)
            return 1; // WIFI
        else
            return 0; // 其他网络
    }
    int GetTimeZone() //获取当前时区相对于UTC0时区的差值，单位：秒
    {
        // 获取当前本地时间
        DateTime localTime = DateTime.Now;
        // 获取当前时间对应的UTC时间
        DateTime utcTime = localTime.ToUniversalTime();
        // 计算时间差（本地时间 - UTC时间）
        TimeSpan offset = localTime - utcTime;
        // 将时间差转换为秒
        return (int)offset.TotalSeconds;
    }
    public void ReportEvent_LoadingTime() //上报loading时间
    {
        long LoadingTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - StartTime;
        ReportEvent(1004, null, null, (int)LoadingTime);
    }
    void ReportEvent_DeviceInfo() //上报设备信息
    {
        //目前只有安卓上报
        if (Application.platform == RuntimePlatform.Android)
        {
            AndroidJavaClass aj = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
            AndroidJavaObject p = aj.GetStatic<AndroidJavaObject>("currentActivity");
            bool isVpn = p.CallStatic<bool>("isVpn");
            if (isVpn)
                ReportEvent(1007);
            bool isSimulator = p.CallStatic<bool>("isSimulator");
            if (isSimulator)
                ReportEvent(1008);
            bool isRoot = p.CallStatic<bool>("isRoot");
            if (isRoot)
                ReportEvent(1009);
            bool isDeveloper = p.CallStatic<bool>("isDeveloper");
            if (isDeveloper)
                ReportEvent(1010);
            bool isUsb = p.CallStatic<bool>("isUsb");
            if (isUsb)
                ReportEvent(1011);
            bool isSimCard = p.CallStatic<bool>("isSimcard");
            ReportEvent(1012, null, null, isSimCard ? 1 : 0);
        }
    }

    public void AddTaskValue(string Name, float Value) //增加任务值
    {
        if (Data.TaskData != null && Data.TaskData.Name == Name)
        {
            float OldValue = PlayerPrefs.GetFloat("CashOut_TaskValue");
            float NewValue = OldValue + Value;
            PlayerPrefs.SetFloat("CashOut_TaskValue", NewValue);
            Data.TaskData.NowValue = NewValue;
            _CashOutPanel?.UpdateTask();
            if (PlayerPrefs.GetInt("TaskEventReported") == 0 && NewValue >= Data.TaskData.Target)
            {
                PostEventScript.GetInstance().SendEvent("1305", Name, NewValue.ToString(), Data.TaskData.IsDefault.ToString());
                PlayerPrefs.SetInt("TaskEventReported", 1);
            }
        }
    }

    #endregion

}

#region 接口相关各类请求和响应数据结构

// 各种接口数据汇总 UI常用数据
[System.Serializable]
public class CashOutResponseData
{
    public string UserID;
    public long ConvertTime;     // Money转Cash时间戳
    public float Cash;           // 当前Cash
    public List<WithdrawRecordItem> Record; // 提现记录
    public CashOut_TaskData TaskData; //今日任务数据
}

// 基础响应模型
[System.Serializable]
public class BaseResponse
{
    public int code;
    public string msg;
}

// 用户相关数据模型
[System.Serializable]
public class Request_Login
{
    public string platform;
    public string bundle_id;
    public string uuid;
    public string device_ad_id;
    public string device_lang;
    public string model;
    public string manufacturer;
    public string screen_size;
    public string screen_pixel;
}
[System.Serializable]
public class Response_User : BaseResponse
{
    public UserData data;
}
[System.Serializable]
public class UserData
{
    public long id;
    public string nick_name;
    public string avatar;
    public string crt_time;
    public string coin;
    public string cash;
    public string channel;
    public string convert_time;
    public string token;
    public bool is_regist;
}

// 提现相关数据模型
[System.Serializable]
public class RequestData_Withdraw
{
    public string type = "TRANSFER";
    public string platform = "PAYPAL";
    public string amount;  // 改为string类型
    public string receiver_type = "EMAIL";
    public string receiver_value;
}
[System.Serializable]
public class Response_Withdraw : BaseResponse
{
    public int data;
}

// 提现规则数据模型
[System.Serializable]
public class Response_WithdrawRule : BaseResponse
{
    public WithdrawRuleData data;
}
[System.Serializable]
public class WithdrawRuleData
{
    public string min_amount;      // 最小提现金额
    public int day_count;        // 每日提现次数限制
    public string day_max_amount;  // 每日最大提现金额
    public int count;           // 总提现次数
    public string amount;         // 总提现金额
}

// 提现记录数据模型
[System.Serializable]
public class Response_WithdrawRecord : BaseResponse
{
    public WithdrawRecordData data;
}
[System.Serializable]
public class WithdrawRecordData
{
    public long count;
    public List<WithdrawRecordItem> data;
}
[System.Serializable]
public class WithdrawRecordItem
{
    public long id;
    public string amount;
    public string receiver_type;
    public string receiver_value;
    public string status;
    public string crt_time;
}

// ECPM相关数据模型
[System.Serializable]
public class RequestData_ReportEcpm
{
    public string type;
    public string request_id;
    public string amount;
    public string vendor;
    public string placement_id;
    public long timestamp;
    public string signature;
}
[System.Serializable]
public class Response_Ecpm : BaseResponse
{
    public int data;
}

// Adjust相关数据模型
[System.Serializable]
public class RequestData_ReportAdjustID
{
    public string id;
}

//打点相关数据模型
[System.Serializable]
public class RequestData_ReportEvent
{
    public int network;
    public int time_zone;
    public List<RequestData_ReportEvent_Event> events;
}
[System.Serializable]
public class RequestData_ReportEvent_Event
{
    public int type;
    public long timestamp;
    public string string_0;
    public string string_1;
    public int? big_int_0;
}

#endregion

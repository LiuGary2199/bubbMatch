using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using DG.Tweening;
using System.Globalization;
using System.Runtime.InteropServices;

/// <summary> 提现面板 </summary>
public class CashOutPanel : BaseUIForms
{
    public Button CloseBtn; // 关闭按钮
    public GameObject UpdateUI_Loading; // 整体页面的加载
    public Text MoneyText; // 余额显示
    public Text CashText; // 提现金额显示
    public Text LeftTimeText; // 剩余时间显示
    public Button CashOutBtn; // 提现按钮
    public GameObject CashOutBtn_Loading; // 提现按钮的加载
    public GameObject CashOutBtn_Task; //未完成任务时 提现按钮
    public Text CashOutBtn_Task_Text; //未完成任务时 提现按钮文字
    public Text AccountText; // 账户显示
    public Text IDText; // ID显示
    public Image MaxMoneyFill;
    public Text MaxMoneyText;
    public Text DesText; // 说明文字    

    [Header("说明")]
    public GameObject InfoPanel; // 提现说明面板
    public Text InfoText; // 提现说明文字
    public Button InfoPanel_OpenBtn; // 提现说明面板打开按钮
    public Button InfoPanel_CloseBtn; // 提现说明面板关闭按钮
    public Button ContactBtn; // 联系客服按钮
    string MoneyName;

    [Header("记录")]
    public GameObject RecordPanel; // 提现记录面板
    public Button RecordPanel_OpenBtn; // 提现记录面板打开按钮
    public Button RecordPanel_CloseBtn; // 提现记录面板关闭按钮
    public GameObject RecordPanel_Loading; // 提现记录的加载
    public ScrollRect RecordScrollRect;
    public Transform[] RecordItems; // 提现记录项
    public Text PageText; // 页码显示
    public Button PageUpBtn; // 上一页按钮
    public Button PageDownBtn; // 下一页按钮
    public GameObject NoRecordTip; // 没有记录提示
    public Sprite[] StateSprites;
    public Color[] StateTextColors;
    int Page = 0;
    int PageCount;

    [Header("账户")]
    public GameObject AccountPanel; // 账户面板
    public Button AccountPanel_OpenBtn; // 账户面板打开按钮
    public Button AccountPanel_CloseBtn; // 账户面板关闭按钮
    public InputField AccountInput; // 账户输入框
    public InputField ConfirmAccountInput; // 确认账户输入框
    public Text AccountErrorText; // 账户输入错误提示
    public Button ConfirmBtn; // 确认按钮

    [Header("确认账户")]
    public GameObject ConfirmAccountPanel; // 确认账户面板
    public Button ConfirmAccountPanel_CloseBtn; // 确认账户关闭按钮
    public Button PolicyBtn; // 政策按钮
    public Toggle PolicyToggle; // 政策勾选框
    public Button SbmitBtn; // 确认按钮
    public GameObject SbmitBtn_Loading; // 确认按钮的加载
    public Text Confirm_AccountText; // 账户显示
    public Button EditBtn; // 编辑按钮
#if UNITY_IOS
    [DllImport("__Internal")] // 打开外部链接
    internal extern static void openUrl(string url);
#endif

    [Header("新手引导")]
    public GameObject GuidePanel; // 新手引导面板
    public Text GuideTitleText; // 新手引导标题
    public Text GuideText; // 新手引导内容
    public Transform[] GuideItems1;
    public Transform[] GuideItems2;
    public GameObject GuideStep2Go; //第二步引导特殊处理
    public GameObject[] GuideStepPoints;
    public Button GuideNextBtn; // 下一步按钮
    int GuideStep;



    private void Start()
    {
        CashOutManager.GetInstance()._CashOutPanel = this;
        CloseBtn.onClick.AddListener(() => { CloseUIForm(nameof(CashOutPanel)); });
        CashOutBtn.onClick.AddListener(() => { OnCashOutBtn(); });
        MoneyName = NetInfoMgr.instance.CashOut_Data.MoneyName;
        InfoText.text = NetInfoMgr.instance.CashOut_Data.Description;
        InfoPanel_OpenBtn.onClick.AddListener(() => OpenPanel(InfoPanel));
        InfoPanel_CloseBtn.onClick.AddListener(() => ClosePanel(InfoPanel));
        ContactBtn.GetComponent<Text>().text = NetInfoMgr.instance.BaseUrl;
        ContactBtn.onClick.AddListener(() =>
        {
            string url = NetInfoMgr.instance.BaseUrl;
#if UNITY_ANDROID || UNITY_EDITOR
            Application.OpenURL(url);
#elif UNITY_IOS
            openUrl(url);
#endif
        });
        RecordPanel_OpenBtn.onClick.AddListener(() =>
        {
            OpenPanel(RecordPanel);
            StartLoadingAnim(RecordPanel_Loading);
            CashOutManager.GetInstance().GetWithdrawRecord();
        });
        RecordPanel_CloseBtn.onClick.AddListener(() => ClosePanel(RecordPanel));
        PageUpBtn.onClick.AddListener(() => PageUpBtnClick());
        PageDownBtn.onClick.AddListener(() => PageDownBtnClick());
        AccountPanel_OpenBtn.onClick.AddListener(() => OpenPanel(AccountPanel));
        AccountPanel_CloseBtn.onClick.AddListener(() => ClosePanel(AccountPanel));
        ConfirmAccountPanel_CloseBtn.onClick.AddListener(() => ClosePanel(ConfirmAccountPanel));
        EditBtn.onClick.AddListener(() =>
        {
            ClosePanel(ConfirmAccountPanel);
            OpenPanel(AccountPanel);
        });
        AccountInput.onEndEdit.AddListener((Info) => OnInputEnd());
        ConfirmAccountInput.onEndEdit.AddListener((Info) => OnInputEnd());
        AccountErrorText.text = "";
        AccountText.text = CashOutManager.GetInstance().Account;
        Confirm_AccountText.text = CashOutManager.GetInstance().Account;
        if (string.IsNullOrEmpty(AccountText.text))
        {
            AccountText.text = "Account";
            Confirm_AccountText.text = "Account";
        }
        IDText.text = "UID:" + CashOutManager.GetInstance().Data.UserID;
        PolicyBtn.onClick.AddListener(() =>
        {
            string url = NetInfoMgr.instance.BaseUrl + "/privacy_policy.html";
#if UNITY_ANDROID || UNITY_EDITOR
            Application.OpenURL(url);
#elif UNITY_IOS
        openUrl(url);
#endif
        });
        PolicyToggle.isOn = SaveDataManager.GetBool("CashOut_PolicyAgree");
        ConfirmBtn.onClick.AddListener(() => OnConfimBtn());
        SbmitBtn.onClick.AddListener(() => { OnSbmitBtn(); });
        UpdateMoney();
    }

    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
        UpdateUserInfo();
    }

    public override void Hidding()
    {
        base.Hidding();
        GameEvents.TutorialClickAction?.Invoke();
        CashOutManager.GetInstance().WaitToSendEvent1304();
    }

    public void UpdateUserInfo() //拉取用户信息 显示加载界面
    {
        //StartLoadingAnim(UpdateUI_Loading);
        CashOutManager.GetInstance().UpdateUserInfo();
    }
    public void UpdateTime(string timeStr) //更新剩余时间
    {
        LeftTimeText.text = timeStr;
    }

    void OnInputEnd() //输入框数据校验
    {
        string Account = AccountInput.text;
        string ConfirmAccount = ConfirmAccountInput.text;
        if (string.IsNullOrEmpty(Account))
            AccountErrorText.text = "* Account cannot be empty";
        else if (!System.Text.RegularExpressions.Regex.IsMatch(Account, @"^([a-zA-Z0-9_\-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([a-zA-Z0-9\-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$"))
            AccountErrorText.text = "* Account format error";
        else if (!string.IsNullOrEmpty(ConfirmAccount) && Account != ConfirmAccount)
            AccountErrorText.text = "* Accounts don't match";
        else
            AccountErrorText.text = "";

        if (!string.IsNullOrEmpty(AccountErrorText.text))
        {
            AccountErrorText.transform.DOKill();
            AccountErrorText.transform.localPosition = new Vector2(0, AccountErrorText.transform.localPosition.y);
            AccountErrorText.transform.DOLocalMoveX(-30, .1f).SetEase(Ease.InOutBack);
            AccountErrorText.transform.DOLocalMoveX(50, .2f).SetDelay(.1f).SetEase(Ease.InOutBack);
            AccountErrorText.transform.DOLocalMoveX(0, .1f).SetDelay(.3f).SetEase(Ease.InOutBack);
        }
    }
    void OnCashOutBtn() //点击提现 没账户则打开填账户面板 有账户打开确认面板
    {
        if (CashOutManager.GetInstance().Data == null)
            return;
        if (string.IsNullOrEmpty(CashOutManager.GetInstance().Account))
        {
            OpenPanel(AccountPanel);
            return;
        }
        OpenPanel(ConfirmAccountPanel);
    }
    void OnConfimBtn() //确认修改账户
    {
        string Account = AccountInput.text;
        string ConfirmAccount = ConfirmAccountInput.text;
        if (string.IsNullOrEmpty(Account) || string.IsNullOrEmpty(ConfirmAccount))
        {
            ToastManager.GetInstance().ShowToast("Account cannot be empty");
            return;
        }
        if (!string.IsNullOrEmpty(AccountErrorText.text))
        {
            ToastManager.GetInstance().ShowToast(AccountErrorText.text);
            return;
        }

        CashOutManager.GetInstance().Account = AccountInput.text;
        SaveDataManager.SetString("CashOut_Account", AccountInput.text);
        AccountText.text = AccountInput.text;
        Confirm_AccountText.text = AccountInput.text;
        ClosePanel(AccountPanel);
        AccountText.transform.DOKill();
        AccountText.transform.localScale = Vector2.one;
        AccountText.transform.DOScale(Vector2.one * 1.4f, .3f).SetLoops(2, LoopType.Yoyo);
    }
    void OnEditBtn() //重填账户
    {
        ClosePanel(ConfirmAccountPanel);
        OpenPanel(AccountPanel);
    }
    void OnSbmitBtn() //确认是否同意协议 同意后调用提现接口
    {
        if (!PolicyToggle.isOn)
        {
            ToastManager.GetInstance().ShowToast("Please agree to the policy");
            return;
        }
        SaveDataManager.SetBool("CashOut_PolicyAgree", true);
        StartLoadingAnim(CashOutBtn_Loading);
        StartLoadingAnim(SbmitBtn_Loading);
        CashOutManager.GetInstance().Withdraw();
    }

    public void UpdateMoney()
    {
        if (!gameObject.activeSelf)
            return;
        //文字滚动动画
        float MoneyStart = float.Parse(MoneyText.text, CultureInfo.CurrentCulture);
        float MoneyEnd = CashOutManager.GetInstance().Money;
        DOTween.To(() => MoneyStart, x => MoneyText.text = x.ToString("F2"), MoneyEnd, 1f);
        CashText.text = CashOutManager.GetInstance().Data.Cash.ToString("F2");
        //进度条动画
        float MaxMoney = float.Parse(NetInfoMgr.instance.CashOut_Data.convert_goal, CultureInfo.CurrentCulture);
        DOTween.To(() => MaxMoneyFill.fillAmount, x => MaxMoneyFill.fillAmount = x, Mathf.Min(1, MoneyEnd / MaxMoney), 1f);
        DOTween.To(() => MoneyStart, x => MaxMoneyText.text = x.ToString("F2") + "/" + MaxMoney, MoneyEnd, 1f).OnComplete(() =>
        {
            if (MoneyEnd >= MaxMoney)
            {
                MaxMoneyText.text = "ACHIEVED";
                DesText.text = $"The more {MoneyName} you collect,the more rewards will be converted!";
            }
            else
                DesText.text = $"If the collected {MoneyName} reach the goal before the timer ends, the {MoneyName} will be converted into redeemable rewards.";
        });
    }
    public void MoneyToCashAnim()
    {
        if (!gameObject.activeSelf)
            return;
        //文字滚动动画
        float MoneyStart = float.Parse(MoneyText.text, CultureInfo.CurrentCulture);
        float CashOutStart = float.Parse(CashText.text, CultureInfo.CurrentCulture);
        float CashOutEnd = CashOutManager.GetInstance().Data.Cash;
        DOTween.To(() => MoneyStart, x => MoneyText.text = x.ToString("F2"), 0, 1f);
        DOTween.To(() => CashOutStart, x => CashText.text = x.ToString("F2"), CashOutEnd, 1f).SetDelay(.7f);
        //进度条动画
        float MaxMoney = float.Parse(NetInfoMgr.instance.CashOut_Data.convert_goal, CultureInfo.CurrentCulture);
        DOTween.To(() => Mathf.Min(1, MoneyStart / MaxMoney), x => MaxMoneyFill.fillAmount = x, 0, 1f);
        DOTween.To(() => MoneyStart, x => MaxMoneyText.text = x.ToString("F2") + "/" + MaxMoney, 0, 1f);
        DesText.text = $"The more {MoneyName} you collect,the more rewards will be converted!";
    }

    public void UpdateTask() //刷新任务
    {
        if (CashOutManager.GetInstance().Data.TaskData != null && CashOutManager.GetInstance().Data.TaskData.Name != "Null")
        {
            // Cash余额任务 不走前端进度统计 使用后台Data.Cash数值
            if (CashOutManager.GetInstance().Data.TaskData.Name == "Cash" && CashOutManager.GetInstance().Data.Cash < CashOutManager.GetInstance().Data.TaskData.Target)
            {
                CashOutBtn.gameObject.SetActive(false);
                CashOutBtn_Task.SetActive(true);
                CashOutBtn_Task_Text.text = string.Format(CashOutManager.GetInstance().Data.TaskData.Description, CashOutManager.GetInstance().Data.Cash, CashOutManager.GetInstance().Data.TaskData.Target);
            }
            // 其他任务 前端统计任务完成进度 
            else if (CashOutManager.GetInstance().Data.TaskData.Name != "Cash" && CashOutManager.GetInstance().Data.TaskData.NowValue < CashOutManager.GetInstance().Data.TaskData.Target)
            {
                CashOutBtn.gameObject.SetActive(false);
                CashOutBtn_Task.SetActive(true);
                CashOutBtn_Task_Text.text = string.Format(CashOutManager.GetInstance().Data.TaskData.Description, CashOutManager.GetInstance().Data.TaskData.NowValue, CashOutManager.GetInstance().Data.TaskData.Target);
            }
            // 任务完成 显示提现按钮
            else
            {
                CashOutBtn.gameObject.SetActive(true);
                CashOutBtn_Task.SetActive(false);
            }
        }
        // 无任务 显示提现按钮
        else
        {
            CashOutBtn.gameObject.SetActive(true);
            CashOutBtn_Task.SetActive(false);
        }
    }

    public void UpdateRecord(int Page = 0) //更新单页10个提现记录
    {
        this.Page = Page;
        NoRecordTip.SetActive(false);
        PageText.transform.parent.gameObject.SetActive(false);
        for (int i = 0; i < RecordItems.Length; i++)
            RecordItems[i].gameObject.SetActive(false);

        if (CashOutManager.GetInstance().Data.Record == null || CashOutManager.GetInstance().Data.Record.Count == 0)
        {
            NoRecordTip.SetActive(true);
            return;
        }

        int RecordCount = CashOutManager.GetInstance().Data.Record.Count;
        PageCount = RecordCount / 10;
        if (RecordCount % 10 != 0)
            PageCount++;
        PageText.text = (Page + 1) + "/" + PageCount;
        if (PageCount > 1)
            PageText.transform.parent.gameObject.SetActive(true);

        // 计算当前页应显示的记录数
        int startIndex = Page * 10;
        int remainingItems = RecordCount - startIndex;
        int itemsToShow = Mathf.Min(10, remainingItems);

        if (itemsToShow <= 0) return; // 安全检查

        List<WithdrawRecordItem> OnePageRecord = CashOutManager.GetInstance().Data.Record.GetRange(startIndex, itemsToShow);
        for (int i = 0; i < OnePageRecord.Count; i++)
        {
            RecordItems[i].gameObject.SetActive(true);
            RecordItems[i].transform.Find("钱Text").GetComponent<Text>().text = "$" + OnePageRecord[i].amount;
            if (System.DateTime.TryParse(OnePageRecord[i].crt_time, out System.DateTime convertTime))
                RecordItems[i].transform.Find("时间Text").GetComponent<Text>().text = "REDEEM IN " + convertTime.ToString("yyyy/MM/dd HH:mm:ss");
            switch (OnePageRecord[i].status)
            {
                case "INIT":
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().text = "Submitted";
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().color = StateTextColors[0];
                    RecordItems[i].transform.Find("状态背景").GetComponent<Image>().sprite = StateSprites[0];
                    break;
                case "ING":
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().text = "Pending";
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().color = StateTextColors[1];
                    RecordItems[i].transform.Find("状态背景").GetComponent<Image>().sprite = StateSprites[1];
                    break;
                case "SUCCESS":
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().text = "Completed";
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().color = StateTextColors[2];
                    RecordItems[i].transform.Find("状态背景").GetComponent<Image>().sprite = StateSprites[2];
                    break;
                case "FAIL":
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().text = "Failed";
                    RecordItems[i].transform.Find("状态背景/状态Text").GetComponent<Text>().color = StateTextColors[3];
                    RecordItems[i].transform.Find("状态背景").GetComponent<Image>().sprite = StateSprites[3];
                    break;
            }
        }
        RecordScrollRect.verticalNormalizedPosition = 0;
        RecordScrollRect.normalizedPosition = new Vector2(0, 1);
    }
    public void PageUpBtnClick() //上一页
    {
        if (Page > 0)
        {
            Page--;
            UpdateRecord(Page);
        }
    }
    public void PageDownBtnClick() //下一页
    {
        if (Page < PageCount - 1)
        {
            Page++;
            UpdateRecord(Page);
        }
    }

    void ShowGuide()
    {
        if (GuideStep == 4)
        {
            ClosePanel(GuidePanel);
            PlayerPrefs.SetInt("CashOut_Guide", 1);
            return;
        }

        string[] Titles = new string[]
        {
            "Earn Easily",
            "Convert Automatically",
            "Redeem Easily",
            "Arriving Rapidly"
        };
        string[] Descriptions = new string[]
        {
            "Play More! Earn More!",
            $"{MoneyName} convert every 3 Hrs.",
            "1 easy task per day unlocks instant redeem!",
            "Redeem your rewards to your PAYPAL account."
        };
        if (GuideStep == 0)
        {
            GuideItems1[0].localPosition = Vector2.zero;
            GuideItems1[1].localPosition = new Vector2(0, -300);
            GuideItems1[2].localPosition = new Vector2(0, -300);
            GuideItems1[3].localPosition = new Vector2(0, -300);
            GuideItems2[0].localPosition = Vector2.zero;
            GuideItems2[1].localPosition = new Vector2(0, -300);
            GuideItems2[2].localPosition = new Vector2(0, -300);
            GuideItems2[3].localPosition = new Vector2(0, -300);
            GuideNextBtn.GetComponentInChildren<Text>().text = "Continue";
        }
        else if (GuideStep == 1)
        {
            GuideItems1[0].DOLocalMoveY(-300, .5f).SetEase(Ease.InBack);
            GuideItems2[0].DOLocalMoveY(-300, .5f).SetEase(Ease.InBack);
            GuideItems1[1].DOLocalMoveY(0, .5f).SetEase(Ease.OutBack).SetDelay(.5f);
            GuideItems2[1].DOLocalMoveY(0, .5f).SetEase(Ease.OutBack).SetDelay(.5f);
            GuideNextBtn.GetComponentInChildren<Text>().text = "Continue";
        }
        else if (GuideStep == 2)
        {
            GuideItems1[1].DOLocalMoveY(-300, .5f).SetEase(Ease.InBack);
            GuideItems2[1].DOLocalMoveY(-300, .5f).SetEase(Ease.InBack);
            GuideItems1[2].DOLocalMoveY(0, .5f).SetEase(Ease.OutBack).SetDelay(.5f);
            GuideItems2[2].DOLocalMoveY(0, .5f).SetEase(Ease.OutBack).SetDelay(.5f);
            GuideNextBtn.GetComponentInChildren<Text>().text = "Continue";
        }
        else if (GuideStep == 3)
        {
            GuideItems1[2].DOLocalMoveY(-300, .5f).SetEase(Ease.InBack);
            GuideItems2[2].DOLocalMoveY(-300, .5f).SetEase(Ease.InBack);
            GuideItems1[3].DOLocalMoveY(0, .5f).SetEase(Ease.OutBack).SetDelay(.5f);
            GuideItems2[3].DOLocalMoveY(0, .5f).SetEase(Ease.OutBack).SetDelay(.5f);
            GuideNextBtn.GetComponentInChildren<Text>().text = "Get it!";
        }
        GuideStep2Go.SetActive(GuideStep == 1);
        GuideTitleText.DOKill();
        GuideTitleText.text = "";
        GuideTitleText.DOFade(0, .5f).OnComplete(() => GuideTitleText.text = Titles[GuideStep]);
        GuideTitleText.DOFade(1, .5f).SetDelay(.51f);
        GuideText.DOKill();
        GuideText.text = "";
        GuideText.DOText(Descriptions[GuideStep], 1f);
        for (int i = 0; i < GuideStepPoints.Length; i++)
            GuideStepPoints[i].SetActive(i == GuideStep);
    }

    public void CloseLoading_UpdateUI()
    {
        StopLoadingAnim(UpdateUI_Loading);

        //加载结束后 打点
        Invoke(nameof(SendEvent_1301), 1);

        //刷新任务
        UpdateTask();

        //新手引导
        if (PlayerPrefs.GetInt("CashOut_Guide") == 0)
        {
            GuideStep = 0;
            OpenPanel(GuidePanel);
            ShowGuide();
            GuideNextBtn.onClick.RemoveAllListeners();
            GuideNextBtn.onClick.AddListener(() =>
            {
                GuideStep++;
                ShowGuide();
            });
        }
    }
    public void CloseLoading_Record()
    {
        StopLoadingAnim(RecordPanel_Loading);
    }
    void CloseLoading3()
    {
        StopLoadingAnim(SbmitBtn_Loading);
    }
    public void CloseLoading_Withdraw(bool IsCashOutSuccess = false)
    {
        StopLoadingAnim(CashOutBtn_Loading);
        StopLoadingAnim(SbmitBtn_Loading);

        if (IsCashOutSuccess)
        {
            ToastManager.GetInstance().ShowToast("Withdraw success");
            ClosePanel(ConfirmAccountPanel);
        }
    }

    void OpenPanel(GameObject Panel)
    {
        Panel.SetActive(true);
        Image PanelImage = Panel.GetComponent<Image>();
        PanelImage.DOKill();
        PanelImage.color = new Color32(0, 0, 0, 0);
        PanelImage.DOFade(.4f, 0.2f);
        Transform Bg = Panel.transform.Find("背景");
        Bg.DOKill();
        Bg.localScale = Vector2.zero;
        Bg.DOScale(Vector2.one, 0.3f).SetEase(Ease.OutBack).SetDelay(0.2f);
    }
    void ClosePanel(GameObject Panel)
    {
        Transform Bg = Panel.transform.Find("背景");
        Bg.DOKill();
        Bg.localScale = Vector2.one;
        Bg.DOScale(Vector2.zero, 0.2f).SetEase(Ease.InBack);
        Image PanelImage = Panel.GetComponent<Image>();
        PanelImage.DOKill();
        PanelImage.color = new Color32(0, 0, 0, 180);
        PanelImage.DOFade(0, 0.3f).SetDelay(0.2f).OnComplete(() => Panel.SetActive(false));
    }

    void StartLoadingAnim(GameObject LoadingPanel)
    {
        LoadingPanel.gameObject.SetActive(true);
        Image[] Points = new Image[LoadingPanel.transform.Find("加载动画").childCount];
        Color PointColor = LoadingPanel.transform.Find("加载动画").GetChild(0).GetComponent<Image>().color;
        if (PointColor == Color.white)
            PointColor = new Color(1, 1, 1, 1);
        else
            PointColor = new Color(.7f, .7f, .7f, 1);
        for (int i = 0; i < Points.Length; i++)
        {
            Points[i] = LoadingPanel.transform.Find("加载动画").GetChild(i).GetComponent<Image>();
            Points[i].DOKill();
            Points[i].color = PointColor;
            Points[i].DOFade(.5f, 0.45f).SetDelay(i * 0.05f).SetLoops(-1, LoopType.Restart);
            Points[i].transform.DOKill();
            Points[i].transform.localScale = Vector2.one;
            Points[i].transform.DOScale(Vector2.one * .4f, 0.45f).SetDelay(i * 0.05f).SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear);
        }
    }
    void StopLoadingAnim(GameObject LoadingPanel)
    {
        LoadingPanel.gameObject.SetActive(false);
        Image[] Points = new Image[LoadingPanel.transform.Find("加载动画").childCount];
        for (int i = 0; i < Points.Length; i++)
        {
            Points[i] = LoadingPanel.transform.Find("加载动画").GetChild(i).GetComponent<Image>();
            Points[i].DOKill();
            Points[i].transform.DOKill();
        }
    }

    public void SendEvent_1301() //打点 拉取数据成功 打开商店
    {
        PostEventScript.GetInstance().SendEvent("1301", LeftTimeText.text, CashOutManager.GetInstance().Money.ToString());
    }
}

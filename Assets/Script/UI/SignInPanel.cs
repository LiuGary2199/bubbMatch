using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class SignInPanel : BaseUIForms
{
    // 常量定义
    private const int MAX_SIGN_DAYS = 7;
    private const float REWARD_DELAY = 0.5f;

    [Header("UI Components")]
    public List<SignRewardItem> list_Reward;
    public Button claimButton;

    [Header("Private Fields")]
    /// <summary>
    /// 已经签到的天数
    /// </summary>
    private int checkNums = 0;
    private int curCheckIndex = 0;
    private bool isCheck = false;
    
    [Header("Settings")]
    /// <summary>
    /// 是否隐藏未解锁的奖励金额（显示为"？？？"）
    /// </summary>
    public bool hideUnlockedRewards = true;

    public Button closebtn;
    private Tween tween;

    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);

        try
        {
            InitTodayIsCanCheck();
            RewardPanelInit();
            InitSignInPanel();
        }
        catch (Exception e)
        {
            Debug.LogError($"SignInPanel Display error: {e.Message}");
        }
    }

    void Start()
    {
        if (claimButton != null)
        {
            claimButton.onClick.AddListener(OnClaimClick);
        }
        else
        {
            Debug.LogError("SignInPanel: claimButton is null!");
        }
        closebtn.onClick.AddListener(() =>
        {
            tween?.Kill();
            UIManager.GetInstance().CloseOrReturnUIForms(this.GetType().Name);
        });
    }

    /// <summary>
    /// 签到按钮点击事件
    /// </summary>
    private void OnClaimClick()
    {
        if (!CanClaim())
            return;

        try
        {
           ADManager.Instance.playRewardVideo((success) =>
        {
            if (success)
            {
                PostEventScript.GetInstance().SendEvent("1019", "0");
                // 播放音效
                // MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_Progress_Box);
                checkNums++;
            Debug.Log($"After checkNums++: checkNums={checkNums}");

            // 先保存签到数据
            SetCheckData();
            Debug.Log($"After SetCheckData: checkNums={checkNums}");

            // 重新检查今天是否可签到（确保状态一致）
            InitTodayIsCanCheck();
            Debug.Log($"After InitTodayIsCanCheck: checkNums={checkNums}, isCheck={isCheck}");

            // 更新UI状态
            RewardPanelInit();
            MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_UIButton);
            // 获取奖励
            GetClaimReward();
               
            }
        }, "9");           
        }
        catch (Exception e)
        {
            Debug.LogError($"OnClaimClick error: {e.Message}");
        }
    }

    /// <summary>
    /// 检查是否可以领取奖励
    /// </summary>
    private bool CanClaim()
    {
        if (checkNums >= MAX_SIGN_DAYS)
        {
            Debug.Log("已达到最大签到天数");
            return false;
        }

        if (!isCheck)
        {
            Debug.Log("今天已经签到过了");
            UIManager.GetInstance().ShowUIForms(nameof(Toast),"Reward Claimed");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 修改奖励领取状态
    /// </summary>
    private void RewardPanelInit()
    {
        if (NetInfoMgr.instance == null)
        {
            Debug.LogError("NetInfoMgr.instance is null!");
            return;
        }

        NetInfoMgr.instance.InitSignInData();
        List<List<RewardData>> list = NetInfoMgr.instance.List_SignInData;

        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("SignIn data list is empty");
            return;
        }

        for (int i = 0; i < list_Reward.Count && i < list.Count; i++)
        {
            if (list_Reward[i] != null)
            {
                bool isClaimed = i < checkNums;           // 是否已领取
                bool isAvailable = false;                 // 是否可领取
                bool isLastDay = i == MAX_SIGN_DAYS - 1;  // 是否是最后一天
                bool shouldHideReward = false;            // 是否应该隐藏奖励金额

                // 只有当前可签到的一天才能领取
                if (i == checkNums && isCheck)
                {
                    isAvailable = true;
                }

                // 判断是否应该隐藏奖励金额
                if (hideUnlockedRewards)
                {
                    // 隐藏条件：不是已领取的 && 不是当前可领取的 && 不是最后一天
                    shouldHideReward = !isClaimed && !isAvailable && !isLastDay;
                }

                // 添加调试信息
                Debug.Log($"Day {i}: checkNums={checkNums}, isCheck={isCheck}, isClaimed={isClaimed}, isAvailable={isAvailable}, isLastDay={isLastDay}, shouldHideReward={shouldHideReward}");

                list_Reward[i].SetClaimedState(isClaimed);
                list_Reward[i].SetRewardData(list[i], isAvailable, shouldHideReward);
            }
        }
    }

    private void GetClaimReward()
    {
        // 获取奖励之后刷新界面
        // InitTodayIsCanCheck(); // 已经在OnClaimClick中调用了，这里不需要重复调用
        StartCoroutine(GetReward());
    }

    IEnumerator GetReward()
    {
        yield return new WaitForSeconds(REWARD_DELAY);

        List<List<RewardData>> list = NetInfoMgr.instance.List_SignInData;
        RewardData panelData = new RewardData();
        RewardData rewardData = list[curCheckIndex - 1][0];
        double rewardValue = rewardData.rewardNum;
        HomePanel.Instance.AddCash(rewardValue, null);
        tween = DOVirtual.DelayedCall(0.3f, () =>
        {
            tween?.Kill();
            UIManager.GetInstance().CloseOrReturnUIForms(this.GetType().Name);
        });
    }

    /// <summary>
    /// 检查今天是否可签到
    /// </summary>
    public void InitTodayIsCanCheck()
    {
        try
        {
            int[] checkDay = SaveDataManager.GetIntArray("CheckDay");
            checkNums = SaveDataManager.GetInt("CheckNum");
            curCheckIndex = checkNums;

            DateTime dateTime = DateTime.Now;
            int[] time = new int[3] { dateTime.Year, dateTime.Month, dateTime.Day };

            if (checkDay == null || checkDay.Length == 0)
            {
                isCheck = true;
                Debug.Log("First time signing in, isCheck = true");
            }
            else
            {
                DateTime dt1 = new DateTime(checkDay[0], checkDay[1], checkDay[2]);
                DateTime dt2 = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day);
                TimeSpan span = dt2.Subtract(dt1);

                isCheck = span.Days > 0;

                Debug.Log($"Last check: {dt1}, Today: {dt2}, Days difference: {span.Days}, isCheck: {isCheck}");

                // 如果超过7天，重置签到计数
                if (isCheck && checkNums >= MAX_SIGN_DAYS)
                {
                    checkNums = 0;
                    SaveDataManager.SetInt("CheckNum", checkNums);
                }
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"InitTodayIsCanCheck error: {e.Message}");
            isCheck = false;
        }
    }

    /// <summary>
    /// 保存签到数据
    /// </summary>
    public void SetCheckData()
    {
        try
        {
            SaveDataManager.SetInt("CheckNum", checkNums);
            DateTime dateTime = DateTime.Now;
            int[] time = new int[3] { dateTime.Year, dateTime.Month, dateTime.Day };
            SaveDataManager.SetIntArray("CheckDay", time);
        }
        catch (Exception e)
        {
            Debug.LogError($"SetCheckData error: {e.Message}");
        }
    }

    public void InitSignInPanel()
    {
        if (NetInfoMgr.instance == null || NetInfoMgr.instance.List_SignInData == null)
        {
            Debug.LogError("SignIn data not available");
            return;
        }

        List<List<RewardData>> list = NetInfoMgr.instance.List_SignInData;

        for (int i = 0; i < list.Count && i < list_Reward.Count; i++)
        {
            if (list_Reward[i] != null)
            {
                bool isClaimed = i < checkNums;           // 是否已领取
                bool isAvailable = i == checkNums && isCheck; // 是否可领取
                bool isLastDay = i == MAX_SIGN_DAYS - 1;  // 是否是最后一天
                bool shouldHideReward = false;            // 是否应该隐藏奖励金额

                // 判断是否应该隐藏奖励金额
                if (hideUnlockedRewards)
                {
                    // 隐藏条件：不是已领取的 && 不是当前可领取的 && 不是最后一天
                    shouldHideReward = !isClaimed && !isAvailable && !isLastDay;
                }

                var textlist = list_Reward[i].list_Text;
                if (textlist != null)
                {
                    for (int j = 0; j < textlist.Count && j < list[i].Count; j++)
                    {
                        if (textlist[j] != null)
                        {
                            if (shouldHideReward)
                            {
                                textlist[j].text = "xxx";
                            }
                            else
                            {
                                textlist[j].text = list[i][j].rewardNum.ToString();
                            }
                        }
                    }
                }
            }
        }
    }
}

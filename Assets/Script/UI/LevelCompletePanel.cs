using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Spine.Unity;
using Spine;

public class LevelCompletePanel : BaseUIForms
{
    [Header("按钮")]
    public Button ADButton;
    public Button NextLevelButton;
    public GameObject ADText;
    [Header("转盘组")]
    public SlotGroup SlotBG;

    public GameObject RewardCashImage;
    public GameObject RewardGoldImage;
    public Text RewardText;

    private double rewardValue;
    private bool hasClickedAdBtn;
    public RectTransform grtMoreRect;
    public SkeletonGraphic m_SkeletonGraphic;
    private string ADstate = "1";
    public Tween tween;

    // Start is called before the first frame update
    void Start()
    {
        m_SkeletonGraphic.AnimationState.Complete += OnAnimationComplete;
        ADButton.onClick.AddListener(() =>
        {
            tween?.Kill();
            ADButton.enabled = false;
            NextLevelButton.enabled = false;
            if (isNewUser())
            {
                playSlot();
            }
            else
            {
                ADManager.Instance.playRewardVideo((success) =>
                {
                    if (success)
                    {
                        ADstate = "1";
                        playSlot();
                    }
                    else
                    {
                        ADButton.enabled = true;
                        NextLevelButton.enabled = true;
                    }
                }, "2");
            }
        });

        NextLevelButton.onClick.AddListener(() =>
        {
            ADButton.enabled = false;
            NextLevelButton.enabled = false;
            ADstate = "0";
            CloseAnim();
        });

    }

    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
        MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_PopShow);
        m_SkeletonGraphic.AnimationState.ClearTracks();
        NextLevelButton.gameObject.SetActive(false);
        ADButton.enabled = true;
        NextLevelButton.enabled = true;
        if (isNewUser())
        {
            ADText.SetActive(false);
            grtMoreRect.anchoredPosition = new Vector2(0, 9);
            NextLevelButton.gameObject.SetActive(false);
        }
        else
        {
            ADText.SetActive(true);
            grtMoreRect.anchoredPosition = new Vector2(35.5f, 9);
            NextLevelButton.gameObject.SetActive(true);
        }
        NextLevelButton.enabled = true;

        ADButton.gameObject.SetActive(true);
        hasClickedAdBtn = false;

        // 根据实际项目计算奖励
        rewardValue = NetInfoMgr.instance.GameData.passlevel;
        //       rewardValue = 1 * GameUtil.GetCashMulti();
        RewardText.text = "+" + NumberUtil.DoubleToStr(rewardValue);

        SlotBG.initMulti();
        tween = DOVirtual.DelayedCall(2f, () =>
        {
            tween?.Kill();
            if (!isNewUser())
            {
                NextLevelButton.gameObject.SetActive(true);
            }

        });
        m_SkeletonGraphic.AnimationState.SetAnimation(0, "in", false);
    }
    private void OnAnimationComplete(TrackEntry trackEntry)
    {
        if (trackEntry != null)
        {
            if (trackEntry.Animation.Name == "in")
            {
                m_SkeletonGraphic.AnimationState.SetAnimation(0, "idle", true);
            }
            if (trackEntry.Animation.Name == "end")
            {
                GoNext();
            }
        }
    }

    private bool isNewUser()
    {
        return !PlayerPrefs.HasKey(CConfig.sv_FirstSlot + "Bool") || SaveDataManager.GetBool(CConfig.sv_FirstSlot);
    }
    // 计算本次slot应该获得的奖励
    private int getSlotMultiIndex()
    {
        // 新用户，第一次固定翻5倍
        if (isNewUser())
        {
            int index = 0;
            foreach (SlotItem wg in NetInfoMgr.instance.InitData.slot_group)
            {
                if (wg.multi == 5)
                {
                    return index;
                }
                index++;
            }
        }
        else
        {
            int sumWeight = 0;
            foreach (SlotItem wg in NetInfoMgr.instance.InitData.slot_group)
            {
                sumWeight += wg.weight;
            }
            int r = Random.Range(0, sumWeight);
            int nowWeight = 0;
            int index = 0;
            foreach (SlotItem wg in NetInfoMgr.instance.InitData.slot_group)
            {
                nowWeight += wg.weight;
                if (nowWeight > r)
                {
                    return index;
                }
                index++;
            }

        }
        return 0;
    }


    private void playSlot()
    {
        NextLevelButton.gameObject.SetActive(false);
        ADButton.gameObject.SetActive(false);
        int index = getSlotMultiIndex();
        SlotBG.slot(index, (multi) =>
        {
            // slot结束后的回调
            AnimationController.ChangeNumber(rewardValue, rewardValue * multi, 0, RewardText, "+", () =>
            {
                rewardValue = rewardValue * multi;
                RewardText.text = "+" + NumberUtil.DoubleToStr(rewardValue);
                hasClickedAdBtn = true;
                DOVirtual.DelayedCall(0.5f, () =>
                {
                     
                    CloseAnim();
                });
            });
        });

        SaveDataManager.SetBool(CConfig.sv_FirstSlot, false);
    }
    public void CloseAnim(){
         m_SkeletonGraphic.AnimationState.SetAnimation(0, "end", false);
    }

    public void GoNext()
    {
        tween?.Kill();
        NextLevelButton.enabled = false;
        HomePanel.Instance.AddCash(rewardValue, RewardCashImage.transform);
        if (!hasClickedAdBtn)
        {
            ADManager.Instance.NoThanksAddCount();
        }
        int currentLevel = GameManager.Instance.GetLevel();
        
        // 🎯 检查是否是第一关过关，如果是则弹出RateUsPanel
        CheckAndShowRateUsPanel(currentLevel);
        GameManager.Instance.SetLevel(currentLevel + 1);
        Debug.Log($"Level模式升级到: {currentLevel + 1}");
        CloseUIForm(GetType().Name);
        PostEventScript.GetInstance().SendEvent("1014", ADstate);
        // 开始下一关
        HomePanel.Instance.StartNextLevel();
    }
    
    /// <summary>
    /// 检查并显示RateUsPanel（第一关过关时）
    /// </summary>
    /// <param name="completedLevel">刚完成的关卡数</param>
    private void CheckAndShowRateUsPanel(int completedLevel)
    {
        // 检查是否是第一关过关
        if (completedLevel == 1)
        {
            // 检查是否已经显示过评级弹框
            if (!SaveDataManager.GetBool(CConfig.sv_HasShowRatePanel) && !CommonUtil.IsApple()) 
            {
                Debug.Log("🎯 第一关过关，准备弹出RateUsPanel");
                
                // 延迟一点时间再弹出，确保LevelCompletePanel完全关闭
                DOVirtual.DelayedCall(0.5f, () =>
                {
                    UIManager.GetInstance().ShowUIForms(nameof(RateUsPanel));
                    // 标记已经显示过评级弹框
                    SaveDataManager.SetBool(CConfig.sv_HasShowRatePanel, true);
                });
            }
            else
            {
                Debug.Log("RateUsPanel已经显示过，跳过");
            }
        }
    }
}

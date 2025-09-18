using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
public class LowRewardPanel : BaseUIForms
{
    [Header("按钮")]
    public Button ADButton;
    public Button GetButton;
    public GameObject ADText;

    public Text RewardText;
    public RectTransform ADTextAdTextTrans;
    public Transform rewardTrans;
    private double rewardValue;
    private bool hasClickedAdBtn;
    public Tween tween;
    private string AdState = "1";
    // Start is called before the first frame update
    void Start()
    {
        ADButton.onClick.AddListener(() =>
        {
            ADButton.enabled = false;
            GetButton.enabled = false;
            if (isNewUser())
            {
                SaveDataManager.SetBool(CConfig.sv_FirstLowReward, false);
                NumAnim();
            }
            else
            {
                ADManager.Instance.playRewardVideo((success) =>
                {
                    if (success)
                    {
                        AdState = "1";
                        SaveDataManager.SetBool(CConfig.sv_FirstLowReward, false);
                        NumAnim();
                    }
                    else
                    {
                        ADButton.enabled = true;
                        GetButton.enabled = true;
                    }
                }, "1");
            }
        });

        GetButton.onClick.AddListener(() =>
        {
            AdState = "0";
            if (GameManager.Instance.GetGameType() == GameType.Level)
            {
                PostEventScript.GetInstance().SendEvent("1004", "0");
            }
            else
            {
                PostEventScript.GetInstance().SendEvent("1018", "0");

            }
            ADButton.enabled = false;
            GetButton.enabled = false;
            HomePanel.Instance.AddCash(rewardValue, rewardTrans);
            ADManager.Instance.NoThanksAddCount();
            CloseUIForm(GetType().Name);
        });

    }

    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
        MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_PopcashShow);

        ADButton.enabled = true;
        GetButton.enabled = true;
        GetButton.gameObject.SetActive(false);
        rewardValue = NumberUtil.SafeConvertToDouble(uiFormParams);
        RewardText.text = "+" + NumberUtil.ConvertWithDecimalFix(uiFormParams);
        if (isNewUser())
        {
            ADTextAdTextTrans.anchoredPosition = new Vector2(0, 4.5f);
            ADText.SetActive(false);
        }
        else
        {
            ADTextAdTextTrans.anchoredPosition = new Vector2(33.6f, 4.5f);
            ADText.SetActive(true);
        }
        tween?.Kill();
        tween = DOVirtual.DelayedCall(1f, () =>
        {
            tween?.Kill();
            if (!isNewUser())
            {
                GetButton.gameObject.SetActive(true);
            }
        });
    }

    private bool isNewUser()
    {
        return !PlayerPrefs.HasKey(CConfig.sv_FirstLowReward + "Bool") || SaveDataManager.GetBool(CConfig.sv_FirstLowReward);
    }
   
    public void NumAnim()
    {
        AnimationController.ChangeNumber(rewardValue, rewardValue * 5, 0, RewardText, "+", () =>
        {
            rewardValue = rewardValue * 5;
            RewardText.text = "+" + NumberUtil.DoubleToStr(rewardValue);
            hasClickedAdBtn = true;
            HomePanel.Instance.AddCash(rewardValue, rewardTrans);
            DOVirtual.DelayedCall(0.5f, () =>
            {
                if (GameManager.Instance.GetGameType() == GameType.Level)
                {
                    PostEventScript.GetInstance().SendEvent("1004", "1");
                }
                else
                {
                    PostEventScript.GetInstance().SendEvent("1018", "1");

                }
                CloseUIForm(GetType().Name);
            });
        });
    }
}

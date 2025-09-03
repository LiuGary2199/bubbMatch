using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class AdManagerTestPanel : BaseUIForms
{
    public Text LastPlayTimeCounterText;
    public Text Counter101Text;
    public Text Counter102Text;
    public Text Counter103Text;
    public Text TrialNumText;
    public Button PlayRewardedAdButton;
    public Button PlayInterstitialAdButton;
    public Button NoThanksButton;
    public Button TrialNumButton;
    public Button CloseButton;
    public Text TimeInterstitialText;
    public Button PauseTimeInterstitialButton;
    public Button ResumeTimeInterstitialButton;

    private void Start()
    {
        InvokeRepeating(nameof(ShowCounterText), 0, 0.5f);

        CloseButton.onClick.AddListener(() => {
            CloseUIForm(GetType().Name);
        });

        PlayRewardedAdButton.onClick.AddListener(() => {
            ADManager.Instance.playRewardVideo((success) => { }, "10");
        });

        PlayInterstitialAdButton.onClick.AddListener(() => {
            ADManager.Instance.playInterstitialAd(1);
        });

        NoThanksButton.onClick.AddListener(() => {
            ADManager.Instance.NoThanksAddCount();
        });

        TrialNumButton.onClick.AddListener(() => {
            ADManager.Instance.UpdateTrialNum(SaveDataManager.GetInt(CConfig.sv_ad_trial_num) + 1);
            TrialNumText.text = SaveDataManager.GetInt(CConfig.sv_ad_trial_num).ToString();
        });

        PauseTimeInterstitialButton.onClick.AddListener(() => {
            ADManager.Instance.PauseTimeInterstitial();
            ShowPauseTimeInterstitial();
        });

        ResumeTimeInterstitialButton.onClick.AddListener(() => {
            ADManager.Instance.ResumeTimeInterstitial();
            ShowPauseTimeInterstitial();
        });

    }

    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
        TrialNumText.text = SaveDataManager.GetInt(CConfig.sv_ad_trial_num).ToString();
        ShowPauseTimeInterstitial();
    }

    private void ShowCounterText()
    {
        LastPlayTimeCounterText.text = ADManager.Instance.lastPlayTimeCounter.ToString();
        Counter101Text.text = ADManager.Instance.counter101.ToString();
        Counter102Text.text = ADManager.Instance.counter102.ToString();
        Counter103Text.text = ADManager.Instance.counter103.ToString();
    }

    private void ShowPauseTimeInterstitial()
    {
        TimeInterstitialText.text = ADManager.Instance.pauseTimeInterstitial ? "已暂停" : "未暂停";
    }
}

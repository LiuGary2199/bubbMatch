using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
public class ContinueOrFailPanel : BaseUIForms
{
    public GameObject m_ContinuePanel;
    public GameObject m_FailPanel;

    public Button m_ContinueBtn;
    public Button m_FailBtn;
    public Button m_SkipBtn;
    public Text m_LevelText;
    public Image m_completeImage;
    public Text m_completeText;

    public void Start()
    {
        m_ContinueBtn.onClick.AddListener(() =>
        {
            m_ContinueBtn.enabled = false;
            ADManager.Instance.playRewardVideo((success) =>
            {
                if (success)
                {
                    //继续游戏 通知出去
                    GameEvents.GameFailContinue?.Invoke();
                    PostEventScript.GetInstance().SendEvent("1015","1");
                    CloseUIForm(GetType().Name);
                }
                else
                {
                    m_ContinueBtn.enabled = true;
                }
            }, "7");
        });
        m_SkipBtn.onClick.AddListener(() =>
        {
            m_ContinuePanel.SetActive(false);
            m_FailPanel.SetActive(true);

        });
        m_FailBtn.onClick.AddListener(() =>
        {
            //重新游戏
            PostEventScript.GetInstance().SendEvent("1015","0");
            SaveDataManager.SetInt(CConfig.svmagnetUseForChallenge,0);
            SaveDataManager.SetInt(CConfig.svmagnetcleanForChallenge,0);
            SaveDataManager.SetInt(CConfig.svmagnetrefForChallenge,0);
            GameEvents.GameRestart?.Invoke();
            CloseUIForm(GetType().Name);
        });

    }

    public override void Display(object uiFormParams)
    {
        MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_PopShow);

        int isContinue = (int)uiFormParams;
        if (isContinue == 1)
        {
            m_ContinuePanel.SetActive(true);
            m_FailPanel.SetActive(false);
        }
        else
        {
            m_ContinuePanel.SetActive(false);
            m_FailPanel.SetActive(true);
        }
        base.Display(uiFormParams);
        if (GameManager.Instance.GetGameType() == GameType.Level)
        {
            m_LevelText.text = "LEVEL " + GameManager.Instance.GetLevel();
        }
        else
        {
            m_LevelText.text = "CHALLENGE ";

        }
        m_completeImage.fillAmount = HomePanel.Instance.ShowProgress();
        float a = HomePanel.Instance.ShowProgress() * 100f;
        m_completeText.text = a.ToString("F2") + "%";
    }

}

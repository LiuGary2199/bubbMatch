using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class TipsChallenge : BaseUIForms
{
    public Button ToChallenge;
    public Button CloseView;
    public Button CloseBtn;
    void Start()
    {
        ToChallenge.onClick.AddListener(() =>
        {   GameManager.Instance.SetGameType(GameType.Challenge);
            SaveDataManager.SetBool(CConfig.sv_FirstChallenge, true);
            GameEvents.FirstChallenge?.Invoke();
            CloseUIForm(GetType().Name);
        });
        CloseView.onClick.AddListener(() =>
        {   GameManager.Instance.SetGameType(GameType.Level);
            SaveDataManager.SetBool(CConfig.sv_FirstChallenge, true);
            GameEvents.FirstChallenge?.Invoke();
            CloseUIForm(GetType().Name);
        });
        CloseBtn.onClick.AddListener(() =>
        {
            GameManager.Instance.SetGameType(GameType.Level);
            SaveDataManager.SetBool(CConfig.sv_FirstChallenge, true);
            GameEvents.FirstChallenge?.Invoke();
            CloseUIForm(GetType().Name);
        });
    }
    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
    }

    public override void Hidding()
    {
        base.Hidding();
    }
  
}

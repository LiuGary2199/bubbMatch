using System.Collections;
using System.Collections.Generic;
using Lofelt.NiceVibrations;
using UnityEngine;

public class MainManager : MonoBehaviour
{
    public static MainManager instance;

    private bool ready = false;
    // 基础分辨率（例如1920×1080）
    private float baseWidth = 1080f;
    private float baseHeight = 1920f;
    float currentWidth =0;
    float currentHeight = 0;
    private float _scaleRatio;

    private void Awake()
    {
        instance = this;
        Application.targetFrameRate = 240;
    }

    public void gameInit()
    {
        bool isNewPlayer = !PlayerPrefs.HasKey(CConfig.sv_IsNewPlayer + "Bool") || SaveDataManager.GetBool(CConfig.sv_IsNewPlayer);

        AdjustInitManager.Instance.InitAdjustData(isNewPlayer);

        if (isNewPlayer)
        {
             // 新用户
            SaveDataManager.SetBool(CConfig.sv_IsNewPlayer, false);
            SaveDataManager.SetInt(CConfig.sv_tools_magnet, 1);
            SaveDataManager.SetInt(CConfig.sv_tools_clean, 1);
            SaveDataManager.SetInt(CConfig.sv_tools_ref, 1);
            SaveDataManager.SetInt(CConfig.sv_ad_trial_num,1);
            SaveDataManager.SetInt(CConfig.sv_challenge_num,0);
            SaveDataManager.SetBool(CConfig.sv_TutorialGuide, false);
            SaveDataManager.SetBool(CConfig.sv_FirstChallenge,false);
        }
        if (!PlayerPrefs.HasKey("sv_vibrationType"))
        {
            SaveDataManager.SetInt("sv_vibrationType", 1);
        }

        HapticController.hapticsEnabled = (SaveDataManager.GetInt("sv_vibrationType") == 1);
        MusicMgr.GetInstance().PlayBg(MusicType.SceneMusic.Sound_BGM);

        currentWidth = Screen.width;
        currentHeight = Screen.height;
        float baseAspect = baseWidth / baseHeight;
        float currentAspect = currentWidth / currentHeight;
        if (currentAspect < baseAspect)
        {//2340 1080
            Debug.Log("宽屏适配");
            UIManager.GetInstance().ShowUIForms("HomePanel1");
        }
        else
        {//1920   1080
            Debug.Log("宽屏适配1");
            UIManager.GetInstance().ShowUIForms(nameof(HomePanel));
        }

       

        ready = true;
    }

    //切前后台也需要检测屏蔽 防止游戏中途更改手机状态
    private void OnApplicationFocus(bool focusStatus)
    {
        if (focusStatus)
            CommonUtil.AndroidBlockCheck();
    }
}

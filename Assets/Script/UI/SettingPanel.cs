using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Lofelt.NiceVibrations;
using UnityEngine.UI;

public class SettingPanel : BaseUIForms
{
    public Button Sound_Button;
    public Button Music_Button;
    public Button vibrationBtn;
    public Image SoundIcon;
    public Image MusicIcon;
    public Button Continue_Button;
    public Button Restart_Button;
    public Sprite MusicCloseSprite;
    public Sprite MusicOpenSprite;
    public Sprite SoundCloseSprite;
    public Sprite SoundOpenSprite;
    public GameObject vibrationOn;
    public GameObject vibrationOff;
    private string vibrationKey;

    protected override void Awake()
    {
        base.Awake();
        vibrationKey = "sv_vibrationType";
        if (!PlayerPrefs.HasKey(vibrationKey))
        {
            SaveDataManager.SetInt(vibrationKey, 1);
        }
    }
    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
        
        // 直接转换为字符串
        string paramStr = uiFormParams?.ToString() ?? "0";
        
        if(paramStr == "0")
        {
            Continue_Button.gameObject.SetActive(false);
        }
        else
        {
            Continue_Button.gameObject.SetActive(true);
        }
        MusicIcon.sprite = MusicMgr.GetInstance().BgMusicSwitch ? MusicOpenSprite : MusicCloseSprite;
        SoundIcon.sprite = MusicMgr.GetInstance().EffectMusicSwitch ? SoundOpenSprite : SoundCloseSprite;
        vibrationOn.gameObject.SetActive(SaveDataManager.GetInt(vibrationKey) == 1);
        vibrationOff.gameObject.SetActive(SaveDataManager.GetInt(vibrationKey) != 1);

    }
    // Start is called before the first frame update
    void Start()
    {
        Continue_Button.onClick.AddListener(() =>
        {
            GameEvents.GameRestart?.Invoke();
            CloseUIForm(GetType().Name);
        });
        Restart_Button.onClick.AddListener(() =>
        {
            
            CloseUIForm(GetType().Name);
        });

        Music_Button.onClick.AddListener(() =>
        {

            // MusicMgr.GetInstance().BgMusicSwitch = !MusicMgr.GetInstance().BgMusicSwitch;
            // MusicIcon.sprite = MusicMgr.GetInstance().BgMusicSwitch ? MusicOpenSprite : MusicCloseSprite;
        });
        Sound_Button.onClick.AddListener(() =>
        {

            MusicMgr.GetInstance().EffectMusicSwitch = !MusicMgr.GetInstance().EffectMusicSwitch;
            SoundIcon.sprite = MusicMgr.GetInstance().EffectMusicSwitch ? SoundOpenSprite : SoundCloseSprite;
        });
        vibrationBtn.onClick.AddListener(() =>
        {
            int vibrationType = SaveDataManager.GetInt(vibrationKey) * -1;
            vibrationOn.gameObject.SetActive((vibrationType == 1));
            vibrationOff.gameObject.SetActive((vibrationType != 1));
            SaveDataManager.SetInt(vibrationKey, vibrationType);
            HapticController.hapticsEnabled = (vibrationType == 1);
        });
    }


}

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class HomePanel : BaseUIForms
{
    public static HomePanel Instance;
    public GameObject CoinObj;
    public GameObject CashoutBtn;
    public GameObject CashoutBtnbg;

    public Image Coinimage;
    public Text CoinStr;
    public Text cashNumText;
    public Image cashImg;
    public Button m_PlayLevelBtn;
    public Button m_PlayDailyBtn;
    public Button m_DailyRewardBtn;
    public Button m_OutSettingBtn;
    public Button m_SettingBtn;
    public Button m_BackBtn;

    public GameObject m_MainPage;
    public RectTransform m_CashTrans;
    public GameArea m_GameArea;
    public Text m_LevelBtnText;
    public Text m_LevelBtnText2;


    public ObjectPool m_ClickPool;
    public GameObject ClickPoolParent;
    public GameObject ClickPoolObject;

    public ObjectPool m_DisappearPool;
    public GameObject DisappearParent;
    public GameObject DisappearObject;
    public GameObject bubbflytrans;
    public GameObject ClickMask;
    public NoteView m_NoteView;
    private void Start()
    {
        if (CommonUtil.IsApple())
        {
            CoinObj.SetActive(true);
            CashoutBtn.gameObject.SetActive(false);
            CashoutBtnbg.gameObject.SetActive(false);
        }
        else
        {
            CoinObj.SetActive(false);
            CashoutBtn.gameObject.SetActive(true);
        }

        GameEvents.ClickParticle += (pos) =>
        {
            InsClickParticle(pos);
        };
        GameEvents.CollectParticle += (bubbles) =>
        {
            InsCollectParticle(bubbles);
        };

        // 监听重新开始游戏事件
        GameEvents.GameRestart += OnGameRestart;
        GameEvents.FirstChallenge += OnFirstChallenge;


        Instance = this;
        m_MainPage.SetActive(true);
        HomePanel.Instance.HideClickMask();
        m_LevelBtnText.text = "LEVEL " + GameManager.Instance.GetLevel();
        if (GameManager.Instance.GetGameType() == GameType.Challenge)
        {
            m_LevelBtnText2.text = "Challenge";
        }else{
            m_LevelBtnText2.text = "LEVEL " + GameManager.Instance.GetLevel();
        }
        m_GameArea.Init();
        m_PlayLevelBtn.onClick.AddListener(() =>
        {
            Debug.Log("Play Level Button Clicked");
            GameManager.Instance.SetGameType(GameType.Level);
            StartGame();
            if (!CommonUtil.IsApple())
            {
                if (!SaveDataManager.GetBool(CConfig.sv_TutorialGuide))
                {
                    TutorialGuide.Instance.ShowCLickMAshk();
                }
            }
           
        });
        m_PlayDailyBtn.onClick.AddListener(() =>
        {
            Debug.Log("Play Daily Button Clicked");
            if (GameManager.Instance.GetLevel() <= NetInfoMgr.instance.GameData.challengelevel)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append("Unlock daily challenges by passing ");
                sb.Append(NetInfoMgr.instance.GameData.challengelevel);
                sb.Append(" levels");
                UIManager.GetInstance().ShowUIForms(nameof(Toast), sb.ToString());
                return;
            }
            SaveDataManager.SetInt(CConfig.svmagnetUseForChallenge, 0);
            SaveDataManager.SetInt(CConfig.svmagnetcleanForChallenge, 0);
            SaveDataManager.SetInt(CConfig.svmagnetrefForChallenge, 0);
            UIManager.GetInstance().ShowUIForms(nameof(DailyPopPanel));
            GameManager.Instance.SetGameType(GameType.Challenge);
            StartGame();
        });
        CoinStr.text = NumberUtil.DoubleToStr(GameDataManager.GetInstance().getToken());
        m_DailyRewardBtn.onClick.AddListener(() =>
        {
            OpenUIForm(nameof(SignInPanel));
        });
        m_OutSettingBtn.onClick.AddListener(() =>
        {
            UIManager.GetInstance().ShowUIForms(nameof(SettingPanel), "0");
        });
        m_SettingBtn.onClick.AddListener(() =>
        {
            OpenSettingPanel();
        });
        ClickPoolInit();
        m_BackBtn.onClick.AddListener(() =>
        {
            OnBackToMainPage();
        });
        DisappearPoolInit();
    }

    private void ClickPoolInit()
    {
        // 初始化对象池
        m_ClickPool = new ObjectPool();
        m_ClickPool.Init("m_BallPool", ClickPoolParent.transform);
        m_ClickPool.Prefab = ClickPoolObject.gameObject;
    }
    private void DisappearPoolInit()
    {
        // 初始化对象池
        m_DisappearPool = new ObjectPool();
        m_DisappearPool.Init("m_BallPool", DisappearParent.transform);
        m_DisappearPool.Prefab = DisappearObject.gameObject;
    }
    
    /// <summary>
    /// 检测键盘输入
    /// </summary>
    private void Update()
    {
        // 检测W键按下
        if (Input.GetKeyDown(KeyCode.W))
        {
            Debug.Log("检测到W键按下，触发公告");
            TriggerManualAnnouncement();
        }
    }
    
    /// <summary>
    /// 手动触发公告
    /// </summary>
    private void TriggerManualAnnouncement()
    {
        if (m_NoteView != null)
        {
            m_NoteView.ManualTriggerAnnouncement();
        }
        else
        {
            Debug.LogWarning("NoteView为空，无法触发公告");
        }
    }
    public void InsClickParticle(Transform pos)
    {
        var obj = m_ClickPool.Get();
        obj.transform.position = pos.position;
        obj.SetActive(true);
        DOVirtual.DelayedCall(2f, () =>
        {
            m_ClickPool.Recycle(obj);
        });
    }

    public void InsCollectParticle(List<BubbleItem> bubbles)
    {
        var obj = m_DisappearPool.Get();
        obj.transform.position = bubbles[0].transform.position;
        obj.transform.localScale = Vector3.one;
        CollectParticleMgr collectParticleMgr = obj.GetComponent<CollectParticleMgr>();
        ImageEnum imageEnum = bubbles[0].imageEnum;
        collectParticleMgr.showParticle(bubbles[0].imageEnum);
        DOVirtual.DelayedCall(2f, () =>
        {
            collectParticleMgr.closeParticle();
            m_DisappearPool.Recycle(obj);
        });

        foreach (var bubble in bubbles)
        {
            BubbleItem bubbleItem = bubble.GetComponent<BubbleItem>();
            bubbleItem.DisableBubble();
            // 🎯 修改：不再使用对象池回收，直接销毁
            Destroy(bubble.gameObject);
        }
    }

    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
    }

    private void OpenSettingPanel()
    {
        UIManager.GetInstance().ShowUIForms(nameof(SettingPanel), "1");
    }

    public void StartGame()
    {
        if (GameManager.Instance.GetGameType() == GameType.Challenge)
        {
            m_LevelBtnText2.text = "Challenge";
            PostEventScript.GetInstance().SendEvent("1017");
            int challenge_num = SaveDataManager.GetInt(CConfig.sv_challenge_num);
            challenge_num++;
            SaveDataManager.SetInt(CConfig.sv_challenge_num, challenge_num);
        }
        else
        {
            m_LevelBtnText2.text = "LEVEL " + GameManager.Instance.GetLevel();
            if(GameManager.Instance.GetLevel()> NetInfoMgr.instance.GameData.challengelevel && !SaveDataManager.GetBool(CConfig.sv_FirstChallenge))
            {
                UIManager.GetInstance().ShowUIForms(nameof(TipsChallenge));
                return;
            }
        }
        
        m_MainPage.SetActive(false);
        m_GameArea.GameStart();
        m_NoteView.Init();
        m_GameArea.collectAreaManager.RefShowTips();
        m_GameArea.RefShowTips();
    }



    /// <summary>
    /// 开始下一关
    /// </summary>
    public void StartNextLevel()
    {
        // 更新关卡显示文本
        m_LevelBtnText.text = "LEVEL " + GameManager.Instance.GetLevel();
        if (GameManager.Instance.GetGameType() == GameType.Challenge)
        {
            m_LevelBtnText2.text = "Challenge";
        }else{
            m_LevelBtnText2.text = "LEVEL " + GameManager.Instance.GetLevel();
        }

        // 延迟一帧后开始游戏，确保UI状态正确
        StartCoroutine(DelayedStartGame());
    }

    private IEnumerator DelayedStartGame()
    {
        yield return null;
        StartGame();
    }



    public void AddCash(double cash, Transform objTrans = null)
    {
        GameDataManager.GetInstance().addToken(cash);
        CashAddAnimation(objTrans, 5);
    }
    private void CashAddAnimation(Transform startTransform, double num)
    {

        if (CommonUtil.IsApple())//审核用  没逼用
        {
            AddAnimation(startTransform, Coinimage.transform, Coinimage.gameObject, CoinStr,
                      GameDataManager.GetInstance().getToken(), num);
        }
        else
        {
            AddAnimation(startTransform, cashImg.transform, cashImg.gameObject, cashNumText, GameDataManager.GetInstance().getToken(), num);
        }
    }
    private void AddAnimation(Transform startTransform, Transform endTransform, GameObject icon, Text text,
       double textValue, double num)
    {
        if (startTransform != null)
        {
            AnimationController.GoldMoveBest(icon, Mathf.Max((int)num, 1), startTransform, endTransform,
                () =>
                {
                    ///MusicMgr.GetInstance().PlayEffect(MusicType.SceneMusic.sound_getcoin);
                    AnimationController.ChangeNumber(double.Parse(text.text), textValue, 0.1f, text,
                        () => { text.text = NumberUtil.DoubleToStr(textValue); });
                });
        }
        else
        {
            AnimationController.ChangeNumber(double.Parse(text.text), textValue, 0.1f, text,
                () => { text.text = NumberUtil.DoubleToStr(textValue); });
        }
    }

    public void ShowClickMask()
    {
        Debug.Log("ShowClickMask");
        ClickMask.SetActive(true);
    }
    public void HideClickMask()
    {
        ClickMask.SetActive(false);
    }
    public float ShowProgress()
    {
        if (m_GameArea == null)
        {
            Debug.LogWarning("ShowProgress: m_GameArea is null");
            return 0f;
        }

        float progress = m_GameArea.GetProgress();
        // 转换为百分比 (0.0f - 1.0f -> 0.0f - 100.0f)
        return progress ;
    }

    /// <summary>
    /// 重新开始游戏的处理方法
    /// </summary>
    private void OnGameRestart()
    {
        Debug.Log("重新开始游戏");

        // 重置游戏区域
        if (m_GameArea != null)
        {
            m_GameArea.ResetGame();
        }
        
        // 🎯 重置广告解锁的槽位状态
        if (m_GameArea != null && m_GameArea.collectAreaManager != null)
        {
            m_GameArea.collectAreaManager.ResetUnlockStatus();
        }
        
        // 重新开始游戏
        StartGame();
    }

    /// <summary>
    /// 重新开始游戏的处理方法
    /// </summary>
    private void OnFirstChallenge()
    {
        Debug.Log("重新开始游戏");

        // 重置游戏区域
        if (m_GameArea != null)
        {
            m_GameArea.ResetGame();
        }
        
        // 🎯 重置广告解锁的槽位状态
        if (m_GameArea != null && m_GameArea.collectAreaManager != null)
        {
            m_GameArea.collectAreaManager.ResetUnlockStatus();
        }
        
        // 重新开始游戏
        StartGame();
    }

    /// <summary>
    /// 返回主页的处理方法
    /// </summary>
    private void OnBackToMainPage()
    {
        Debug.Log("返回主页");

        // 1. 停止公告系统
        if (m_NoteView != null)
        {
            m_NoteView.StopAnnouncementSystem();
        }

        // 2. 重置游戏区域（清空棋盘）
        if (m_GameArea != null)
        {
            m_GameArea.ResetGame();
        }

        // 3. 隐藏点击遮罩
        HideClickMask();

        // 4. 显示主页
        m_MainPage.SetActive(true);
    }
}

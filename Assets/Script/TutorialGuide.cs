using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TutorialGuide : MonoBehaviour
{

    public static TutorialGuide Instance { get; private set; }
    public GameObject BlackMask;
    public GameObject clickMask;
    public GameObject clicksphand;


    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        BlackMask.SetActive(false);
        clickMask.SetActive(false);
        clicksphand.SetActive(false);
    }

    private void Start()
    {
        // 监听关卡加载完成事件
        GameEvents.TutorialClickAction += OnLevelLoadComplete;
    }

    public void ShowCLickMAshk()
    {
        BlackMask.SetActive(true);
        clickMask.SetActive(true);
        clicksphand.SetActive(true);
    }
    private void OnLevelLoadComplete()
    {
        SaveDataManager.SetBool(CConfig.sv_TutorialGuide, true);
        BlackMask.SetActive(false);
        clickMask.SetActive(false);
        clicksphand.SetActive(false);
    }
    private void OnDestroy()
    {
        GameEvents.TutorialClickAction -= OnLevelLoadComplete;
    }

}

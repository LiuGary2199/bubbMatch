using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ToolsButtons : MonoBehaviour
{
    public ToolsType toolsType;
    public GameObject toolsADBtn;
    public GameObject toolsCountobj;
    public Text toolsCountText;
    public Button toolsUse;
    private int toolsCount;
    public Action<ToolsType> OnToolsUse;


    public void Usetools(ToolsType toolsType)
    {
        switch (toolsType)
        {
            case ToolsType.MAGNET:
                if (GameManager.Instance.GetGameType() == GameType.Challenge)
                {
                    if (SaveDataManager.GetInt(CConfig.svmagnetUseForChallenge) > 0)
                    {
                        UIManager.GetInstance().ShowUIForms(nameof(Toast), "One item max for Challenge Mode.");
                        return;
                    }
                    SaveDataManager.SetInt(CConfig.svmagnetUseForChallenge, 1);
                }
                if (SaveDataManager.GetInt(CConfig.sv_tools_magnet) > 0)
                {
                    SaveDataManager.SetInt(CConfig.sv_tools_magnet, SaveDataManager.GetInt(CConfig.sv_tools_magnet) - 1);
                }
                OnToolsUse?.Invoke(toolsType);
                break;
            case ToolsType.CLEAN:
                if (GameManager.Instance.GetGameType() == GameType.Challenge)
                {
                    if (SaveDataManager.GetInt(CConfig.svmagnetcleanForChallenge) > 0)
                    {
                        UIManager.GetInstance().ShowUIForms(nameof(Toast), "One item max for Challenge Mode.");
                        return;
                    }
                    SaveDataManager.SetInt(CConfig.svmagnetcleanForChallenge, 1);
                }
                if (SaveDataManager.GetInt(CConfig.sv_tools_clean) > 0)
                {
                    SaveDataManager.SetInt(CConfig.sv_tools_clean, SaveDataManager.GetInt(CConfig.sv_tools_clean) - 1);
                }
                OnToolsUse?.Invoke(toolsType);
                break;
            case ToolsType.REFRESH:
                if (GameManager.Instance.GetGameType() == GameType.Challenge)
                {
                    if (SaveDataManager.GetInt(CConfig.svmagnetrefForChallenge) > 0)
                    {
                        UIManager.GetInstance().ShowUIForms(nameof(Toast), "One item max for Challenge Mode.");
                        return;
                    }
                    SaveDataManager.SetInt(CConfig.svmagnetrefForChallenge, 1);
                }
                if (SaveDataManager.GetInt(CConfig.sv_tools_ref) > 0)
                {
                    SaveDataManager.SetInt(CConfig.sv_tools_ref, SaveDataManager.GetInt(CConfig.sv_tools_ref) - 1);
                }
                OnToolsUse?.Invoke(toolsType);
                break;
        }
        GetToolsCount();
    }
    public void Init()
    {
        GetToolsCount();
        toolsUse.onClick.RemoveAllListeners();
        toolsUse.onClick.AddListener(() =>
        {
            toolsUse.enabled = false;
            string index = "";
            switch (toolsType)
            {
                case ToolsType.MAGNET:
                    index = "4";
                    break;
                case ToolsType.CLEAN:
                    index = "5";
                    break;
                case ToolsType.REFRESH:
                    index = "6";
                    break;
            };
            if (toolsCount > 0)
            {
                Usetools(toolsType);
                switch (toolsType)
                {
                    case ToolsType.MAGNET:
                        if (GameManager.Instance.GetGameType() == GameType.Level)
                        {
                            PostEventScript.GetInstance().SendEvent("1005", "0");
                        }else
                        {
                            PostEventScript.GetInstance().SendEvent("1009", "0");
                        }
                        break;
                    case ToolsType.CLEAN:
                        if (GameManager.Instance.GetGameType() == GameType.Level)
                        {
                            PostEventScript.GetInstance().SendEvent("1006", "0");
                        }else
                        {
                            PostEventScript.GetInstance().SendEvent("1010", "0");
                        }
                        break;
                    case ToolsType.REFRESH:
                        if (GameManager.Instance.GetGameType() == GameType.Level)
                        {
                            PostEventScript.GetInstance().SendEvent("1007", "0");
                        }else
                        {
                            PostEventScript.GetInstance().SendEvent("1011", "0");
                        }
                        break;
                }
            }
            else
            {
                if (GameManager.Instance.GetGameType() == GameType.Challenge)
                {
                    switch (toolsType)
                    {
                        case ToolsType.None:
                            break;
                        case ToolsType.MAGNET:
                            if (SaveDataManager.GetInt(CConfig.svmagnetUseForChallenge) > 0)
                            {
                                UIManager.GetInstance().ShowUIForms(nameof(Toast), "One item max for Challenge Mode.");
                                toolsUse.enabled = true;
                                return;
                            }
                            break;
                        case ToolsType.CLEAN:
                            if (SaveDataManager.GetInt(CConfig.svmagnetcleanForChallenge) > 0)
                            {
                                UIManager.GetInstance().ShowUIForms(nameof(Toast), "One item max for Challenge Mode.");
                                toolsUse.enabled = true;
                                return;
                            }
                            break;
                        case ToolsType.REFRESH:
                            if (SaveDataManager.GetInt(CConfig.svmagnetrefForChallenge) > 0)
                            {
                                UIManager.GetInstance().ShowUIForms(nameof(Toast), "One item max for Challenge Mode.");
                                toolsUse.enabled = true;
                                return;
                            }
                            break;
                        default:
                            break;
                    }
                }
                    

                ADManager.Instance.playRewardVideo((success) =>
               {
                   if (success)
                   {
                       Usetools(toolsType);
                       switch (toolsType)
                       {
                           case ToolsType.MAGNET:
                               if (GameManager.Instance.GetGameType() == GameType.Level)
                               {
                                   PostEventScript.GetInstance().SendEvent("1005", "1");
                               }
                               else
                               {
                                   PostEventScript.GetInstance().SendEvent("1009", "1");
                               }
                               break;
                           case ToolsType.CLEAN:
                               if (GameManager.Instance.GetGameType() == GameType.Level)
                               {
                                   PostEventScript.GetInstance().SendEvent("1006", "1");
                               }
                               else
                               {
                                   PostEventScript.GetInstance().SendEvent("1010", "1");
                               }
                               break;
                           case ToolsType.REFRESH:
                               if (GameManager.Instance.GetGameType() == GameType.Level)
                               {
                                   PostEventScript.GetInstance().SendEvent("1007", "1");
                               }
                               else
                               {
                                   PostEventScript.GetInstance().SendEvent("1011", "1");
                               }
                               break;
                       }
                   }
                   else
                   {
                       toolsUse.enabled = true;
                   }
               }, index);
            }
            GetToolsCount();
        });
    }

    public void GetToolsCount()
    {
        toolsCountobj.gameObject.SetActive(false);
        toolsADBtn.gameObject.SetActive(false);
        string toolsName = "";
        switch (toolsType)
        {
            case ToolsType.MAGNET:
                toolsName = CConfig.sv_tools_magnet;
                break;
            case ToolsType.CLEAN:
                toolsName = CConfig.sv_tools_clean;
                break;
            case ToolsType.REFRESH:
                toolsName = CConfig.sv_tools_ref;
                break;
        }
        toolsCount = SaveDataManager.GetInt(toolsName);
        if (toolsCount > 0)
        {
            toolsCountobj.gameObject.SetActive(true);
            toolsADBtn.gameObject.SetActive(false);

            toolsCountText.text = toolsCount.ToString();
        }
        else
        {
            toolsADBtn.gameObject.SetActive(true);
            toolsCountobj.gameObject.SetActive(false);

        }
        toolsUse.enabled = true;
    }

}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    static public GameManager Instance;
    public GameType GameType;
    private void Awake()
    {
        Instance = this;
    }



    public void SetGameType(GameType gameType)
    {
       // Debug.Log("SetGameType: " + gameType);
        GameType = gameType; // Ĭ������Ϊ�ؿ�ģʽ
    }
    public GameType GetGameType()
    {
        //Debug.Log("GetGameType: " + GameType);
        return GameType; // ���ص�ǰ��Ϸ����
    }

    public int GetLevel()
    {
        int level = SaveDataManager.GetInt(CConfig.sv_ad_trial_num);
        return level;
    }

    public void SetLevel(int level)
    {
        SaveDataManager.SetInt(CConfig.sv_ad_trial_num,level);
    }
}

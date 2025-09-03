using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameDataManager : MonoSingleton<GameDataManager>
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    public void InitGameData()
    {
    }

    // 金币
    public double getGold()
    {

        return SaveDataManager.GetDouble(CConfig.sv_GoldCoin);
    }

    public void addGold(double gold)
    {
        addGold(gold, MainManager.instance.transform);
    }

    public void addGold(double gold, Transform startTransform)
    {
        double oldGold = SaveDataManager.GetDouble(CConfig.sv_GoldCoin);
        SaveDataManager.SetDouble(CConfig.sv_GoldCoin, oldGold + gold);
        if (gold > 0)
        {
            SaveDataManager.SetDouble(CConfig.sv_CumulativeGoldCoin, SaveDataManager.GetDouble(CConfig.sv_CumulativeGoldCoin) + gold);
        }
        MessageData md = new MessageData(oldGold);
        md.valueTransform = startTransform;
        MessageCenterLogic.GetInstance().Send(CConfig.mg_ui_addgold, md);
    }
    
    // 现金
    public double getToken()
    {
        //return SaveDataManager.GetDouble(CConfig.sv_Token);
        return CashOutManager.GetInstance().Money;
    }

    public void addToken(double token)
    {
        CashOutManager.GetInstance().AddMoney((float)token);

        double oldToken = PlayerPrefs.HasKey(CConfig.sv_Token) ? double.Parse(SaveDataManager.GetString(CConfig.sv_Token)) : 0;
        double newToken = oldToken + token;
        SaveDataManager.SetDouble(CConfig.sv_Token, newToken);
        if (token > 0)
        {
            double allToken = SaveDataManager.GetDouble(CConfig.sv_CumulativeToken);
            SaveDataManager.SetDouble(CConfig.sv_CumulativeToken, allToken + token);
        }

        //addToken(token, MainManager.instance.transform);
    }
    public void addToken(double token, Transform startTransform)
    {
        double oldToken = PlayerPrefs.HasKey(CConfig.sv_Token) ? double.Parse(SaveDataManager.GetString(CConfig.sv_Token)) : 0;
        double newToken = oldToken + token;
        SaveDataManager.SetDouble(CConfig.sv_Token, newToken);
        if (token > 0)
        {
            double allToken = SaveDataManager.GetDouble(CConfig.sv_CumulativeToken);
            SaveDataManager.SetDouble(CConfig.sv_CumulativeToken, allToken + token);
        }
        MessageData md = new MessageData(oldToken);
        md.valueTransform = startTransform;
        MessageCenterLogic.GetInstance().Send(CConfig.mg_ui_addtoken, md);
    }

    //Amazon卡
    public double getAmazon()
    {
        return SaveDataManager.GetDouble(CConfig.sv_Amazon);
    }

    public void addAmazon(double amazon)
    {
        addAmazon(amazon, MainManager.instance.transform);
    }
    public void addAmazon(double amazon, Transform startTransform)
    {
        double oldAmazon = PlayerPrefs.HasKey(CConfig.sv_Amazon) ? double.Parse(SaveDataManager.GetString(CConfig.sv_Amazon)) : 0;
        double newAmazon = oldAmazon + amazon;
        SaveDataManager.SetDouble(CConfig.sv_Amazon, newAmazon);
        if (amazon > 0)
        {
            double allAmazon = SaveDataManager.GetDouble(CConfig.sv_CumulativeAmazon);
            SaveDataManager.SetDouble(CConfig.sv_CumulativeAmazon, allAmazon + amazon);
        }
        MessageData md = new MessageData(oldAmazon);
        md.valueTransform = startTransform;
        MessageCenterLogic.GetInstance().Send(CConfig.mg_ui_addamazon, md);
    }
}

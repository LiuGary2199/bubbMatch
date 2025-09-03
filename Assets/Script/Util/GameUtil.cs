using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
public class GameUtil
{
    /// <summary>
    /// 获取multi系数
    /// </summary>
    /// <returns></returns>
    private static double GetMulti(RewardType type, double cumulative, MultiGroup[] multiGroup)
    {
        foreach (MultiGroup item in multiGroup)
        {
            if (item.max > cumulative)
            {
                if (type == RewardType.Cash)
                {
                    float random = UnityEngine.Random.Range((float)NetInfoMgr.instance.InitData.cash_random[0], (float)NetInfoMgr.instance.InitData.cash_random[1]);
                    return item.multi * (1 + random);
                }
                else
                {
                    return item.multi;
                }
            }
        }
        return 1;
    }
      public static double GetInterstitialData()
  {
      double num = 0;
      RewardData interstitialData = NetInfoMgr.instance.GameData.addatalist[0];
      double cashReward = interstitialData.reward_num;
      num = Math.Round(cashReward, 2);
      return num;
  }

    public static double GetGoldMulti()
    {
        return GetMulti(RewardType.Gold, SaveDataManager.GetDouble(CConfig.sv_CumulativeGoldCoin), NetInfoMgr.instance.InitData.gold_group);
    }

    public static double GetCashMulti()
    {
        return GetMulti(RewardType.Cash, SaveDataManager.GetDouble(CConfig.sv_CumulativeToken), NetInfoMgr.instance.InitData.cash_group);
    }
    public static double GetAmazonMulti()
    {
        return GetMulti(RewardType.Amazon, SaveDataManager.GetDouble(CConfig.sv_CumulativeAmazon), NetInfoMgr.instance.InitData.amazon_group);
    }
}


/// <summary>
/// 奖励类型
/// </summary>
public enum RewardType { Gold, Cash, Amazon }

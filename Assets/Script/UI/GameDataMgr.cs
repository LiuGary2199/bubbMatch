using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

public class GameDataMgr : MonoSingleton<GameDataMgr> {
    //签到奖励
    public List<List<RewardData>> List_SignInData = new List<List<RewardData>>();
    public void InitSignInData()
    {
        List_SignInData.Clear();
        for (int i = 0; i < NetInfoMgr.instance.GameData.dailydatelist.Count; i++)
        {
            List<RewardData> list = new List<RewardData>();
            for (int j = 0; j < NetInfoMgr.instance.GameData.dailydatelist[i].Count; j++)
            {
                int num = NetInfoMgr.instance.GameData.dailydatelist[i][j].reward_num;
                num *= (int)NetInfoMgr.instance.InitData.gold_group[0].multi;
                var data = new RewardData("Cash", num);
                list.Add(data);
            }
            List_SignInData.Add(list);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//登录服务器返回数据
public class RootData
{
    public int code { get; set; }
    public string msg { get; set; }
    public ServerData data { get; set; }
}
//用户登录信息
public class ServerUserData
{
    public int code { get; set; }
    public string msg { get; set; }
    public int data { get; set; }
}
//服务器的数据
public class ServerData
{
    public string init { get; set; }
    public string version { get; set; }

    public string apple_pie { get; set; }
    public string inter_b2f_count { get; set; }
    public string inter_freq { get; set; }
    public string relax_interval { get; set; }
    public string trial_MaxNum { get; set; }
    public string nextlevel_interval { get; set; }
    public string adjust_init_rate_act { get; set; }
    public string adjust_init_act_position { get; set; }
    public string adjust_init_adrevenue { get; set; }
    //public string soho_shop { get; set; }
    public string CashOut_Data { get; set; } //真提现数据
    public string BlockRule { get; set; } //屏蔽规则
    public string game_data { get; set; } //屏蔽规则
}
public class GameData
{
     public List<BaseRewardData> bubbledatalist; // 气泡奖励列表
    public List<List<BaseRewardData>> dailydatelist { get; set; }
    public int passlevel { get; set; }
    public int bubble_cd { get; set; }
    public int normalmatch { get; set; }
    public int cashmatch { get; set; }
    public List<RewardData> addatalist; // 广告奖励列表
    public int challengelevel { get; set; }
    public int notictime { get; set; }
    
}
public class Init
{
    public List<SlotItem> slot_group { get; set; }

    public double[] cash_random { get; set; }
    public MultiGroup[] cash_group { get; set; }
    public MultiGroup[] gold_group { get; set; }
    public MultiGroup[] amazon_group { get; set; }
}

public class SlotItem
{
    public int multi { get; set; }
    public int weight { get; set; }
}

public class MultiGroup
{
    public int max { get; set; }
    public int multi { get; set; }
}

public class UserRootData
{
    public int code { get; set; }
    public string msg { get; set; }
    public string data { get; set; }
}

public class LocationData
{
    public double X;
    public double Y;
    public double Radius;
}

public class UserInfoData
{
    public double lat;
    public double lon;
    public string query; //ip地址
    public string regionName; //地区名称
    public string city; //城市名称
    public bool IsHaveApple; //是否有苹果
}

public class BlockRuleData //屏蔽规则
{
    public LocationData[] LocationList; //屏蔽位置列表
    public string[] CityList; //屏蔽城市列表
    public string[] IPList; //屏蔽IP列表
    public string fall_down; //自然量
    public bool BlockVPN; //屏蔽VPN
    public bool BlockSimulator; //屏蔽模拟器
    public bool BlockRoot; //屏蔽root
    public bool BlockDeveloper; //屏蔽开发者模式
    public bool BlockUsb; //屏蔽USB调试
    public bool BlockSimCard; //屏蔽SIM卡
}

public class CashOutData //提现
{
    public string MoneyName; //货币名称
    public string Description; //玩法描述
    public string convert_goal; //兑换目标
    public List<CashOut_TaskData> TaskList; //任务列表
}

public class CashOut_TaskData
{
    public string Name; //任务名称
    public float NowValue; //当前值
    public double Target; //目标值
    public string Description; //任务描述
    public bool IsDefault; //是否默认（循环）任务
}
public class BaseRewardData
{
    public string type { get; set; }
    public int weight { get; set; }
    public int reward_num { get; set; }
}
public class RewardData
{
    public string type;  //道具类型
    public double rewardNum;  //奖励数量
    public int reward_num; // 奖励数量
    public RewardData() { }
    public RewardData(string type, int rewardNum)
    {
        this.type = type;
        this.rewardNum = rewardNum;
    }
}
public enum GameType
{
    None = 0,
    Level = 1,
    Challenge = 2,
}
public enum ToolsType
{
    None = 0,
    MAGNET = 1,
    CLEAN = 2,
    REFRESH = 3,
}
public enum ImageEnum
{
    IMG0,
    IMG1,
    IMG2,
    IMG3,
    IMG4,
    IMG5,
    IMG6,
    IMG7,
    IMG8,
    IMG9,
    IMG10,
    IMG11,
    IMG12,
    IMG13,
    IMG14,
    IMG15,
    IMG16,
    IMG17,
    IMG18
}

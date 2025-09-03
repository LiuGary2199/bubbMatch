/**
 * 
 * 音乐类型管理的枚举列表
 * 
 * **/

//所有音乐名称的枚举列表

public class MusicType
{
    //ui使用的音乐音效
    public enum UIMusic
    {
        None,
        Sound_GoldCoin,
        Sound_OneArmBandit,
        Sound_PopShow,
        Sound_UIButton,
        Sound_PopcashShow,
        Sound_ballclick,
        Sound_match,
        Sound_matchCash,

    }

    //场景中的音效，包括场景中所有音效，包括背景音效
    public enum SceneMusic
    {
        None,
        Sound_BGM
    }

}


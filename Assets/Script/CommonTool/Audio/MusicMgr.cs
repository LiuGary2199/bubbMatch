/***
 * 
 * 音乐管理器
 * 
 * **/
using LitJson;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MusicMgr : MonoSingleton<MusicMgr>
{
    //音频组件管理队列的对象
    private AudioSourceQueue AudioQueue;
    // 用于播放背景音乐的音乐源
    private AudioSource m_bgMusic = null;
    //播放音效的音频组件管理列表
    private List<AudioSource> PlayAudioSourceList;
    //检查已经播放的音频组件列表中没有播放的组件的更新频率
    private float CheckInterval = 2f;
    //背景音乐开关
    private bool _BgMusicSwitch;
    //音效开关
    private bool _EffectMusicSwitch;
    //音乐音量
    private float _BgVolume = 1f;
    //音效音量
    private float _EffectVolume = 1f;
    string BGM_Name = "";

    public Dictionary<string, AudioModel> AudioSettingDict;

    // 控制背景音乐音量大小
    public float BgVolume
    {
        get
        {
            return BgMusicSwitch ? getVolume(BGM_Name) : 0f;
        }
        set
        {
            _BgVolume = value;
            //背景音乐开的状态下，声音随控制调节
        }
    }

    //控制音效音量的大小
    public float EffectVolmue
    {
        get { return _EffectVolume; }
        set
        {
            _EffectVolume = value;
            SetAllEffectVolume();
        }
    }
    //控制背景音乐开关
    public bool BgMusicSwitch
    {
        get
        {

            _BgMusicSwitch = SaveDataManager.GetBool("_BgMusicSwitch");
            return _BgMusicSwitch;
        }
        set
        {
            if (m_bgMusic)
            {
                _BgMusicSwitch = value;
                SaveDataManager.SetBool("_BgMusicSwitch", _BgMusicSwitch);
                m_bgMusic.volume = BgVolume;
            }
        }
    }
    public void setBgmCloseOneTime()
    {
        m_bgMusic.volume = 0;
    }
    public void setBgmReplaceOneTime()
    {
        m_bgMusic.volume = BgVolume;
    }
    //控制音效开关
    public bool EffectMusicSwitch
    {
        get
        {
            _EffectMusicSwitch = SaveDataManager.GetBool("_EffectMusicSwitch");
            return _EffectMusicSwitch;
        }
        set
        {
            _EffectMusicSwitch = value;
            SaveDataManager.SetBool("_EffectMusicSwitch", _EffectMusicSwitch);

        }
    }
    public MusicMgr()
    {
        PlayAudioSourceList = new List<AudioSource>();
    }
    protected override void Awake()
    {
        if (!PlayerPrefs.HasKey("first_music_setBool") || !SaveDataManager.GetBool("first_music_set"))
        {
            SaveDataManager.SetBool("first_music_set", true);
            SaveDataManager.SetBool("_BgMusicSwitch", true);
            SaveDataManager.SetBool("_EffectMusicSwitch", true);
        }
        AudioQueue = new AudioSourceQueue(this);

        TextAsset json = Resources.Load<TextAsset>("Audio/AudioInfo");
        AudioSettingDict = JsonMapper.ToObject<Dictionary<string, AudioModel>>(json.text);
    }
    private void Start()
    {
        StartCoroutine(nameof(CheckUnUseAudioComponent));
    }
    /// <summary>
    /// 定时检查没有使用的音频组件并回收
    /// </summary>
    /// <returns></returns>
    IEnumerator CheckUnUseAudioComponent()
    {
        while (true)
        {
            //定时更新
            yield return new WaitForSeconds(CheckInterval);
            for (int i = 0; i < PlayAudioSourceList.Count; i++)
            {
                //防止数据越界
                if (i < PlayAudioSourceList.Count)
                {
                    //确保物体存在
                    if (PlayAudioSourceList[i])
                    {
                        //音频为空或者没有播放为返回队列条件
                        if ((PlayAudioSourceList[i].clip == null || !PlayAudioSourceList[i].isPlaying))
                        {
                            //返回队列
                            AudioQueue.UnUseAudioComponent(PlayAudioSourceList[i]);
                            //从播放列表中删除
                            PlayAudioSourceList.Remove(PlayAudioSourceList[i]);
                        }
                    }
                    else
                    {
                        //移除在队列中被销毁但是是在list中存在的垃圾数据
                        PlayAudioSourceList.Remove(PlayAudioSourceList[i]);
                    }
                }

            }
        }
    }
    /// <summary>
    /// 设置当前播放的所有音效的音量
    /// </summary>
    private void SetAllEffectVolume()
    {
        for (int i = 0; i < PlayAudioSourceList.Count; i++)
        {
            if (PlayAudioSourceList[i] && PlayAudioSourceList[i].isPlaying)
            {
                PlayAudioSourceList[i].volume = _EffectMusicSwitch ? _EffectVolume : 0f;
            }
        }
    }
    /// <summary>
    /// 播放背景音乐，传进一个音频剪辑的name
    /// </summary>
    /// <param name="bgName"></param>
    /// <param name="restart"></param>
    private void PlayBgBase(object bgName, bool restart = false)
    {

        BGM_Name = bgName.ToString();
        if (m_bgMusic == null)
        {
            //拿到一个音频组件  背景音乐组件在某一时间段唯一存在
            m_bgMusic = AudioQueue.GetAudioComponent();
            //开启循环
            m_bgMusic.loop = true;
            //开始播放
            m_bgMusic.playOnAwake = false;
            //加入播放列表
            //PlayAudioSourceList.Add(m_bgMusic);
        }

        if (!BgMusicSwitch)
        {
            m_bgMusic.volume = 0;
        }

        //定义一个空的字符串
        string curBgName = string.Empty;
        //如果这个音乐源的音频剪辑不为空的话
        if (m_bgMusic.clip != null)
        {
            //得到这个音频剪辑的name
            curBgName = m_bgMusic.clip.name;
        }

        // 根据用户的音频片段名称, 找到AuioClip, 然后播放,
        //ResourcesMgr是提前定义好的查找音频剪辑对应路径的单例脚本，并动态加载出来
        AudioClip clip = Resources.Load<AudioClip>(AudioSettingDict[BGM_Name].filePath);
        //如果找到了，不为空
        if (clip != null)
        {
            //如果这个音频剪辑已经复制给类音频源，切正在播放，那么直接跳出
            if (clip.name == curBgName && !restart)
            {
                return;
            }
            //否则，把改音频剪辑赋值给音频源，然后播放
            m_bgMusic.clip = clip;
            m_bgMusic.volume = BgVolume;
            m_bgMusic.Play();
        }
        else
        {
            //没找到直接报错
            // 异常, 调用写日志的工具类.
            //UnityEngine.Debug.Log("没有找到音频片段");
            if (m_bgMusic.isPlaying)
            {
                m_bgMusic.Stop();
            }
            m_bgMusic.clip = null;
        }
    }
    /// <summary>
    /// 播放音效
    /// </summary>
    /// <param name="effectName"></param>
    /// <param name="defAudio"></param>
    /// <param name="volume"></param>
    private void PlayEffectBase(object effectName, bool defAudio = true, float volume = 1f)
    {
        if (!EffectMusicSwitch)
        {
            return;
        }
        //获取音频组件
        AudioSource m_effectMusic = AudioQueue.GetAudioComponent();
        if (m_effectMusic.isPlaying)
        {
            //Debug.Log("-------------------------------当前音效正在播放,直接返回");
            return;
        };
        m_effectMusic.loop = false;
        m_effectMusic.playOnAwake = false;
        m_effectMusic.volume = getVolume(effectName.ToString());
        //Debug.Log(m_effectMusic.volume);
        //根据查找路径加载对应的音频剪辑
        AudioClip clip = Resources.Load<AudioClip>(AudioSettingDict[effectName.ToString()].filePath);
        //如果为空的话，直接报错，然后跳出
        if (clip == null)
        {
            //UnityEngine.Debug.Log("没有找到音效片段");
            //没加入播放列表直接返回给队列
            AudioQueue.UnUseAudioComponent(m_effectMusic);
            return;
        }
        m_effectMusic.clip = clip;
        //加入播放列表
        PlayAudioSourceList.Add(m_effectMusic);
        //否则，就是clip不为空的话，如果defAudio=true，直接播放
        if (defAudio)
        {
            m_effectMusic.PlayOneShot(clip, volume);
        }
        else
        {
            //指定点播放
            AudioSource.PlayClipAtPoint(clip, Camera.main.transform.position, volume);
        }
    }

    //播放各种音频剪辑的调用方法，MusicType是提前写好的存放各种音乐名称的枚举类，便于外面直接调用
    public void PlayBg(MusicType.UIMusic bgName, bool restart = false)
    {
        PlayBgBase(bgName, restart);
    }

    public void PlayBg(MusicType.SceneMusic bgName, bool restart = false)
    {
        PlayBgBase(bgName, restart);
    }

    //播放各种音频剪辑的调用方法，MusicType是提前写好的存放各种音乐名称的枚举类，便于外面直接调用
    public void PlayEffect(MusicType.UIMusic effectName, bool defAudio = true, float volume = 1f)
    {
        PlayEffectBase(effectName, defAudio, volume);
    }

    public void PlayEffect(MusicType.SceneMusic effectName, bool defAudio = true, float volume = 1f)
    {
        PlayEffectBase(effectName, defAudio, volume);
    }
    float getVolume(string name)
    {
        if (AudioSettingDict == null)
        {
            TextAsset json = Resources.Load<TextAsset>("Audio/AudioInfo");
            AudioSettingDict = JsonMapper.ToObject<Dictionary<string, AudioModel>>(json.text);
        }

        if (AudioSettingDict.ContainsKey(name))
        {
            return (float)AudioSettingDict[name].volume;

        }
        else
        {
            return 1;
        }
    }

}
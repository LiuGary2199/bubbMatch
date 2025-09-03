/***
 * 
 * AudioSource组件管理(音效，背景音乐除外)
 * 
 * **/
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class AudioSourceQueue 
{
    //音乐的管理者
    private GameObject AudioMgr;
    //音乐组件管理队列
    private List<AudioSource> AudioComponentQueue ;
    //音乐组件默认容器最大值  
    private int MaxCount = 25;
    public AudioSourceQueue(MusicMgr audioMgr)
    {
        AudioMgr = audioMgr.gameObject;
        InitAudioSourceQueue();
    }
  
    /// <summary>
    /// 初始化队列
    /// </summary>
    private void InitAudioSourceQueue()
    {
        AudioComponentQueue = new List<AudioSource>();
        for(int i = 0; i < MaxCount; i++)
        {
            AddAudioSourceForAudoMgr();
        }
    }
    /// <summary>
    /// 给音乐的管理者添加音频组件，同时组件加入队列
    /// </summary>
    private AudioSource AddAudioSourceForAudoMgr()
    {
        AudioSource audio = AudioMgr.AddComponent<AudioSource>();
        AudioComponentQueue.Add(audio);
        return audio;
    }
    /// <summary>
    /// 获取一个音频组件
    /// </summary>
    /// <param name="audioMgr"></param>
    /// <returns></returns>
    public AudioSource GetAudioComponent()
    {
        if (AudioComponentQueue.Count > 0)
        {
            AudioSource audio = AudioComponentQueue.Find(t => !t.isPlaying);
            if (audio)
            {
                AudioComponentQueue.Remove(audio);
                return audio;
            }
            //队列中没有了，需额外添加
            return AddAudioSourceForAudoMgr();
            //直接返回队列中存在的组件
            //return AudioComponentQueue.Dequeue();
        }
        else
        {
            //队列中没有了，需额外添加
            return  AddAudioSourceForAudoMgr();
        }
    }
    /// <summary>
    /// 没有被使用的音频组件返回给队列
    /// </summary>
    /// <param name="audio"></param>
    public void UnUseAudioComponent(AudioSource audio)
    {
        if (AudioComponentQueue.Contains(audio)) return;
        if (AudioComponentQueue.Count >= MaxCount)
        {
            GameObject.Destroy(audio);
            //Debug.Log("删除组件");
        }
        else
        {
            audio.clip = null;
            AudioComponentQueue.Add(audio);
        }

        //Debug.Log("队列长度是" + AudioComponentQueue.Count);
    }
    
}

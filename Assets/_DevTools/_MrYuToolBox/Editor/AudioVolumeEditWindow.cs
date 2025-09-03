using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using LitJson;
using System;

public class AudioVolumeEditWindow : EditorWindow
{
    Vector3 scrollPos = Vector2.zero;
    Dictionary<string, AudioModel> UIAudioGroup;
    Dictionary<string, AudioModel> SceneAudioGroup;
    //序列化对象
    protected SerializedObject _serializedObject;
    [MenuItem("遇先生工具包/一键[调音]")]
    static void ShowAudioEditWindow()
    {
        EditorWindow.GetWindow(typeof(AudioVolumeEditWindow));

    }
    protected void OnEnable()
    {
        //使用当前类初始化
        _serializedObject = new SerializedObject(this);
        refreshAudioData();
    }
    void refreshAudioData()
    {
        UIAudioGroup = new Dictionary<string, AudioModel>();
        SceneAudioGroup = new Dictionary<string, AudioModel>();

        string Filepath = Application.streamingAssetsPath + "/Audio/AudioInfo" + ".json";
        Filepath = Filepath.Replace("StreamingAssets", "Resources");
        
        FileInfo FileInfo = new FileInfo(Filepath);
        Dictionary<string, AudioModel> audioDic = new Dictionary<string, AudioModel>();
        if (FileInfo.Exists)
        {
            audioDic = LoadJson(Filepath);
        }

        //UI
        foreach(object music in Enum.GetValues(typeof(MusicType.UIMusic)))
        {
            string name = music.ToString();
            if (name == "None") continue;
            AudioModel model = new AudioModel();
            if (audioDic.ContainsKey(name))
            {
                model = audioDic[name];
            }
            else
            {
                model.name = name;
                model.volume = 1;
                model.filePath = "";
            }
            UIAudioGroup.Add(name, model);
        }

        // Scene
        foreach (object music in Enum.GetValues(typeof(MusicType.SceneMusic)))
        {
            string name = music.ToString();
            if (name == "None") continue;
            AudioModel model = new AudioModel();
            if (audioDic.ContainsKey(name))
            {
                model = audioDic[name];
            }
            else
            {
                model.name = name;
                model.volume = 1;
                model.filePath = "";
            }
            SceneAudioGroup.Add(name, model);
        }
    }
    public Dictionary<string, AudioModel> LoadJson(string Filepath)
    {
        StreamReader reader = new StreamReader(Filepath);
        string jsonString = reader.ReadToEnd();
        Dictionary<string, AudioModel> audioGroup = JsonMapper.ToObject<Dictionary<string, AudioModel>>(jsonString);
        reader.Dispose();
        reader.Close();
        return audioGroup;
    }
    public void SaveJson(string Filepath)
    {
        Dictionary<string, AudioModel> dic = new Dictionary<string, AudioModel>();
        MergeDictionaries.Merge(dic, UIAudioGroup);
        MergeDictionaries.Merge(dic, SceneAudioGroup);
        string JsonData = JsonMapper.ToJson(dic);
        StreamWriter ResourceWrite = new StreamWriter(Filepath);
        ResourceWrite.WriteLine(JsonData);
        ResourceWrite.Dispose();
        ResourceWrite.Close();
    }
    void OnGUI()
    {
        GUILayout.Space(10);
        GUILayout.Label("音量编辑");
        scrollPos = GUILayout.BeginScrollView(scrollPos);
        GUILayout.Space(20);
        GUILayout.Label("Scene");
        List<string> SceneKeys = new List<string>(SceneAudioGroup.Keys);
        for (int i = 0; i < SceneAudioGroup.Keys.Count; i++)
        {
            string key = SceneKeys[i];
            GUILayout.Label(key);

            // 音量
            GUILayout.BeginHorizontal();
            GUILayout.Label("音量", GUILayout.Width(150));
            double volume = SceneAudioGroup[key].volume;
            volume = GUILayout.HorizontalSlider((float)volume, 0f, 1f, GUILayout.Width(100));
            volume = float.Parse(GUILayout.TextField(volume.ToString(), GUILayout.Width(50)));
            SceneAudioGroup[key].volume = volume;
            GUILayout.EndHorizontal();
            // 文件
            AudioClip audio = string.IsNullOrEmpty(SceneAudioGroup[key].filePath) ? null : Resources.Load<AudioClip>(SceneAudioGroup[key].filePath);
            audio = (AudioClip)EditorGUILayout.ObjectField("音频文件", audio, typeof(AudioClip), false);
            if (audio != null)
            {
                string filePath = AssetDatabase.GetAssetPath(audio).Replace("Assets/Resources/", "");
                SceneAudioGroup[key].filePath = filePath.Substring(0, filePath.LastIndexOf("."));
            }
            else
            {
                SceneAudioGroup[key].filePath = "";
            }

            GUILayout.Space(10);
        }

        GUILayout.Space(20);
        GUILayout.Label("UI");
        List<string> UIKeys = new List<string>(UIAudioGroup.Keys);
        for (int i = 0; i < UIAudioGroup.Keys.Count; i++)
        {
            string key = UIKeys[i];
            GUILayout.Label(key);
            // 音量
            GUILayout.BeginHorizontal();
            GUILayout.Label("音量", GUILayout.Width(150));
            double volume = UIAudioGroup[key].volume;
            volume = GUILayout.HorizontalSlider((float)volume, 0f, 1f, GUILayout.Width(100));
            volume = float.Parse(GUILayout.TextField(volume.ToString(), GUILayout.Width(50)));
            UIAudioGroup[key].volume = volume;
            GUILayout.EndHorizontal();
            // 文件
            AudioClip audio = string.IsNullOrEmpty(UIAudioGroup[key].filePath) ? null : Resources.Load<AudioClip>(UIAudioGroup[key].filePath);
            audio = (AudioClip)EditorGUILayout.ObjectField("音频文件", audio, typeof(AudioClip), false);
            if (audio != null)
            {
                string filePath = AssetDatabase.GetAssetPath(audio).Replace("Assets/Resources/", "");
                UIAudioGroup[key].filePath = filePath.Substring(0, filePath.LastIndexOf("."));
            }
            else
            {
                UIAudioGroup[key].filePath = "";
            }

            GUILayout.Space(10);
        }
        
        GUILayout.EndScrollView();
        GUILayout.Space(10);
        if (GUILayout.Button("保存"))
        {
            string Filepath = Application.streamingAssetsPath + "/Audio/AudioInfo" + ".json";
            Filepath = Filepath.Replace("StreamingAssets", "Resources");
            SaveJson(Filepath);
            AssetDatabase.Refresh();
        }
    }
}

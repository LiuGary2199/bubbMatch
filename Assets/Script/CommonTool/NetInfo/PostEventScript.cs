using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Networking;
using System;
public class PostEventScript : MonoSingleton<PostEventScript>
{
    public string version = "1.2";
    public string GameCode = NetInfoMgr.instance.GameCode;
    //channel
#if UNITY_IOS
    private string Channel = "AppStore";
#elif UNITY_ANDROID
    private string Channel = "GooglePlay";
#else
    private string Channel = "GooglePlay";
#endif


    private void OnApplicationPause(bool pause)
    {
        PostEventScript.GetInstance().sendGameProgress();
    }

    public Text text;

    protected override void Awake()
    {
        base.Awake();

        version = Application.version;
        StartCoroutine(nameof(autoMessage));
    }
    IEnumerator autoMessage()
    {
        while (true)
        {
            yield return new WaitForSeconds(120f);
            PostEventScript.GetInstance().sendGameProgress();
        }
    }
    private void Start()
    {
        if (SaveDataManager.GetInt("event_day") != DateTime.Now.Day && SaveDataManager.GetString("user_servers_id").Length != 0)
        {
            SaveDataManager.SetInt("event_day", DateTime.Now.Day);
        }
    }
    public void SendNoParaEvent(string event_id)
    {
        SendEvent(event_id);
    }
    public void sendGameProgress(List<string> valueList = null)
    {
        if (SaveDataManager.GetDouble(CConfig.sv_CumulativeGoldCoin) == 0)
        {
            SaveDataManager.SetDouble(CConfig.sv_CumulativeGoldCoin, SaveDataManager.GetDouble(CConfig.sv_GoldCoin));
        }
        if (SaveDataManager.GetDouble(CConfig.sv_CumulativeToken) == 0)
        {
            SaveDataManager.SetDouble(CConfig.sv_CumulativeToken, SaveDataManager.GetDouble(CConfig.sv_Token));
        }
        if (valueList == null)
        {
            valueList = new List<string>() {
                SaveDataManager.GetInt(CConfig.sv_ad_trial_num).ToString(),
                SaveDataManager.GetInt(CConfig.sv_challenge_num).ToString(),
                GameDataManager.GetInstance().getToken().ToString(),
                //SaveDataManager.GetInt(SlotConfig.sv_SlotSpinCount).ToString()
            };
        }

        if (SaveDataManager.GetString(CConfig.sv_LocalServerId) == null)
        {
            return;
        }
        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField("gameCode", GameCode);
        wwwForm.AddField("userId", SaveDataManager.GetString(CConfig.sv_LocalServerId));

        wwwForm.AddField("gameVersion", version);

        wwwForm.AddField("channel", Channel);

        for (int i = 0; i < valueList.Count; i++)
        {
            wwwForm.AddField("resource" + (i + 1), valueList[i]);
        }



        StartCoroutine(SendPost(NetInfoMgr.instance.BaseUrl + "/api/client/game_progress", wwwForm,
        (error) =>
        {
            Debug.Log(error);
        },
        (message) =>
        {
            Debug.Log(message);
        }));
    }
    public void SendEvent(string event_id, string p1 = null, string p2 = null, string p3 = null)
    {
        if (text != null)
        {
            if (int.Parse(event_id) < 9100 && int.Parse(event_id) >= 9000)
            {
                if (p1 == null)
                {
                    p1 = "";
                }
                text.text += "\n" + DateTime.Now.ToString() + "id:" + event_id + "  p1:" + p1;
            }
        }
        if (SaveDataManager.GetString(CConfig.sv_LocalServerId) == null)
        {
            NetInfoMgr.instance.Login();
            return;
        }
        WWWForm wwwForm = new WWWForm();
        wwwForm.AddField("gameCode", GameCode);
        wwwForm.AddField("userId", SaveDataManager.GetString(CConfig.sv_LocalServerId));
        //Debug.Log("userId:" + SaveDataManager.GetString(CConfig.sv_LocalServerId));
        wwwForm.AddField("version", version);
        //Debug.Log("version:" + version);
        wwwForm.AddField("channel", Channel);
        //Debug.Log("channel:" + channal);
        wwwForm.AddField("operateId", event_id);
        Debug.Log("operateId:" + event_id);


        if (p1 != null)
        {
            wwwForm.AddField("params1", p1);
        }
        if (p2 != null)
        {
            wwwForm.AddField("params2", p2);
        }
        if (p3 != null)
        {
            wwwForm.AddField("params3", p3);
        }
        StartCoroutine(SendPost(NetInfoMgr.instance.BaseUrl + "/api/client/log", wwwForm,
        (error) =>
        {
            Debug.Log(error);
        },
        (message) =>
        {
            Debug.Log(message);
        }));
    }
    IEnumerator SendPost(string _url, WWWForm wwwForm, Action<string> fail, Action<string> success)
    {
        //Debug.Log(SerializeDictionaryToJsonString(dic));
        using UnityWebRequest request = UnityWebRequest.Post(_url, wwwForm);
        yield return request.SendWebRequest();
        if (request.isNetworkError || request.isNetworkError)
        {
            fail(request.error);
            endRequest();
            request.Dispose();
        }
        else
        {
            success(request.downloadHandler.text);
            endRequest();
            request.Dispose();
        }
    }
    private void endRequest()
    {
        StopCoroutine(nameof(SendPost));
    }


}
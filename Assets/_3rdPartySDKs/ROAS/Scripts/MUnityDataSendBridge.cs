using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MUnityDataSendBridge
{
    private static MUnityDataSendBridge _instance = new MUnityDataSendBridge();
    private readonly AndroidJavaObject _mUnityDataSend;
    public bool isDebug = false;
#if UNITY_IOS || UNITY_IPHONE
    [DllImport("__Internal")]
    private static extern void _setDebug(bool debug);

#endif
    private MUnityDataSendBridge()
    {
        if (Application.platform == RuntimePlatform.Android) {
            _mUnityDataSend = new AndroidJavaObject("com.mbridge.msdk.unity.MUnityDataReceiver");
        }
    }

    public static MUnityDataSendBridge getInstance()
    {
        return _instance;
    }

    public void initialize(string appID, string appKey)
    {
        try
        {
            AndroidJavaObject applicationContext = getApplicationContext();
            if (applicationContext != null)
            {
                _mUnityDataSend.CallStatic("initialize", applicationContext, appID, appKey);
            }
            else
            {
                Debug.LogError("Failed to get application context.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception occurred while calling trackAdRevenue: " + ex.Message);
        }
    }

    public void trackAdRevenue(string trackAdJsonStr,string extraJsonStr)
    {
        
        try
        {
            AndroidJavaObject applicationContext = getApplicationContext();
            if (applicationContext != null)
            {
                _mUnityDataSend.CallStatic("trackAdRevenue", applicationContext, trackAdJsonStr, extraJsonStr);
            }
            else
            {
                Debug.LogError("Failed to get application context.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception occurred while calling trackAdRevenue: " + ex.Message);
        }
    }

    public void trackAdRevenue(string trackAdJsonStr)
    {
        trackAdRevenue(trackAdJsonStr,"");
    }

    public void trackAdCustom(string trackAdJsonStr)
    {
        try
        {
            AndroidJavaObject applicationContext = getApplicationContext();
            if (applicationContext != null)
            {
                _mUnityDataSend.CallStatic("trackAdCustom", applicationContext, trackAdJsonStr);
            }
            else
            {
                Debug.LogError("Failed to get application context.");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception occurred while calling trackAdRevenue: " + ex.Message);
        }
    }

    private AndroidJavaObject getApplicationContext()
    {
        try
        {
            using (AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
            {
                using (AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    return jo.Call<AndroidJavaObject>("getApplicationContext");
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception occurred while getting application context: " + ex.Message);
        }
        return null;
    }

    public void setDebug(bool debug)
    {
        try
        {
            isDebug = debug;
#if UNITY_ANDROID
            _mUnityDataSend.CallStatic("setDebug", debug);
#elif UNITY_IOS || UNITY_IPHONE
            _setDebug(debug);
#endif
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Exception occurred while calling trackAdRevenue: " + ex.Message);
        }
    }
}
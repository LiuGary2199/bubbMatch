using System.Runtime.InteropServices;
using UnityEngine;

public class RateUsManager : MonoBehaviour
{
    public static RateUsManager instance;

    public string appid;
    //获取IOS函数声明
#if UNITY_IOS
    [DllImport("__Internal")]
    internal extern static void openRateUsUrl(string appId);
#endif

    private void Awake()
    {
        instance = this;
    }

    public void OpenAPPinMarket()
    {
#if UNITY_ANDROID || UNITY_EDITOR
        Application.OpenURL("market://details?id=" + appid);
#elif UNITY_IOS
        openRateUsUrl(appid);
#endif
    }
}

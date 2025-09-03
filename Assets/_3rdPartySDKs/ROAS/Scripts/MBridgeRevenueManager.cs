using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public class MBridgeRevenueManager
{
    public const string MBridge_Version = "1.0.3";
    
#if UNITY_IOS || UNITY_IPHONE
    [DllImport("__Internal")]
    private static extern void _trackAdRevenueWithAdRevenueModel(string info);
    
    [DllImport("__Internal")]
    private static extern void _trackAdRevenueWithAdCustomModel(string info);

#endif

    public static void TrackCustom(MBridgeRevenueCustomAdData adData)
    {
       Dictionary<string, object> dic = new Dictionary<string, object>();
       var properties = typeof(MBridgeRevenueCustomAdData).GetProperties();
       foreach (var property in properties)
       {
           var propertyName = property.Name;
           var propertyValue = property.GetValue(adData);
           if (propertyValue != null)
           {
               dic.Add(propertyName, propertyValue);
           }
       }
       dic.Add("mBridge_Version", MBridge_Version);
       var json=ROSA.ThirdParty.MiniJson.Json.Serialize(dic);
#if UNITY_IOS || UNITY_IPHONE
        _trackAdRevenueWithAdCustomModel(json);
#elif UNITY_ANDROID
        MUnityDataSendBridge.getInstance().trackAdCustom(json);
#endif


    }
   public static void Track(MBridgeRevenueParamsEntity mBridgeRevenueParamsEntity)
    {
        try
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            dic.Add("attributionPlatformName", mBridgeRevenueParamsEntity.attributionPlatformName);
            dic.Add("attributionUserID", mBridgeRevenueParamsEntity.attributionPlatformUserId);
            dic.Add("mBridge_Version", MBridge_Version);
            #region AdMob

#if AdMob
            dic.Add("mediationName", mediationName.Admob.ToString());
            dic.Add("adUnitID", mBridgeRevenueParamsEntity.adUnitID);
            if (mBridgeRevenueParamsEntity.adType == admobAdType.None)
                dic.Add("adType", "");
            else
                dic.Add("adType", mBridgeRevenueParamsEntity.adType.ToString());

            if (mBridgeRevenueParamsEntity.adValue == null)
                dic.Add("adValue", "");
            else
            {
                Dictionary<string, object> dicadValue = new Dictionary<string, object>();
                dicadValue.Add("CurrencyCode", mBridgeRevenueParamsEntity.adValue.CurrencyCode);
                dicadValue.Add("Precision", mBridgeRevenueParamsEntity.adValue.Precision);
                dicadValue.Add("Value", mBridgeRevenueParamsEntity.adValue.Value);

                dic.Add("adValue", dicadValue);
            }
            if (mBridgeRevenueParamsEntity.responseInfo == null)
            {
                dic.Add("loadedadapterResponseInfo", "");
            }
            else
            {
                var re = mBridgeRevenueParamsEntity.responseInfo.GetLoadedAdapterResponseInfo();
                Dictionary<string, object> dicadapterResponseInfo = new Dictionary<string, object>();

                dicadapterResponseInfo.Add("AdapterClassName", re.AdapterClassName);
                dicadapterResponseInfo.Add("AdSourceId", re.AdSourceId);
                dicadapterResponseInfo.Add("AdSourceName", re.AdSourceName);
                dicadapterResponseInfo.Add("AdSourceInstanceId", re.AdSourceInstanceId);
                dicadapterResponseInfo.Add("AdSourceInstanceName", re.AdSourceInstanceName);
                dicadapterResponseInfo.Add("AdUnitMapping", re.AdUnitMapping);
                dicadapterResponseInfo.Add("AdError", re.AdError);
                dicadapterResponseInfo.Add("LatencyMillis", re.LatencyMillis);
                dic.Add("loadedadapterResponseInfo", dicadapterResponseInfo);
            }
            var admobAdJson=ROSA.ThirdParty.MiniJson.Json.Serialize(dic);

            if (MUnityDataSendBridge.getInstance().isDebug)
                Debug.Log("ROAS  AdMob Ad JSON"+ admobAdJson);
#if UNITY_IOS || UNITY_IPHONE
            _trackAdRevenueWithAdRevenueModel(admobAdJson);
#elif UNITY_ANDROID

            var responseInfo = "";
            if (mBridgeRevenueParamsEntity!=null && mBridgeRevenueParamsEntity.responseInfo!=null) {
                responseInfo = mBridgeRevenueParamsEntity.responseInfo.ToString();
            }

            MUnityDataSendBridge.getInstance().trackAdRevenue(admobAdJson, responseInfo);

#endif
#endif
            #endregion
            
            #region ironSource
#if ironSource

            if (mBridgeRevenueParamsEntity!=null && mBridgeRevenueParamsEntity.instanceid == null)
            {
                dic.Add("irinstanceid", "");
            }
            else {
                dic.Add("irinstanceid", mBridgeRevenueParamsEntity.instanceid);
            }
           
            dic.Add("mediationName", mediationName.IronSource.ToString());
            if (mBridgeRevenueParamsEntity.ironSourceImpressionData == null)
                dic.Add("ironSourceImpressionData","");
            else
                dic.Add("ironSourceImpressionData", ROSA.ThirdParty.MiniJson.Json.Deserialize(mBridgeRevenueParamsEntity.ironSourceImpressionData.allData));
            var ironSourceAdJsonStr = ROSA.ThirdParty.MiniJson.Json.Serialize(dic);
            if (MUnityDataSendBridge.getInstance().isDebug)
                Debug.Log("ROAS  ironSource Ad JSON" + ironSourceAdJsonStr);
#if UNITY_IOS || UNITY_IPHONE
            _trackAdRevenueWithAdRevenueModel(ironSourceAdJsonStr);
#elif UNITY_ANDROID
            MUnityDataSendBridge.getInstance().trackAdRevenue(ironSourceAdJsonStr);        

#endif
#endif
           #endregion

            #region MAX
#if MAX
            dic.Add("mediationName", mediationName.Max.ToString());
            if (mBridgeRevenueParamsEntity.AdInfo == null)
                dic.Add("adInfo", "");
            else
            {
                var adInfo = mBridgeRevenueParamsEntity.AdInfo;
                Dictionary<string, object> dicadInfo = new Dictionary<string, object>();
                dicadInfo.Add("AdUnitIdentifier", adInfo.AdUnitIdentifier);
                dicadInfo.Add("AdFormat", adInfo.AdFormat);
                dicadInfo.Add("NetworkName", adInfo.NetworkName);
                dicadInfo.Add("NetworkPlacement", adInfo.NetworkPlacement);
                dicadInfo.Add("Placement", adInfo.Placement);
                dicadInfo.Add("Revenue", adInfo.Revenue);
                dicadInfo.Add("CreativeIdentifier", adInfo.CreativeIdentifier);
                dicadInfo.Add("RevenuePrecision", adInfo.RevenuePrecision);
                var WaterfallInfo = mBridgeRevenueParamsEntity.AdInfo.WaterfallInfo;
                Dictionary<string, object> dicWaterfallInfo = new Dictionary<string, object>();

                dicWaterfallInfo.Add("Name", WaterfallInfo.Name);
                dicWaterfallInfo.Add("TestName", WaterfallInfo.TestName);

                var NetworkResponses = mBridgeRevenueParamsEntity.AdInfo.WaterfallInfo.NetworkResponses;
                Dictionary<string, object> dicNetworkResponses = new Dictionary<string, object>();

                foreach (var item in NetworkResponses)
                {
                    //Debug.Log("SerializeObject AdLoadState" + (int)item.AdLoadState);
                    if ((int)item.AdLoadState == 1)
                    {
                        dicNetworkResponses.Add("AdLoadState", (int)item.AdLoadState);
                        var MediatedNetwork = item.MediatedNetwork;
                        Dictionary<string, object> dicMediatedNetwork = new Dictionary<string, object>();

                        dicMediatedNetwork.Add("Name", MediatedNetwork.Name);
                        dicMediatedNetwork.Add("AdapterClassName", MediatedNetwork.AdapterClassName);
                        dicMediatedNetwork.Add("AdapterVersion", MediatedNetwork.AdapterVersion);
                        dicMediatedNetwork.Add("SdkVersion", MediatedNetwork.SdkVersion);
                        dicNetworkResponses.Add("MediatedNetwork", dicMediatedNetwork);
                        dicNetworkResponses.Add("IsBidding", item.IsBidding);
                        dicNetworkResponses.Add("Credentials", item.Credentials);
                        dicNetworkResponses.Add("LatencyMillis", item.LatencyMillis);
                        dicNetworkResponses.Add("Error", item.Error);
                    }
                }
                dicWaterfallInfo.Add("NetworkResponses", dicNetworkResponses);
                dicWaterfallInfo.Add("LatencyMillis", WaterfallInfo.LatencyMillis);

                dicadInfo.Add("WaterfallInfo", dicWaterfallInfo);
                dicadInfo.Add("DspName", adInfo.DspName);
                dic.Add("adInfo", dicadInfo);
            }
            var maxJsonStr=ROSA.ThirdParty.MiniJson.Json.Serialize(dic);
            if (MUnityDataSendBridge.getInstance().isDebug)
                Debug.Log("ROAS  MAX Ad JSON" + maxJsonStr);
#if UNITY_IOS || UNITY_IPHONE
            _trackAdRevenueWithAdRevenueModel(maxJsonStr);

#elif UNITY_ANDROID

            MUnityDataSendBridge.getInstance().trackAdRevenue(maxJsonStr);

#endif
#endif
            #endregion

            #region TRADPLUS
#if TRADPLUS
            dic.Add("mediationName", mediationName.Tradeplus.ToString());
            if (mBridgeRevenueParamsEntity.tradplusadInfo == null)
                dic.Add("adInfo", "");
            else
                dic.Add("adInfo",mBridgeRevenueParamsEntity.tradplusadInfo);
           
            var TPAdJsonStr = ROSA.ThirdParty.MiniJson.Json.Serialize(dic);
            if (MUnityDataSendBridge.getInstance().isDebug)
                Debug.Log("ROAS  tradplus Ad JSON" + TPAdJsonStr);
#if UNITY_IOS || UNITY_IPHONE
            _trackAdRevenueWithAdRevenueModel(TPAdJsonStr);
#elif UNITY_ANDROID
            MUnityDataSendBridge.getInstance().trackAdRevenue(TPAdJsonStr);
#endif
#endif


            #endregion
        }
        
        catch (System.Exception ex)
        {
            Debug.LogError("ROSA Unity Plugins Error"+ ex);
        }
     




    }

}
public enum mediationName
{
    Max,
    Admob,
    IronSource,
    Tradeplus
}

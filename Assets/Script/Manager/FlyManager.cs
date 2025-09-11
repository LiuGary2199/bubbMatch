using System.Collections;
using System.Collections.Generic;
using UnityEngine;
public class FlyManager : MonoBehaviour
{
    public GameObject FlyItem;
    public static FlyManager Instance;

    public bool isOpenFly;
    public int leftOrRight;

    private int _startOpenTime;
    private int _flyAddTime;
    
    private void Awake()
    {
        Instance = this;
        _flyAddTime = 0;
        isOpenFly = true;
        _startOpenTime = NetInfoMgr.instance.GameData.bubble_cd;
        leftOrRight = 0;
    }

    private void OnEnable()
    {
        OpenIEFly();
    }
   
    public void OpenIEFly()
    {
        StopCoroutine(nameof(OpenFlyBubble));
        StartCoroutine(nameof(OpenFlyBubble));
    }
    IEnumerator OpenFlyBubble()
    {
        while (isOpenFly)
        {   
            if (_flyAddTime >= _startOpenTime)
            {
                CreateFlyItem();
            }
            _flyAddTime++;
            yield return new WaitForSeconds(1);
        }
    }

    public void DeleteFlyItem()
    {
        if (transform.childCount > 0)
        {
            transform.GetChild(0).GetComponent<FlyItem>().DestroyFlyItem();
            isOpenFly = true;
        }
    }

    public void CreateFlyItem()
    {
        if (!isOpenFly) { return; }
        // 新增：引导阶段禁止飞行气泡
       // if (SaveDataManager.GetInt(CConfig.sv_ad_trial_num) <= 1)
       // {
        //    return;
       // }
      if ( !CommonUtil.IsApple())
        {
            isOpenFly = false;
            _flyAddTime = 0;
            GameObject obj = Instantiate(FlyItem.gameObject);
            obj.transform.SetParent(transform);
            obj.transform.localScale = Vector3.one;
            obj.transform.localPosition = leftOrRight == 0 ? new Vector3(-650, 450, 0) : new Vector3(650, 450, 0);
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SlotGroup : MonoBehaviour
{
    public GameObject InitGroup;

    private GameObject templateMultiObject;
    private float itemWidth = 142f; // 两个item的position.x之差

    public Sprite[] sprites;

    // Start is called before the first frame update
    void Start()
    {
        templateMultiObject = InitGroup.transform.Find("SlotCard_1").gameObject;
        float x = itemWidth * 3;
        int multiCount = NetInfoMgr.instance.InitData.slot_group.Count;
        for (int i = 0; i < 5; i++)
        {
            for (int j = 0; j < multiCount; j++)
            {
                GameObject fangkuai = Instantiate(templateMultiObject, InitGroup.transform);
                fangkuai.transform.localPosition = new Vector3(x + itemWidth * multiCount * i + itemWidth * j, templateMultiObject.transform.localPosition.y, 0);
                fangkuai.transform.Find("Text").GetComponent<Text>().text = "×" + NetInfoMgr.instance.InitData.slot_group[j].multi;
                fangkuai.transform.GetComponent<Image>().sprite = sprites[j];
            }
        }
    }

    public void initMulti()
    {
        InitGroup.GetComponent<RectTransform>().localPosition = new Vector3(0, -10, 0);
    }

    public void slot(int index, Action<int> finish)
    {
        MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_OneArmBandit);
        AnimationController.HorizontalScroll(InitGroup, -(itemWidth * 2 + itemWidth * NetInfoMgr.instance.InitData.slot_group.Count * 3 + itemWidth * (index + 1)), () =>
        {
            finish?.Invoke(NetInfoMgr.instance.InitData.slot_group[index].multi);
        });
    }
}

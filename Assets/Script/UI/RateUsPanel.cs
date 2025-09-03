using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class RateUsPanel : BaseUIForms
{
    public Button[] Stars;
    public Sprite star1Sprite;
    public Sprite star2Sprite;

    // Start is called before the first frame update
    void Start()
    {
        foreach (Button star in Stars)
        {
            star.onClick.AddListener(() =>
            {
                string indexStr = System.Text.RegularExpressions.Regex.Replace(star.gameObject.name, @"[^0-9]+", "");
                int index = indexStr == "" ? 0 : int.Parse(indexStr);
                lightStart(index);
            });
        }
    }

    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
        for (int i = 0; i < 5; i++)
        {
            Stars[i].gameObject.GetComponent<Image>().sprite = star2Sprite;
        }
    }


    private void lightStart(int index)
    {
        for (int i = 0; i < 5; i++)
        {
            Stars[i].gameObject.GetComponent<Image>().sprite = i <= index ? star1Sprite : star2Sprite;
        }
        if (index < 3)
        {
            StartCoroutine(closePanel());
        } else
        {
            // 跳转到应用商店
            RateUsManager.instance.OpenAPPinMarket();
            StartCoroutine(closePanel());
        }
        
        // 打点
        PostEventScript.GetInstance().SendEvent("1016", (index + 1).ToString());
    }

    IEnumerator closePanel(float waitTime = 0.5f)
    {
        yield return new WaitForSeconds(waitTime);
        CloseUIForm(GetType().Name);
    }
}

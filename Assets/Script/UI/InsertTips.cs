using System;
using DG.Tweening;
using Spine.Unity;
using UnityEngine;
using UnityEngine.UI;

public class InsertTips : BaseUIForms
{
    public static InsertTips Instance;

    public Text rewardText;

   
    public override void Display(object uiFormParams)
    {
        base.Display(uiFormParams);
    }

    protected override void Awake()
    {
        base.Awake();
        Instance = this;
    }

    private void Start()
    {
    }

    public void InitData(double num)
    {
        rewardText.text = num.ToString();
    }
    public override void Hidding()
    {
        base.Hidding();
    }
}
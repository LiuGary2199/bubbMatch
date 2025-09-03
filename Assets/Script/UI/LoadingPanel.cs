using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Spine.Unity;
using Spine;

public class LoadingPanel : MonoBehaviour
{
    public Image sliderImage;
    public Text progressText;
    public SkeletonGraphic m_SkeletonGraphic;

    void Start()
    {
         m_SkeletonGraphic.AnimationState.Complete += OnAnimationComplete;
        sliderImage.fillAmount = 0;
        progressText.text = "0%";
        CashOutManager.GetInstance().StartTime = System.DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    }
 private void OnAnimationComplete(TrackEntry trackEntry)
    {
        if (trackEntry != null)
        {
            if (trackEntry.Animation.Name == "animation")
            {
                m_SkeletonGraphic.AnimationState.SetAnimation(0, "animation2", true);
            }
            
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (sliderImage.fillAmount <= 0.8f || (NetInfoMgr.instance.ready && CashOutManager.GetInstance().Ready))
        //if (sliderImage.fillAmount <= 0.8f || (NetInfoMgr.instance.ready ))
        {
            sliderImage.fillAmount += Time.deltaTime / 3f;
            progressText.text = (int)(sliderImage.fillAmount * 100) + "%";
            if (sliderImage.fillAmount >= 1)
            {
                // 安卓平台特殊屏蔽规则 被屏蔽玩家显示提示 阻止进入
                if (CommonUtil.AndroidBlockCheck())
                    return;
                //主动调用一次IsApple 判断是否符合屏蔽规则
                CommonUtil.IsApple();

                Destroy(transform.parent.gameObject);
                MainManager.instance.gameInit();

                CashOutManager.GetInstance().ReportEvent_LoadingTime();
            }
        }
    }
}

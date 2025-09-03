using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SignRewardItem : MonoBehaviour
{
    [Header("UI Components")]
    public Text dayText;
    public GameObject claimedImage;

    public GameObject reward;
    public GameObject fx_Parent;
    
    [Header("Reward Lists")]
    public List<Image> list_Reward = new List<Image>();
    public List<Text> list_Text = new List<Text>();

    /// <summary>
    /// 设置天数文本
    /// </summary>
    /// <param name="str">天数文本</param>
    public void SetDayText(string str)
    {
        if (dayText != null)
        {
            dayText.text = str;
        }
        else
        {
            Debug.LogWarning("SignRewardItem: dayText is null!");
        }
    }

    /// <summary>
    /// 设置领取状态
    /// </summary>
    /// <param name="isCla">true表示已领取，false表示未领取</param>
    public void SetClaimedState(bool isCla)
    {
        if (claimedImage != null)
        {
            claimedImage.SetActive(isCla);
        }
        else
        {
            Debug.LogWarning("SignRewardItem: claimedImage is null!");
        }
    }
    
    /// <summary>
    /// 设置奖励状态和数据显示
    /// </summary>
    /// <param name="list">奖励数据列表</param>
    /// <param name="isReward">true表示可领取，false表示不可领取</param>
    /// <param name="hideRewardAmount">true表示隐藏奖励金额（显示为"？？？"），false表示显示实际金额</param>
    public void SetRewardData(List<RewardData> list, bool isReward, bool hideRewardAmount = false)
    {
        if (list == null)
        {
            Debug.LogWarning("SignRewardItem: RewardData list is null!");
            return;
        }
        
        // 设置奖励对象状态（可领取时显示）
        if (reward != null)
        {
            reward.SetActive(isReward);
        }
        
        // 设置奖励数据显示
        SetRewardDisplay(list, hideRewardAmount);
    }
    
    /// <summary>
    /// 设置奖励显示数据
    /// </summary>
    /// <param name="list">奖励数据列表</param>
    /// <param name="hideRewardAmount">true表示隐藏奖励金额（显示为"？？？"），false表示显示实际金额</param>
    private void SetRewardDisplay(List<RewardData> list, bool hideRewardAmount = false)
    {
        if (list == null || list.Count == 0)
        {
            Debug.LogWarning("SignRewardItem: RewardData list is empty!");
            return;
        }
        
        // 设置奖励数量文本
        for (int i = 0; i < list_Text.Count && i < list.Count; i++)
        {
            if (list_Text[i] != null)
            {
                if (hideRewardAmount)
                {
                    list_Text[i].text = "xxx";
                }
                else
                {
                    list_Text[i].text = list[i].rewardNum.ToString();
                }
            }
        }

        
        
        // 设置奖励图标
      //  for (int i = 0; i < list_Reward.Count && i < list.Count; i++)
      //  {
           // if (list_Reward[i] != null)
         //   {
           //     RewardType type = list[i].type;
                // 使用实际的奖励类型，而不是强制转换
                // list_Reward[i].sprite = LGNUtils.GetInstance().GetRewardImagePath(type);
                
                // TODO: 实现根据奖励类型设置图标的逻辑
            //    SetRewardIcon(list_Reward[i], type);
           // }
       // }
    }
    
    /// <summary>
    /// 根据奖励类型设置图标
    /// </summary>
    /// <param name="image">目标图片组件</param>
    /// <param name="rewardType">奖励类型</param>
    private void SetRewardIcon(Image image, RewardType rewardType)
    {
        if (image == null) return;
        
        // TODO: 实现根据奖励类型获取图标的逻辑
        // 这里可以根据实际需求实现
        switch (rewardType)
        {
            case RewardType.Gold:
                // 设置金币图标
                break;
            case RewardType.Cash:
                // 设置现金图标
                break;
            case RewardType.Amazon:
                // 设置亚马逊图标
                break;
            default:
                // 设置默认图标
                break;
        }
    }
}


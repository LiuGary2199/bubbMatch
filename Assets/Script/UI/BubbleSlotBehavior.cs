using UnityEngine;
using DG.Tweening;

/// <summary>
/// 泡泡槽位行为脚本 - 参考 SlotBehavior.cs 的设计
/// 负责管理单个槽位的状态和泡泡存储
/// </summary>
public class BubbleSlotBehavior : MonoBehaviour
{
    [Header("槽位设置")]
    [SerializeField] private int slotIndex; // 槽位索引
    [SerializeField] private Transform bubbleAnchor; // 泡泡锚点位置
    
    [Header("状态管理")]
    private bool isOccupied = false; // 是否被占用
    private BubbleItem currentBubble = null; // 当前存储的泡泡
    private int sortingOrder; // 显示层级
    
    [Header("动画设置")]
    [SerializeField] private float moveAnimDuration = 0.15f; // 移动动画时长
    [SerializeField] private float scaleAnimDuration = 0.1f; // 缩放动画时长
    
    #region 公共属性
    public bool IsOccupied => isOccupied;
    public BubbleItem CurrentBubble => currentBubble;
    public ImageEnum BubbleType => currentBubble?.imageEnum ?? (ImageEnum)(-1);
    public int SlotIndex => slotIndex;
    public string BubbleName => currentBubble != null ? currentBubble.imageEnum.ToString() : "";
    #endregion
    
    #region 初始化
    public void Initialize(int index)
    {
        slotIndex = index;
        sortingOrder = index * 5 + 10; // 参考原脚本的层级计算
        ClearSlot();
        
        // 如果没有设置锚点，使用自身位置
        if (bubbleAnchor == null)
        {
            bubbleAnchor = transform;
        }
    }
    #endregion
    
    #region 槽位操作
    /// <summary>
    /// 设置泡泡到槽位 - 参考 SlotBehavior.SetPosition
    /// </summary>
    public void SetBubble(BubbleItem bubble)
    {
        if (bubble == null)
        {
            Debug.LogError($"BubbleSlotBehavior: 尝试设置空泡泡到槽位 {slotIndex}");
            return;
        }
        
        currentBubble = bubble;
        isOccupied = true;
        
        // 设置父对象和基本属性
        bubble.transform.SetParent(transform);
        // 设置为最后渲染（置于最上层）
        bubble.transform.SetAsLastSibling();
        
        Debug.Log($"槽位 {slotIndex} 设置泡泡: {bubble.imageEnum}");
    }
    
    /// <summary>
    /// 清空槽位 - 参考 SlotBehavior.InitData
    /// </summary>
    public void ClearSlot()
    {
        isOccupied = false;
        currentBubble = null;
        
        Debug.Log($"槽位 {slotIndex} 已清空");
    }
    
    /// <summary>
    /// 移除泡泡但不销毁
    /// </summary>
    public BubbleItem RemoveBubble()
    {
        BubbleItem bubble = currentBubble;
        ClearSlot();
        return bubble;
    }
    #endregion
    
    #region 智能移动 - 参考 TileBehavior.SubmitMove
    /// <summary>
    /// 智能移动泡泡到指定位置
    /// </summary>
    public void MoveBubbleTo(Vector3 targetPosition, System.Action onComplete = null)
    {
        if (currentBubble == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        // 使用简单直线移动
        StartSimpleMoveAnimation(targetPosition, onComplete);
    }
    
    /// <summary>
    /// 移动泡泡到另一个槽位
    /// </summary>
    public void MoveBubbleToSlot(BubbleSlotBehavior targetSlot, System.Action onComplete = null)
    {
        if (currentBubble == null || targetSlot == null)
        {
            onComplete?.Invoke();
            return;
        }
        
        Vector3 targetPosition = targetSlot.bubbleAnchor.position;
        BubbleItem movingBubble = currentBubble;
        
        // 先从当前槽位移除
        RemoveBubble();
        
        // 执行移动动画
        StartSimpleMoveAnimation(targetPosition, () =>
        {
            // 移动完成后设置到目标槽位
            targetSlot.SetBubble(movingBubble);
            onComplete?.Invoke();
        });
    }
    
    /// <summary>
    /// 执行简单移动动画（直线移动，无旋转缩放）
    /// </summary>
    private void StartSimpleMoveAnimation(Vector3 targetPosition, System.Action onComplete)
    {
        if (currentBubble == null) 
        {
            onComplete?.Invoke();
            return;
        }
        
        Transform bubbleTransform = currentBubble.transform;
        
        // 设置为最后渲染（移动时置于最上层）
        bubbleTransform.SetAsLastSibling();
        
        // 简单的直线移动，无旋转、无缩放变化
        bubbleTransform.DOMove(targetPosition, moveAnimDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 动画完成
                onComplete?.Invoke();
            });
    }
    #endregion
    
    #region 辅助方法
    /// <summary>
    /// 设置泡泡为最上层显示（移动时使用）
    /// </summary>
    private void SetBubbleToTop(BubbleItem bubble)
    {
        if (bubble != null)
        {
            bubble.transform.SetAsLastSibling();
        }
    }
    
    /// <summary>
    /// 检查是否与指定泡泡类型相同
    /// </summary>
    public bool IsSameType(ImageEnum bubbleType)
    {
        return isOccupied && currentBubble.imageEnum == bubbleType;
    }
    
    /// <summary>
    /// 检查是否与另一个槽位的泡泡类型相同
    /// </summary>
    public bool IsSameType(BubbleSlotBehavior otherSlot)
    {
        return otherSlot != null && 
               isOccupied && 
               otherSlot.isOccupied && 
               currentBubble.imageEnum == otherSlot.currentBubble.imageEnum;
    }
    #endregion
    
    #region Debug 方法
    void OnDrawGizmos()
    {
        // 在编辑器中显示槽位信息
        if (bubbleAnchor != null)
        {
            Gizmos.color = isOccupied ? Color.red : Color.green;
            Gizmos.DrawWireSphere(bubbleAnchor.position, 0.2f);
            
            #if UNITY_EDITOR
            UnityEditor.Handles.Label(bubbleAnchor.position + Vector3.up * 0.5f, 
                $"Slot {slotIndex}\n{(isOccupied ? BubbleType.ToString() : "Empty")}");
            #endif
        }
    }
    #endregion
}

using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using Lofelt.NiceVibrations;

/// <summary>
/// 增强版泡泡物品脚本 - 统一使用简单直线移动动画
/// </summary>
public class BubbleItem : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] public Image m_Image;
    [SerializeField] public Image m_ImageBg;
    [SerializeField] public Button ClickBtn;
    [SerializeField] public Rigidbody2D m_Rigidbody;
    [SerializeField] public GameObject m_Flyparticle;
    [SerializeField] public GameObject m_cashFlyparticle;

    [SerializeField] public GameObject m_di;

    [Header("精灵资源")]
    [SerializeField] public Sprite m_NormalSprite;
    [SerializeField] public Sprite m_CashSprite;

    [Header("泡泡属性")]
    public ImageEnum imageEnum;

    [Header("事件回调")]
    public Action<BubbleItem> OnclickItem;
    public Action OnAniFinish;

    [Header("状态管理")]
    private bool isSubmitted = false; // 是否已提交到收集区域
    private bool isClickable = true; // 是否可点击
    private bool isAnimating = false; // 是否正在动画中
    private bool isInCleanArea = false; // 是否在清理区

    public bool IsSubmitted => isSubmitted;
    public bool IsClickable => isClickable && !isAnimating;
    public bool IsAnimating => isAnimating;
    public bool IsInCleanArea => isInCleanArea;

    /// <summary>
    /// 对象销毁时清理所有DOTween动画
    /// </summary>
    private void OnDestroy()
    {
        // 停止所有与此对象相关的DOTween动画
        if (transform != null)
        {
            transform.DOKill();
        }

        // 重置状态
        isAnimating = false;
        isSubmitted = false;
        isClickable = false;

        Debug.Log($"BubbleItem {name}: 对象销毁，已清理所有DOTween动画");
    }

    public void SetImage(Sprite sprite, ImageEnum Enum)
    {
        imageEnum = Enum;

        // 直接设置传入的sprite，不再覆盖
        if (sprite != null)
        {
            m_Image.sprite = sprite;
        }

        if (Enum == ImageEnum.IMG0)
        {
            m_ImageBg.sprite = m_CashSprite;
        }
        else
        {
            m_ImageBg.sprite = m_NormalSprite;
        }

        // 初始化状态
        ResetBubble();
    }

    void Start()
    {
        ClickBtn.onClick.RemoveAllListeners();
        ClickBtn.onClick.AddListener(OnBubbleClick);
    }

    private void OnBubbleClick()
    {
          // 🎯 新增：检查收集区是否满了且没有三消
        if (!isInCleanArea)
        {
            if (HomePanel.Instance != null && HomePanel.Instance.m_GameArea != null && HomePanel.Instance.m_GameArea.collectAreaManager != null)
            {
                // 检查收集区是否满了
                if (!HomePanel.Instance.m_GameArea.collectAreaManager.HasAvailableSpace())
                {
                    // 检查是否有可消除的泡泡
                    if (!CheckHasEliminatableBubbles(HomePanel.Instance.m_GameArea.collectAreaManager))
                    {
                        // 收集区满了且没有三消，触发游戏失败
                        Debug.Log("收集区满了且没有三消，触发游戏失败");
                        GameEvents.GameOver?.Invoke();
                        return;
                    }
                }
            }
        }
        // 添加调试信息
        Debug.Log($"BubbleItem {name} 被点击 - IsClickable: {IsClickable}, isSubmitted: {isSubmitted}, isAnimating: {isAnimating}, isInCleanArea: {isInCleanArea}");
        // 检查是否可以点击
        if (!IsClickable || isSubmitted)
        {
            Debug.Log($"BubbleItem {name} 无法点击，播放反馈动画");
            // 播放无法点击的反馈动画
            PlayInvalidClickFeedback();
            return;
        }
        // 检查是否在清理区
        if (isInCleanArea)
        {
            Debug.Log($"BubbleItem {name} 在清理区被点击，通知移除");
            // 通知 FullStayArea 移除这个泡泡
            GameEvents.BubbleRemovedFromCleanArea?.Invoke(this);
        }
        if (imageEnum == ImageEnum.IMG0)
        {
            if (m_cashFlyparticle != null)
            {
                m_cashFlyparticle.SetActive(true);
            }
        }
        else
        {
            if (m_Flyparticle != null)
            {
                m_Flyparticle.SetActive(true);
            }
        }
        MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_ballclick);
        GameEvents.ClickParticle?.Invoke(this.transform);
        HapticPatterns.PlayPreset(HapticPatterns.PresetType.HeavyImpact);
        OnclickItem?.Invoke(this);
    }

    /// <summary>
    /// 无效点击的反馈动画
    /// </summary>
    private void PlayInvalidClickFeedback()
    {
        if (isAnimating) return;

        transform.DOShakePosition(0.3f, 0.1f, 10, 90, false, true)
            .SetEase(Ease.OutElastic);
    }

    /// <summary>
    /// 重置泡泡状态（重新启用按钮）
    /// </summary>
    public void ResetBubble()
    {
        isSubmitted = false;
        isClickable = true;
        isAnimating = false;
        isInCleanArea = false; // 重置清理区状态
        ClickBtn.interactable = true;
        m_Rigidbody.simulated = true;
        if (m_ImageBg != null)
        {
            m_ImageBg.gameObject.SetActive(true);
        }
    }

    /// <summary>
    /// 禁用泡泡（被收集后调用）
    /// </summary>
    public void DisableBubble()
    {
        if (m_ImageBg != null)
        {
            m_ImageBg.gameObject.SetActive(false);
        }
        ClickBtn.interactable = false;
        isClickable = false;
    }

    /// <summary>
    /// 标记为已提交
    /// </summary>
    public void MarkAsSubmitted()
    {
        isSubmitted = true;
        isClickable = false;
    }

    public void MoveToCleanAreaState()
    {
        isSubmitted = false;
        isClickable = true;
        ClickBtn.interactable = true;
        isInCleanArea = true; // 设置为在清理区状态
    }

    /// <summary>
    /// 关闭背景图片（用于磁铁按钮生成的泡泡）
    /// </summary>
    public void DisableImageBg()
    {
        if (m_ImageBg != null)
        {
            m_ImageBg.gameObject.SetActive(false);
        }

        // 关闭物理模拟
        if (m_Rigidbody != null)
        {
            m_Rigidbody.simulated = false;
        }

        // 保持点击按钮可用，这样磁铁泡泡才能被点击收集
        // 不需要设置 ClickBtn.interactable = false;
    }

    /// <summary>
    /// 移动到指定槽位（统一使用简单直线移动）
    /// </summary>
    public void MoveToSlot(BubbleSlotBehavior targetSlot, System.Action onComplete = null)
    {
        Vector3 targetPosition = targetSlot.transform.position;
        MoveToPosition(targetPosition, onComplete);
    }

    /// <summary>
    /// 移动到指定位置（统一使用简单直线移动）
    /// </summary>
    public void MoveToPosition(Vector3 targetPosition, System.Action onComplete = null)
    {
        // 检查对象是否已被销毁
        if (this == null || gameObject == null)
        {
            Debug.LogWarning("BubbleItem已被销毁，无法执行移动动画");
            onComplete?.Invoke();
            return;
        }

        // 强制停止所有当前动画
        ForceStopAllAnimations();

        StartSimpleMoveAnimation(targetPosition, onComplete);
    }

    /// <summary>
    /// 强制停止所有动画
    /// </summary>
    private void ForceStopAllAnimations()
    {
        // 检查对象是否已被销毁
        if (this == null || gameObject == null || transform == null)
        {
            Debug.LogWarning("BubbleItem已被销毁，无法停止动画");
            return;
        }

        try
        {
            // 停止所有DOTween动画
            transform.DOKill();

            // 重置动画状态
            isAnimating = false;

            // 重置旋转和缩放
            transform.rotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.74f; // 恢复到正常大小

            Debug.Log($"BubbleItem {name}: 强制停止所有动画，准备执行移动");
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"停止动画时发生异常: {e.Message}");
            isAnimating = false;
        }
    }

    /// <summary>
    /// 执行移动动画（统一使用简单直线移动，无旋转缩放）
    /// </summary>
    private void StartSimpleMoveAnimation(Vector3 targetPosition, System.Action onComplete)
    {
        // 检查对象是否已被销毁
        if (this == null || gameObject == null || transform == null)
        {
            Debug.LogWarning("BubbleItem已被销毁，无法执行移动动画");
            onComplete?.Invoke();
            return;
        }

        isAnimating = true;

        // 禁用物理模拟
        if (m_Rigidbody != null)
        {
            m_Rigidbody.simulated = false;
        }

        // 设置为最上层
        transform.SetAsLastSibling();

        try
        {
            // 简单的直线移动，无旋转、无缩放变化
            transform.DOMove(targetPosition, 0.15f)
                .SetEase(Ease.OutQuad)
                .OnComplete(() =>
                {
                    // 再次检查对象是否仍然有效
                    if (this != null && gameObject != null)
                    {
                        isAnimating = false;
                        onComplete?.Invoke();
                    }
                });
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"创建移动动画时发生异常: {e.Message}");
            isAnimating = false;
            onComplete?.Invoke();
        }
    }

    /// <summary>
    /// 检查收集区域是否正在处理三消
    /// </summary>
    private bool IsCollectAreaProcessingMatches()
    {
        if (HomePanel.Instance != null && HomePanel.Instance.m_GameArea != null && HomePanel.Instance.m_GameArea.collectAreaManager != null)
        {
            bool isProcessing = HomePanel.Instance.m_GameArea.collectAreaManager.IsProcessingMatches;
            // 添加调试信息
            if (isProcessing)
            {
                Debug.Log($"BubbleItem {name}: 检测到三消处理中，禁止点击");
            }
            return isProcessing;
        }
        else
        {
            Debug.LogWarning($"BubbleItem {name}: 无法找到GameArea或CollectAreaManager");
        }
        return false;
    }

    /// <summary>
    /// 检查是否有可消除的泡泡
    /// </summary>
    private bool CheckHasEliminatableBubbles(CollectAreaManager collectAreaManager)
    {
        var occupiedSlots = collectAreaManager.GetOccupiedSlotsPublic();
        int availableSlots = collectAreaManager.GetAvailablePositions();
        
        // 检查是否有连续三个相同类型的泡泡
        for (int i = 0; i < availableSlots - 2; i++)
        {
            if (i < occupiedSlots.Count && i + 1 < occupiedSlots.Count && i + 2 < occupiedSlots.Count)
            {
                var slot0 = occupiedSlots[i];
                var slot1 = occupiedSlots[i + 1];
                var slot2 = occupiedSlots[i + 2];
                
                if (slot0 != null && slot0.CurrentBubble != null &&
                    slot1 != null && slot1.CurrentBubble != null &&
                    slot2 != null && slot2.CurrentBubble != null)
                {
                    ImageEnum type0 = slot0.CurrentBubble.imageEnum;
                    ImageEnum type1 = slot1.CurrentBubble.imageEnum;
                    ImageEnum type2 = slot2.CurrentBubble.imageEnum;
                    
                    if (type0 == type1 && type1 == type2)
                    {
                        return true;
                    }
                }
            }
        }
        
        return false;
    }

    /// <summary>
    /// 碰撞检测 - 当小球碰到BottomRest时回到生成点
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 检查是否碰到了BottomRest
        if (other.CompareTag("BottomRest") || other.name.Contains("BottomRest"))
        {
            ReturnToSpawnPoint();
        }
    }

    /// <summary>
    /// 回到生成点位置
    /// </summary>
    private void ReturnToSpawnPoint()
    {
        // 使用HomePanel中的GameArea引用
        if (HomePanel.Instance == null || HomePanel.Instance.m_GameArea == null || 
            HomePanel.Instance.m_GameArea.BallInsTrans == null || HomePanel.Instance.m_GameArea.BallInsTrans.Count == 0)
        {
            return;
        }
        // 获取第一个生成点位置
        Vector3 spawnPosition = HomePanel.Instance.m_GameArea.BallInsTrans[0].position;
        // 直接设置位置到生成点
        transform.position = spawnPosition;
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using DG.Tweening;

public class CollectArea : MonoBehaviour
{
    [Header("位置设置")]
    [SerializeField] private Transform[] positions; // 8个位置的Transform
    
    [Header("收集管理")]
    [SerializeField] private List<BubbleItem> collectedBubbles = new List<BubbleItem>(); // 当前收集的泡泡
    [SerializeField] private bool isPos8Unlocked = false; // 第8位置是否解锁
    private int animatingBubbles = 0; // 正在动画中的泡泡数量
    private bool isProcessingMatches = false; // 是否正在处理三消
    
    [Header("动画设置")]
    [SerializeField] private float upwardMoveDuration = 0.15f; // 向上移动时长（更快）
    [SerializeField] private float rotateDuration = 0.3f; // 转正时长（拉长）
    [SerializeField] private float scaleDuration = 0.5f; // 缩放时长（拉长）
    [SerializeField] private float moveAnimDuration = 0.05f; // 曲线移动时长（更快）
    [SerializeField] private float targetScale = 0.73f; // 目标缩放
    [SerializeField] private float upwardOffset = 15f; // 向上移动距离（减少幅度）
    
    [Header("三消设置")]
    [SerializeField] private float matchMoveSpeed = 0.3f; // 匹配移动速度
    [SerializeField] private float eliminateDelay = 0.2f; // 消除延迟
    [SerializeField] private float eliminateScale = 1.2f; // 消除时的放大倍数
    [SerializeField] private float eliminateDuration = 0.3f; // 消除动画时长
    [SerializeField] private float fillMoveSpeed = 0.4f; // 补位移动速度
    
    // 获取当前可用位置数量
    private int GetAvailablePositions()
    {
        return isPos8Unlocked ? 8 : 7;
    }
    
    // 检查是否还有空位（包括动画中的泡泡）
    public bool HasAvailableSpace()
    {
        // 如果正在处理三消，暂时不接受新泡泡
        if (isProcessingMatches)
        {
            Debug.Log("正在处理三消，暂不接受新泡泡");
            return false;
        }
        
        int totalOccupied = collectedBubbles.Count + animatingBubbles;
        return totalOccupied < GetAvailablePositions();
    }
    
    // 收集新泡泡
    public bool CollectBubble(BubbleItem newBubble)
    {
        if (!HasAvailableSpace())
        {
            Debug.LogError($"❌收集区域已满！当前：{collectedBubbles.Count}，动画中：{animatingBubbles}，总容量：{GetAvailablePositions()}，正在处理三消：{isProcessingMatches}");
            return false;
        }
        // 先禁用物理碰撞
        if (newBubble.m_Rigidbody != null)
        {
            newBubble.m_Rigidbody.simulated = false;
        }
        
        // 立即占用一个位置（防止快速点击冲突）
        animatingBubbles++;
        
        // 计算插入位置
        int insertIndex = FindInsertPosition(newBubble.imageEnum);
        
        // 开始动画，在动画完成后再插入列表
        StartSmoothCollectAnimation(newBubble, insertIndex);
        
        Debug.Log($"收集泡泡: {newBubble.imageEnum}, 插入位置: {insertIndex}, 动画中泡泡数: {animatingBubbles}");
        
        return true;
        
    }
    
    // 查找插入位置
    private int FindInsertPosition(ImageEnum newImageEnum)
    {
        // 查找相同ImageEnum的泡泡组
        var sameTypeBubbles = new List<int>();
        
        for (int i = 0; i < collectedBubbles.Count; i++)
        {
            if (collectedBubbles[i].imageEnum == newImageEnum)
            {
                sameTypeBubbles.Add(i);
            }
        }
        
        // 如果找到相同类型的泡泡，插入到该组的最后
        if (sameTypeBubbles.Count > 0)
        {
            return sameTypeBubbles.Last() + 1;
        }
        
        // 如果没有相同类型，添加到最后
        return collectedBubbles.Count;
    }
    
    // 开始流畅的收集动画队列
    private void StartSmoothCollectAnimation(BubbleItem bubble, int positionIndex)
    {
        if (positionIndex >= positions.Length) return;
        
        Vector3 startPosition = bubble.transform.position;
        Vector3 targetPosition = positions[positionIndex].position;
        
        // 第一阶段：向上移动 + 转正 + 变小（并行执行）
        Vector3 upwardPosition = startPosition + Vector3.up * (upwardOffset / 100f);
        
        // 向上移动
        bubble.transform.DOMoveY(upwardPosition.y, upwardMoveDuration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() =>
            {
                // 向上移动完成后立即开始曲线移动，消除顿挫感
                StartCurveMovement(bubble, bubble.transform.position, targetPosition, positionIndex);
            });
            
        // 转正（立即开始，和向上移动同时进行）
        bubble.transform.DORotate(Vector3.zero, rotateDuration)
            .SetEase(Ease.OutQuart);
            
        // 缩放（立即开始，和其他动画同时进行）
        bubble.transform.DOScale(targetScale, scaleDuration)
            .SetEase(Ease.OutBack);
        
        // 触发完成回调
        bubble.OnAniFinish?.Invoke();
    }
    
         // 曲线移动动画
     private void StartCurveMovement(BubbleItem bubble, Vector3 fromPosition, Vector3 targetPosition, int insertIndex)
     {
         // 计算曲线路径的中间点（向上弧形）
         Vector3 midPoint = (fromPosition + targetPosition) * 0.5f;
         midPoint.y += Vector3.Distance(fromPosition, targetPosition) * 0.25f; // 向上偏移
         
         // 创建曲线移动路径
         Vector3[] path = new Vector3[] { fromPosition, midPoint, targetPosition };
         
         // 使用DOPath创建曲线移动，加速效果
         bubble.transform.DOPath(path, moveAnimDuration, PathType.CatmullRom)
             .SetEase(Ease.InQuart)
             .OnComplete(() =>
             {
                 // 移动完成后重新计算插入位置并插入列表
                 int finalInsertIndex = FindInsertPosition(bubble.imageEnum);
                 collectedBubbles.Insert(finalInsertIndex, bubble);
                 bubble.transform.SetParent(transform);

                 // 减少动画中的泡泡计数
                 animatingBubbles--;
                 
                 // 重新排列其他泡泡
                 UpdateAllBubblesPositions();
                 // 检查三消
                 CheckForMatches();
                 
                 Debug.Log($"泡泡 {bubble.imageEnum} 动画完成，插入位置: {insertIndex}");
             });
     }
    
    // 更新所有泡泡的位置（重排时使用）
    private void UpdateAllBubblesPositions()
    {
        for (int i = 0; i < collectedBubbles.Count; i++)
        {
            if (i < positions.Length)
            {
                // 重排时使用简单的移动动画，不需要完整的收集动画
                Vector3 targetPosition = positions[i].position;
                collectedBubbles[i].transform.DOMove(targetPosition, moveAnimDuration * 0.5f)
                    .SetEase(Ease.OutQuart);
            }
        }
    }
    
    // 检查三消匹配
    private void CheckForMatches()
    {
        // 查找所有可能的三消组合
        for (int i = 0; i <= collectedBubbles.Count - 3; i++)
        {
            ImageEnum currentType = collectedBubbles[i].imageEnum;
            
            // 检查从位置i开始的连续相同类型
            List<int> matchIndexes = new List<int> { i };
            
            for (int j = i + 1; j < collectedBubbles.Count; j++)
            {
                if (collectedBubbles[j].imageEnum == currentType)
                {
                    matchIndexes.Add(j);
                }
                else
                {
                    break; // 遇到不同类型就停止
                }
            }
            
            // 如果找到3个或更多相同的，执行三消
            if (matchIndexes.Count >= 3)
            {
                StartMatch3Animation(matchIndexes);
                return; // 一次只处理一个三消组合
            }
        }
    }
    
    // 开始三消动画
    private void StartMatch3Animation(List<int> matchIndexes)
    {
        isProcessingMatches = true;
        Debug.Log($"发现三消！位置: {string.Join(",", matchIndexes)}");
        
        // 第一步：后面的泡泡移动到前面的位置
        int firstIndex = matchIndexes[0];
        Vector3 firstPosition = positions[firstIndex].position;
        
        // 获取要移动的泡泡（后面两个）
        List<BubbleItem> bubblestoMove = new List<BubbleItem>();
        for (int i = 1; i < matchIndexes.Count; i++)
        {
            bubblestoMove.Add(collectedBubbles[matchIndexes[i]]);
        }
        
        // 移动后面的泡泡到第一个位置
        Sequence moveSequence = DOTween.Sequence();
        foreach (var bubble in bubblestoMove)
        {
            moveSequence.Join(bubble.transform.DOMove(firstPosition, matchMoveSpeed)
                .SetEase(Ease.OutQuart));
        }
        
        // 移动完成后开始消除动画
        moveSequence.OnComplete(() =>
        {
            StartEliminateAnimation(matchIndexes);
        });
    }
    
    // 开始消除动画
    private void StartEliminateAnimation(List<int> matchIndexes)
    {
        // 获取要消除的泡泡
        List<BubbleItem> bubblestoEliminate = new List<BubbleItem>();
        foreach (int index in matchIndexes)
        {
            bubblestoEliminate.Add(collectedBubbles[index]);
        }
        
        // 获取第一个元素的位置
        Vector3 firstPosition = collectedBubbles[matchIndexes[0]].transform.position;
        
        // 消除动画：后两个元素快速移动到第一个元素位置，然后消失
        Sequence eliminateSequence = DOTween.Sequence();
        
        // 后两个元素快速移动到第一个元素位置
        for (int i = 1; i < bubblestoEliminate.Count; i++)
        {
            eliminateSequence.Join(bubblestoEliminate[i].transform.DOMove(firstPosition, 0.2f)
                .SetEase(Ease.InQuart));
        }
        
        // 移动完成后调用CollectParticle事件
        eliminateSequence.OnComplete(() =>
        {
            // 调用CollectParticle事件
                    if (GameEvents.CollectParticle != null)
        {
            GameEvents.CollectParticle.Invoke(bubblestoEliminate);
        }
            
        
            // 等待消失动画完成后处理后续逻辑
            DOVirtual.DelayedCall(0.1f, () =>
            {
                CompleteElimination(matchIndexes);
            });
        });
    }
    
    // 完成消除，移除泡泡并补位
    private void CompleteElimination(List<int> matchIndexes)
    {
        Debug.Log($"开始消除 {matchIndexes.Count} 个泡泡，当前收集数: {collectedBubbles.Count}");
        
        // 销毁泡泡对象
        foreach (int index in matchIndexes)
        {
            if (index < collectedBubbles.Count && collectedBubbles[index] != null)
            {
                Destroy(collectedBubbles[index].gameObject);
            }
        }
        
        // 从后往前移除，避免索引变化
        for (int i = matchIndexes.Count - 1; i >= 0; i--)
        {
            if (matchIndexes[i] < collectedBubbles.Count)
            {
                collectedBubbles.RemoveAt(matchIndexes[i]);
            }
        }
        
        // 强制重置动画计数器（三消可能导致计数错误）
        animatingBubbles = 0;
        
        Debug.Log($"消除完成，剩余泡泡数: {collectedBubbles.Count}，重置动画计数: {animatingBubbles}");
        
        // 补位动画：后面的泡泡往前移动
        StartFillForwardAnimation();
    }
    
    // 开始补位动画
    private void StartFillForwardAnimation()
    {
        Debug.Log($"开始补位动画，当前泡泡数: {collectedBubbles.Count}");
        
        if (collectedBubbles.Count == 0)
        {
            Debug.Log("没有泡泡需要补位");
            return;
        }
        
        Sequence fillSequence = DOTween.Sequence();
        
        for (int i = 0; i < collectedBubbles.Count; i++)
        {
            if (i < positions.Length && collectedBubbles[i] != null)
            {
                Vector3 targetPosition = positions[i].position;
                fillSequence.Join(collectedBubbles[i].transform.DOMove(targetPosition, fillMoveSpeed)
                    .SetEase(Ease.OutQuart));
            }
        }
        
        // 补位完成后检查是否有新的三消
        fillSequence.OnComplete(() =>
        {
            Debug.Log($"补位动画完成，最终状态：收集数{collectedBubbles.Count}，动画中{animatingBubbles}");
            isProcessingMatches = false; // 重置处理状态
            animatingBubbles = 0; // 再次确保计数器正确
            CheckForMatches(); // 递归检查新的三消
        });
    }
    
    // 重置计数器（调试用）
    public void ResetAnimatingCount()
    {
        animatingBubbles = 0;
        isProcessingMatches = false;
        Debug.Log("手动重置所有计数器");
    }
    
    // 清空收集区域（重新开始游戏时调用）
    public void ClearAreaForNewGame()
    {
        foreach (var bubble in collectedBubbles)
        {
            if (bubble != null)
            {
                Destroy(bubble.gameObject);
            }
        }
        collectedBubbles.Clear();
        animatingBubbles = 0;
        isProcessingMatches = false;
        Debug.Log("清空收集区域，重置所有状态");
    }
    
    // 解锁第8个位置（看广告后调用）
    public void UnlockPosition8()
    {
        isPos8Unlocked = true;
        Debug.Log("第8个位置已解锁！");
    }
    
    // 重置解锁状态（新局开始时调用）
    public void ResetUnlockStatus()
    {
        isPos8Unlocked = false;
    }
    
    // 移除泡泡（消除时调用）
    public void RemoveBubble(BubbleItem bubble)
    {
        if (collectedBubbles.Contains(bubble))
        {
            collectedBubbles.Remove(bubble);
            UpdateAllBubblesPositions();
        }
    }
    
    // 清空收集区域
    public void ClearArea()
    {
        foreach (var bubble in collectedBubbles)
        {
            if (bubble != null)
            {
                Destroy(bubble.gameObject);
            }
        }
        collectedBubbles.Clear();
    }
    
    void Start()
    {
        // 验证位置数组
        if (positions == null || positions.Length != 8)
        {
            Debug.LogError("CollectArea: 需要设置8个位置的Transform！");
        }
    }
}


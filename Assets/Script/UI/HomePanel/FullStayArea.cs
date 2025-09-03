using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

/// <summary>
/// 全满停留区域 - 用于存储从收集区域清理过来的泡泡
/// 最多存储7个泡泡，按照从左往右的顺序排列
/// </summary>
public class FullStayArea : MonoBehaviour
{
    [Header("存储设置")]
    [SerializeField] private Transform[] storagePositions; // 7个存储位置的Transform
    [SerializeField] private int maxStorageCapacity = 7; // 最大存储容量
    
    [Header("动画设置")]
    [SerializeField] private float moveAnimDuration = 0.2f; // 移动动画时长（更快）
    [SerializeField] private float bubbleScale = 0.8f; // 存储中泡泡的缩放
    
    [Header("状态管理")]
    private List<BubbleItem> storedBubbles = new List<BubbleItem>(); // 当前存储的泡泡列表
    private bool isProcessing = false; // 是否正在处理泡泡移动
    
    /// <summary>
    /// 当前存储的泡泡数量
    /// </summary>
    public int StoredBubbleCount => storedBubbles.Count;
    
    /// <summary>
    /// 是否还有存储空间
    /// </summary>
    public bool HasStorageSpace => storedBubbles.Count < maxStorageCapacity;
    
    /// <summary>
    /// 获取可存储的数量
    /// </summary>
    public int GetAvailableStorageSpace => maxStorageCapacity - storedBubbles.Count;
    
    void Start()
    {
        ValidateSetup();
        // 监听泡泡从清理区移除的事件
        GameEvents.BubbleRemovedFromCleanArea += OnBubbleRemovedFromCleanArea;
    }
    
    /// <summary>
    /// 验证设置
    /// </summary>
    private void ValidateSetup()
    {
        if (storagePositions == null || storagePositions.Length != maxStorageCapacity)
        {
            Debug.LogError($"FullStayArea: 需要设置{maxStorageCapacity}个存储位置！");
            return;
        }
        
        for (int i = 0; i < storagePositions.Length; i++)
        {
            if (storagePositions[i] == null)
            {
                Debug.LogError($"FullStayArea: 存储位置 {i} 未设置！");
                    }
    }
    
    void OnDestroy()
    {
        // 取消监听事件，防止内存泄漏
        GameEvents.BubbleRemovedFromCleanArea -= OnBubbleRemovedFromCleanArea;
    }
}
    

    /// <summary>
    /// 批量添加泡泡（用于清理功能）
    /// </summary>
    /// <param name="bubbles">要存储的泡泡列表</param>
    /// <returns>实际存储的泡泡数量</returns>
    public int AddBubbles(List<BubbleItem> bubbles)
    {
        if (bubbles == null || bubbles.Count == 0)
        {
            Debug.LogWarning("FullStayArea: 没有泡泡需要存储！");
            return 0;
        }
        
        int availableSpace = GetAvailableStorageSpace;
        int bubblesToStore = Mathf.Min(bubbles.Count, availableSpace);
        
        if (bubblesToStore == 0)
        {
            Debug.LogWarning("FullStayArea: 没有足够的存储空间！");
            return 0;
        }
        
        Debug.Log($"开始同时存储 {bubblesToStore} 个泡泡到FullStayArea");
        
        // 同时存储所有泡泡
        StartCoroutine(AddBubblesSimultaneously(bubbles, bubblesToStore));
        
        return bubblesToStore;
    }
    
    /// <summary>
    /// 同时添加多个泡泡的协程
    /// </summary>
    private IEnumerator AddBubblesSimultaneously(List<BubbleItem> bubbles, int countToStore)
    {
        isProcessing = true;
        //List<BubbleItem> newbubbles = new List<BubbleItem>();
       // for(int i = bubbles.Count - 1; i >= 0; i--){
           // if(bubbles[i] != null){
               // newbubbles.Add(bubbles[i]);
           // }
        //}
        // 创建所有泡泡的移动动画序列
        Sequence moveSequence = DOTween.Sequence();
        
        for (int i = 0; i < countToStore; i++)
        {
            if (bubbles[i] != null)
            {
                // 简单左对齐：新泡泡直接添加到下一个可用位置
                int storageIndex = storedBubbles.Count + i;
                Vector3 targetPosition = storagePositions[storageIndex].position;
                
                // 禁用泡泡的物理模拟
                if (bubbles[i].m_Rigidbody != null)
                {
                    bubbles[i].m_Rigidbody.simulated = false;
                }
                
                // 设置泡泡为最上层
                bubbles[i].transform.SetAsLastSibling();
                
                // 将移动和缩放动画添加到序列中
                moveSequence.Join(bubbles[i].transform.DOMove(targetPosition, moveAnimDuration).SetEase(Ease.Linear));
                moveSequence.Join(bubbles[i].transform.DOScale(0.5f, moveAnimDuration).SetEase(Ease.OutBack)); // 清理区大小变为0.5
            }
        }
        
        // 等待所有动画完成
        yield return moveSequence.WaitForCompletion();
        
        // 动画完成后，将所有泡泡添加到存储列表并设置状态
        for (int i = 0; i < countToStore; i++)
        {
            if (bubbles[i] != null)
            {
                storedBubbles.Add(bubbles[i]);
                bubbles[i].transform.SetParent(transform);
                
                // 调用泡泡的MoveToCleanAreaState方法设置清理区状态
                bubbles[i].MoveToCleanAreaState();
            }
        }
        // 重置处理状态
        isProcessing = false;
    }
    
    /// <summary>
    /// 处理泡泡从清理区移除的事件
    /// </summary>
    /// <param name="bubble">要移除的泡泡</param>
    private void OnBubbleRemovedFromCleanArea(BubbleItem bubble)
    {
        if (bubble == null) return;
        
        // 找到泡泡在存储列表中的索引
        int index = storedBubbles.IndexOf(bubble);
        if (index != -1)
        {
     // 调用 RemoveBubbleAt 移除泡泡
            RemoveBubbleAt(index);
        }
        else
        {
            Debug.LogWarning($"FullStayArea: 未找到要移除的泡泡 {bubble.imageEnum}");
        }
    }
    
    /// <summary>
    /// 移除指定位置的泡泡
    /// </summary>
    /// <param name="index">要移除的泡泡索引</param>
    /// <returns>被移除的泡泡</returns>
    public BubbleItem RemoveBubbleAt(int index)
    {
        if (index < 0 || index >= storedBubbles.Count)
        {
            Debug.LogError($"FullStayArea: 无效的索引 {index}！");
            return null;
        }
        
        // 输出移除前的状态
        string beforeStatus = "移除前状态: ";
        for (int i = 0; i < storedBubbles.Count; i++)
        {
            if (storedBubbles[i] != null)
            {
                beforeStatus += $"[{i}:{storedBubbles[i].imageEnum}] ";
            }
        }
        Debug.Log(beforeStatus);
        
        BubbleItem removedBubble = storedBubbles[index];
        
        // 立即从列表中移除泡泡，确保位置状态正确更新
        storedBubbles.RemoveAt(index);
        
        // 左对齐：让剩余泡泡向前移动，填补空位
        for (int i = 0; i < storedBubbles.Count; i++)
        {
            if (storedBubbles[i] != null)
            {
                Vector3 targetPosition = storagePositions[i].position;
                // 播放移动动画到正确位置
                storedBubbles[i].transform.DOMove(targetPosition, moveAnimDuration * 0.3f)
                    .SetEase(Ease.OutQuad);
                Debug.Log($"泡泡 {storedBubbles[i].imageEnum} 从列表索引 {i} 移动到位置 {i}（左对齐）");
            }
        }
        
        // 输出移动后的状态
        string afterMoveStatus = "移动后状态: ";
        for (int i = 0; i < storedBubbles.Count; i++)
        {
            if (storedBubbles[i] != null)
            {
                afterMoveStatus += $"[{i}:{storedBubbles[i].imageEnum}] ";
            }
        }
        Debug.Log(afterMoveStatus);
        
        Debug.Log($"从位置 {index} 移除泡泡 {removedBubble.imageEnum}，剩余数量: {storedBubbles.Count}");
        return removedBubble;
    }
    
    /// <summary>
    /// 重新排列剩余泡泡的协程
    /// </summary>
    private IEnumerator RearrangeBubbles()
    {
        isProcessing = true;
        
        // 等待一帧，确保移除操作完成
        yield return null;
        
        Debug.Log($"开始重新排列，当前泡泡数: {storedBubbles.Count}");
        
        // 重新排列所有泡泡 - 确保按顺序排列，不互换位置
        for (int i = 0; i < storedBubbles.Count; i++)
        {
            if (storedBubbles[i] != null)
            {
                Vector3 targetPosition = storagePositions[i].position;
                
                // 播放移动动画到正确位置
                storedBubbles[i].transform.DOMove(targetPosition, moveAnimDuration * 0.5f)
                    .SetEase(Ease.OutQuad);
                
                Debug.Log($"泡泡 {storedBubbles[i].imageEnum} 移动到位置 {i}");
            }
        }
        
        // 等待动画完成
        yield return new WaitForSeconds(moveAnimDuration * 0.3f);
        
        isProcessing = false;
        Debug.Log($"重新排列完成，最终泡泡数: {storedBubbles.Count}");
        
        // 输出最终排列状态
        string finalStatus = "最终排列状态: ";
        for (int i = 0; i < storedBubbles.Count; i++)
        {
            if (storedBubbles[i] != null)
            {
                finalStatus += $"[{i}:{storedBubbles[i].imageEnum}] ";
            }
        }
        Debug.Log(finalStatus);
    }
    
    /// <summary>
    /// 清空存储区域
    /// </summary>
    public void ClearStorage()
    {
        foreach (var bubble in storedBubbles)
        {
            if (bubble != null)
            {
                Destroy(bubble.gameObject);
            }
        }
        
        storedBubbles.Clear();
        Debug.Log("FullStayArea: 存储区域已清空");
    }
    
    /// <summary>
    /// 获取存储的泡泡列表（只读）
    /// </summary>
    public IReadOnlyList<BubbleItem> GetStoredBubbles()
    {
        return storedBubbles.AsReadOnly();
    }
    
    /// <summary>
    /// 检查是否包含指定类型的泡泡
    /// </summary>
    /// <param name="bubbleType">要检查的泡泡类型</param>
    /// <returns>是否包含该类型</returns>
    public bool ContainsBubbleType(ImageEnum bubbleType)
    {
        return storedBubbles.Exists(bubble => bubble != null && bubble.imageEnum == bubbleType);
    }
    
    /// <summary>
    /// 获取指定类型的泡泡数量
    /// </summary>
    /// <param name="bubbleType">要统计的泡泡类型</param>
    /// <returns>该类型的泡泡数量</returns>
    public int GetBubbleTypeCount(ImageEnum bubbleType)
    {
        int count = 0;
        foreach (var bubble in storedBubbles)
        {
            if (bubble != null && bubble.imageEnum == bubbleType)
            {
                count++;
            }
        }
        return count;
    }
    
    /// <summary>
    /// 输出当前状态信息（调试用）
    /// </summary>
    [ContextMenu("Print Storage Status")]
    public void PrintStorageStatus()
    {
        Debug.Log($"=== FullStayArea 状态 ===");
        Debug.Log($"存储容量: {maxStorageCapacity}");
        Debug.Log($"当前存储: {storedBubbles.Count}");
        Debug.Log($"可用空间: {GetAvailableStorageSpace}");
        Debug.Log($"正在处理: {isProcessing}");
        
        for (int i = 0; i < storedBubbles.Count; i++)
        {
            if (storedBubbles[i] != null)
            {
                Debug.Log($"位置 {i}: {storedBubbles[i].imageEnum}");
            }
        }
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using System.Linq;
using UnityEngine.UI;

/// <summary>
/// 收集区域管理器 - 重新设计，借鉴 New Folder 的逻辑
/// </summary>
public class CollectAreaManager : MonoBehaviour
{
    [Header("槽位系统")]
    [SerializeField] private BubbleSlotBehavior[] bubbleSlots; // 8个槽位
    [SerializeField] private bool isPos8Unlocked = false; // 第8位置是否解锁
    
    [Header("动画设置")]
    [SerializeField] private float moveAnimDuration = 0.15f; // 移动动画时长
    [SerializeField] private float eliminateDelay = 0.2f; // 消除延迟
    [SerializeField] private float eliminateDuration = 0.3f; // 消除动画时长
    [SerializeField] private Vector3 bubbleScale = Vector3.one * 0.74f; // 泡泡最终缩放
    
    [Header("状态管理")]
    private bool isProcessingMatches = false; // 是否正在处理三消
    private bool isGapFilling = false; // 是否正在补位
    private int continuousMatches = 0; // 连续消除次数
    private bool hasTriggeredGameOver = false; // 是否已经触发过游戏失败

    public GameObject Tipsobj;
    
    /// <summary>
    /// 是否正在处理三消（公共属性，供外部检查）
    /// </summary>
    public bool IsProcessingMatches => isProcessingMatches;
    
    /// <summary>
    /// 是否正在补位（公共属性，供外部检查）
    /// </summary>
    public bool IsGapFilling => isGapFilling;
    
    [Header("事件回调")]
    public System.Action<int> OnMatchesFound; // 发现三消时的回调
    public System.Action<BubbleItem> OnBubbleEliminated; // 泡泡被消除时的回调
    public System.Action OnAreaFull; // 区域满时的回调
    public System.Action OnAreaEmpty; // 区域空时的回调

    public Button ADDPosBtn;
    
    #region 初始化
    void Start()
    {
        InitializeSlots();
        ValidateSetup();
        ADDPosBtn.onClick.AddListener(UnlockPosition8);
    }
    
    /// <summary>
    /// 初始化所有槽位
    /// </summary>
    private void InitializeSlots()
    {
        for (int i = 0; i < bubbleSlots.Length; i++)
        {
            if (bubbleSlots[i] != null)
            {
                bubbleSlots[i].Initialize(i);
            }
        }
        
        // Debug.Log($"CollectAreaManager: 初始化 {bubbleSlots.Length} 个槽位");
    }

    public void RefShowTips()
    {
        if (CommonUtil.IsApple())
        {
            Tipsobj.SetActive(false);
            return;
        }
        if(GameManager.Instance.GetGameType() == GameType.Level)
        {
            Tipsobj.SetActive(false);
            return;
        }
        if(isPos8Unlocked){
            Tipsobj.SetActive(false);
        }else{
            Tipsobj.SetActive(true);
        }
    }
    
    /// <summary>
    /// 验证设置
    /// </summary>
    private void ValidateSetup()
    {
        if (bubbleSlots == null || bubbleSlots.Length != 8)
        {
            Debug.LogError("CollectAreaManager: 需要设置8个BubbleSlotBehavior！");
            return;
        }
        
        for (int i = 0; i < bubbleSlots.Length; i++)
        {
            if (bubbleSlots[i] == null)
            {
                Debug.LogError($"CollectAreaManager: 槽位 {i} 未设置！");
            }
        }
    }
    #endregion
    
    #region 公共接口
    /// <summary>
    /// 获取当前可用位置数量
    /// </summary>
    public int GetAvailablePositions()
    {
        return isPos8Unlocked ? 8 : 7;
    }
    
    /// <summary>
    /// 检查是否还有空位
    /// </summary>
    public bool HasAvailableSpace()
    {
        // 只考虑已占用的槽位
        int occupiedCount = GetOccupiedSlots().Count;
        return occupiedCount < GetAvailablePositions();
    }
    
    /// <summary>
    /// 智能收集泡泡 - 完全参考 GameControl.SubmitElement 的逻辑
    /// </summary>
    public bool CollectBubbleIntelligent(BubbleItem newBubble)
    {
        if (!HasAvailableSpace())
        {
            // Debug.Log("收集区域暂时已满，新泡泡无法收集");
            // 不触发游戏失败，只是拒绝收集，给三消机会完成
            return false;
        }
        
        if (newBubble == null)
        {
            // Debug.LogError("CollectAreaManager: 尝试收集空泡泡！");
            return false;
        }
        
        // 检查泡泡是否已经被收集
        if (newBubble.IsSubmitted)
        {
            // Debug.LogWarning($"泡泡 {newBubble.imageEnum} 已经被收集，跳过重复收集");
            return false;
        }
        
        // 按照参考代码逻辑：先执行插入逻辑，最后才标记状态
        // 执行智能插入逻辑
        SubmitBubbleToSlot(newBubble);
        
        return true;
    }
    
    /// <summary>
    /// 解锁第8个位置
    /// </summary>
    public void UnlockPosition8()
    {
        ADDPosBtn.enabled = false;
        ADManager.Instance.playRewardVideo((success)=>
        {
            if(success)
            {
                isPos8Unlocked = true;
                ADDPosBtn.gameObject.SetActive(false);

                if (GameManager.Instance.GetGameType() == GameType.Level)
                {
                    PostEventScript.GetInstance().SendEvent("1008", "1");
                }else
                {
                    PostEventScript.GetInstance().SendEvent("1012", "1");
                }
                RefShowTips();
            }else{
                ADDPosBtn.enabled = true;
            }
        },"3");
        
        // Debug.Log("CollectAreaManager: 第8个位置已解锁！");
    }
    
    /// <summary>
    /// 重置解锁状态
    /// </summary>
    public void ResetUnlockStatus()
    {
        isPos8Unlocked = false;
        ADDPosBtn.gameObject.SetActive(true);
        ADDPosBtn.enabled = true;
        RefShowTips();
    }
    
    /// <summary>
    /// 清空收集区域
    /// </summary>
    public void ClearAreaForNewGame()
    {
        StopAllCoroutines();
        isProcessingMatches = false;
        isGapFilling = false; // 重置补位状态
        continuousMatches = 0;
        hasTriggeredGameOver = false; // 重置游戏失败标志
        
        foreach (var slot in bubbleSlots)
        {
            if (slot != null && slot.IsOccupied)
            {
                BubbleItem bubble = slot.RemoveBubble();
                if (bubble != null)
                {
                    Destroy(bubble.gameObject);
                }
            }
        }
        
        // Debug.Log("CollectAreaManager: 收集区域已清空，游戏失败状态已重置");
    }
    #endregion
    
    #region 核心逻辑 - 借鉴 New Folder 的设计
    /// <summary>
    /// 智能提交泡泡到槽位 - 完全按照 GameControl.SubmitElement 的逻辑
    /// </summary>
    private void SubmitBubbleToSlot(BubbleItem bubbleItem)
    {
        // 检查是否正在补位，如果是则等待补位完成
        if (isGapFilling)
        {
            // Debug.Log($"正在补位中，等待补位完成后再处理新泡泡 {bubbleItem.imageEnum}");
            
            // 立即停止当前移动动画
            bubbleItem.transform.DOKill();
            
            // 🎯 修复：等待补位完成后再处理新泡泡
            StartCoroutine(WaitForGapFillComplete(bubbleItem));
            return;
        }
        
        int availableSlots = GetAvailablePositions();
        
        // 完全按照参考代码逻辑：顺序查找槽位
        for (int i = 0; i < availableSlots; i++)
        {
            if (!bubbleSlots[i].IsOccupied)
            {
                // 🎯 修复：先设置槽位状态，再执行移动动画
                // 找到空槽位：直接插入
                bubbleSlots[i].SetBubble(bubbleItem);
                bubbleItem.MoveToPosition(bubbleSlots[i].transform.position, () => { OnMoveComplete(); });
                break;
            }
            else
            {
                // 槽位被占用：检查是否与当前槽位类型相同
                if (bubbleSlots[i].BubbleType == bubbleItem.imageEnum)
                {
                    // 相同类型：执行插入逻辑（将后面的泡泡向右移动）
                    PerformInsertion(bubbleItem, i);
                    break;
                }
                // 如果类型不同，继续查找下一个槽位
            }
        }
        
        // 按照参考代码：最后才标记状态
        bubbleItem.MarkAsSubmitted();
        // 按照参考代码：最后才移除对象（我们这里是禁用）
        bubbleItem.DisableBubble();
    }
    

    
    /// <summary>
    /// 执行插入逻辑 - 修复插入位置计算
    /// </summary>
    private void PerformInsertion(BubbleItem newBubble, int sameTypeIndex)
    {
        int availableSlots = GetAvailablePositions();
        
        // Debug.Log($"执行插入逻辑：泡泡 {newBubble.imageEnum} 插入到相同类型组后面，相同类型位置：{sameTypeIndex}");
        
        // 计算应该插入的位置：找到相同类型组的最后一个位置
        int insertIndex = sameTypeIndex;
        for (int i = sameTypeIndex + 1; i < availableSlots; i++)
        {
            if (bubbleSlots[i].IsOccupied && bubbleSlots[i].BubbleType == newBubble.imageEnum)
            {
                insertIndex = i;
            }
            else
            {
                break; // 遇到不同类型或空槽位就停止
            }
        }
        insertIndex++; // 插入到相同类型组的后面
        
        // Debug.Log($"计算插入位置：相同类型组结束位置 {insertIndex - 1}，新泡泡插入到位置 {insertIndex}");
        
        // 将后面所有泡泡向右移动一位（从后往前处理，避免覆盖）
        for (int j = availableSlots - 1; j >= insertIndex; j--)
        {
            if (bubbleSlots[j].IsOccupied && bubbleSlots[j].CurrentBubble != null)
            {
                int targetIndex = j + 1;
                if (targetIndex < availableSlots)
                {
                    // Debug.Log($"移位：槽位 {j} → 槽位 {targetIndex}");
                    // 🎯 修复：先设置槽位状态，再执行移动动画
                    bubbleSlots[targetIndex].SetBubble(bubbleSlots[j].CurrentBubble);
                    bubbleSlots[j].CurrentBubble.MoveToPosition(bubbleSlots[targetIndex].transform.position, () => { });
                }
            }
        }

        // 🎯 修复：先设置槽位状态，再执行移动动画
        // 新泡泡插入到计算出的位置
        Debug.Log($"🔧 设置泡泡 {newBubble.imageEnum} 到槽位 {insertIndex}");
        bubbleSlots[insertIndex].SetBubble(newBubble);
        Debug.Log($"🔧 槽位 {insertIndex} 状态: 占用={bubbleSlots[insertIndex].IsOccupied}, 泡泡={bubbleSlots[insertIndex].CurrentBubble?.imageEnum}");
        newBubble.MoveToPosition(bubbleSlots[insertIndex].transform.position, () => { 
            Debug.Log($"🔧 泡泡 {newBubble.imageEnum} 移动完成，调用 OnMoveComplete");
            OnMoveComplete(); 
        });
    }
    
    /// <summary>
    /// 移动泡泡到指定槽位（简化版，因为槽位状态已提前设置）
    /// </summary>
    private void MoveBubbleToSlot(BubbleItem bubble, int slotIndex)
    {
        if (slotIndex >= bubbleSlots.Length || bubbleSlots[slotIndex] == null)
        {
            // Debug.LogError($"无效的槽位索引: {slotIndex}");
            return;
        }
        
        // 槽位状态已经在 SubmitBubbleToSlot 中设置，这里只需要执行移动动画
        bubble.MoveToPosition(bubbleSlots[slotIndex].transform.position, () =>
        {
            // 移动完成后检测三消
            OnMoveComplete();
        });
    }
    
    /// <summary>
    /// 从一个槽位移动泡泡到另一个槽位 - 按照 GameControl 的逻辑
    /// </summary>
    private void MoveBubbleFromSlotToSlot(int fromIndex, int toIndex)
    {
        if (fromIndex >= bubbleSlots.Length || toIndex >= bubbleSlots.Length)
        {
            // Debug.LogError($"无效的槽位索引: from={fromIndex}, to={toIndex}");
            return;
        }
        
        BubbleSlotBehavior fromSlot = bubbleSlots[fromIndex];
        BubbleSlotBehavior toSlot = bubbleSlots[toIndex];
        
        if (!fromSlot.IsOccupied) return;
        
        BubbleItem bubble = fromSlot.RemoveBubble();
        if (bubble != null)
        {
            // 按照参考代码逻辑：先设置目标槽位状态，再执行移动动画
            toSlot.SetBubble(bubble);
            
            // 执行移动动画（异步）
            bubble.MoveToPosition(toSlot.transform.position, () =>
            {
                // Debug.Log($"移位完成：泡泡 {bubble.imageEnum} 从槽位 {fromIndex} 移动到槽位 {toIndex}");
            });
        }
    }
    
    /// <summary>
    /// 补位专用的移动方法 - 使用简单的直线移动，避免跳动
    /// </summary>
    private void MoveBubbleFromSlotToSlotForGapFill(int fromIndex, int originalIndex, int toIndex)
    {
        if (fromIndex >= bubbleSlots.Length || toIndex >= bubbleSlots.Length)
        {
            // Debug.LogError($"无效的槽位索引: from={fromIndex}, to={toIndex}");
            return;
        }
        
        BubbleSlotBehavior fromSlot = bubbleSlots[fromIndex];
        BubbleSlotBehavior toSlot = bubbleSlots[toIndex];
        
        if (!fromSlot.IsOccupied) return;
        
        BubbleItem bubble = fromSlot.RemoveBubble();
        if (bubble != null)
        {
            // 按照参考代码逻辑：先设置目标槽位状态，再执行移动动画，最后清空原位置
            toSlot.SetBubble(bubble);
            
            // 执行补位移动动画（直线移动，无旋转缩放）
            bubble.MoveToPosition(toSlot.transform.position, () =>
            {
                // Debug.Log($"补位移动完成：泡泡 {bubble.imageEnum} 从槽位 {fromIndex} 移动到槽位 {toIndex}");
            });
            
            // 按照参考代码：清空原位置（参考 SlotBehavior.InitData）
            fromSlot.ClearSlot();
            // Debug.Log($"补位后清空原槽位 {fromIndex}");
        }
    }
    

    

    

    #endregion
    
    #region 三消检测和处理 - 参考 GameControl.MoveEnd
    /// <summary>
    /// 移动完成回调 - 参考 MoveEnd 的逻辑
    /// </summary>
    private void OnMoveComplete()
    {
        // 防止重复调用
        if (isProcessingMatches)
        {
            Debug.Log("🔧 OnMoveComplete: 正在处理三消，跳过重复调用");
            return;
        }
        
        Debug.Log("🔧 OnMoveComplete: 开始检测三消");
        // 调试：打印当前槽位状态
        PrintCurrentSlotStatus();
        
        // 检测三消
        CheckAndProcessMatches();
        
        // 检查区域状态
        CheckAreaStatus();
        
        // 移除立即失败判断，让三消有机会完成
        // 只有在三消完成后，区域确实满了，才考虑失败
    }
    
    /// <summary>
    /// 打印当前槽位状态（调试用）
    /// </summary>
    private void PrintCurrentSlotStatus()
    {
        string status = "🔧 当前槽位状态: ";
        for (int i = 0; i < GetAvailablePositions(); i++)
        {
            if (bubbleSlots[i].IsOccupied)
            {
                status += $"[{i}:{bubbleSlots[i].BubbleType}] ";
            }
            else
            {
                status += $"[{i}:空] ";
            }
        }
        Debug.Log(status);
    }
    
    /// <summary>
    /// 检测并处理三消 - 完全参考 MoveEnd 的三消检测逻辑
    /// </summary>
    private void CheckAndProcessMatches()
    {
        int availableSlots = GetAvailablePositions();
        
        Debug.Log($"🔧 开始检测三消，可用槽位: {availableSlots}");
        
        for (int i = 0; i < availableSlots - 2; i++)
        {
            // 检查连续三个槽位是否都有泡泡且类型相同
            bool slot0Occupied = bubbleSlots[i].IsOccupied;
            bool slot1Occupied = bubbleSlots[i + 1].IsOccupied;
            bool slot2Occupied = bubbleSlots[i + 2].IsOccupied;
            
            Debug.Log($"🔧 检查位置 {i}-{i+2}: 占用状态[{slot0Occupied}][{slot1Occupied}][{slot2Occupied}]");
            
            if (slot0Occupied && slot1Occupied && slot2Occupied)
            {
                ImageEnum type0 = bubbleSlots[i].BubbleType;
                ImageEnum type1 = bubbleSlots[i + 1].BubbleType;
                ImageEnum type2 = bubbleSlots[i + 2].BubbleType;
                bool isMatch = type0 == type1 && type1 == type2;
                
                Debug.Log($"🔧 位置 {i}-{i+2}: 类型[{type0}][{type1}][{type2}], 匹配:{isMatch}");
                
                if (isMatch)
                {
                    Debug.Log($"🔧 发现三消！位置: {i}-{i+2}, 类型: {type0}");
                    ProcessMatchAtPosition(i);
                    break; // 一次只处理一个三消，避免复杂情况
                }
            }
        }
    }
    
    /// <summary>
    /// 处理指定位置的三消 - 完全参考 GameControl.MoveEnd 的逻辑
    /// </summary>
    private void ProcessMatchAtPosition(int startIndex)
    {
        isProcessingMatches = true;
        continuousMatches++;
        
        // 🎯 完全参考 GameControl.MoveEnd 的处理方式
        // 1. 立即清空槽位
        // 2. 立即执行补位
        // 3. 递归检测新的三消
        
        Debug.Log($"🔧 发现三消！位置: {startIndex}-{startIndex + 2}");
        
        // 收集要消除的泡泡
        List<BubbleItem> matchedBubbles = new List<BubbleItem>();
        for (int i = 0; i < 3; i++)
        {
            BubbleSlotBehavior slot = bubbleSlots[startIndex + i];
            if (slot.IsOccupied && slot.CurrentBubble != null)
            {
                matchedBubbles.Add(slot.CurrentBubble);
                // 立即标记为已提交，防止被重复处理
                slot.CurrentBubble.MarkAsSubmitted();
            }
        }
        
        if (matchedBubbles.Count == 3)
        {
            // 触发消除动画
            OnMatchesFound?.Invoke(continuousMatches);
            StartEliminateAnimation(new List<int> { startIndex, startIndex + 1, startIndex + 2 });
            
            // 🎯 关键：立即清空槽位并执行补位（参考 GameControl.MoveEnd）
            // 立即清空槽位
            for (int i = 0; i < 3; i++)
            {
                bubbleSlots[startIndex + i].ClearSlot();
                Debug.Log($"🔧 立即清空槽位 {startIndex + i}");
            }
            
            // 立即执行补位（参考 GameControl.MoveEnd 的补位逻辑）
            ExecuteImmediateGapFill(startIndex);
        }
        else
        {
            // 如果泡泡数量不对，重置处理状态
            isProcessingMatches = false;
            Debug.LogWarning($"🔧 三消检测异常：位置{startIndex}只找到{matchedBubbles.Count}个泡泡");
        }
    }

     public void StartEliminateAnimation(List<int> matchIndexes)
    {
        // 获取要消除的泡泡
        List<BubbleItem> bubblestoEliminate = new List<BubbleItem>();
        foreach (int index in matchIndexes)
        {
            if (index < bubbleSlots.Length && bubbleSlots[index].IsOccupied)
            {
                bubblestoEliminate.Add(bubbleSlots[index].CurrentBubble);
            }
        }
        if (bubblestoEliminate.Count == 0) return;
        Vector3 firstPosition = bubblestoEliminate[0].transform.position;
        Sequence eliminateSequence = DOTween.Sequence();
        for (int i = 1; i < bubblestoEliminate.Count; i++)
        {
            eliminateSequence.Join(bubblestoEliminate[i].transform.DOMove(firstPosition, 0.2f)
                .SetEase(Ease.InQuart));
        }
        eliminateSequence.OnComplete(() =>
        {
            if(bubblestoEliminate[0].imageEnum == ImageEnum.IMG0)
            {
                PostEventScript.GetInstance().SendEvent("1003");
                MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_matchCash);
                int num = NetInfoMgr.instance.GameData.cashmatch;
                UIManager.GetInstance().ShowUIForms(nameof(LowRewardPanel),num);
            }else{
                MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_match);
                int num = NetInfoMgr.instance.GameData.normalmatch;
                HomePanel.Instance.AddCash(num,bubblestoEliminate[0].transform);
            }
            if (GameEvents.CollectParticle != null)
            {
                GameEvents.CollectParticle.Invoke( bubblestoEliminate);
            }
            foreach (var bubble in bubblestoEliminate)
            {
                bubble.transform.DOScale(0f, 0.3f).SetEase(Ease.InBack);
            }
            
            DOVirtual.DelayedCall(0.3f, () =>
            {
                // 🎯 修复：槽位已经在 ProcessMatchAtPosition 中立即清空了，不需要再次清空
                // 补位也已经在 ProcessMatchAtPosition 中立即执行了，不需要再次补位
                Debug.Log($"🔧 三消动画完成");
                
                // 通知GameArea检查游戏状态（三消完成后可能达到胜利条件）
                GameEvents.OnThreeMatchCompleted?.Invoke();
                
                // 三消结束后检查是否过关
                CheckGameWinCondition();
            });
        });
    }
    

    
    /// <summary>
    /// 立即执行补位 - 完全参考 GameControl.MoveEnd 的补位逻辑
    /// </summary>
    private void ExecuteImmediateGapFill(int eliminatedStartIndex)
    {
        int availableSlots = GetAvailablePositions();
        int moveStartIndex = eliminatedStartIndex + 3;
        
        Debug.Log($"🔧 立即补位：消除位置{eliminatedStartIndex}，移动起始位置{moveStartIndex}");
        
        // 按照参考代码逻辑：将后面的泡泡前移3个位置
        for (int j = moveStartIndex; j < availableSlots; j++)
        {
            if (bubbleSlots[j].IsOccupied)
            {
                int targetIndex = j - 3;
                if (targetIndex >= 0)
                {
                    Debug.Log($"🔧 立即补位移动：槽位{j} → 槽位{targetIndex}");
                    // 按照参考代码：先移动，再设置状态，最后清空原位置
                    MoveBubbleFromSlotToSlotForGapFill(j, j, targetIndex);
                }
            }
        }
        
        // 🎯 关键：补位完成后立即检测新的三消（参考 GameControl.MoveEnd）
        // 延迟一帧确保所有移动状态更新完成
        StartCoroutine(DelayedMatchCheckAfterGapFill());
    }
    
    /// <summary>
    /// 延迟检测三消（补位完成后）
    /// </summary>
    private IEnumerator DelayedMatchCheckAfterGapFill()
    {
        // 等待一帧确保所有移动状态更新完成
        yield return null;
        
        // 重置处理状态
        isProcessingMatches = false;
        Debug.Log($"🔧 补位完成，开始检测新的三消");
        
        // 检测是否因为补位形成了新的三消
        CheckAndProcessMatches();
    }

    /// <summary>
    /// 消除后填补空位 - 完全按照 GameControl.MoveEnd 的逻辑
    /// </summary>
    private IEnumerator FillGapsAfterElimination(int eliminatedStartIndex)
    {
        // 设置补位状态标志
        isGapFilling = true;
        // Debug.Log("设置补位状态：isGapFilling = true");
        
        int availableSlots = GetAvailablePositions();
        int moveStartIndex = eliminatedStartIndex + 3;
        
        // Debug.Log($"开始补位：消除位置{eliminatedStartIndex}，移动起始位置{moveStartIndex}");
        
        // 按照参考代码逻辑：将后面的泡泡前移3个位置
        for (int j = moveStartIndex; j < availableSlots; j++)
        {
            if (bubbleSlots[j].IsOccupied)
            {
                int targetIndex = j - 3;
                if (targetIndex >= 0)
                {
                    // Debug.Log($"补位移动：槽位{j} → 槽位{targetIndex}");
                    // 按照参考代码：先移动，再设置状态，最后清空原位置
                    MoveBubbleFromSlotToSlotForGapFill(j, j, targetIndex);
                }
            }
        }
        
        // 等待所有移动动画完成（0.05秒 * 移动的泡泡数量）
        int movedBubbles = 0;
        for (int j = moveStartIndex; j < availableSlots; j++)
        {
            if (bubbleSlots[j].IsOccupied) movedBubbles++;
        }
        
        float totalWaitTime = Mathf.Max(0.05f * movedBubbles, 0.1f); // 至少等待0.1秒
        yield return new WaitForSeconds(totalWaitTime);
        
        // Debug.Log($"补位完成，移动了{movedBubbles}个泡泡");
        
        // 重置补位状态标志
        isGapFilling = false;
        // Debug.Log("重置补位状态：isGapFilling = false");
        
        // 验证补位结果
        // PrintCurrentSlotStatus();
        
        // 🎯 关键修改：补位完成后自动检测三消（参考 GameControl.MoveEnd 的逻辑）
        // Debug.Log("补位完成，开始检测是否形成新的三消...");
        
        // 延迟一帧确保所有动画状态更新完成
        yield return null;
        
        // 检测是否因为补位形成了新的三消
        CheckAndProcessMatches();
    }
    #endregion
    
    #region 状态查询和辅助方法
    /// <summary>
    /// 获取已占用的槽位列表
    /// </summary>
    private List<BubbleSlotBehavior> GetOccupiedSlots()
    {
        return bubbleSlots.Where(slot => slot != null && slot.IsOccupied).ToList();
    }
    
    /// <summary>
    /// 获取已占用的槽位列表（公共接口，供外部使用）
    /// </summary>
    public List<BubbleSlotBehavior> GetOccupiedSlotsPublic()
    {
        return GetOccupiedSlots();
    }
    
    /// <summary>
    /// 从指定槽位移除泡泡
    /// </summary>
    /// <param name="bubble">要移除的泡泡</param>
    /// <returns>是否成功移除</returns>
    public bool RemoveBubbleFromSlot(BubbleItem bubble)
    {
        if (bubble == null) return false;
        
        for (int i = 0; i < bubbleSlots.Length; i++)
        {
            if (bubbleSlots[i] != null && bubbleSlots[i].CurrentBubble == bubble)
            {
                bubbleSlots[i].ClearSlot();
                // Debug.Log($"从槽位 {i} 移除泡泡 {bubble.imageEnum}");
                return true;
            }
        }
        
        return false;
    }
    
    /// <summary>
    /// 获取指定类型的泡泡数量
    /// </summary>
    public int GetBubbleCountByType(ImageEnum bubbleType)
    {
        return bubbleSlots.Count(slot => slot != null && slot.IsOccupied && slot.BubbleType == bubbleType);
    }
    
    /// <summary>
    /// 检查区域状态
    /// </summary>
    private void CheckAreaStatus()
    {
        int occupiedCount = GetOccupiedSlots().Count;
        
        if (occupiedCount == 0)
        {
            OnAreaEmpty?.Invoke();
        }
        
        // 检查是否应该触发游戏失败
        CheckGameFailureCondition();
    }
    
    /// <summary>
    /// 检查游戏失败条件 - 槽位满了且没有可消除的泡泡
    /// </summary>
    private void CheckGameFailureCondition()
    {
        // 只有在不处理三消和补位时才检查失败条件
        if (isProcessingMatches || isGapFilling)
        {
            // Debug.Log("正在处理三消或补位，跳过失败条件检查");
            return;
        }
        
        // 检查槽位是否已满
        if (!HasAvailableSpace())
        {
            // Debug.Log("槽位已满，检查是否有可消除的泡泡...");
            
            // 检查是否还有可消除的泡泡
            if (!HasEliminatableBubbles())
            {
                // Debug.Log("❌ 游戏失败：槽位已满且没有可消除的泡泡！");
                TriggerGameFailure();
            }
            else
            {
                // Debug.Log("槽位已满但还有可消除的泡泡，游戏继续");
            }
        }
    }
    
    /// <summary>
    /// 检查游戏胜利条件 - 三消结束后检测是否过关
    /// 过关条件：暂存区、清理区、场上自由球、未生成球都没了
    /// </summary>
    private void CheckGameWinCondition()
    {
        // 只有在不处理三消和补位时才检查胜利条件
        if (isProcessingMatches || isGapFilling)
        {
            // Debug.Log("正在处理三消或补位，跳过胜利条件检查");
            return;
        }
        
        // 检查暂存区是否为空
        bool storageAreaEmpty = GetOccupiedSlots().Count == 0;
        
        // 检查清理区是否为空
        bool cleanAreaEmpty = IsCleanAreaEmpty();
        
        // 检查场上自由球是否为空
        bool freeBubblesEmpty = IsFreeBubblesEmpty();
        
        // 检查未生成球是否为空
        bool remainingBubblesEmpty = IsRemainingBubblesEmpty();
        
        // Debug.Log($"胜利条件检查 - 暂存区空:{storageAreaEmpty}, 清理区空:{cleanAreaEmpty}, 自由球空:{freeBubblesEmpty}, 未生成球空:{remainingBubblesEmpty}");
        
        // 所有条件都满足时触发胜利
        if (storageAreaEmpty && cleanAreaEmpty && freeBubblesEmpty && remainingBubblesEmpty)
        {
            // Debug.Log("🎉 游戏胜利！所有泡泡都已消除完毕！");
            TriggerGameWin();
        }
    }
    
    /// <summary>
    /// 检查清理区是否为空
    /// </summary>
    private bool IsCleanAreaEmpty()
    {
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            return fullStayArea.StoredBubbleCount == 0;
        }
        return true; // 如果没有清理区组件，认为为空
    }
    
    /// <summary>
    /// 检查场上自由球是否为空
    /// </summary>
    private bool IsFreeBubblesEmpty()
    {
        // 通过GameArea获取场上自由球数量
        GameArea gameArea = FindObjectOfType<GameArea>();
        if (gameArea != null)
        {
            // 使用反射或公共方法获取自由球数量
            // 这里假设GameArea有公共方法可以获取自由球数量
            return gameArea.GetFreeBubblesCount() == 0;
        }
        return true; // 如果没有GameArea组件，认为为空
    }
    
    /// <summary>
    /// 检查未生成球是否为空
    /// </summary>
    private bool IsRemainingBubblesEmpty()
    {
        // 通过GameArea获取剩余球数量
        GameArea gameArea = FindObjectOfType<GameArea>();
        if (gameArea != null)
        {
            // 使用反射或公共方法获取剩余球数量
            // 这里假设GameArea有公共方法可以获取剩余球数量
            return gameArea.GetRemainingBubblesCount() == 0;
        }
        return true; // 如果没有GameArea组件，认为为空
    }
    
    /// <summary>
    /// 触发游戏胜利
    /// </summary>
    private void TriggerGameWin()
    {
         Debug.Log("🎯 游戏胜利已触发：所有泡泡都已消除完毕");
        
        // 触发游戏胜利事件
        GameEvents.GameWin?.Invoke();
    }
    
    /// <summary>
    /// 检查是否还有可消除的泡泡
    /// </summary>
    public bool HasEliminatableBubbles()
    {
        int availableSlots = GetAvailablePositions();
        
        // 检查是否有连续三个相同类型的泡泡
        for (int i = 0; i < availableSlots - 2; i++)
        {
            if (bubbleSlots[i].IsOccupied && 
                bubbleSlots[i + 1].IsOccupied && 
                bubbleSlots[i + 2].IsOccupied)
            {
                ImageEnum type0 = bubbleSlots[i].BubbleType;
                ImageEnum type1 = bubbleSlots[i + 1].BubbleType;
                ImageEnum type2 = bubbleSlots[i + 2].BubbleType;
                
                if (type0 == type1 && type1 == type2)
                {
                    // Debug.Log($"发现可消除的泡泡：位置 {i}-{i + 2}，类型 {type0}");
                    return true;
                }
            }
        }
        
        // Debug.Log("没有发现可消除的泡泡");
        return false;
    }
    
    /// <summary>
    /// 触发游戏失败
    /// </summary>
    private void TriggerGameFailure()
    {
        // 防止重复触发
        if (hasTriggeredGameOver)
        {
            // Debug.Log("游戏失败已经触发过，跳过重复触发");
            return;
        }
        
        hasTriggeredGameOver = true;
        
        // 触发游戏失败事件
        OnAreaFull?.Invoke();
        
        // 通知GameArea触发游戏失败
        GameEvents.GameOver?.Invoke();
        
        // Debug.Log("🎯 游戏失败已触发：槽位已满且无消除可能");
    }
    
    /// <summary>
    /// 获取连续消除次数
    /// </summary>
    public int GetContinuousMatches()
    {
        return continuousMatches;
    }
    
    /// <summary>
    /// 重置连续消除计数
    /// </summary>
    public void ResetContinuousMatches()
    {
        continuousMatches = 0;
    }
    
    /// <summary>
    /// 手动检查游戏失败条件（公共接口）
    /// </summary>
    public void CheckGameFailureManually()
    {
        // Debug.Log("手动检查游戏失败条件");
        CheckGameFailureCondition();
    }
    
    /// <summary>
    /// 手动检查游戏胜利条件（公共接口）
    /// </summary>
    public void CheckGameWinManually()
    {
        // Debug.Log("手动检查游戏胜利条件");
        CheckGameWinCondition();
    }
    
    /// <summary>
    /// 等待补位完成后处理新泡泡
    /// </summary>
    private IEnumerator WaitForGapFillComplete(BubbleItem bubbleItem)
    {
        // 等待补位完成
        while (isGapFilling)
        {
            yield return null;
        }
        
        // 补位完成后，重新处理这个泡泡
        // Debug.Log($"补位完成，重新处理泡泡 {bubbleItem.imageEnum}");
        SubmitBubbleToSlot(bubbleItem);
    }
    
    /// <summary>
    /// 验证槽位状态一致性（调试用）
    /// </summary>
    [ContextMenu("Validate Slot States")]
    public void ValidateSlotStates()
    {
        Debug.Log("=== 开始验证槽位状态一致性 ===");
        
        int availableSlots = GetAvailablePositions();
        bool hasInconsistency = false;
        
        for (int i = 0; i < availableSlots; i++)
        {
            bool slotOccupied = bubbleSlots[i].IsOccupied;
            BubbleItem currentBubble = bubbleSlots[i].CurrentBubble;
            
            // 检查状态一致性
            if (slotOccupied && currentBubble == null)
            {
                Debug.LogError($"槽位 {i}: 状态为占用但泡泡为空！");
                hasInconsistency = true;
            }
            else if (!slotOccupied && currentBubble != null)
            {
                Debug.LogError($"槽位 {i}: 状态为空但泡泡不为空！");
                hasInconsistency = true;
            }
            else if (currentBubble != null && currentBubble.transform.parent != bubbleSlots[i].transform)
            {
                Debug.LogError($"槽位 {i}: 泡泡的父对象不是当前槽位！");
                hasInconsistency = true;
            }
            else
            {
                Debug.Log($"槽位 {i}: 状态正常 - 占用:{slotOccupied}, 泡泡:{currentBubble?.imageEnum}");
            }
        }
        
        if (!hasInconsistency)
        {
            Debug.Log("✅ 所有槽位状态一致！");
        }
        else
        {
            Debug.LogError("❌ 发现槽位状态不一致！");
        }
        
        Debug.Log("=== 槽位状态验证完成 ===");
    }
    
    /// <summary>
    /// 重置游戏失败状态
    /// </summary>
    public void ResetGameFailureState()
    {
        hasTriggeredGameOver = false;
        // Debug.Log("游戏失败状态已重置");
    }
    
    /// <summary>
    /// 清理后触发补位逻辑
    /// </summary>
    public void TriggerGapFillAfterClean()
    {
        // Debug.Log("清理完成后触发补位逻辑");
        StartCoroutine(FillGapsAfterClean());
    }
    
    /// <summary>
    /// 清理后的补位协程
    /// </summary>
    private IEnumerator FillGapsAfterClean()
    {
        // 设置补位状态标志
        isGapFilling = true;
        // Debug.Log("设置清理后补位状态：isGapFilling = true");
        
        int availableSlots = GetAvailablePositions();
        
        // 🎯 修复：使用更简单有效的补位逻辑
        // 将所有有泡泡的槽位重新排列到前面，消除所有空位
        
        // 收集所有有泡泡的槽位
        List<BubbleItem> occupiedBubbles = new List<BubbleItem>();
        for (int i = 0; i < availableSlots; i++)
        {
            if (bubbleSlots[i].IsOccupied && bubbleSlots[i].CurrentBubble != null)
            {
                occupiedBubbles.Add(bubbleSlots[i].CurrentBubble);
                // 清空原槽位
                bubbleSlots[i].ClearSlot();
            }
        }
        
        if (occupiedBubbles.Count == 0)
        {
            // 没有泡泡需要补位
            isGapFilling = false;
            yield break;
        }
        
        // Debug.Log($"发现 {occupiedBubbles.Count} 个泡泡需要重新排列");
        
        // 将所有泡泡重新排列到前面的槽位（左对齐）
        for (int i = 0; i < occupiedBubbles.Count; i++)
        {
            if (occupiedBubbles[i] != null)
            {
                // 设置到新槽位
                bubbleSlots[i].SetBubble(occupiedBubbles[i]);
                
                // 移动到新位置
                occupiedBubbles[i].MoveToPosition(bubbleSlots[i].transform.position, () =>
                {
                    // Debug.Log($"补位移动完成：泡泡 {occupiedBubbles[i].imageEnum} 移动到槽位 {i}");
                });
            }
        }
        
        // 等待补位动画完成
        yield return new WaitForSeconds(0.3f);
        
        // 重置补位状态标志
        isGapFilling = false;
        // Debug.Log("重置清理后补位状态：isGapFilling = false");
        
        // 补位完成后检测三消
        CheckAndProcessMatches();
    }
    #endregion
    
    #region Debug 方法
    [System.Diagnostics.Conditional("UNITY_EDITOR")]
    private void OnDrawGizmos()
    {
        if (bubbleSlots == null) return;
        
        for (int i = 0; i < bubbleSlots.Length; i++)
        {
            if (bubbleSlots[i] != null)
            {
                Vector3 pos = bubbleSlots[i].transform.position;
                Gizmos.color = i < GetAvailablePositions() ? Color.green : Color.gray;
                Gizmos.DrawWireCube(pos, Vector3.one * 0.5f);
                
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(pos + Vector3.up * 0.8f, $"Slot {i}");
                #endif
            }
        }
    }

    /// <summary>
    /// 测试快速连续收集（调试用）
    /// </summary>
    [ContextMenu("Test Quick Collection")]
    public void TestQuickCollection()
    {
        // Debug.Log("=== 开始快速连续收集测试 ===");
        
        // 创建测试泡泡
        for (int i = 0; i < 3; i++)
        {
            GameObject testBubbleObj = new GameObject($"TestBubble_{i}");
            BubbleItem testBubble = testBubbleObj.AddComponent<BubbleItem>();
            testBubble.imageEnum = (ImageEnum)(i % 3); // 循环使用3种类型
            
            // 设置随机位置
            Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
            testBubble.transform.position = randomPos;
            
            // 立即收集
            CollectBubbleIntelligent(testBubble);
            
            // Debug.Log($"测试泡泡 {i} ({testBubble.imageEnum}) 已收集，位置: {randomPos}");
        }
        
        // Debug.Log("=== 快速连续收集测试完成 ===");
    }

    /// <summary>
    /// 测试三消功能（调试用）
    /// </summary>
    [ContextMenu("Test Three Match")]
    public void TestThreeMatch()
    {
        // Debug.Log("=== 开始三消测试 ===");
        
        // 清空当前区域
        ClearAreaForNewGame();
        
        // 创建3个相同类型的泡泡
        for (int i = 0; i < 3; i++)
        {
            GameObject testBubbleObj = new GameObject($"MatchBubble_{i}");
            BubbleItem testBubble = testBubbleObj.AddComponent<BubbleItem>();
            testBubble.imageEnum = ImageEnum.IMG1; // 使用相同类型
            
            // 设置随机位置
            Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
            testBubble.transform.position = randomPos;
            
            // 立即收集
            CollectBubbleIntelligent(testBubble);
            
            // Debug.Log($"三消测试泡泡 {i} ({testBubble.imageEnum}) 已收集");
        }
        
        // 手动触发三消检测
        StartCoroutine(DelayedMatchCheck());
        
        // Debug.Log("=== 三消测试完成 ===");
    }

    /// <summary>
    /// 测试相同类型插入（调试用）
    /// </summary>
    [ContextMenu("Test Same Type Insert")]
    public void TestSameTypeInsert()
    {
        // Debug.Log("=== 开始相同类型插入测试 ===");
        
        // 清空当前区域
        ClearAreaForNewGame();
        
        // 先创建2个相同类型的泡泡
        for (int i = 0; i < 2; i++)
        {
            GameObject testBubbleObj = new GameObject($"SameTypeBubble_{i}");
            BubbleItem testBubble = testBubbleObj.AddComponent<BubbleItem>();
            testBubble.imageEnum = ImageEnum.IMG1; // 使用相同类型
            
            // 设置随机位置
            Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
            testBubble.transform.position = randomPos;
            
            // 立即收集
            CollectBubbleIntelligent(testBubble);
            
            // Debug.Log($"相同类型泡泡 {i} ({testBubble.imageEnum}) 已收集");
        }
        
        // 等待一下，然后添加第三个相同类型的泡泡
        StartCoroutine(DelayedSameTypeInsert());
        
        // Debug.Log("=== 相同类型插入测试完成 ===");
    }

    /// <summary>
    /// 延迟插入第三个相同类型泡泡
    /// </summary>
    private IEnumerator DelayedSameTypeInsert()
    {
        yield return new WaitForSeconds(1f); // 等待前两个泡泡动画完成
        
        // Debug.Log("插入第三个相同类型泡泡");
        
        GameObject thirdBubbleObj = new GameObject("ThirdSameTypeBubble");
        BubbleItem thirdBubble = thirdBubbleObj.AddComponent<BubbleItem>();
        thirdBubble.imageEnum = ImageEnum.IMG1; // 使用相同类型
        
        // 设置随机位置
        Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
        thirdBubble.transform.position = randomPos;
        
        // 收集第三个泡泡
        CollectBubbleIntelligent(thirdBubble);
        
        // Debug.Log($"第三个相同类型泡泡 ({thirdBubble.imageEnum}) 已收集");
    }

    /// <summary>
    /// 延迟检查三消
    /// </summary>
    private IEnumerator DelayedMatchCheck()
    {
        yield return new WaitForSeconds(1f); // 等待动画完成
        
        // Debug.Log("手动触发三消检测");
        CheckAndProcessMatches();
    }
    
    /// <summary>
    /// 输出当前状态信息
    /// </summary>
    [ContextMenu("Print Status")]
    public void PrintStatus()
    {
        // Debug.Log($"=== CollectAreaManager 状态 ===");
        // Debug.Log($"可用槽位: {GetAvailablePositions()}");
        // Debug.Log($"已占用: {GetOccupiedSlots().Count}");
        // Debug.Log($"是否还有空位: {HasAvailableSpace()}");
        // Debug.Log($"正在处理三消: {isProcessingMatches}");
        // Debug.Log($"正在补位: {isGapFilling}");
        // Debug.Log($"连续消除: {continuousMatches}");
        
        for (int i = 0; i < bubbleSlots.Length; i++)
        {
            if (bubbleSlots[i] != null)
            {
                string status = bubbleSlots[i].IsOccupied ? 
                    bubbleSlots[i].BubbleType.ToString() : "Empty";
                // Debug.Log($"槽位 {i}: {status}");
            }
        }
    }
    
    /// <summary>
    /// 测试槽位检测功能
    /// </summary>
    [ContextMenu("Test Slot Detection")]
    public void TestSlotDetection()
    {
        // Debug.Log("=== 开始槽位检测测试 ===");
        
        // 清空当前区域
        ClearAreaForNewGame();
        
        // 测试空状态
        // Debug.Log($"初始状态 - 可用槽位: {GetAvailablePositions()}, 已占用: {GetOccupiedSlots().Count}, 有空位: {HasAvailableSpace()}");
        
        // 填满所有槽位
        int availablePositions = GetAvailablePositions();
        for (int i = 0; i < availablePositions; i++)
        {
            GameObject testBubbleObj = new GameObject($"TestBubble_{i}");
            BubbleItem testBubble = testBubbleObj.AddComponent<BubbleItem>();
            testBubble.imageEnum = (ImageEnum)(i % 3); // 循环使用3种类型
            
            // 设置随机位置
            Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
            testBubble.transform.position = randomPos;
            
            // 收集泡泡
            bool collected = CollectBubbleIntelligent(testBubble);
            // Debug.Log($"泡泡 {i} 收集结果: {collected}, 当前已占用: {GetOccupiedSlots().Count}, 有空位: {HasAvailableSpace()}");
        }
        
        // 测试满状态
        // Debug.Log($"填满后状态 - 可用槽位: {GetAvailablePositions()}, 已占用: {GetOccupiedSlots().Count}, 有空位: {HasAvailableSpace()}");
        
        // 尝试收集额外泡泡（应该失败）
        GameObject extraBubbleObj = new GameObject("ExtraBubble");
        BubbleItem extraBubble = extraBubbleObj.AddComponent<BubbleItem>();
        extraBubble.imageEnum = ImageEnum.IMG1;
        extraBubble.transform.position = new Vector3(0, 0, 0);
        
        bool extraCollected = CollectBubbleIntelligent(extraBubble);
        // Debug.Log($"额外泡泡收集结果: {extraCollected} (应该为false)");
        
        // Debug.Log("=== 槽位检测测试完成 ===");
    }
    
    /// <summary>
    /// 测试游戏失败检测功能
    /// </summary>
    [ContextMenu("Test Game Failure Detection")]
    public void TestGameFailureDetection()
    {
        // Debug.Log("=== 开始游戏失败检测测试 ===");
        
        // 清空当前区域
        ClearAreaForNewGame();
        
        // 填满槽位，但确保没有可消除的泡泡
        int availablePositions = GetAvailablePositions();
        for (int i = 0; i < availablePositions; i++)
        {
            GameObject testBubbleObj = new GameObject($"TestBubble_{i}");
            BubbleItem testBubble = testBubbleObj.AddComponent<BubbleItem>();
            // 使用不同的类型，确保没有三消
            testBubble.imageEnum = (ImageEnum)(i % 4); // 使用4种类型循环
            
            // 设置随机位置
            Vector3 randomPos = new Vector3(UnityEngine.Random.Range(-5f, 5f), UnityEngine.Random.Range(-5f, 5f), 0);
            testBubble.transform.position = randomPos;
            
            // 收集泡泡
            bool collected = CollectBubbleIntelligent(testBubble);
            // Debug.Log($"泡泡 {i} 收集结果: {collected}");
        }
        
        // 手动触发失败检测
        // Debug.Log("手动触发游戏失败检测...");
        CheckGameFailureManually();
        
        // Debug.Log("=== 游戏失败检测测试完成 ===");
    }
    
    /// <summary>
    /// 计算正确的目标位置（基于补位后的状态）
    /// </summary>
    private Vector3 CalculateCorrectTargetPosition(BubbleItem bubble)
    {
        int correctSlotIndex = FindCorrectSlotIndex(bubble);
        if (correctSlotIndex >= 0 && correctSlotIndex < bubbleSlots.Length)
        {
            return bubbleSlots[correctSlotIndex].transform.position;
        }
        
        // 如果找不到正确槽位，返回当前位置（避免错误移动）
        // Debug.LogWarning($"无法找到泡泡 {bubble.imageEnum} 的正确槽位，保持当前位置");
        return bubble.transform.position;
    }
    
    /// <summary>
    /// 查找泡泡应该插入的正确槽位索引
    /// </summary>
    private int FindCorrectSlotIndex(BubbleItem bubble)
    {
        int availableSlots = GetAvailablePositions();
        
        // 按照正常逻辑查找槽位
        for (int i = 0; i < availableSlots; i++)
        {
            if (!bubbleSlots[i].IsOccupied)
            {
                // 找到空槽位
                return i;
            }
            else if (bubbleSlots[i].BubbleType == bubble.imageEnum)
            {
                // 找到相同类型，计算插入位置
                int insertIndex = i;
                for (int j = i + 1; j < availableSlots; j++)
                {
                    if (bubbleSlots[j].IsOccupied && bubbleSlots[j].BubbleType == bubble.imageEnum)
                    {
                        insertIndex = j;
                    }
                    else
                    {
                        break;
                    }
                }
                return insertIndex + 1;
            }
        }
        
        // 如果所有槽位都被占用，返回-1表示无法插入
        return -1;
    }
    #endregion


}


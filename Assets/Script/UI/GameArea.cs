using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Linq;
using UnityEngine;
using UnityEngine.U2D;
using UnityEngine.UI;
using DG.Tweening;
using Spine.Unity;
using Spine;



public class GameArea : MonoBehaviour
{
    public SpriteAtlas ballAtlas; // 拖拽图集到这里
    // 🎯 修改：不再需要对象池，因为直接实例化
    // public ObjectPool m_BallPool;
    public GameObject BallPoolParent;
    public BubbleItem ballObject;
    public List<Transform> BallInsTrans; // 球生成点列表
    public CollectAreaManager collectAreaManager; // 使用新的智能收集系统
    public SkeletonGraphic m_SkeletonGraphic;
    public Transform toolsUse1;
    public Transform toolsUse2;


    [Header("生成设置")]
    public float spawnInterval = 0.1f; // 生成间隔时间（秒）
    public Vector2 positionOffset = new Vector2(30f, 30f); // 位置随机偏移范围（像素）
    public int initialBubbleCount = 24; // 初始屏幕泡泡数量

    [Header("游戏模式设置")]
    public int challengeModeTotal = 7200; // 挑战模式总泡泡数
    public float obstaclePercentage = 0.24f; // 开始生成障碍的剩余百分比
    
    [Header("挑战模式难度设置")]
    public int baseBubbleTypes = 8; // 基础泡泡类型数量
    public float firstPhasePercentage = 0.2f; // 第一阶段百分比（前20%）
    public float phaseIncrementPercentage = 0.05f; // 每阶段递增百分比（5%）
    public int maxBubbleTypes = 19; // 最大泡泡类型数量

    private Coroutine spawnCoroutine; // 用于控制生成协程
    private List<BubbleItem> m_BubbleItems = new List<BubbleItem>();
    private int totalBubblesForLevel; // 当前关卡总泡泡数
    private int bubblesRemaining; // 剩余泡泡数
    private bool isObstacleMode = false; // 是否开启障碍模式
    public List<ToolsButtons> toolsButtons = new List<ToolsButtons>();
    public int challengeFailCount = 0;

    public Image progressImage;
    public RectTransform particalObj;
    [Header("保底机制")]
    private bool hasTriggeredGuarantee = false; // 是否已触发保底操作
    private bool hasTriggeredLevelGuarantee = false; // Level模式保底机制是否已触发
    private float lastLevelGuaranteeCheckTime = 0f; // 上次Level保底检测时间
    private float levelGuaranteeCheckInterval = 1f; // Level保底检测间隔（秒）

    public GameObject Tipsobj;

    public void Init()
    {
        m_SkeletonGraphic.AnimationState.Complete += OnAnimationComplete;
        GameEvents.GameFailContinue += () =>
        {
            m_SkeletonGraphic.transform.position = toolsUse2.position;
            m_SkeletonGraphic.Skeleton.SetToSetupPose();
            m_SkeletonGraphic.AnimationState.ClearTracks();
            m_SkeletonGraphic.AnimationState.SetAnimation(0, "2", false);
        };
        GameEvents.GameOver += () =>
        {
            if (GameManager.Instance.GetGameType() == GameType.Level)
            {
                //传入1表示可以复活
                UIManager.GetInstance().ShowUIForms(nameof(ContinueOrFailPanel), 1);
            }
            else
            {
                if (challengeFailCount < 1)
                {
                    challengeFailCount++;
                    UIManager.GetInstance().ShowUIForms(nameof(ContinueOrFailPanel), 1);
                }
                else
                {
                    UIManager.GetInstance().ShowUIForms(nameof(ContinueOrFailPanel), 0);
                }

            }
        };

        // 监听三消完成事件，检查游戏状态
        GameEvents.OnThreeMatchCompleted += () =>
        {
            // Debug.Log("收到三消完成事件，检查游戏状态");
            // 🎯 新增：三消完成后更新进度条
            UpdateProgressBar();
            CheckGameEnd();
        };
        
        // 监听游戏胜利事件
        GameEvents.GameWin += () =>
        {
            // Debug.Log("收到游戏胜利事件");
            OnGameWin();
        };

        BallPoolInit();
        for (int i = 0; i < toolsButtons.Count; i++)
        {
            toolsButtons[i].Init();
            toolsButtons[i].OnToolsUse += OnToolsUse;
        }
    }
    private void OnAnimationComplete(TrackEntry trackEntry)
    {
        if (trackEntry != null)
        {
            if (trackEntry.Animation.Name == "1")
            {
                OnClickMagnetBtn();
            }
            else if (trackEntry.Animation.Name == "2")
            {
                OnClickCleanBtn();
            }
            else if (trackEntry.Animation.Name == "3")
            {
                OnClickRefreshBtn();
            }
        }
    }
    private void OnToolsUse(ToolsType toolsType)
    {
        HomePanel.Instance.ShowClickMask();
        switch (toolsType)
        {
            case ToolsType.MAGNET:
                m_SkeletonGraphic.transform.position = toolsUse1.position;
                m_SkeletonGraphic.Skeleton.SetToSetupPose();
                m_SkeletonGraphic.AnimationState.ClearTracks();
                m_SkeletonGraphic.AnimationState.SetAnimation(0, "1", false);

                break;
            case ToolsType.CLEAN:
                m_SkeletonGraphic.transform.position = toolsUse2.position;
                m_SkeletonGraphic.Skeleton.SetToSetupPose();
                m_SkeletonGraphic.AnimationState.ClearTracks();
                m_SkeletonGraphic.AnimationState.SetAnimation(0, "2", false);
                break;
            case ToolsType.REFRESH:
                m_SkeletonGraphic.transform.position = toolsUse1.position;
                m_SkeletonGraphic.Skeleton.SetToSetupPose();
                m_SkeletonGraphic.AnimationState.ClearTracks();
                m_SkeletonGraphic.AnimationState.SetAnimation(0, "3", false);
                break;
        }
    }

    private void OnClickMagnetBtn()
    {
        // Debug.Log("OnClickMagnetBtn - 开始磁铁匹配逻辑");
        // 检查收集区域是否有泡泡
        if (collectAreaManager == null)
        {
            // Debug.LogError("CollectAreaManager 未设置！");
            return;
        }
        var occupiedSlots = GetOccupiedSlotsFromCollectArea();
        if (occupiedSlots.Count == 0)
        {
            // 暂存区没有泡泡，检查是否还有剩余泡泡可以生成
            if (bubblesRemaining <= 0)
            {
                // 没有剩余泡泡时，从场上自由球中寻找3个相同的类型
                // Debug.Log("暂存区没有泡泡，且没有剩余泡泡，从场上自由球中寻找3个相同的");
                FindAndCollectThreeMatchingFreeBubbles();
                return;
            }
            
            // 暂存区没有泡泡，生成3个相同类型的泡泡（随机选择一种类型）
            ImageEnum randomType = (ImageEnum)Random.Range(0, 19);
            // Debug.Log($"暂存区没有泡泡，生成3个相同类型的泡泡: {randomType}");
            SpawnMagnetBubblesForMatch(randomType, 3);
            return;
        }
        // 统计暂存区中各种类型的数量
        Dictionary<ImageEnum, int> typeCounts = new Dictionary<ImageEnum, int>();
        foreach (var slot in occupiedSlots)
        {
            if (slot.CurrentBubble != null)
            {
                ImageEnum bubbleType = slot.CurrentBubble.imageEnum;
                if (typeCounts.ContainsKey(bubbleType))
                {
                    typeCounts[bubbleType]++;
                }
                else
                {
                    typeCounts[bubbleType] = 1;
                }
            }
        }

        // 找到数量最多的类型
        ImageEnum targetType = ImageEnum.IMG0;
        int maxCount = 0;
        int leftmostIndex = int.MaxValue;
        foreach (var kvp in typeCounts)
        {
            if (kvp.Value > maxCount)
            {
                maxCount = kvp.Value;
                targetType = kvp.Key;
                // 找到最左边的位置
                for (int i = 0; i < occupiedSlots.Count; i++)
                {
                    if (occupiedSlots[i].CurrentBubble != null &&
                        occupiedSlots[i].CurrentBubble.imageEnum == targetType)
                    {
                        leftmostIndex = Mathf.Min(leftmostIndex, i);
                        break;
                    }
                }
            }
            else if (kvp.Value == maxCount)
            {
                // 数量相等时，选择最左边的
                for (int i = 0; i < occupiedSlots.Count; i++)
                {
                    if (occupiedSlots[i].CurrentBubble != null &&
                        occupiedSlots[i].CurrentBubble.imageEnum == kvp.Key)
                    {
                        if (i < leftmostIndex)
                        {
                            leftmostIndex = i;
                            targetType = kvp.Key;
                        }
                        break;
                    }
                }
            }
        }

        // Debug.Log($"找到目标类型: {targetType}，数量: {maxCount}");
        // 在场景中寻找自由泡泡，看是否有相同的类型
        int matchingBubblesFound = 0;
        int targetBubblesNeeded = 3 - maxCount;
        // Debug.Log($"需要找到 {targetBubblesNeeded} 个匹配的自由泡泡来凑成3个");
        // 寻找多个匹配的泡泡，直到凑成3个或没有更多匹配
        while (matchingBubblesFound < targetBubblesNeeded)
        {
            BubbleItem matchingBubble = FindMatchingFreeBubble(targetType);
            if (matchingBubble != null)
            {
                // Debug.Log($"找到第 {matchingBubblesFound + 1} 个匹配的自由泡泡 {targetType}，触发点击");
                // 触发该泡泡的点击
                OnClickBBItem(matchingBubble);
                matchingBubblesFound++;
            }
            else
            {
                // Debug.Log($"没有找到更多匹配的自由泡泡，已找到 {matchingBubblesFound} 个");
                break;
            }
        }

        // 如果找到的匹配泡泡不够，生成剩余的泡泡
        if (matchingBubblesFound < targetBubblesNeeded)
        {
            int bubblesToGenerate = targetBubblesNeeded - matchingBubblesFound;
            // Debug.Log($"需要生成 {bubblesToGenerate} 个 {targetType} 类型泡泡来凑成3个");
            SpawnMagnetBubblesForMatch(targetType, bubblesToGenerate);
        }
        else
        {
            HomePanel.Instance.HideClickMask();
            // Debug.Log($"已找到足够的匹配泡泡，不需要生成新的");
        }
    }
    /// <summary>
    /// 在场景中寻找指定类型的自由泡泡
    /// </summary>
    private BubbleItem FindMatchingFreeBubble(ImageEnum targetType)
    {
        foreach (var bubble in m_BubbleItems)
        {
            if (bubble != null && bubble.imageEnum == targetType)
            {
                return bubble;
            }
        }
        return null;
    }

    /// <summary>
    /// 从场上自由球中寻找并收集3个相同类型的泡泡
    /// </summary>
    private void FindAndCollectThreeMatchingFreeBubbles()
    {
        // Debug.Log("=== 开始从场上自由球中寻找3个相同的类型 ===");
        
        // 统计场上所有自由球的类型分布
        Dictionary<ImageEnum, List<BubbleItem>> typeBubbles = new Dictionary<ImageEnum, List<BubbleItem>>();
        
        foreach (var bubble in m_BubbleItems)
        {
            if (bubble != null)
            {
                ImageEnum bubbleType = bubble.imageEnum;
                if (!typeBubbles.ContainsKey(bubbleType))
                {
                    typeBubbles[bubbleType] = new List<BubbleItem>();
                }
                typeBubbles[bubbleType].Add(bubble);
            }
        }
        
        // 找到数量大于等于3的类型
        ImageEnum targetType = ImageEnum.IMG0;
        int maxCount = 0;
        
        foreach (var kvp in typeBubbles)
        {
            if (kvp.Value.Count >= 3 && kvp.Value.Count > maxCount)
            {
                maxCount = kvp.Value.Count;
                targetType = kvp.Key;
            }
        }
        
        if (maxCount >= 3)
        {
            // Debug.Log($"找到类型 {targetType}，有 {maxCount} 个，开始收集前3个");
            
            // 收集前3个相同类型的泡泡
            var bubblesToCollect = typeBubbles[targetType].Take(3).ToList();
            
            foreach (var bubble in bubblesToCollect)
            {
                // Debug.Log($"收集自由球: {bubble.imageEnum}");
                OnClickBBItem(bubble);
            }
            
            // Debug.Log($"成功收集了 {bubblesToCollect.Count} 个 {targetType} 类型的泡泡");
        }
        else
        {
            // Debug.Log("场上没有3个相同类型的自由球，磁铁按钮无法使用");
        }
        
        HomePanel.Instance.HideClickMask();
    }

    /// <summary>
    /// 为磁铁按钮生成指定数量的泡泡
    /// </summary>
    private void SpawnMagnetBubblesForMatch(ImageEnum? targetType, int count)
    {
        if (count <= 0) return;

        // 检查剩余泡泡数量
        if (bubblesRemaining <= 0)
        {
            // Debug.LogWarning("没有剩余泡泡可以生成，磁铁按钮无法使用");
            HomePanel.Instance.HideClickMask();
            return;
        }
        
        if (bubblesRemaining < count)
        {
            // Debug.LogWarning($"剩余泡泡数量不足！需要{count}个，剩余{bubblesRemaining}个");
            count = bubblesRemaining;
        }

        for (int i = 0; i < count; i++)
        {
            SpawnMagnetBubble(targetType);
        }

        // Debug.Log($"磁铁按钮生成了 {count} 个泡泡，剩余: {bubblesRemaining}");
    }


    /// <summary>
    /// 从预生成序列中删除指定类型的泡泡
    /// </summary>
    /// <param name="targetType">要删除的泡泡类型</param>
    /// <returns>是否成功删除</returns>
    private bool RemoveBubbleFromSequence(ImageEnum targetType)
    {
        // 从当前索引开始查找
        for (int i = levelBubbleIndex; i < levelBubbleSequence.Count; i++)
        {
            if (levelBubbleSequence[i] == targetType)
            {
                levelBubbleSequence.RemoveAt(i);
                // Debug.Log($"从序列中删除泡泡: {targetType}，位置: {i}，剩余序列长度: {levelBubbleSequence.Count}");
                return true; // 成功删除
            }
        }
        
        // Debug.LogWarning($"序列中找不到{targetType}类型的泡泡！当前索引: {levelBubbleIndex}，序列长度: {levelBubbleSequence.Count}");
        return false; // 没找到对应类型
    }

    /// <summary>
    /// 生成单个磁铁泡泡
    /// </summary>
    private void SpawnMagnetBubble(ImageEnum? targetType)
    {
        if (bubblesRemaining <= 0) return;

        // 确定泡泡类型
        ImageEnum enumValue;
        if (targetType.HasValue)
        {
            enumValue = targetType.Value;
        }
        else
        {
            // 如果没有指定类型，随机生成
            enumValue = (ImageEnum)Random.Range(0, 19);
        }

        // 🎯 关键修复：从预生成序列中删除对应类型的泡泡
        // 磁铁泡泡虽然是"虚拟生成"的，但会消耗序列中的泡泡
        bool removed = RemoveBubbleFromSequence(enumValue);
        if (!removed)
        {
            Debug.LogWarning($"序列中找不到{enumValue}类型的泡泡！磁铁道具无法使用");
            HomePanel.Instance.HideClickMask();
            return;
        }

        // 🎯 修改：不再使用对象池，直接实例化
        GameObject item = Instantiate(ballObject.gameObject);
        int index = Random.Range(0, BallInsTrans.Count);

        // 获取基础位置并添加随机偏移
        Vector3 basePosition = BallInsTrans[index].position;
        Vector3 randomOffset = new Vector3(
            Random.Range(-positionOffset.x, positionOffset.x) / 100f,
            Random.Range(-positionOffset.y, positionOffset.y) / 100f,
            0f
        );
        item.transform.SetParent(BallPoolParent.transform);
        item.transform.position = basePosition + randomOffset;
        item.transform.rotation = Quaternion.identity;
        item.transform.localScale = Vector3.one;

        // 设置泡泡图片和类型
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("block_");
        stringBuilder.Append(((int)enumValue).ToString());
        string imageName = stringBuilder.ToString();

        BubbleItem bubbleItem = item.GetComponent<BubbleItem>();
        bubbleItem.SetImage(GetBallSprite(imageName), enumValue);

        // 关闭背景图片
        bubbleItem.DisableImageBg();

        // 磁铁泡泡不需要点击事件，直接设置为可收集状态
        // 但保留OnclickItem以防万一
        bubbleItem.OnclickItem = (Item) =>
        {
            // Debug.Log("磁铁泡泡被点击（理论上不应该发生）");
            OnClickBBItem(Item);
        };

        // 直接触发收集逻辑，模拟点击效果
        // Debug.Log($"磁铁泡泡 {enumValue} 生成完成，直接触发收集");
        OnClickBBItem(bubbleItem);

        bubblesRemaining--;

        HomePanel.Instance.HideClickMask();
    }

    /// <summary>
    /// 清理按钮点击事件 - 从收集区域左侧移动泡泡到FullStayArea，考虑清理区容量限制
    /// </summary>
    private void OnClickCleanBtn()
    {
        // Debug.Log("OnClickCleanBtn - 开始清理收集区域");
        // 检查是否有可清理的泡泡
        if (collectAreaManager == null)
        {
            // Debug.LogError("CollectAreaManager 未设置！");
            HomePanel.Instance.HideClickMask();
            return;
        }

        // 获取收集区域中已占用的槽位
        var occupiedSlots = GetOccupiedSlotsFromCollectArea();
        if (occupiedSlots.Count == 0)
        {
            // Debug.Log("收集区域中没有可清理的泡泡");
            HomePanel.Instance.HideClickMask();
            return;
        }

        // 🎯 修复：检查清理区的剩余容量
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea == null)
        {
            // Debug.LogError("未找到FullStayArea组件！");
            HomePanel.Instance.HideClickMask();
            return;
        }

        // 检查清理区是否有剩余空间
        int availableSpace = fullStayArea.GetAvailableStorageSpace;
        if (availableSpace <= 0)
        {
            // Debug.LogWarning("清理区存储空间已满，无法清理更多泡泡！");
            HomePanel.Instance.HideClickMask();
            return;
        }

        // 🎯 关键修复：根据清理区剩余容量和暂存区泡泡数量，计算实际可移动的泡泡数量
        // 最多移动3个，但不能超过清理区的剩余容量
        int bubblesToMove = Mathf.Min(3, occupiedSlots.Count, availableSpace);
        
        if (bubblesToMove <= 0)
        {
            // Debug.Log("没有泡泡可以清理（暂存区为空或清理区已满）");
            HomePanel.Instance.HideClickMask();
            return;
        }

        var bubblesToClean = new List<BubbleItem>();

        // 从左往右获取泡泡（根据实际可移动数量）
        for (int i = 0; i < occupiedSlots.Count && bubblesToClean.Count < bubblesToMove; i++)
        {
            if (occupiedSlots[i] != null && occupiedSlots[i].CurrentBubble != null)
            {
                bubblesToClean.Add(occupiedSlots[i].CurrentBubble);
            }
        }

        // Debug.Log($"准备清理 {bubblesToClean.Count} 个泡泡（清理区剩余容量：{availableSpace}）");

        // 开始清理动画
        StartCoroutine(CleanBubblesToFullStayArea(bubblesToClean));
    }

    /// <summary>
    /// 从收集区域获取已占用的槽位
    /// </summary>
    private List<BubbleSlotBehavior> GetOccupiedSlotsFromCollectArea()
    {
        if (collectAreaManager != null)
        {
            return collectAreaManager.GetOccupiedSlotsPublic();
        }

        // Debug.LogError("CollectAreaManager 为空！");
        return new List<BubbleSlotBehavior>();
    }

    /// <summary>
    /// 清理泡泡到FullStayArea的协程
    /// </summary>
    private IEnumerator CleanBubblesToFullStayArea(List<BubbleItem> bubblesToClean)
    {
        if (bubblesToClean.Count == 0)
        {
            // Debug.Log("没有泡泡需要清理");
            yield break;
        }

        // 查找FullStayArea组件
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea == null)
        {
            // Debug.LogError("未找到FullStayArea组件！");
            yield break;
        }

        // 🎯 修复：再次检查FullStayArea的存储空间（双重保险）
        int availableSpace = fullStayArea.GetAvailableStorageSpace;
        if (availableSpace <= 0)
        {
            // Debug.LogWarning("FullStayArea存储空间已满，无法清理更多泡泡！");
            yield break;
        }

        // 🎯 修复：确保要清理的泡泡数量不超过可用空间
        int actualBubblesToClean = Mathf.Min(bubblesToClean.Count, availableSpace);
        if (actualBubblesToClean < bubblesToClean.Count)
        {
            // Debug.LogWarning($"清理区容量不足！只能清理 {actualBubblesToClean} 个泡泡，跳过 {bubblesToClean.Count - actualBubblesToClean} 个");
            // 调整要清理的泡泡列表
            bubblesToClean = bubblesToClean.Take(actualBubblesToClean).ToList();
        }

        // Debug.Log($"开始将 {bubblesToClean.Count} 个泡泡同时移动到FullStayArea（可用空间：{availableSpace}）");

        // 同时从收集区域移除所有泡泡
        foreach (var bubble in bubblesToClean)
        {
            if (bubble != null)
            {
                RemoveBubbleFromCollectArea(bubble);
            }
        }

        // 同时添加所有泡泡到FullStayArea（使用批量添加方法）
        int storedCount = fullStayArea.AddBubbles(bubblesToClean);

        // 等待清理动画完成
        yield return new WaitForSeconds(0.5f);

        // 🎯 关键修复：清理完成后触发收集区的补位逻辑
        if (collectAreaManager != null)
        {
            // 触发收集区的补位检测
            collectAreaManager.TriggerGapFillAfterClean();
        }

        // Debug.Log($"清理完成，已将 {storedCount} 个泡泡同时移动到FullStayArea，并触发补位");
        HomePanel.Instance.HideClickMask();
    }

    /// <summary>
    /// 从收集区域移除泡泡
    /// </summary>
    private void RemoveBubbleFromCollectArea(BubbleItem bubble)
    {
        if (collectAreaManager != null)
        {
            bool removed = collectAreaManager.RemoveBubbleFromSlot(bubble);
            if (!removed)
            {
                // Debug.LogWarning($"无法从收集区域移除泡泡 {bubble.imageEnum}");
            }
        }
        else
        {
            // Debug.LogError("CollectAreaManager 为空！");
        }
    }
    /// <summary>
    /// 刷新按钮点击事件 - 智能刷新逻辑
    /// </summary>
    private void OnClickRefreshBtn()
    {
        // Debug.Log("OnClickRefreshBtn - 开始智能刷新");
        
        // 检查是否有可刷新的泡泡
        if (m_BubbleItems.Count == 0)
        {
            // Debug.Log("没有可刷新的泡泡");
            return;
        }

        // 开始刷新流程
        StartCoroutine(ExecuteSmartRefresh());
    }

    /// <summary>
    /// 执行智能刷新的协程
    /// </summary>
    private IEnumerator ExecuteSmartRefresh()
    {
        // Debug.Log("=== 开始智能刷新 ===");
        
        // 1. 判断刷新策略
        if (NeedToDropMoreBubbles())
        {
            // Debug.Log("情况1：需要掉落新球，执行智能生成策略");
            yield return StartCoroutine(ExecuteRefreshWithNewBubbles());
        }
        else
        {
            // Debug.Log("情况2：不需要掉落新球，执行换皮刷新策略");
            yield return StartCoroutine(ExecuteRefreshWithTypeSwap());
        }
        
        // Debug.Log("=== 智能刷新完成 ===");
        HomePanel.Instance.HideClickMask();
    }

    /// <summary>
    /// 判断是否需要掉落新球
    /// </summary>
    private bool NeedToDropMoreBubbles()
    {
        // 如果还有规划泡泡没掉落，肯定需要
        if (bubblesRemaining > 0)
        {
            // Debug.Log($"还有 {bubblesRemaining} 个规划泡泡需要掉落");
            return true;
        }
        
        // 如果规划泡泡掉完了，分析场上状态
        if (bubblesRemaining <= 0)
        {
            // 分析暂存区和清理区的类型分布
            var storageAndCleanCounts = AnalyzeStorageAndCleanAreas();
            
            // 检查是否有无法消除的组合
            foreach (var kvp in storageAndCleanCounts)
            {
                if (kvp.Value % 3 != 0)
                {
                    // Debug.Log($"类型 {kvp.Key} 数量 {kvp.Value} 不是3的倍数，需要掉落新球");
                    return true;
                }
            }
            
            // Debug.Log("场上所有类型都是3的倍数，不需要掉落新球");
            return false;
        }
        
        return false;
    }

    /// <summary>
    /// 情况1：需要掉落新球时的智能生成策略
    /// </summary>
    private IEnumerator ExecuteRefreshWithNewBubbles()
    {
        // Debug.Log("=== 执行智能生成策略 ===");
        
        // 1. 分析暂存区和清理区的类型分布
        var storageAndCleanCounts = AnalyzeStorageAndCleanAreas();
        // Debug.Log($"暂存区+清理区分析：{string.Join(", ", storageAndCleanCounts.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
        
        // 2. 基于暂存区+清理区的需求，智能生成泡泡类型
        var smartTypes = GenerateSmartBubbleTypes(storageAndCleanCounts);
        // Debug.Log($"智能生成的类型分布：{string.Join(", ", smartTypes)}");
        
        // 3. 执行刷新动画
        yield return StartCoroutine(ExecuteRefreshAnimation(smartTypes));
        
        // 4. 刷新完成后重新计算掉落序列
        OnRefreshCompleted();
    }

    /// <summary>
    /// 情况2：不需要掉落新球时的换皮刷新策略
    /// </summary>
    private IEnumerator ExecuteRefreshWithTypeSwap()
    {
        // Debug.Log("=== 执行换皮刷新策略 ===");
        
        // 1. 获取关卡允许的类型
        var allowedTypes = GetLevelAllowedTypes();
        // Debug.Log($"关卡允许的类型：{string.Join(", ", allowedTypes)}");
        
        // 2. 智能分配类型，确保每种类型都是3的倍数
        var newTypes = GenerateBalancedTypesForSwap(m_BubbleItems.Count, allowedTypes);
        // Debug.Log($"换皮后的类型分布：{string.Join(", ", newTypes)}");
        
        // 3. 执行刷新动画
        yield return StartCoroutine(ExecuteRefreshAnimation(newTypes));
        
        // Debug.Log("换皮刷新完成，场上泡泡类型已重新分配");
    }

    /// <summary>
    /// 分析暂存区和清理区的泡泡分布
    /// </summary>
    private Dictionary<ImageEnum, int> AnalyzeStorageAndCleanAreas()
    {
        Dictionary<ImageEnum, int> typeCounts = new Dictionary<ImageEnum, int>();
        
        // 统计暂存区
        if (collectAreaManager != null)
        {
            var occupiedSlots = collectAreaManager.GetOccupiedSlotsPublic();
            foreach (var slot in occupiedSlots)
            {
                if (slot != null && slot.CurrentBubble != null)
                {
                    ImageEnum type = slot.CurrentBubble.imageEnum;
                    if (typeCounts.ContainsKey(type))
                        typeCounts[type]++;
                    else
                        typeCounts[type] = 1;
                }
            }
        }
        
        // 统计清理区
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            var storedBubbles = fullStayArea.GetStoredBubbles();
            foreach (var bubble in storedBubbles)
            {
                if (bubble != null)
                {
                    ImageEnum type = bubble.imageEnum;
                    if (typeCounts.ContainsKey(type))
                        typeCounts[type]++;
                    else
                        typeCounts[type] = 1;
                }
            }
        }
        
        return typeCounts;
    }

    /// <summary>
    /// 基于暂存区+清理区需求，智能生成泡泡类型
    /// </summary>
    private List<ImageEnum> GenerateSmartBubbleTypes(Dictionary<ImageEnum, int> storageAndCleanCounts)
    {
        // Debug.Log("=== 开始智能生成泡泡类型 ===");
        
        List<ImageEnum> result = new List<ImageEnum>();
        var allowedTypes = GetLevelAllowedTypes();
        
        // Debug.Log($"关卡允许的类型：{string.Join(", ", allowedTypes)}");
        
        // 1. 优先满足暂存区+清理区的需求（确保三消）
        foreach (var kvp in storageAndCleanCounts)
        {
            ImageEnum bubbleType = kvp.Key;
            int currentCount = kvp.Value;
            
            // 计算需要补充的数量，确保是3的倍数
            int remainder = currentCount % 3;
            if (remainder > 0)
            {
                int needToAdd = 3 - remainder;
                
                // 添加需要的泡泡类型
                for (int i = 0; i < needToAdd; i++)
                {
                    result.Add(bubbleType);
                }
                // Debug.Log($"类型 {bubbleType} 补充 {needToAdd} 个，形成三消");
            }
            else
            {
                // Debug.Log($"类型 {bubbleType} 已经是3的倍数({currentCount}个)，无需补充");
            }
        }
        
        // 2. 如果还有剩余位置，用关卡允许的类型智能填充
        int remainingSlots = m_BubbleItems.Count - result.Count;
        if (remainingSlots > 0)
        {
            // Debug.Log($"还有 {remainingSlots} 个位置需要填充");
            
            // 智能填充，确保每种类型都是3的倍数
            var fillTypes = GenerateSmartFillTypes(remainingSlots, allowedTypes);
            result.AddRange(fillTypes);
            
            // Debug.Log($"填充类型：{string.Join(", ", fillTypes)}");
        }
        
        // Debug.Log($"最终生成的类型分布：{string.Join(", ", result)}");
        // Debug.Log($"总数：{result.Count}，场上泡泡数：{m_BubbleItems.Count}");
        
        return result;
    }

    /// <summary>
    /// 智能填充类型生成（确保每种类型都是3的倍数）
    /// </summary>
    private List<ImageEnum> GenerateSmartFillTypes(int count, List<ImageEnum> allowedTypes)
    {
        // Debug.Log($"开始智能填充 {count} 个位置");
        
        List<ImageEnum> fillTypes = new List<ImageEnum>();
        
        // 确保数量是3的倍数
        int adjustedCount = (count / 3) * 3;
        if (adjustedCount < count)
        {
            adjustedCount += 3;
        }
        
        // 智能分配，确保每种类型都是3的倍数
        int typesNeeded = adjustedCount / 3;
        int typesPerType = typesNeeded / allowedTypes.Count;
        int remainder = typesNeeded % allowedTypes.Count;
        
        for (int i = 0; i < allowedTypes.Count; i++)
        {
            int typeCount = typesPerType;
            if (i < remainder) typeCount++;
            
            // 每种类型生成3的倍数
            for (int j = 0; j < typeCount * 3; j++)
            {
                fillTypes.Add(allowedTypes[i]);
            }
        }
        
        // 调整到实际需要的数量
        while (fillTypes.Count > count)
        {
            fillTypes.RemoveAt(fillTypes.Count - 1);
        }
        
        // Debug.Log($"智能填充完成，生成了 {fillTypes.Count} 个类型");
        return fillTypes;
    }

    /// <summary>
    /// 为换皮刷新生成平衡的类型分布
    /// </summary>
    private List<ImageEnum> GenerateBalancedTypesForSwap(int count, List<ImageEnum> allowedTypes)
    {
        // Debug.Log($"开始为换皮刷新生成 {count} 个平衡类型");
        
        List<ImageEnum> result = new List<ImageEnum>();
        
        // 确保数量是3的倍数
        int adjustedCount = (count / 3) * 3;
        if (adjustedCount < count)
        {
            adjustedCount += 3;
        }
        
        // 智能分配，确保每种类型都是3的倍数
        int typesNeeded = adjustedCount / 3;
        int typesPerType = typesNeeded / allowedTypes.Count;
        int remainder = typesNeeded % allowedTypes.Count;
        
        for (int i = 0; i < allowedTypes.Count; i++)
        {
            int typeCount = typesPerType;
            if (i < remainder) typeCount++;
            
            // 每种类型生成3的倍数
            for (int j = 0; j < typeCount * 3; j++)
            {
                result.Add(allowedTypes[i]);
            }
        }
        
        // 调整到实际需要的数量
        while (result.Count > count)
        {
            result.RemoveAt(result.Count - 1);
        }
        
        // 打乱顺序，增加随机性
        for (int i = 0; i < result.Count; i++)
        {
            int randomIndex = Random.Range(0, result.Count);
            ImageEnum temp = result[i];
            result[i] = result[randomIndex];
            result[randomIndex] = temp;
        }
        
        // Debug.Log($"换皮类型生成完成，生成了 {result.Count} 个类型");
        return result;
    }

    /// <summary>
    /// 分析刷新后的场上状态（自由球 + 暂存区 + 清理区）
    /// </summary>
    private Dictionary<ImageEnum, int> AnalyzeCurrentFieldState()
    {
        // Debug.Log("=== 分析刷新后的场上状态 ===");
        
        Dictionary<ImageEnum, int> fieldState = new Dictionary<ImageEnum, int>();
        
        // 1. 统计自由球（刷新后的状态）
        foreach (var bubble in m_BubbleItems)
        {
            if (bubble != null)
            {
                ImageEnum type = bubble.imageEnum;
                if (fieldState.ContainsKey(type))
                    fieldState[type]++;
                else
                    fieldState[type] = 1;
            }
        }
        
        // 2. 统计暂存区
        if (collectAreaManager != null)
        {
            var occupiedSlots = collectAreaManager.GetOccupiedSlotsPublic();
            foreach (var slot in occupiedSlots)
            {
                if (slot != null && slot.CurrentBubble != null)
                {
                    ImageEnum type = slot.CurrentBubble.imageEnum;
                    if (fieldState.ContainsKey(type))
                        fieldState[type]++;
                    else
                        fieldState[type] = 1;
                }
            }
        }
        
        // 3. 统计清理区
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            var storedBubbles = fullStayArea.GetStoredBubbles();
            foreach (var bubble in storedBubbles)
            {
                if (bubble != null)
                {
                    ImageEnum type = bubble.imageEnum;
                    if (fieldState.ContainsKey(type))
                        fieldState[type]++;
                    else
                        fieldState[type] = 1;
                }
            }
        }
        
        // Debug.Log($"场上状态分析完成：{string.Join(", ", fieldState.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
        return fieldState;
    }

    /// <summary>
    /// 基于场上状态智能生成掉落序列
    /// </summary>
    private void GenerateSmartBubbleSequence(Dictionary<ImageEnum, int> fieldState)
    {
        // Debug.Log("=== 开始基于场上状态智能生成掉落序列 ===");
        
        levelBubbleSequence.Clear();
        
        // 1. 计算还需要生成多少个泡泡
        int remainingToGenerate = bubblesRemaining;
        // Debug.Log($"还需要生成 {remainingToGenerate} 个泡泡");
        
        // 2. 分析场上每种类型的余数
        Dictionary<ImageEnum, int> typeRemainders = new Dictionary<ImageEnum, int>();
        foreach (var kvp in fieldState)
        {
            int remainder = kvp.Value % 3;
            if (remainder > 0)
            {
                typeRemainders[kvp.Key] = remainder;
                // Debug.Log($"类型 {kvp.Key} 当前有 {kvp.Value} 个，余数 {remainder}");
            }
        }
        
        // 3. 优先生成能形成三消的类型
        var allowedTypes = GetLevelAllowedTypes();
        int generatedCount = 0;
        
        foreach (var kvp in typeRemainders)
        {
            if (generatedCount >= remainingToGenerate) break;
            
            ImageEnum type = kvp.Key;
            int remainder = kvp.Value;
            int needToAdd = 3 - remainder;
            
            // 确保不超过剩余数量
            int actualAdd = Mathf.Min(needToAdd, remainingToGenerate - generatedCount);
            
            for (int i = 0; i < actualAdd; i++)
            {
                levelBubbleSequence.Add(type);
                generatedCount++;
            }
            
            // Debug.Log($"为类型 {type} 生成 {actualAdd} 个，形成三消");
        }
        
        // 4. 如果还有剩余位置，用关卡允许的类型智能填充
        int remainingSlots = remainingToGenerate - generatedCount;
        if (remainingSlots > 0)
        {
            // Debug.Log($"还有 {remainingSlots} 个位置需要填充");
            
            // 智能填充，确保每种类型都是3的倍数
            var fillTypes = GenerateSmartFillTypes(remainingSlots, allowedTypes);
            levelBubbleSequence.AddRange(fillTypes);
            
            // Debug.Log($"填充类型：{string.Join(", ", fillTypes)}");
        }
        
        // 5. 打乱序列，增加随机性
        for (int i = 0; i < levelBubbleSequence.Count; i++)
        {
            int randomIndex = Random.Range(0, levelBubbleSequence.Count);
            ImageEnum temp = levelBubbleSequence[i];
            levelBubbleSequence[i] = levelBubbleSequence[randomIndex];
            levelBubbleSequence[randomIndex] = temp;
        }
        
        // Debug.Log($"智能掉落序列生成完成：总球数{levelBubbleSequence.Count}");
        // Debug.Log($"序列内容：{string.Join(", ", levelBubbleSequence)}");
    }

    /// <summary>
    /// 执行刷新动画
    /// </summary>
    private IEnumerator ExecuteRefreshAnimation(List<ImageEnum> refreshTypes)
    {
        // Debug.Log("开始执行刷新动画");

        // 1. 所有泡泡同时变小
        yield return StartCoroutine(ScaleDownAllBubbles());

        // 2. 所有泡泡同时换图
        for (int i = 0; i < m_BubbleItems.Count && i < refreshTypes.Count; i++)
        {
            if (m_BubbleItems[i] != null)
            {
                string newImageName = $"block_{((int)refreshTypes[i]).ToString()}";
                Sprite newSprite = GetBallSprite(newImageName);
                m_BubbleItems[i].SetImage(newSprite, refreshTypes[i]);
            }
        }

        // 3. 所有泡泡同时变大
        yield return StartCoroutine(ScaleUpAllBubbles());
    }

    /// <summary>
    /// 所有泡泡同时变小的协程
    /// </summary>
    private IEnumerator ScaleDownAllBubbles()
    {
        // Debug.Log("所有泡泡同时变小");

        if (m_BubbleItems.Count <= 0)
        {
            yield break;
        }

        Sequence scaleDownSequence = DOTween.Sequence();

        for (int i = 0; i < m_BubbleItems.Count; i++)
        {
            if (m_BubbleItems[i] != null)
            {
                // 变小时禁用物理模拟
                if (m_BubbleItems[i].m_Rigidbody != null)
                {
                    m_BubbleItems[i].m_Rigidbody.simulated = false;
                }

                scaleDownSequence.Join(m_BubbleItems[i].transform.DOScale(0.3f, 0.3f).SetEase(Ease.InBack));
            }
        }

        yield return scaleDownSequence.WaitForCompletion();
        // Debug.Log("所有泡泡变小完成");
    }

    /// <summary>
    /// 所有泡泡同时变大的协程
    /// </summary>
    private IEnumerator ScaleUpAllBubbles()
    {
        // Debug.Log("所有泡泡同时变大");

        if (m_BubbleItems.Count <= 0)
        {
            yield break;
        }

        Sequence scaleUpSequence = DOTween.Sequence();

        for (int i = 0; i < m_BubbleItems.Count; i++)
        {
            if (m_BubbleItems[i] != null)
            {
                scaleUpSequence.Join(m_BubbleItems[i].transform.DOScale(1f, 0.4f).SetEase(Ease.OutBack));
            }
        }

        yield return scaleUpSequence.WaitForCompletion();

        // 变大完成后启用物理模拟
        for (int i = 0; i < m_BubbleItems.Count; i++)
        {
            if (m_BubbleItems[i] != null && m_BubbleItems[i].m_Rigidbody != null)
            {
                m_BubbleItems[i].m_Rigidbody.simulated = true;
            }
        }

        // Debug.Log("所有泡泡变大完成");
    }

    /// <summary>
    /// 刷新完成后的处理
    /// </summary>
    private void OnRefreshCompleted()
    {
        // Debug.Log("=== 刷新完成，智能重新计算掉落序列 ===");
        
        // 1. 分析刷新后的场上状态
        var currentFieldState = AnalyzeCurrentFieldState();
        // Debug.Log($"刷新后场上状态：{string.Join(", ", currentFieldState.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}");
        
        // 2. 基于场上状态智能生成掉落序列
        GenerateSmartBubbleSequence(currentFieldState);
        
        // 3. 重置掉落索引
        levelBubbleIndex = 0;
        
        // 4. 验证新的序列
        ValidateBubbleSequence();
        
        // Debug.Log($"刷新后掉落序列：总球数{levelBubbleSequence.Count}，当前索引{levelBubbleIndex}");
        // Debug.Log("后续动态掉落将使用基于场上状态的智能序列，确保可消除性");
    }
    
    /// <summary>
    /// 检查保底机制 - Level模式下确保所有泡泡都能完美消除
    /// </summary>
    private void CheckGuaranteeMechanism()
    {
        // 只在Level模式下且未触发过保底时执行
        GameType gameType = GameManager.Instance.GetGameType();
        if (gameType != GameType.Level || hasTriggeredGuarantee)
        {
            return;
        }
        
        // 检查是否所有球都已掉落完成
        if (bubblesRemaining > 0)
        {
            return;
        }
        
        // 分析场上所有泡泡的可消除性
        var bubbleAnalysis = AnalyzeAllBubblesForElimination();
        
        // 检查是否有无法完美消除的泡泡
        if (HasUneliminatableBubbles(bubbleAnalysis))
        {
            // 触发保底机制
            TriggerGuaranteeMechanism(bubbleAnalysis);
        }
    }
    
    /// <summary>
    /// 分析所有区域的泡泡，统计每种类型的数量
    /// </summary>
    private Dictionary<ImageEnum, int> AnalyzeAllBubblesForElimination()
    {
        Dictionary<ImageEnum, int> typeCounts = new Dictionary<ImageEnum, int>();
        
        // 1. 统计场上自由泡泡
        foreach (var bubble in m_BubbleItems)
        {
            if (bubble != null)
            {
                ImageEnum type = bubble.imageEnum;
                if (typeCounts.ContainsKey(type))
                    typeCounts[type]++;
                else
                    typeCounts[type] = 1;
            }
        }
        
        // 2. 统计暂存区泡泡
        if (collectAreaManager != null)
        {
            var occupiedSlots = collectAreaManager.GetOccupiedSlotsPublic();
            foreach (var slot in occupiedSlots)
            {
                if (slot != null && slot.CurrentBubble != null)
                {
                    ImageEnum type = slot.CurrentBubble.imageEnum;
                    if (typeCounts.ContainsKey(type))
                        typeCounts[type]++;
                    else
                        typeCounts[type] = 1;
                }
            }
        }
        
        // 3. 统计清理区泡泡
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            var storedBubbles = fullStayArea.GetStoredBubbles();
            foreach (var bubble in storedBubbles)
            {
                if (bubble != null)
                {
                    ImageEnum type = bubble.imageEnum;
                    if (typeCounts.ContainsKey(type))
                        typeCounts[type]++;
                    else
                        typeCounts[type] = 1;
                }
            }
        }
        
        return typeCounts;
    }
    
    /// <summary>
    /// 检查是否有无法完美消除的泡泡
    /// </summary>
    private bool HasUneliminatableBubbles(Dictionary<ImageEnum, int> typeCounts)
    {
        foreach (var kvp in typeCounts)
        {
            if (kvp.Value % 3 != 0)
            {
                return true; // 有无法完美消除的类型
            }
        }
        return false;
    }
    
    /// <summary>
    /// 触发保底机制 - 生成补充泡泡确保完美消除
    /// </summary>
    private void TriggerGuaranteeMechanism(Dictionary<ImageEnum, int> typeCounts)
    {
        hasTriggeredGuarantee = true;
        
        // 计算需要补充的泡泡
        List<ImageEnum> bubblesToSpawn = CalculateGuaranteeBubbles(typeCounts);
        
        if (bubblesToSpawn.Count > 0)
        {
            // 生成补充泡泡
            StartCoroutine(SpawnGuaranteeBubbles(bubblesToSpawn));
        }
    }
    
    /// <summary>
    /// 计算保底需要补充的泡泡
    /// </summary>
    private List<ImageEnum> CalculateGuaranteeBubbles(Dictionary<ImageEnum, int> typeCounts)
    {
        List<ImageEnum> bubblesToSpawn = new List<ImageEnum>();
        
        foreach (var kvp in typeCounts)
        {
            ImageEnum type = kvp.Key;
            int count = kvp.Value;
            int remainder = count % 3;
            
            if (remainder > 0)
            {
                // 需要补充到下一个3的倍数
                int needToAdd = 3 - remainder;
                for (int i = 0; i < needToAdd; i++)
                {
                    bubblesToSpawn.Add(type);
                }
            }
        }
        
        return bubblesToSpawn;
    }
    
    /// <summary>
    /// 生成保底泡泡的协程
    /// </summary>
    private IEnumerator SpawnGuaranteeBubbles(List<ImageEnum> bubblesToSpawn)
    {
        // 更新剩余泡泡数
        bubblesRemaining = bubblesToSpawn.Count;
        
        // 逐个生成保底泡泡
        for (int i = 0; i < bubblesToSpawn.Count; i++)
        {
            SpawnSingleBubbleWithType(bubblesToSpawn[i]);
            yield return new WaitForSeconds(spawnInterval);
        }
        
        // 保底泡泡生成完成
        bubblesRemaining = 0;
    }

    /// <summary>
    /// 获取关卡允许的类型
    /// </summary>
    private List<ImageEnum> GetLevelAllowedTypes()
    {
        List<ImageEnum> allowedTypes = new List<ImageEnum>();
        
        if (levelBubbleSequence.Count > 0)
        {
            // 从关卡序列中提取类型
            HashSet<ImageEnum> usedTypes = new HashSet<ImageEnum>();
            foreach (var type in levelBubbleSequence)
            {
                usedTypes.Add(type);
            }
            allowedTypes.AddRange(usedTypes);
        }
        else
        {
            // 如果序列未生成，使用基础类型
            allowedTypes.AddRange(new[] { ImageEnum.IMG0, ImageEnum.IMG1, ImageEnum.IMG2 });
        }
        
        return allowedTypes;
    }

    private void BallPoolInit()
    {
        // 🎯 修改：不再需要初始化对象池，因为直接实例化
        // Debug.Log("对象池初始化已跳过，使用直接实例化方式");
    }

    public void GameStart()
    {
        // Debug.Log("Game Started");
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
        }

        // 根据游戏模式计算总泡泡数
        GameType gameType = GameManager.Instance.GetGameType();
        if (gameType == GameType.Level)
        {
            // Level模式：根据等级计算泡泡数量
            int currentLevel = GameManager.Instance.GetLevel();
            totalBubblesForLevel = CalculateBubblesForLevel(currentLevel);
        }
        else
        {
            challengeFailCount = 0;
            // Challenge模式：固定7200个泡泡
            totalBubblesForLevel = challengeModeTotal;
        }

        bubblesRemaining = totalBubblesForLevel;
        isObstacleMode = false;

        // 重置Level模式的泡泡序列
        levelBubbleSequence.Clear();
        levelBubbleIndex = 0;
        
        // 重置保底机制状态
        hasTriggeredGuarantee = false;
        hasTriggeredLevelGuarantee = false;
        lastLevelGuaranteeCheckTime = 0f;

        // 🎯 新增：重置进度条
        ResetProgressBar();

        // Debug.Log($"游戏模式: {gameType}, 当前等级: {GameManager.Instance.GetLevel()}, 总泡泡数: {totalBubblesForLevel}");

        // 重置收集区域状态
        collectAreaManager.ClearAreaForNewGame();

        // 开始协程生成初始泡泡球
        spawnCoroutine = StartCoroutine(SpawnInitialBubbles());
    }

    // 计算等级对应的泡泡数量（确保是3的倍数）
    private int CalculateBubblesForLevel(int level)
    {
        // 基础公式：每级递增，但保证是3的倍数
        // 第1关：30个，第2关：42个，第3关：54个...
        int baseCount = 30 + (level - 1) * 12; // 每级增加12个（3的倍数）

        // 确保结果是3的倍数
        if (baseCount % 3 != 0)
        {
            baseCount = ((baseCount / 3) + 1) * 3;
        }

        return baseCount;
    }

    /// <summary>
    /// 生成初始泡泡球
    /// </summary>
    private IEnumerator SpawnInitialBubbles()
    {
        int initialCount = Mathf.Min(initialBubbleCount, bubblesRemaining);

        for (int i = 0; i < initialCount; i++)
        {
            SpawnSingleBubble();
            yield return new WaitForSeconds(spawnInterval);
        }

        spawnCoroutine = null;
        // Debug.Log($"初始泡泡生成完成，剩余: {bubblesRemaining}");
    }

    /// <summary>
    /// 生成单个泡泡
    /// </summary>
    private void SpawnSingleBubble()
    {
        if (bubblesRemaining <= 0) return;

        // 🎯 修改：不再使用对象池，直接实例化
        GameObject item = Instantiate(ballObject.gameObject);
        int index = Random.Range(0, BallInsTrans.Count);

        // 获取基础位置并添加随机偏移
        Vector3 basePosition = BallInsTrans[index].position;
        Vector3 randomOffset = new Vector3(
            Random.Range(-positionOffset.x, positionOffset.x) / 100f,
            Random.Range(-positionOffset.y, positionOffset.y) / 100f,
            0f
        );
        item.transform.SetParent(BallPoolParent.transform);
        item.transform.position = basePosition + randomOffset;
        item.transform.rotation = Quaternion.identity;
        item.transform.localScale = Vector3.one;
        // 根据模式生成不同的泡泡类型
        ImageEnum enumValue = GetBubbleType();
        StringBuilder stringBuilder = new StringBuilder();
        stringBuilder.Append("block_");
        stringBuilder.Append(((int)enumValue).ToString());
        string imageName = stringBuilder.ToString();

        BubbleItem bubbleItem = item.GetComponent<BubbleItem>();
        bubbleItem.SetImage(GetBallSprite(imageName), enumValue);
        bubbleItem.OnclickItem = (Item) =>
        {
            // Debug.Log("BubbleItem clicked");
            OnClickBBItem(Item);
        };

        m_BubbleItems.Add(bubbleItem);
        bubblesRemaining--;

        // 检查是否需要开启障碍模式
        CheckObstacleMode();
    }

    /// <summary>
    /// 获取泡泡类型（根据模式和障碍机制）
    /// </summary>
    private ImageEnum GetBubbleType()
    {
        GameType gameType = GameManager.Instance.GetGameType();

        if (gameType == GameType.Level)
        {
            // Level模式：生成能够完美消除的泡泡
            return GenerateLevelBubble();
        }
        else if (isObstacleMode)
        {
            // Challenge模式障碍期：生成更多对子球，减少三消可能性
            return GenerateObstacleBubble();
        }
        else
        {
            // Challenge模式正常期：智能生成，确保有足够的相同类型
            return GenerateChallengeNormalBubble();
        }
    }

    // Level模式的泡泡序列（每关生成时预计算）
    private List<ImageEnum> levelBubbleSequence = new List<ImageEnum>();
    private int levelBubbleIndex = 0;

    /// <summary>
    /// Level模式生成泡泡（确保能够完美消除）
    /// </summary>
    private ImageEnum GenerateLevelBubble()
    {
        // 如果序列为空或已用完，重新生成完美序列
        if (levelBubbleSequence.Count == 0 || levelBubbleIndex >= levelBubbleSequence.Count)
        {
            GeneratePerfectBubbleSequence();
            levelBubbleIndex = 0;
        }

        ImageEnum result = levelBubbleSequence[levelBubbleIndex];
        levelBubbleIndex++;
        return result;
    }

    /// <summary>
    /// 生成完美的泡泡序列（确保能被3整除）
    /// </summary>
    private void GeneratePerfectBubbleSequence()
    {
        levelBubbleSequence.Clear();

        int totalBubbles = totalBubblesForLevel;

        int level = GameManager.Instance.GetLevel();

        // Debug.Log($"=== 第{level}关关卡规划开始 ===");
        // Debug.Log($"关卡总球数: {totalBubbles}");

        // 1. 计算现金球（IMG0）数量：总球数的10%，确保是3的倍数
        int cashBubbleCount = Mathf.RoundToInt(totalBubbles * 0.1f);
        int cashBubbleGroups = Mathf.FloorToInt(cashBubbleCount / 3f);
        int finalCashBubbleCount = cashBubbleGroups * 3;

        // Debug.Log($"现金球计算: 总数{cashBubbleCount} → 组数{cashBubbleGroups} → 最终数量{finalCashBubbleCount}");

        // 2. 计算其他球类型数量：每过2关增加1种新类型
        int otherTypeCount = 3 + Mathf.FloorToInt((level - 1) / 2);
        otherTypeCount = Mathf.Min(otherTypeCount, 19); // 限制在ImageEnum范围内

        // Debug.Log($"其他球类型数量: {otherTypeCount}种 (IMG1-IMG{otherTypeCount})");

        // 3. 计算其他球的总数量
        int otherBubbleCount = totalBubbles - finalCashBubbleCount;
        // Debug.Log($"其他球总数量: {otherBubbleCount}");

        // 4. 确保其他球数量是3的倍数
        int adjustedOtherBubbleCount = (otherBubbleCount / 3) * 3;
        if (adjustedOtherBubbleCount < otherBubbleCount)
        {
            adjustedOtherBubbleCount += 3;
        }

        // 5. 调整现金球数量以匹配总数
        int adjustedCashBubbleCount = totalBubbles - adjustedOtherBubbleCount;
        adjustedCashBubbleCount = (adjustedCashBubbleCount / 3) * 3; // 确保是3的倍数

        // Debug.Log($"调整后: 现金球{adjustedCashBubbleCount}个，其他球{adjustedOtherBubbleCount}个，总计{adjustedCashBubbleCount + adjustedOtherBubbleCount}个");

        // 6. 生成其他球类型序列（分散放置，避免连续相同类型）
        int bubblesPerType = adjustedOtherBubbleCount / otherTypeCount;
        int remainder = adjustedOtherBubbleCount % otherTypeCount;

        // 先收集所有需要生成的泡泡类型和数量
        List<(ImageEnum type, int count)> typeCounts = new List<(ImageEnum, int)>();
        
        for (int typeIndex = 1; typeIndex <= otherTypeCount; typeIndex++)
        {
            int count = bubblesPerType;
            if (typeIndex <= remainder) count++; // 分配剩余泡泡

            // 确保每种类型都是3的倍数
            count = (count / 3) * 3;
            if (count == 0) count = 3;

            ImageEnum bubbleType = (ImageEnum)typeIndex;
            typeCounts.Add((bubbleType, count));
            // Debug.Log($"类型 {bubbleType}: {count}个");
        }

        // 🎯 真正分散放置：确保每种类型都能形成3消
        // 计算每种类型需要多少个3消组
        Dictionary<ImageEnum, int> typeGroups = new Dictionary<ImageEnum, int>();
        foreach (var (type, count) in typeCounts)
        {
            typeGroups[type] = count / 3; // 每种类型有几个3消组
        }
        
        // 按3消组分散放置
        int maxGroups = typeGroups.Values.Max();
        for (int groupIndex = 0; groupIndex < maxGroups; groupIndex++)
        {
            foreach (var (type, count) in typeCounts)
            {
                int groups = count / 3;
                if (groupIndex < groups)
                {
                    // 每个3消组连续放置3个相同类型
                    for (int j = 0; j < 3; j++)
                    {
                        levelBubbleSequence.Add(type);
                    }
                }
            }
        }

        // 7. 将现金球随机插入到序列中
        List<ImageEnum> cashBubbles = new List<ImageEnum>();
        for (int i = 0; i < adjustedCashBubbleCount; i++)
        {
            cashBubbles.Add(ImageEnum.IMG0);
        }
        
        // 随机插入现金球到序列中
        foreach (var cashBubble in cashBubbles)
        {
            int randomIndex = Random.Range(0, levelBubbleSequence.Count + 1);
            levelBubbleSequence.Insert(randomIndex, cashBubble);
        }
        
        // Debug.Log($"现金球随机插入完成: {adjustedCashBubbleCount}个");

        // Debug.Log($"=== 第{level}关关卡规划完成 ===");
        // Debug.Log($"总球数: {levelBubbleSequence.Count}");
        // Debug.Log($"现金球: {levelBubbleSequence.Count(t => t == ImageEnum.IMG0)}个");
        // Debug.Log($"其他类型: {levelBubbleSequence.Count(t => t != ImageEnum.IMG0)}个");

        // 验证3消完整性
        ValidateBubbleSequence();
    }

    /// <summary>
    /// 挑战模式正常期智能生成泡泡 - 渐进式难度，确保有足够的相同类型
    /// </summary>
    private ImageEnum GenerateChallengeNormalBubble()
    {
        // 统计场上现有泡泡的类型分布
        Dictionary<ImageEnum, int> typeCounts = new Dictionary<ImageEnum, int>();
        
        foreach (var bubble in m_BubbleItems)
        {
            if (bubble != null)
            {
                ImageEnum type = bubble.imageEnum;
                if (typeCounts.ContainsKey(type))
                    typeCounts[type]++;
                else
                    typeCounts[type] = 1;
            }
        }
        
        // 策略1：如果有某种类型已经有2个，生成第3个形成三消
        foreach (var kvp in typeCounts)
        {
            if (kvp.Value == 2)
            {
                return kvp.Key; // 生成第3个，形成三消
            }
        }
        
        // 策略2：如果有某种类型只有1个，50%几率生成第2个
        List<ImageEnum> singleTypes = new List<ImageEnum>();
        foreach (var kvp in typeCounts)
        {
            if (kvp.Value == 1)
            {
                singleTypes.Add(kvp.Key);
            }
        }
        
        if (singleTypes.Count > 0 && Random.Range(0f, 1f) < 0.5f)
        {
            return singleTypes[Random.Range(0, singleTypes.Count)]; // 生成第2个
        }
        
        // 策略3：根据消除进度动态调整类型范围
        int currentBubbleTypes = GetCurrentBubbleTypesCount();
        return GenerateRandomBubbleType(currentBubbleTypes);
    }
    
    /// <summary>
    /// 根据消除进度获取当前应该使用的泡泡类型数量
    /// </summary>
    private int GetCurrentBubbleTypesCount()
    {
        // 计算已消除的百分比
        float eliminatedPercentage = 1.0f - ((float)bubblesRemaining / totalBubblesForLevel);
        
        // 第一阶段：前20%使用基础类型数量
        if (eliminatedPercentage <= firstPhasePercentage)
        {
            return baseBubbleTypes; // 8种类型
        }
        
        // 后续阶段：每消除5%增加1种类型
        float remainingPercentage = eliminatedPercentage - firstPhasePercentage;
        int additionalTypes = Mathf.FloorToInt(remainingPercentage / phaseIncrementPercentage);
        
        int totalTypes = baseBubbleTypes + additionalTypes;
        
        // 限制在最大类型数量内
        return Mathf.Min(totalTypes, maxBubbleTypes);
    }
    
    /// <summary>
    /// 生成指定数量范围内的随机泡泡类型
    /// </summary>
    private ImageEnum GenerateRandomBubbleType(int typeCount)
    {
        // 生成0到typeCount-1范围内的随机数
        int randomIndex = Random.Range(0, typeCount);
        return (ImageEnum)randomIndex;
    }
    
    /// <summary>
    /// 生成障碍泡泡 - 专门生成无法消除的泡泡，阻止挑战完成
    /// </summary>
    private ImageEnum GenerateObstacleBubble()
    {
        // 统计场上现有泡泡的类型分布
        Dictionary<ImageEnum, int> typeCounts = new Dictionary<ImageEnum, int>();
        
        foreach (var bubble in m_BubbleItems)
        {
            if (bubble != null)
            {
                ImageEnum type = bubble.imageEnum;
                if (typeCounts.ContainsKey(type))
                    typeCounts[type]++;
                else
                    typeCounts[type] = 1;
            }
        }
        
        // 策略1：避免生成第3个相同类型（阻止三消）
        foreach (var kvp in typeCounts)
        {
            if (kvp.Value == 2)
            {
                // 场上已有2个相同类型，绝对不能生成第3个
                // 生成一个完全不同的类型
                return GenerateAntiMatchBubble(typeCounts);
            }
        }
        
        // 策略2：避免生成第2个相同类型（阻止形成对子）
        foreach (var kvp in typeCounts)
        {
            if (kvp.Value == 1)
            {
                // 场上已有1个类型，70%几率不生成第2个
                if (Random.Range(0f, 1f) < 0.7f)
                {
                    return GenerateAntiMatchBubble(typeCounts);
                }
            }
        }
        
        // 策略3：生成大量不同的小众类型，增加类型分散度
        return GenerateScatteredBubbleType();
    }
    
    /// <summary>
    /// 生成反匹配泡泡 - 生成与现有类型完全不同的泡泡
    /// </summary>
    private ImageEnum GenerateAntiMatchBubble(Dictionary<ImageEnum, int> existingTypes)
    {
        // 获取所有已存在的类型
        HashSet<ImageEnum> existingTypeSet = new HashSet<ImageEnum>(existingTypes.Keys);
        
        // 生成一个不存在的类型
        List<ImageEnum> availableTypes = new List<ImageEnum>();
        for (int i = 0; i < maxBubbleTypes; i++)
        {
            ImageEnum type = (ImageEnum)i;
            if (!existingTypeSet.Contains(type))
            {
                availableTypes.Add(type);
            }
        }
        
        // 如果有可用的新类型，随机选择一个
        if (availableTypes.Count > 0)
        {
            return availableTypes[Random.Range(0, availableTypes.Count)];
        }
        
        // 如果所有类型都已存在，选择数量最少的类型（但避免形成三消）
        ImageEnum leastCommonType = ImageEnum.IMG0;
        int minCount = int.MaxValue;
        
        foreach (var kvp in existingTypes)
        {
            if (kvp.Value < minCount && kvp.Value < 2) // 只选择数量少于2的类型
            {
                minCount = kvp.Value;
                leastCommonType = kvp.Key;
            }
        }
        
        return leastCommonType;
    }
    
    /// <summary>
    /// 生成分散的泡泡类型 - 增加类型分散度，减少相同类型聚集
    /// </summary>
    private ImageEnum GenerateScatteredBubbleType()
    {
        // 使用更大的类型范围，增加分散度
        int[] scatteredTypes = { 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18 }; // 使用后面的类型
        return (ImageEnum)scatteredTypes[Random.Range(0, scatteredTypes.Length)];
    }

    /// <summary>
    /// 检查是否需要开启障碍模式（仅挑战模式）
    /// </summary>
    private void CheckObstacleMode()
    {
        // 只有挑战模式才有障碍机制
        GameType gameType = GameManager.Instance.GetGameType();
        if (gameType != GameType.Challenge) return;

        float remainingPercentage = (float)bubblesRemaining / totalBubblesForLevel;

        if (!isObstacleMode && remainingPercentage <= obstaclePercentage)
        {
            isObstacleMode = true;
            // Debug.Log($"挑战模式开启障碍机制！剩余百分比: {remainingPercentage:P2}");
        }
    }

    /// <summary>
    /// 停止生成泡泡球
    /// </summary>
    public void StopSpawning()
    {
        if (spawnCoroutine != null)
        {
            StopCoroutine(spawnCoroutine);
            spawnCoroutine = null;
            // Debug.Log("Bubble spawning stopped");
        }
    }

    /// <summary>
    /// 重置游戏状态
    /// </summary>
    public void ResetGame()
    {
        // Debug.Log("重置游戏状态");

        // 停止当前生成协程
        StopSpawning();

        // 清理所有活跃的泡泡
        foreach (var bubble in m_BubbleItems)
        {
            if (bubble != null)
            {
                bubble.DisableBubble();
                // 🎯 修改：不再使用对象池回收，直接销毁
                Destroy(bubble.gameObject);
            }
        }
        m_BubbleItems.Clear();

        // 清理暂存区（收集区域）
        if (collectAreaManager != null)
        {
            collectAreaManager.ClearAreaForNewGame();
            // Debug.Log("暂存区已清理");
        }

        // 清理清理区（FullStayArea）
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            fullStayArea.ClearStorage();
            // Debug.Log("清理区已清理");
        }

        // 重置游戏数据
        bubblesRemaining = 0;
        totalBubblesForLevel = 0;
        isObstacleMode = false;

        // 重置关卡泡泡序列
        levelBubbleSequence.Clear();
        levelBubbleIndex = 0;

        // 重置挑战模式失败计数
        challengeFailCount = 0;

        // 🎯 新增：重置进度条
        ResetProgressBar();

        // Debug.Log("游戏状态重置完成 - 暂存区和清理区都已清理");
    }


    public void OnClickBBItem(BubbleItem bubbleItem)
    {
        // 检查收集区域是否有空位
        if (collectAreaManager.HasAvailableSpace())
        {
            // 使用新的智能收集系统
            bool collected = collectAreaManager.CollectBubbleIntelligent(bubbleItem);
            if (collected)
            {
                // Debug.Log($"泡泡 {bubbleItem.imageEnum} 被智能收集，启用相邻排列");

                // 检查对象是否仍然有效
                if (bubbleItem != null)
                {
                    bubbleItem.DisableBubble();
                }
                // 从活跃列表中移除（但不销毁，因为现在在收集区域中）
                if (m_BubbleItems.Contains(bubbleItem))
                {
                    m_BubbleItems.Remove(bubbleItem);
                }
                // 点击收集后生成新泡泡
                SpawnNewBubbleOnClick();
            }
            else
            {
                // Debug.LogWarning("智能收集失败！");
            }
        }
        else
        {
            // Debug.Log("收集区域已满，无法收集更多泡泡！");
        }
    }

    /// <summary>
    /// 点击后生成新泡泡（动态掉落逻辑）
    /// </summary>
    private void SpawnNewBubbleOnClick()
    {
        if (bubblesRemaining > 0)
        {
            // 使用新的动态生成逻辑
            ImageEnum newBubbleType = GetNextBubbleTypeForLevel();
            SpawnSingleBubbleWithType(newBubbleType);
            // Debug.Log($"动态生成新泡泡: {newBubbleType}，剩余: {bubblesRemaining}");
        }
        else
        {
            // Debug.Log("所有泡泡已生成完毕！");
            // 检查是否需要触发保底机制
            CheckGuaranteeMechanism();
            CheckGameEnd();
        }
    }

    /// <summary>
    /// 根据游戏模式获取下一个球类型
    /// </summary>
    private ImageEnum GetNextBubbleTypeForLevel()
    {
        GameType gameType = GameManager.Instance.GetGameType();

        if (gameType == GameType.Level)
        {
            // 🎯 新增：输出当前队列状态
            // Debug.Log($"=== 当前生成队列状态 ===");
            // Debug.Log($"队列总长度: {levelBubbleSequence.Count}");
            // Debug.Log($"当前索引: {levelBubbleIndex}");
            // Debug.Log($"剩余数量: {levelBubbleSequence.Count - levelBubbleIndex}");
            
            if (levelBubbleIndex < levelBubbleSequence.Count)
            {
                // Debug.Log($"即将生成: 位置{levelBubbleIndex} -> {levelBubbleSequence[levelBubbleIndex]}");
            }
            
            // 输出接下来5个要生成的泡泡类型
            // Debug.Log("接下来5个要生成的泡泡:");
            for (int i = levelBubbleIndex; i < Mathf.Min(levelBubbleIndex + 5, levelBubbleSequence.Count); i++)
            {
                // Debug.Log($"  位置{i}: {levelBubbleSequence[i]}");
            }
            // Debug.Log("=== 队列状态输出完成 ===");
            
            // Level模式：从预规划的序列中获取
            if (levelBubbleSequence.Count > 0 && levelBubbleIndex < levelBubbleSequence.Count)
            {
                ImageEnum result = levelBubbleSequence[levelBubbleIndex];
                levelBubbleIndex++;
                return result;
            }
            else
            {
                // 如果序列用完，重新生成
                // Debug.Log("⚠️ 队列已用完，重新生成序列...");
                GeneratePerfectBubbleSequence();
                levelBubbleIndex = 0;
                if (levelBubbleSequence.Count > 0)
                {
                    ImageEnum result = levelBubbleSequence[levelBubbleIndex];
                    levelBubbleIndex++;
                    return result;
                }
            }
        }
        else if (gameType == GameType.Challenge)
        {
            // 🎯 挑战模式：使用GetBubbleType()的逻辑
            return GetBubbleType();
        }

        // 🎯 完全删除备用策略：序列用完时直接报错
        // Debug.LogError("无法获取泡泡类型，关卡序列生成失败！");
        throw new System.InvalidOperationException("关卡泡泡序列已用完，无法生成更多泡泡！");
    }

    /// <summary>
    /// 生成指定类型的单个泡泡
    /// </summary>
    private void SpawnSingleBubbleWithType(ImageEnum bubbleType)
    {
        if (bubblesRemaining <= 0) return;

        // 🎯 修改：不再使用对象池，直接实例化
        GameObject item = Instantiate(ballObject.gameObject);
        int index = Random.Range(0, BallInsTrans.Count);

        // 获取基础位置并添加随机偏移
        Vector3 basePosition = BallInsTrans[index].position;
        Vector3 randomOffset = new Vector3(
            Random.Range(-positionOffset.x, positionOffset.x) / 100f,
            Random.Range(-positionOffset.y, positionOffset.y) / 100f,
            0f
        );
        item.transform.SetParent(BallPoolParent.transform);
        item.transform.position = basePosition + randomOffset;
        item.transform.rotation = Quaternion.identity;
        item.transform.localScale = Vector3.one;

        // 设置泡泡图片和类型
        string imageName = $"block_{((int)bubbleType).ToString()}";
        Sprite sprite = GetBallSprite(imageName);

        BubbleItem bubbleItem = item.GetComponent<BubbleItem>();
        bubbleItem.SetImage(sprite, bubbleType);
        bubbleItem.OnclickItem = (Item) =>
        {
            // Debug.Log("BubbleItem clicked");
            OnClickBBItem(Item);
        };

        m_BubbleItems.Add(bubbleItem);
        bubblesRemaining--;

        // 检查是否需要开启障碍模式
        CheckObstacleMode();

        // Debug.Log($"生成泡泡完成: 类型{bubbleType}，剩余{bubblesRemaining}个");
    }

    /// <summary>
    /// 获取当前游戏状态统计
    /// </summary>
    private Dictionary<string, int> GetCurrentGameState()
    {
        Dictionary<string, int> state = new Dictionary<string, int>();

        // 场景中的自由球数量
        state["freeBubbles"] = m_BubbleItems.Count;

        // 暂存区球数量
        if (collectAreaManager != null)
        {
            var occupiedSlots = collectAreaManager.GetOccupiedSlotsPublic();
            state["storageBubbles"] = occupiedSlots.Count;
        }
        else
        {
            state["storageBubbles"] = 0;
        }

        // 清理区球数量
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            var storedBubbles = fullStayArea.GetStoredBubbles();
            state["cleanAreaBubbles"] = storedBubbles.Count;
        }
        else
        {
            state["cleanAreaBubbles"] = 0;
        }

        // 已生成的总球数
        state["totalGenerated"] = totalBubblesForLevel - bubblesRemaining;

        // 关卡规划的总球数
        state["plannedTotal"] = totalBubblesForLevel;

        return state;
    }

    /// <summary>
    /// 分析当前游戏状态并生成需要的球
    /// </summary>
    private void AnalyzeAndGenerateBubbles()
    {
        var gameState = GetCurrentGameState();

        // Debug.Log("=== 当前游戏状态分析 ===");
        // Debug.Log($"自由球: {gameState["freeBubbles"]}个");
        // Debug.Log($"暂存区: {gameState["storageBubbles"]}个");
        // Debug.Log($"清理区: {gameState["cleanAreaBubbles"]}个");
        // Debug.Log($"已生成: {gameState["totalGenerated"]}个");
        // Debug.Log($"规划总数: {gameState["plannedTotal"]}个");

        // 计算当前总球数
        int currentTotal = gameState["freeBubbles"] + gameState["storageBubbles"] + gameState["cleanAreaBubbles"];

        // 计算还需要生成的球数
        int neededBubbles = gameState["plannedTotal"] - currentTotal;

        if (neededBubbles > 0)
        {
            // Debug.Log($"需要生成 {neededBubbles} 个球");

            // 根据关卡规划生成对应类型的球
            for (int i = 0; i < neededBubbles && bubblesRemaining > 0; i++)
            {
                ImageEnum bubbleType = GetNextBubbleTypeForLevel();
                SpawnSingleBubbleWithType(bubbleType);
            }
        }
        else if (neededBubbles < 0)
        {
            // Debug.LogWarning($"球数过多！当前{currentTotal}个，规划{gameState["plannedTotal"]}个，超出{Mathf.Abs(neededBubbles)}个");
        }
        else
        {
            // Debug.Log("球数平衡，无需生成新球");
        }
    }

    /// <summary>
    /// 检查游戏是否结束
    /// </summary>
    private void CheckGameEnd()
    {
        // 获取所有区域的泡泡状态
        int freeBubbles = m_BubbleItems.Count;
        int storageBubbles = 0;
        int cleanAreaBubbles = 0;

        // 检查暂存区
        if (collectAreaManager != null)
        {
            var occupiedSlots = collectAreaManager.GetOccupiedSlotsPublic();
            storageBubbles = occupiedSlots.Count;
        }

        // 检查清理区
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            var storedBubbles = fullStayArea.GetStoredBubbles();
            cleanAreaBubbles = storedBubbles.Count;
        }

        // Debug.Log($"游戏状态检查 - 自由球:{freeBubbles}个, 暂存区:{storageBubbles}个, 清理区:{cleanAreaBubbles}个, 剩余:{bubblesRemaining}个");

        // 失败条件：没有剩余泡泡，且收集区域已满
        else if (bubblesRemaining <= 0 && !collectAreaManager.HasAvailableSpace())
        {
            // Debug.Log("❌ 游戏结束！收集区域已满且无新泡泡");
            OnGameLose();
        }
        // 游戏继续：还有泡泡需要处理
        else if (bubblesRemaining > 0 || freeBubbles > 0 || storageBubbles > 0 || cleanAreaBubbles > 0)
        {
            // Debug.Log("游戏继续中...");
        }
    }

    /// <summary>
    /// 游戏胜利处理
    /// </summary>
    private void OnGameWin()
    {
        GameType gameType = GameManager.Instance.GetGameType();
        // Debug.Log($"恭喜过关！游戏模式: {gameType}");

        if (gameType == GameType.Level)
        {
            // Level模式：升级处理


            // TODO: 在这里打开Level过关界面
            UIManager.GetInstance().ShowUIForms(nameof(LevelCompletePanel));

        }
        else if (gameType == GameType.Challenge)
        {
            // 挑战模式：完成处理
            // Debug.Log("挑战模式完成！");

            // TODO: 在这里打开挑战模式完成界面
            // 例如：UIManager.GetInstance().ShowUIForms(nameof(ChallengeCompletePanel));
        }
    }

    /// <summary>
    /// 游戏失败处理
    /// </summary>
    private void OnGameLose()
    {
        // Debug.Log("游戏失败，再试一次吧！");
        // 可以添加失败UI、重试选项等
    }

    /// <summary>
    /// 获取当前消除进度：已消除数量 / 总数量
    /// </summary>
    /// <returns>返回当前消除进度值 (0.0f - 1.0f)</returns>
    public float GetProgress()
    {
        if (totalBubblesForLevel <= 0) return 0f;
        
        // 计算已消除的泡泡数量
        int eliminatedCount = CalculateEliminatedBubbles();
        float a = 0;
        if(GameManager.Instance.GetGameType() == GameType.Level)
        {
             a = (float)eliminatedCount / totalBubblesForLevel;
           
        }
        else
        {
            a = (float)ProgressCalculator.CalculateProgress(eliminatedCount);
        }
        Debug.Log("GetProgress: " + a);
        Debug.Log("eliminatedCount: " + eliminatedCount);
        Debug.Log("totalBubblesForLevel: " + totalBubblesForLevel);
        // 返回消除进度：已消除数量 / 总数量
        return a;
    }

    /// <summary>
    /// 更新进度条显示（带动画效果）
    /// </summary>
    private void UpdateProgressBar()
    {
        if (progressImage != null)
        {
            float targetProgress = GetProgress();
            float currentProgress = progressImage.fillAmount;
            particalObj.gameObject.SetActive(true);
            // 使用DOTween创建平滑的进度条动画
            progressImage.DOFillAmount(targetProgress, 0.5f)
                .SetEase(Ease.OutQuart)
                .OnUpdate(() => {
                    particalObj.anchoredPosition = new Vector2 (-208 + progressImage.fillAmount* 416f, particalObj.anchoredPosition.y);
                    // 可选：在动画过程中添加额外的视觉效果
                    // 比如进度条的颜色变化、发光效果等
                })
                .OnComplete(() => {
                    Debug.Log($"进度条动画完成: {targetProgress:P2}");
                    particalObj.gameObject.SetActive(false);
                });
            
            Debug.Log($"进度条动画开始: {currentProgress:P2} → {targetProgress:P2}");
        }
    }

    /// <summary>
    /// 重置进度条
    /// </summary>
    private void ResetProgressBar()
    {
        if (progressImage != null)
        {
            // 直接重置为0，不使用动画
            particalObj.anchoredPosition = new Vector2(-208f, particalObj.anchoredPosition.y);
            particalObj.gameObject.SetActive(false);
            progressImage.fillAmount = 0f;
            Debug.Log("进度条已重置为0");
        }
    }
    
    /// <summary>
    /// 计算已消除的泡泡数量
    /// </summary>
    /// <returns>已消除的泡泡数量</returns>
    private int CalculateEliminatedBubbles()
    {
        // 已消除数量 = 总数量 - 剩余未生成数量 - 当前场上所有泡泡数量
        int currentFieldBubbles = GetCurrentFieldBubbleCount();
        int eliminatedCount = totalBubblesForLevel - bubblesRemaining - currentFieldBubbles;
        
        // 确保结果不为负数
        return Mathf.Max(0, eliminatedCount);
    }
    
    /// <summary>
    /// 获取当前场上所有泡泡数量（包括自由球、暂存区、清理区）
    /// </summary>
    /// <returns>当前场上所有泡泡数量</returns>
    private int GetCurrentFieldBubbleCount()
    {
        int count = 0;
        
        // 1. 场上自由球数量
        count += m_BubbleItems.Count;
        
        // 2. 暂存区泡泡数量
        if (collectAreaManager != null)
        {
            var occupiedSlots = collectAreaManager.GetOccupiedSlotsPublic();
            count += occupiedSlots.Count;
        }
        
        // 3. 清理区泡泡数量
        FullStayArea fullStayArea = FindObjectOfType<FullStayArea>();
        if (fullStayArea != null)
        {
            count += fullStayArea.StoredBubbleCount;
        }
        
        return count;
    }
    
    /// <summary>
    /// 获取场上自由球数量（供CollectAreaManager使用）
    /// </summary>
    /// <returns>场上自由球数量</returns>
    public int GetFreeBubblesCount()
    {
        return m_BubbleItems.Count;
    }
    
    /// <summary>
    /// 获取剩余未生成的球数量（供CollectAreaManager使用）
    /// </summary>
    /// <returns>剩余未生成的球数量</returns>
    public int GetRemainingBubblesCount()
    {
        return bubblesRemaining;
    }


    public Sprite GetBallSprite(string ballName)
    {
        // 从图集中获取指定名称的精灵
        Sprite ballSprite = ballAtlas.GetSprite(ballName);
        if (ballSprite == null)
        {
            // Debug.LogError($"Sprite '{ballName}' not found in atlas.");
        }
        return ballSprite;
    }


    /// <summary>
    /// 验证泡泡序列的3消完整性
    /// </summary>
    private void ValidateBubbleSequence()
    {
        Dictionary<ImageEnum, int> typeCounts = new Dictionary<ImageEnum, int>();

        foreach (var type in levelBubbleSequence)
        {
            if (typeCounts.ContainsKey(type))
                typeCounts[type]++;
            else
                typeCounts[type] = 1;
        }

        bool allValid = true;
        foreach (var kvp in typeCounts)
        {
            if (kvp.Value % 3 != 0)
            {
                // Debug.LogError($"类型 {kvp.Key} 数量 {kvp.Value} 不是3的倍数！");
                allValid = false;
            }
        }

        if (allValid)
        {
            // Debug.Log("✅ 所有球类型都是3的倍数，3消完整性验证通过");
        }
        else
        {
            // Debug.LogError("❌ 3消完整性验证失败！");
        }
    }
    public void RefShowTips()
    {
        Tipsobj.SetActive(GameManager.Instance.GetGameType() == GameType.Challenge);
    }

    private void Update()
    {
        // Level模式保底机制检测（优化：按间隔检测，避免每帧都检测）
        if (Time.time - lastLevelGuaranteeCheckTime >= levelGuaranteeCheckInterval)
        {
            CheckLevelGuaranteeMechanism();
            lastLevelGuaranteeCheckTime = Time.time;
        }
    }

    /// <summary>
    /// Level模式保底机制检测 - 每关只触发一次
    /// 当场上不再生成新球且总球数小于6时，检测是否完美匹配，无法匹配时自动生成小球补全
    /// </summary>
    private void CheckLevelGuaranteeMechanism()
    {
        // 只在Level模式下且未触发过保底时执行
        GameType gameType = GameManager.Instance.GetGameType();
        if (gameType != GameType.Level || hasTriggeredLevelGuarantee)
        {
            return;
        }

        // 检查是否所有球都已掉落完成
        if (bubblesRemaining > 0)
        {
            return;
        }

        // 计算当前场上所有泡泡总数
        int totalFieldBubbles = GetCurrentFieldBubbleCount();
        
        // 检查总球数是否小于6
        if (totalFieldBubbles > 6)
        {
            return;
        }

         Debug.Log($"Level保底检测：场上总球数{totalFieldBubbles}个，开始分析可消除性");

        // 分析场上所有泡泡的可消除性
        var bubbleAnalysis = AnalyzeAllBubblesForElimination();
        
        // 检查是否有无法完美消除的泡泡
        if (HasUneliminatableBubbles(bubbleAnalysis))
        {
            // 触发Level模式保底机制
            TriggerLevelGuaranteeMechanism(bubbleAnalysis);
        }
    }

    /// <summary>
    /// 触发Level模式保底机制 - 生成补充泡泡确保完美消除
    /// </summary>
    private void TriggerLevelGuaranteeMechanism(Dictionary<ImageEnum, int> typeCounts)
    {
        hasTriggeredLevelGuarantee = true;
        
        // Debug.Log("=== 触发Level模式保底机制 ===");
        
        // 计算需要补充的泡泡
        List<ImageEnum> bubblesToSpawn = CalculateLevelGuaranteeBubbles(typeCounts);
        
        if (bubblesToSpawn.Count > 0)
        {
            // Debug.Log($"需要生成 {bubblesToSpawn.Count} 个保底泡泡");
            
            // 生成补充泡泡
            StartCoroutine(SpawnLevelGuaranteeBubbles(bubblesToSpawn));
        }
        else
        {
            // Debug.Log("场上泡泡已完美匹配，无需保底");
        }
    }

    /// <summary>
    /// 计算Level模式保底需要补充的泡泡
    /// </summary>
    private List<ImageEnum> CalculateLevelGuaranteeBubbles(Dictionary<ImageEnum, int> typeCounts)
    {
        List<ImageEnum> bubblesToSpawn = new List<ImageEnum>();
        
        foreach (var kvp in typeCounts)
        {
            ImageEnum type = kvp.Key;
            int count = kvp.Value;
            int remainder = count % 3;
            
            if (remainder > 0)
            {
                // 需要补充到下一个3的倍数
                int needToAdd = 3 - remainder;
                for (int i = 0; i < needToAdd; i++)
                {
                    bubblesToSpawn.Add(type);
                }
                
                // Debug.Log($"类型 {type} 当前有 {count} 个，需要补充 {needToAdd} 个");
            }
        }
        
        return bubblesToSpawn;
    }

    /// <summary>
    /// 生成Level模式保底泡泡的协程
    /// </summary>
    private IEnumerator SpawnLevelGuaranteeBubbles(List<ImageEnum> bubblesToSpawn)
    {
        // Debug.Log($"开始生成 {bubblesToSpawn.Count} 个Level保底泡泡");
        
        // 更新剩余泡泡数（用于进度计算）
        bubblesRemaining = bubblesToSpawn.Count;
        
        // 逐个生成保底泡泡
        for (int i = 0; i < bubblesToSpawn.Count; i++)
        {
            SpawnSingleBubbleWithType(bubblesToSpawn[i]);
            yield return new WaitForSeconds(spawnInterval);
        }
        
        // 保底泡泡生成完成
        bubblesRemaining = 0;
        
        // Debug.Log("Level保底泡泡生成完成，游戏现在可以完美消除");
    }
}

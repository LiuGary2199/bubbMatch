using UnityEngine;
using UnityEditor;

/// <summary>
/// 收集区域设置助手 - 帮助在Unity编辑器中快速配置新的收集系统
/// </summary>
public class CollectAreaSetupHelper : MonoBehaviour
{
    [Header("自动设置")]
    [SerializeField] private Transform collectAreaParent; // 收集区域父对象
    [SerializeField] private GameObject slotPrefab; // 槽位预制体
    [SerializeField] private float slotSpacing = 1.2f; // 槽位间距
    
    [Header("组件引用")]
    [SerializeField] private CollectAreaManager collectAreaManager;
    [SerializeField] private GameArea gameArea;
    
    /// <summary>
    /// 自动创建8个槽位并配置系统
    /// </summary>
    [ContextMenu("Auto Setup Collect Area")]
    public void AutoSetupCollectArea()
    {
        if (collectAreaParent == null)
        {
            Debug.LogError("请先设置 collectAreaParent！");
            return;
        }
        
        if (slotPrefab == null)
        {
            Debug.LogError("请先设置 slotPrefab！");
            return;
        }
        
        // 清理现有子对象
        ClearExistingSlots();
        
        // 创建8个槽位
        BubbleSlotBehavior[] slots = new BubbleSlotBehavior[8];
        
        for (int i = 0; i < 8; i++)
        {
            // 实例化槽位
            GameObject slotObj = Instantiate(slotPrefab, collectAreaParent);
            slotObj.name = $"BubbleSlot_{i}";
            
            // 设置位置
            Vector3 position = new Vector3(i * slotSpacing, 0, 0);
            slotObj.transform.localPosition = position;
            
            // 添加或获取 BubbleSlotBehavior 组件
            BubbleSlotBehavior slotBehavior = slotObj.GetComponent<BubbleSlotBehavior>();
            if (slotBehavior == null)
            {
                slotBehavior = slotObj.AddComponent<BubbleSlotBehavior>();
            }
            
            slots[i] = slotBehavior;
        }
        
        // 配置 CollectAreaManager
        SetupCollectAreaManager(slots);
        
        // 配置 GameArea
        SetupGameArea();
        
        Debug.Log("收集区域自动设置完成！创建了8个槽位。");
    }
    
    /// <summary>
    /// 清理现有槽位
    /// </summary>
    private void ClearExistingSlots()
    {
        if (collectAreaParent == null) return;
        
        // 在编辑器模式下安全删除子对象
        for (int i = collectAreaParent.childCount - 1; i >= 0; i--)
        {
            Transform child = collectAreaParent.GetChild(i);
            
            #if UNITY_EDITOR
            if (Application.isPlaying)
            {
                Destroy(child.gameObject);
            }
            else
            {
                DestroyImmediate(child.gameObject);
            }
            #else
            Destroy(child.gameObject);
            #endif
        }
    }
    
    /// <summary>
    /// 配置 CollectAreaManager
    /// </summary>
    private void SetupCollectAreaManager(BubbleSlotBehavior[] slots)
    {
        if (collectAreaManager == null)
        {
            collectAreaManager = GetComponent<CollectAreaManager>();
            if (collectAreaManager == null)
            {
                collectAreaManager = gameObject.AddComponent<CollectAreaManager>();
            }
        }
        
        // 使用反射设置私有字段
        var slotsField = typeof(CollectAreaManager).GetField("bubbleSlots", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        
        if (slotsField != null)
        {
            slotsField.SetValue(collectAreaManager, slots);
            Debug.Log("CollectAreaManager 槽位已配置");
        }
        else
        {
            Debug.LogWarning("无法自动配置 CollectAreaManager 的 bubbleSlots 字段，请手动在Inspector中拖拽设置");
        }
    }
    
    /// <summary>
    /// 配置 GameArea
    /// </summary>
    private void SetupGameArea()
    {
        if (gameArea == null)
        {
            gameArea = FindObjectOfType<GameArea>();
        }
        
        if (gameArea != null)
        {
            // 使用反射设置字段
            var managerField = typeof(GameArea).GetField("collectAreaManager", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
            
            if (managerField != null)
            {
                managerField.SetValue(gameArea, collectAreaManager);
                Debug.Log("GameArea 已配置新的收集系统");
            }
        }
    }
    
    /// <summary>
    /// 创建示例槽位预制体
    /// </summary>
    [ContextMenu("Create Slot Prefab Template")]
    public void CreateSlotPrefabTemplate()
    {
        GameObject slotTemplate = new GameObject("BubbleSlotTemplate");
        
        // 添加基本组件
        BubbleSlotBehavior slotBehavior = slotTemplate.AddComponent<BubbleSlotBehavior>();
        
        // 添加视觉组件（可选）
        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Quad);
        visual.name = "SlotVisual";
        visual.transform.SetParent(slotTemplate.transform);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localScale = Vector3.one * 0.8f;
        
        // 设置材质为半透明
        Renderer renderer = visual.GetComponent<Renderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Sprites/Default"));
            mat.color = new Color(1, 1, 1, 0.3f);
            renderer.material = mat;
        }
        
        // 移除碰撞器
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null)
        {
            DestroyImmediate(collider);
        }
        
        Debug.Log("槽位预制体模板已创建！请将其保存为预制体后使用。");
        
        // 选中创建的对象
        #if UNITY_EDITOR
        Selection.activeGameObject = slotTemplate;
        #endif
    }
    
    /// <summary>
    /// 验证当前设置
    /// </summary>
    [ContextMenu("Validate Setup")]
    public void ValidateSetup()
    {
        Debug.Log("=== 收集区域设置验证 ===");
        
        // 检查 CollectAreaManager
        if (collectAreaManager == null)
        {
            Debug.LogError("❌ CollectAreaManager 未设置！");
        }
        else
        {
            Debug.Log("✅ CollectAreaManager 已设置");
        }
        
        // 检查 GameArea
        if (gameArea == null)
        {
            gameArea = FindObjectOfType<GameArea>();
        }
        
        if (gameArea == null)
        {
            Debug.LogWarning("⚠️ GameArea 未找到");
        }
        else
        {
            Debug.Log("✅ GameArea 已找到");
        }
        
        // 检查槽位
        if (collectAreaParent != null)
        {
            int slotCount = collectAreaParent.childCount;
            Debug.Log($"槽位数量: {slotCount}/8");
            
            if (slotCount != 8)
            {
                Debug.LogWarning($"⚠️ 槽位数量不正确，应为8个，当前为{slotCount}个");
            }
        }
        
        Debug.Log("=== 验证完成 ===");
    }
    
    #if UNITY_EDITOR
    /// <summary>
    /// 在Inspector中显示帮助信息
    /// </summary>
    void OnValidate()
    {
        // 自动查找组件
        if (collectAreaManager == null)
        {
            collectAreaManager = GetComponent<CollectAreaManager>();
        }
        
        if (gameArea == null)
        {
            gameArea = FindObjectOfType<GameArea>();
        }
    }
    #endif
}


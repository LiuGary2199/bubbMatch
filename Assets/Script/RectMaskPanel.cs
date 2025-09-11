using UnityEngine;
using UnityEngine.UI;

public class RectMaskPanel : MonoBehaviour
{
    [Header("目标设置")]
    public GameObject targetObj;
    public float padding = 10f; // 目标周围的边距

    [Header("动画设置")]
    public float shrinkTime = 0.3f;
    public float targetOffsetX;
    public float targetOffsetY;

    private Material material;
    private RectTransform targetRect;
    private Canvas targetCanvas;
    private RectTransform maskRect;

    public float currentOffsetX;
    public float currentOffsetY;
    public float targetPosX;
    public float targetPosY;

    private float shrinkVelocityX = 0f;
    private float shrinkVelocityY = 0f;
    private GuidanceEventPenetrates eventPenetrate;
    private bool useTargetObj = false;

    private void Start()
    {
        maskRect = GetComponent<RectTransform>();
        material = GetComponent<Image>().material;
        eventPenetrate = GetComponent<GuidanceEventPenetrates>();

        // 检查是否有目标对象
        if (targetObj != null)
        {
            targetRect = targetObj.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                targetCanvas = targetObj.GetComponentInParent<Canvas>();
                if (targetCanvas != null)
                {
                    useTargetObj = true;
                    UpdateTargetParameters();
                }
            }
        }

        if (!useTargetObj)
        {
            // 原逻辑：使用Inspector中设置的参数
            Vector4 centerMat = new Vector4(targetPosX, targetPosY, 0, 0);
            material.SetVector("_Center", centerMat);
        }

        if (eventPenetrate != null && useTargetObj)
        {
            eventPenetrate.SetTargetRect(targetRect);
        }
    }

    private void Update()
    {
        if (useTargetObj)
        {
            UpdateTargetParameters();
        }

        // 原逻辑：平滑动画
        float valueX = Mathf.SmoothDamp(currentOffsetX, targetOffsetX, ref shrinkVelocityX, shrinkTime);
        float valueY = Mathf.SmoothDamp(currentOffsetY, targetOffsetY, ref shrinkVelocityY, shrinkTime);

        if (!Mathf.Approximately(valueX, currentOffsetX))
        {
            currentOffsetX = valueX;
            material.SetFloat("_SliderX", currentOffsetX);
        }

        if (!Mathf.Approximately(valueY, currentOffsetY))
        {
            currentOffsetY = valueY;
            material.SetFloat("_SliderY", currentOffsetY);
        }
    }

    private void UpdateTargetParameters()
    {
        // 获取目标在世界空间的中心点
        Vector3 worldCenter = targetRect.TransformPoint(targetRect.rect.center);
        // 转换为屏幕空间坐标
        Vector2 screenPos = RectTransformUtility.WorldToScreenPoint(targetCanvas.worldCamera, worldCenter);

        // 转换为遮罩面板的本地坐标
        Vector2 localPos;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(maskRect, screenPos, targetCanvas.worldCamera, out localPos);

        // Debug输出详细信息
        //  Debug.Log($"[MaskPanel] 挖孔世界中心 worldCenter={worldCenter}, screenPos={screenPos}, localPos={localPos}, targetCanvas={targetCanvas}, worldCamera={targetCanvas.worldCamera}");
        // Debug.Log($"[MaskPanel] targetRect.position={targetRect.position}, sizeDelta={targetRect.sizeDelta}, rect={targetRect.rect}");

        // 设置遮罩中心为目标中心
        targetPosX = localPos.x;
        targetPosY = localPos.y;
        material.SetVector("_Center", new Vector4(targetPosX, targetPosY, 0, 0));

        // 设置遮罩大小为目标大小加上边距
        targetOffsetX = (targetRect.rect.width / 2) + padding;
        targetOffsetY = (targetRect.rect.height / 2) + padding;
    }

    // 外部调用：设置新的目标对象
    public void SetTarget(GameObject newTarget)
    {
        targetObj = newTarget;

        if (targetObj != null)
        {
            targetRect = targetObj.GetComponent<RectTransform>();
            if (targetRect != null)
            {
                targetCanvas = targetObj.GetComponentInParent<Canvas>();
                if (targetCanvas != null)
                {
                    useTargetObj = true;
                    UpdateTargetParameters();

                    if (eventPenetrate != null)
                    {
                        eventPenetrate.SetTargetRect(targetRect);
                    }
                }
            }
        }
        else
        {
            useTargetObj = false;
        }
    }
}
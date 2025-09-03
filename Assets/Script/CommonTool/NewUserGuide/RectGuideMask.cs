using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// 矩形遮罩镂空
/// </summary>
public class RectGuideMask : MonoBehaviour
{
    public static RectGuideMask instance;
    public GameObject Hand;

    /// <summary>
    /// 高亮显示目标
    /// </summary>
    private GameObject target;

    public Text Text;
    /// <summary>
    /// 区域范围缓存
    /// </summary>
    private Vector3[] corners = new Vector3[4];
    /// <summary>
    /// 镂空区域中心
    /// </summary>
    private Vector4 center;
    /// <summary>
    /// 最终的偏移x
    /// </summary>
    private float targetOffsetX = 0;
    /// <summary>
    /// 最终的偏移y
    /// </summary>
    private float targetOffsetY = 0;
    /// <summary>
    /// 遮罩材质
    /// </summary>
    private Material material;
    /// <summary>
    /// 当前的偏移x
    /// </summary>
    private float currentOffsetX = 0f;
    /// <summary>
    /// 当前的偏移y
    /// </summary>
    private float currentOffsetY = 0f;
    /// <summary>
    /// 高亮区域缩放的动画时间
    /// </summary>
    private float shrinkTime = 0.1f;
    /// <summary>
    /// 事件渗透组件
    /// </summary>
    private GuidanceEventPenetrate eventPenetrate;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        
    }

    /// <summary>
    /// 显示引导遮罩
    /// </summary>
    /// <param name="_target">要引导到的目标对象</param>
    /// <param name="text">引导说明文案</param>

    public void ShowGuide(GameObject _target, string text)
    {
        gameObject.SetActive(true);

        if (_target == null)
        {
            Hand.SetActive(false);
            if (material == null)
            {
                material = GetComponent<Image>().material;
            }
            material.SetVector("_Center", new Vector4(0, 0, 0, 0));
            material.SetFloat("_SliderX", 0);
            material.SetFloat("_SliderY", 0);
            // 如果没有target，点击任意区域关闭引导
            GetComponent<Button>().onClick.AddListener(() =>
            {
                gameObject.SetActive(false);
            });
        }
        else
        {
            DOTween.Kill("NewUserHandAnimation");
            Init(_target);
            GetComponent<Button>().onClick.RemoveAllListeners();
        }

        if (!string.IsNullOrEmpty(text))
        {
            Text.text = text;
            Text.transform.parent.gameObject.SetActive(true);
        }
        else
        {
            Text.transform.parent.gameObject.SetActive(false);
        }
    }


    public void Init(GameObject _target)
    {
        this.target = _target;
        
        eventPenetrate = GetComponent<GuidanceEventPenetrate>();
        if (eventPenetrate != null)
        {
            eventPenetrate.SetTargetImage(_target.GetComponent<Image>());
        }

        Canvas canvas = UIManager.GetInstance().MainCanvas.GetComponent<Canvas>();

        //获取高亮区域的四个顶点的世界坐标
        if (target.GetComponent<RectTransform>() != null)
        {
            target.GetComponent<RectTransform>().GetWorldCorners(corners);
        }
        else
        {
            Vector3 pos = Camera.main.WorldToScreenPoint(_target.transform.position);
            pos = UIManager.GetInstance()._TraUICamera.GetComponent<Camera>().ScreenToWorldPoint(pos);
            float width = 1;
            float height = 1;
            corners[0] = new Vector3(pos.x - width, pos.y - height);
            corners[1] = new Vector3(pos.x - width, pos.y + height);
            corners[2] = new Vector3(pos.x + width, pos.y + height);
            corners[3] = new Vector3(pos.x + width, pos.y - height);
        }
        //计算高亮显示区域在画布中的范围
        targetOffsetX = Vector2.Distance(WorldToCanvasPos(canvas, corners[0]), WorldToCanvasPos(canvas, corners[3])) / 2f;
        targetOffsetY = Vector2.Distance(WorldToCanvasPos(canvas, corners[0]), WorldToCanvasPos(canvas, corners[1])) / 2f;
        //计算高亮显示区域的中心
        float x = corners[0].x + ((corners[3].x - corners[0].x) / 2);
        float y = corners[0].y + ((corners[1].y - corners[0].y) / 2);
        Vector3 centerWorld = new Vector3(x, y, 0);
        Vector2 center = WorldToCanvasPos(canvas, centerWorld);
        //设置遮罩材质中的中心变量
        Vector4 centerMat = new Vector4(center.x, center.y, 0, 0);
        material = GetComponent<Image>().material;
        material.SetVector("_Center", centerMat);
        //计算当前高亮显示区域的半径
        RectTransform canRectTransform = canvas.transform as RectTransform;
        if (canRectTransform != null)
        {
            //获取画布区域的四个顶点
            canRectTransform.GetWorldCorners(corners);
            //计算偏移初始值
            for (int i = 0; i < corners.Length; i++)
            {
                if (i % 2 == 0)
                {
                    currentOffsetX = Mathf.Max(Vector3.Distance(WorldToCanvasPos(canvas, corners[i]), center), currentOffsetX);
                }
                else
                {
                    currentOffsetY = Mathf.Max(Vector3.Distance(WorldToCanvasPos(canvas, corners[i]), center), currentOffsetY);
                }
            }
        }
        //设置遮罩材质中当前偏移的变量
        material.SetFloat("_SliderX", currentOffsetX);
        material.SetFloat("_SliderY", currentOffsetY);
        Hand.transform.localScale = new Vector3(1, 1, 1);
        StartCoroutine(ShowHand(center));
    }

    private IEnumerator ShowHand(Vector2 center)
    {
        Hand.SetActive(false);
        yield return new WaitForSeconds(shrinkTime);
        
        Hand.transform.localPosition = center;
        HandAnimation();
        
        Hand.SetActive(true);
    }
    /// <summary>
    /// 收缩速度
    /// </summary>
    private float shrinkVelocityX = 0f;
    private float shrinkVelocityY = 0f;
    private void Update()
    {
        if (material == null) return;

        currentOffsetX = targetOffsetX;
        material.SetFloat("_SliderX", currentOffsetX);
        currentOffsetY = targetOffsetY;
        material.SetFloat("_SliderY", currentOffsetY);
        //从当前偏移量到目标偏移量差值显示收缩动画
        //float valueX = Mathf.SmoothDamp(currentOffsetX, targetOffsetX, ref shrinkVelocityX, shrinkTime);
        //float valueY = Mathf.SmoothDamp(currentOffsetY, targetOffsetY, ref shrinkVelocityY, shrinkTime);
        //if (!Mathf.Approximately(valueX, currentOffsetX))
        //{
        //    currentOffsetX = valueX;
        //    material.SetFloat("_SliderX", currentOffsetX);
        //}
        //if (!Mathf.Approximately(valueY, currentOffsetY))
        //{
        //    currentOffsetY = valueY;
        //    material.SetFloat("_SliderY", currentOffsetY);
        //}


    }

    /// <summary>
    /// 世界坐标转换为画布坐标
    /// </summary>
    /// <param name="canvas">画布</param>
    /// <param name="world">世界坐标</param>
    /// <returns></returns>
    private Vector2 WorldToCanvasPos(Canvas canvas, Vector3 world)
    {
        Vector2 position;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvas.transform as RectTransform, world, canvas.GetComponent<Camera>(), out position);
        return position;
    }

    public void HandAnimation() 
    {
        
        var s = DOTween.Sequence();
        s.Append(Hand.transform.DOLocalMoveY(Hand.transform.localPosition.y + 10f, 0.5f));
        s.Append(Hand.transform.DOLocalMoveY(Hand.transform.localPosition.y, 0.5f));
        s.Join(Hand.transform.DOScaleY(1.1f, 0.125f));
        s.Join(Hand.transform.DOScaleX(0.9f, 0.125f).OnComplete(()=> 
        {
            Hand.transform.DOScaleY(0.9f, 0.125f);
            Hand.transform.DOScaleX(1.1f, 0.125f).OnComplete(()=> 
            {
                Hand.transform.DOScale(1f, 0.125f);
            });
        }));
        s.SetLoops(-1);
        s.SetId("NewUserHandAnimation");
    }

    public void OnDisable()
    {
        DOTween.Kill("NewUserHandAnimation");
    }
}

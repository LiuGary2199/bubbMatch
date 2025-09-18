using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class NoteView : MonoBehaviour
{
    [Header("UI组件")]
    [SerializeField] private Text noteText; // 公告文字
    [SerializeField] private RectTransform noteRectTransform; // Note的RectTransform

    [Header("动画设置")]
    [SerializeField] private float popupDuration = 0.5f; // 弹出动画时长
    [SerializeField] private float hideDuration = 0.5f; // 收起动画时长
    [SerializeField] private float scrollDuration = 8f; // 跑马灯滚动时长
    [SerializeField] private float intervalTime = 20f; // 公告间隔时间（默认值）

    [Header("位置设置")]
    [SerializeField] private float popupY = -228.7f; // 弹出位置Y
    [SerializeField] private float hideY = 0f; // 收起位置Y

    [Header("跑马灯设置")]
    [SerializeField] private float scrollDistance = 200f; // 滚动距离

    private bool isChallengeMode = false;
    private Coroutine announcementCoroutine;

    /// <summary>
    /// 获取当前公告间隔时间
    /// </summary>
    private float GetCurrentIntervalTime()
    {
        if (NetInfoMgr.instance != null && NetInfoMgr.instance.GameData != null)
        {
            float cashmatchValue = NetInfoMgr.instance.GameData.notictime;
            if (cashmatchValue > 0)
            {
                return cashmatchValue;
            }
        }
        return intervalTime; // 如果获取失败或值为0，使用默认值
    }

    /// <summary>
    /// 检查是否应该显示公告
    /// </summary>
    private bool ShouldShowAnnouncement()
    {
        if (NetInfoMgr.instance != null && NetInfoMgr.instance.GameData != null)
        {
            return NetInfoMgr.instance.GameData.notictime > 0;
        }
        return false;
    }

    public void Init()
    {
        // 获取组件引用
        if (noteText == null)
            noteText = GetComponentInChildren<Text>();
        if (noteRectTransform == null)
            noteRectTransform = GetComponent<RectTransform>();

        // 初始化位置
        Vector3 currentPos = noteRectTransform.anchoredPosition;
        noteRectTransform.anchoredPosition = new Vector3(currentPos.x, hideY, currentPos.z);
        if (CommonUtil.IsApple())
        {

        }
        else
        {
            CheckGameMode();
        }


    }

    /// <summary>
    /// 检查游戏模式并启动公告系统
    /// </summary>
    private void CheckGameMode()
    {
        if (GameManager.Instance != null)
        {
            isChallengeMode = true;
            StartAnnouncementSystem();
            if (isChallengeMode)
            {
                Debug.Log("NoteView: Challenge模式，启动公告系统");
                //StartAnnouncementSystem();
            }
            else
            {
                Debug.Log("NoteView: 非Challenge模式，不启动公告系统");
            }
        }
        else
        {
            Debug.LogWarning("NoteView: 无法找到GameManager，不启动公告系统");
        }
    }

    /// <summary>
    /// 启动公告系统
    /// </summary>
    private void StartAnnouncementSystem()
    {
        if (announcementCoroutine != null)
            StopCoroutine(announcementCoroutine);

        announcementCoroutine = StartCoroutine(AnnouncementLoop());
    }

    /// <summary>
    /// 公告循环协程
    /// </summary>
    private IEnumerator AnnouncementLoop()
    {
        while (isChallengeMode)
        {
            // 检查是否应该显示公告
            if (ShouldShowAnnouncement())
            {
                // 获取当前间隔时间
                float currentInterval = GetCurrentIntervalTime();
                Debug.Log($"NoteView: 公告间隔时间设置为: {currentInterval}秒");

                // 等待间隔时间
                yield return new WaitForSeconds(currentInterval);

                // 执行一次公告
                yield return StartCoroutine(ShowAnnouncement());
            }
            else
            {
                // 如果cashmatch为0，等待一段时间后再次检查
                Debug.Log("NoteView: cashmatch为0，暂停公告系统");
                yield return new WaitForSeconds(5f); // 每5秒检查一次
            }
        }
    }

    /// <summary>
    /// 显示公告
    /// </summary>
    private IEnumerator ShowAnnouncement()
    {
        // 再次检查是否应该显示公告（防止在等待期间状态改变）
        if (!ShouldShowAnnouncement())
        {
            yield break;
        }

        // 生成随机公告文字
        string announcementText = GenerateRandomAnnouncement();
        noteText.text = announcementText;

        Debug.Log($"NoteView: 显示公告: {announcementText}");

        // 1. 弹出动画：从hideY移动到popupY
        yield return noteRectTransform.DOAnchorPosY(popupY, popupDuration)
            .SetEase(Ease.OutBack)
            .WaitForCompletion();

        // 2. 跑马灯滚动：左右滚动两遍
        yield return StartCoroutine(ScrollTextTwice());

        // 3. 收起动画：从popupY移动回hideY
        yield return noteRectTransform.DOAnchorPosY(hideY, hideDuration)
            .SetEase(Ease.InBack)
            .WaitForCompletion();
    }

    /// <summary>
    /// 文字滚动两遍
    /// </summary>
    private IEnumerator ScrollTextTwice()
    {
        // 第一遍：从左到右
        yield return StartCoroutine(ScrollTextOnce(true));

        // 短暂停留
        yield return new WaitForSeconds(0.5f);

        // 第二遍：从右到左
        yield return StartCoroutine(ScrollTextOnce(false));
    }

    /// <summary>
    /// 文字滚动一遍
    /// </summary>
    private IEnumerator ScrollTextOnce(bool leftToRight)
    {
        // 获取Image容器的宽度（父物体）
        float imageWidth = noteRectTransform.rect.width;
        // 获取Text的宽度
        float textWidth = noteText.rectTransform.rect.width;

        // 计算起始和结束位置
        // 由于Text是中心对齐，需要调整计算方式
        float startX = (imageWidth + textWidth) / 2; // Text右边对齐Image右边
        float endX = -(imageWidth + textWidth) / 2;  // Text左边对齐Image左边

        // 重置文字位置到起始位置
        Vector3 currentPos = noteText.rectTransform.anchoredPosition;
        noteText.rectTransform.anchoredPosition = new Vector3(startX, currentPos.y, currentPos.z);

        Debug.Log($"跑马灯滚动：Image宽度={imageWidth}, Text宽度={textWidth}, 起始X={startX}, 结束X={endX}");

        // 执行滚动动画
        yield return noteText.rectTransform.DOAnchorPosX(endX, scrollDuration)
            .SetEase(Ease.Linear)
            .WaitForCompletion();
    }

    /// <summary>
    /// 生成随机公告文字
    /// </summary>
    private string GenerateRandomAnnouncement()
    {
        // 生成随机数字
        int randomNum1 = Random.Range(100, 1000); // 100-999
        int randomNum2 = Random.Range(100, 1000); // 100-999

        // 格式化公告文字，使用Rich Text让$1000变红
        string announcement = $"user{randomNum1}***{randomNum2} has passe the daily challenge and won the <color=red>$2000</color>";

        return announcement;
    }

    /// <summary>
    /// 手动触发公告（用于测试）
    /// </summary>
    [ContextMenu("Test Announcement")]
    public void TestAnnouncement()
    {
        if (isChallengeMode && ShouldShowAnnouncement())
        {
            StartCoroutine(ShowAnnouncement());
        }
        else
        {
            Debug.Log("NoteView: 非Challenge模式或cashmatch为0，无法测试公告");
        }
    }

    /// <summary>
    /// 手动触发一次公告（通过W键调用）
    /// </summary>
    public void ManualTriggerAnnouncement()
    {
        Debug.Log("NoteView: 收到手动触发公告请求");

        // 检查是否已经初始化
        if (noteRectTransform == null)
        {
            Debug.LogWarning("NoteView: 尚未初始化，无法触发公告");
            return;
        }

        // 直接触发公告，不检查模式和cashmatch限制
        StartCoroutine(ShowAnnouncement());
    }

    /// <summary>
    /// 停止公告系统
    /// </summary>
    public void StopAnnouncementSystem()
    {
        if (announcementCoroutine != null)
        {
            StopCoroutine(announcementCoroutine);
            announcementCoroutine = null;
        }

        // 重置位置
        Vector3 currentPos = noteRectTransform.anchoredPosition;
        noteRectTransform.anchoredPosition = new Vector3(currentPos.x, hideY, currentPos.z);

        Debug.Log("NoteView: 公告系统已停止");
    }

    /// <summary>
    /// 重新启动公告系统
    /// </summary>
    public void RestartAnnouncementSystem()
    {
        StopAnnouncementSystem();
        CheckGameMode();
    }

    void OnDestroy()
    {
        StopAnnouncementSystem();
    }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;

public class AnimationController : MonoBehaviour
{
    void Start()
    {
        
    }

    
    void Update()
    {
        
    }

    /// <summary>
    /// 弹窗出现效果
    /// </summary>
    /// <param name="PopBarUp"></param>
    public static void PopShow(GameObject PopBarUp,System.Action finish)
    {
        /*-------------------------------------初始化------------------------------------*/
        PopBarUp.GetComponent<CanvasGroup>().alpha = 0;
        PopBarUp.transform.localScale = new Vector3(0, 0, 0);
        /*-------------------------------------动画效果------------------------------------*/
        PopBarUp.GetComponent<CanvasGroup>().DOFade(1, 0.3f);
        PopBarUp.transform.DOScale(1, 0.5f).SetEase(Ease.OutBack).OnComplete(() => 
        {
            finish();
        });
    }


    /// <summary>
    /// 弹窗消失效果
    /// </summary>
    /// <param name="PopBarDisapper"></param>
    public static void PopHide(GameObject PopBarDisapper,System.Action finish)
    {
        /*-------------------------------------初始化------------------------------------*/
        PopBarDisapper.GetComponent<CanvasGroup>().alpha = 1;
        PopBarDisapper.transform.localScale = new Vector3(1, 1, 1);
        /*-------------------------------------动画效果------------------------------------*/
        PopBarDisapper.GetComponent<CanvasGroup>().DOFade(0, 0.5f);
        PopBarDisapper.transform.DOScale(0, 0.5f).SetEase(Ease.InBack).OnComplete(() => 
        {
            finish();
        });
    }
    /// <summary>
    /// 数字变化动画
    /// </summary>
    /// <param name="startNum"></param>
    /// <param name="endNum"></param>
    /// <param name="text"></param>
    /// <param name="finish"></param>
    public static void ChangeNumber(int startNum, int endNum,float delay, Text text,System.Action finish)
    {
        DOTween.To(() => startNum, x => text.text = x.ToString(), endNum, 0.5f).SetDelay(delay).OnComplete(() =>
        {
            finish();
        });
    }

    public static void ChangeNumber(double startNum, double endNum, float delay, Text text, System.Action finish)
    {
        ChangeNumber(startNum, endNum, delay, text, "", finish);
    }
    public static void ChangeNumber(double startNum, double endNum, float delay, Text text, string prefix, System.Action finish)
    {
        DOTween.To(() => startNum, x => text.text = prefix + NumberUtil.DoubleToStr(x), endNum, 0.5f).SetDelay(delay).OnComplete(() =>
        {
            finish();
        });
    }

    /// <summary>
    /// 收金币
    /// </summary>
    /// <param name="GoldImage">金币图标</param>
    /// <param name="a">金币数量</param>
    /// <param name="StartPosition">起始位置</param>
    /// <param name="EndPosition">最终位置</param>
    /// <param name="finish">结束回调</param>
    public static void GoldMoveBest(GameObject GoldImage, int a, Vector2 StartPosition, Vector2 EndPosition, System.Action finish)
    {
        //如果没有就算了
        if (a == 0)
        {
            finish();
        }
        //数量不超过15个
        else if (a > 15)
        {
            a = 15;
        }
        //每个金币的间隔
        float Delaytime = 0;
        for (int i = 0; i < a; i++)
        {
            int t = i;
            //每次延迟+1
            Delaytime += 0.06f;
            //复制一个图标
            GameObject GoldIcon = Instantiate(GoldImage, GoldImage.transform.parent);
            GoldIcon.SetActive(true);
            //初始化
            GoldIcon.transform.position = StartPosition;
            //GoldIcon.transform.localScale = new Vector3(1.2f, 1.2f, 1.2f);
            //金币弹出随机位置
            float OffsetX = UnityEngine.Random.Range(-0.8f, 0.8f);
            float OffsetY = UnityEngine.Random.Range(-0.8f, 0.8f);
            //创建动画队列
            var s = DOTween.Sequence();
            //金币出现
            s.Append(GoldIcon.transform.DOMove(new Vector3(GoldIcon.transform.position.x + OffsetX, GoldIcon.transform.position.y + OffsetY, GoldIcon.transform.position.z), 0.15f).SetDelay(Delaytime).OnComplete(() =>
            {
                //限制音效播放数量
                //if (Mathf.Sin(t) > 0)
                if (t % 5 == 0)
                {
                    MusicMgr.GetInstance().PlayEffect(MusicType.UIMusic.Sound_GoldCoin);
                }
            }));
            //金币移动到最终位置
            s.Append(GoldIcon.transform.DOMove(EndPosition, 0.6f).SetDelay(0.2f));
            s.Join(GoldIcon.transform.DOScale(1.5f, 0.3f).SetEase(Ease.InBack));
            s.AppendCallback(() =>
            {
                //收尾
                s.Kill();
                Destroy(GoldIcon);
                if (t == a - 1)
                {
                    finish();
                }
            });
        }
    }
    public static void GoldMoveBest(GameObject GoldImage, int a, Transform StartTF, Transform EndTF, System.Action finish) {
        GoldMoveBest(GoldImage, a, StartTF.position, EndTF.position, finish); 
    }

    /// <summary>
    /// 横向滚动
    /// </summary>
    /// <param name="obj"></param>
    /// <param name="addPosition"></param>
    /// <param name="Finish"></param>
    public static void HorizontalScroll(GameObject obj, float addPosition, System.Action Finish)
    {
        float positionX = obj.transform.localPosition.x;
        float endPostion = positionX + addPosition;
        obj.transform.DOLocalMoveX(endPostion, 2f).SetEase(Ease.InOutQuart).OnComplete(() => {
            Finish?.Invoke();
        });
    }


}

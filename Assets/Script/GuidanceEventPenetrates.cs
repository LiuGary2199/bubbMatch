using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 事件渗透
/// </summary>
public class GuidanceEventPenetrates : MonoBehaviour, ICanvasRaycastFilter
{
    private RectTransform targetRect;
    public bool isclick = false;

    public void SetTargetRect(RectTransform rect)
    {
        targetRect = rect;
        isclick = false;
    }
    public bool IsRaycastLocationValid(Vector2 sp, Camera eventCamera)
    {
        if (targetRect == null)
        {
            Debug.Log("[Penetrate] targetRect is null, return false");
            return false;
        }
        bool inHole = RectTransformUtility.RectangleContainsScreenPoint(targetRect, sp, eventCamera);

        //Debug.Log($"[Penetrate] sp={sp}, eventCamera={eventCamera}, targetRect={targetRect}, inHole={inHole}");
        return inHole;
    }
}
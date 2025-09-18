using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 游戏全局事件管理类，提供关卡、提示、撤销等事件的注册与触发
/// </summary>
public static class GameEvents
{
    public static Action<Transform> ClickParticle { get; set; }
    public static Action<List<BubbleItem>> CollectParticle { get; set; }
    public static Action GameOver { get; set; }
    public static Action GameWin { get; set; }
    public static Action GameRestart { get; set; }
    public static Action GameFailContinue { get; set; }
    public static Action<BubbleItem> BubbleRemovedFromCleanArea { get; set; }
    public static Action OnThreeMatchCompleted { get; set; }
    public static Action TutorialClickAction { get; set; }
    public static Action FirstChallenge { get; set; }

}



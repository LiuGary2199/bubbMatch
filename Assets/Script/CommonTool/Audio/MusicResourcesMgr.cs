/**
 * 
 * 加载音频资源的单例管理类
 *  通过用户传递进来的枚举值, 找出该枚举值对应的资源在哪个路径下.
 *  
 *  
 * **/
using UnityEngine;
using System.Collections;
public class MusicResourcesMgr : Singleton<MusicResourcesMgr>
{
    // 面向用户的方法:  方法名<T>指定类型
    /// <summary>
    /// 返回用户需要的资源.
    /// </summary>
    /// <param name="enumName">Enum name.</param>
    /// <typeparam name="T">The 1st type parameter.</typeparam>
    public T Load<T>(object enumName) where T : Object
    {
        // 获取枚举类型的字符串形式
        string enumType = enumName.GetType().Name;
        //空的字符串
        string filePath = string.Empty;
        switch (enumType)
        {
            case "SceneMusic":
                {
                    filePath = "Audio/SceneMusic/" + enumName.ToString();
                    break;
                }
            case "UIMusic":
                {
                    filePath = "Audio/UIMusic/" + enumName.ToString();
                    break;
                }           
            default:
                {
                    break;
                }
        }
        return Resources.Load<T>(filePath);
    }
}


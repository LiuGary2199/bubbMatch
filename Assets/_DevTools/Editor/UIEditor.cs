using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public class UIEditor : EditorWindow
{
    [MenuItem("GameObject/BM工具/复制当前选中节点的路径")]
    static private void MMCopyPath()
    {
        string path = GetObjectPath(Selection.activeGameObject);
        GUIUtility.systemCopyBuffer = path;
    }
    public static string GetObjectPath(GameObject gameObject)
    {
        if (gameObject.transform != gameObject.transform.root)
            return gameObject.transform.root.name + "/" + AnimationUtility.CalculateTransformPath(gameObject.transform, gameObject.transform.root);
        else
        {
            return gameObject.transform.root.name;
        }
    }

    [MenuItem("Assets/BM工具/复制资源加载路径", false, 1)]
    static private void Asset_MMCopyLoadPath()
    {
        string root = $"Assets/Resources/";
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        if (!path.StartsWith(root))
        {
            return;
        }
        var name = Path.GetFileNameWithoutExtension(path);
        var dir = Path.GetDirectoryName(path)
            .Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
            .Replace(root, "");
        GUIUtility.systemCopyBuffer = $"{dir}/{name}";
    }
    
}


using UnityEngine;
using System.Collections;
using UnityEditor;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using NUnit.Framework;
public class FindReferences
{
    [MenuItem("Assets/BM工具/查找资源的引用", false, 10)]
    static private void Find()
    {
        EditorSettings.serializationMode = SerializationMode.ForceText;
        Object[] SelectedAsset = Selection.GetFiltered(typeof(Object), SelectionMode.Assets | SelectionMode.ExcludePrefab);
        //此处添加需要命名的资源后缀名,注意大小写。
        string[] Filtersuffix = new string[] { ".prefab", ".mat", ".dds", ".png", ".jpg", ".shader", ".csv", ".wav", ".mp3" };
        if (SelectedAsset.Length == 0) return;
        foreach (Object tmpFolder in SelectedAsset)
        {
            string path = AssetDatabase.GetAssetPath(tmpFolder);
            if (!string.IsNullOrEmpty(path))
            {
                string guid = AssetDatabase.AssetPathToGUID(path);
                List<string> withoutExtensions = new List<string>() { ".prefab", ".unity", ".mat", ".asset" };
                string[] files = Directory.GetFiles(Application.dataPath, "*.*", SearchOption.AllDirectories).Where(s => withoutExtensions.Contains(Path.GetExtension(s).ToLower())).ToArray();
                int num = 0;
                for (var i = 0; i < files.Length; ++i)
                {
                    string file = files[i];
                    //显示进度条
                    EditorUtility.DisplayProgressBar("匹配资源", "正在匹配资源中...", 1.0f * i / files.Length);
                    if (Regex.IsMatch(File.ReadAllText(file), guid))
                    {
                        Debug.Log(file, AssetDatabase.LoadAssetAtPath<Object>(GetRelativeAssetsPath(file)));
                        num++;
                    }
                }
                if (num == 0)
                {
                    Debug.LogError(tmpFolder.name + "     匹配到" + num + "个", tmpFolder);
                }
                else if (num == 1)
                {
                    Debug.Log(tmpFolder.name + "     匹配到" + num + "个", tmpFolder);
                }
                else
                {
                    Debug.LogWarning(tmpFolder.name + "     匹配到" + num + "个", tmpFolder);
                }
                num = 0;
                //				int startIndex = 0;//				EditorApplication.update = delegate() {//					string file = files [startIndex];////					bool isCancel = EditorUtility.DisplayCancelableProgressBar ("匹配资源中", file, (float)startIndex / (float)files.Length);////					if (Regex.IsMatch (File.ReadAllText (file), guid)) {//						Debug.Log (file, AssetDatabase.LoadAssetAtPath<Object> (GetRelativeAssetsPath (file)));//					}////					startIndex++;//					if (isCancel || startIndex >= files.Length) {//						//						EditorApplication.update = null;//						startIndex = 0;//						Debug.Log ("匹配结束" + tmpFolder.name);//					}////				};
            }
        }
        EditorUtility.ClearProgressBar();
    }
    [MenuItem("Assets/BM工具/查找资源的引用", true)]
    static private bool VFind()
    {
        string path = AssetDatabase.GetAssetPath(Selection.activeObject);
        return (!string.IsNullOrEmpty(path));
    }
    static private string GetRelativeAssetsPath(string path)
    {
        return "Assets" + Path.GetFullPath(path).Replace(Path.GetFullPath(Application.dataPath), "").Replace('\\', '/');
    }

[MenuItem("Assets/BM工具/查找选中资源依赖关系", false, 0)]
    public static void FindDependencies()
    {
        Debug.Log("------------------------------------");
        var refCount = 0;
        for (int i = 0; i < Selection.assetGUIDs.Length; i++)
        {
            string guid = Selection.assetGUIDs[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            Debug.Log($"查找依赖: {path}", Selection.objects[i]);
            foreach (var dependenciePath in AssetDatabase.GetDependencies(path, false))
            {
                if (path != dependenciePath && !dependenciePath.Contains("cs"))
                    Debug.LogError(string.Format("path = {0} 依赖 > {1}", path, dependenciePath));
                    refCount ++;
            }
        }
        Debug.Log($"查找依赖结束: {refCount} 处依赖");
        Debug.Log("------------------------------------");
    }

    [MenuItem("Assets/BM工具/查找选中资源反向依赖关系", false, 1)]
    public static void FindReverseDependencies()
    {
        Debug.Log("------------------------------------");
        Dictionary<string, List<string>> dDependencies = new Dictionary<string, List<string>>();
        for (int i = 0; i < Selection.assetGUIDs.Length; i++)
        {
            string guid = Selection.assetGUIDs[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);
            dDependencies.Add(path, new List<string>());
            Debug.Log($"查找反向依赖: {path}", Selection.objects[i]);
        }

        string[] guids = AssetDatabase.FindAssets("t:Prefab t:Material t:Scene");
        for (int i = 0; i < guids.Length; i++)
        {
            string guid = guids[i];
            string path = AssetDatabase.GUIDToAssetPath(guid);

            if (EditorUtility.DisplayCancelableProgressBar("资源依赖关系查找中", path, (float)(i + 1) / guids.Length))
            {
                EditorUtility.ClearProgressBar();
                return;
            }

            foreach (var dependenciePath in AssetDatabase.GetDependencies(path, false))
            {
                if (path != dependenciePath)
                {
                    if (dDependencies.TryGetValue(dependenciePath, out List<string> list))
                    {
                        list.Add(path);
                    }
                }
            }
        }

        var refCount = 0;
        foreach (var kv in dDependencies)
        {
            kv.Value.ForEach((dependenciePath) =>
            {
                refCount ++;
                Debug.Log(string.Format("path = {0} 被 > {1} 依赖", kv.Key, dependenciePath), AssetDatabase.LoadMainAssetAtPath(dependenciePath));
            });
        }


        EditorUtility.ClearProgressBar();
        Debug.Log($"查找依赖结束: {refCount} 处被依赖");
        Debug.Log("------------------------------------");
    }
}

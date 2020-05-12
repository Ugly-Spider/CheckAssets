#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

[CustomEditor(typeof(CheckAssets))]
public class CheckAssetsInspector : Editor
{
    private List<CheckResult> checkResult;

    void OnDisable()
    {
        EditorUtility.SetDirty(target);
        AssetDatabase.SaveAssets();
    }

    public override void OnInspectorGUI()
    {
        base.OnInspectorGUI();
        CheckAssets checkAssets = (CheckAssets)target;
        if (checkAssets.usefulPath == null) checkAssets.usefulPath = new List<string>();
        if (checkAssets.checkPath == null) checkAssets.checkPath = new List<string>();



        GUILayout.Label("场景/预制体路径:");
        for (int i = 0; i < checkAssets.usefulPath.Count; i++)
        {
            string text = checkAssets.usefulPath[i];
            GUILayout.BeginHorizontal();
            if (GUILayout.Button(text, GUI.skin.label))
            {
            }
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                checkAssets.usefulPath.RemoveAt(i);
            }
            GUILayout.EndHorizontal();
        }

        if (GUILayout.Button("+"))
        {
            string path = EditorUtility.OpenFolderPanel("Select", "Assets", "");
            path = FullPathToAssetsPath(path);
            if (!string.IsNullOrEmpty(path) && !checkAssets.usefulPath.Contains(path))
            {
                checkAssets.usefulPath.Add(path);
            }

            AssetDatabase.SaveAssets();
        }

        GUILayout.Label("资源路径:");
        for (int i = 0; i < checkAssets.checkPath.Count; i++)
        {
            GUILayout.BeginHorizontal();
            string text = checkAssets.checkPath[i];
            //text = "Assets" + text.Replace(Application.dataPath, "");
            if (GUILayout.Button(text, GUI.skin.label))
            {
            }
            if (GUILayout.Button("-", GUILayout.Width(20)))
            {
                checkAssets.checkPath.RemoveAt(i);
            }
            GUILayout.EndHorizontal();
        }
        if (GUILayout.Button("+"))
        {
            string path = EditorUtility.OpenFolderPanel("Select", "Assets", "");
            path = FullPathToAssetsPath(path);
            if (!string.IsNullOrEmpty(path) && !checkAssets.checkPath.Contains(path))
            {
                checkAssets.checkPath.Add(path);
            }

            AssetDatabase.SaveAssets();
        }
        if (GUILayout.Button("检查资源"))
        {
            var allFiles = GetAllFiles(checkAssets.checkPath);
            var dependences = GetDependences(checkAssets.usefulPath);
            checkResult = CheckAssets(allFiles, dependences);
        }

        GUILayout.BeginHorizontal();
        bool flag = GUILayout.Toggle(checkAssets.showNormal, "显示非异常资源");
        if(flag != checkAssets.showNormal)
        {
            checkAssets.showNormal = flag;
            AssetDatabase.SaveAssets();
        }
        GUILayout.EndHorizontal();

        if (checkResult != null && checkResult.Count != 0)
        {
            for (int i = 0; i < checkResult.Count; i++)
            {
                var r = checkResult[i];
                int refCount = r.refSources.Count;
                //GUILayout.BeginVertical(refCount == 1 ? NormalStyle : AlertStyle);
                GUILayout.BeginVertical();
                if (refCount == 1)
                {
                    if (!checkAssets.showNormal) continue;
                }

                GUILayout.BeginHorizontal();
                string assetPath = r.assetPath;
                Texture icon = AssetDatabase.GetCachedIcon(assetPath);
                GUILayout.Label(refCount.ToString(), GUILayout.MaxHeight(16), GUILayout.Width(20));
                if(GUILayout.Button(icon, GUI.skin.label, GUILayout.MaxHeight(16), GUILayout.Width(20)))
                {
                    EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(assetPath));
                }
                GUILayout.Space(10);
                r.foldout = EditorGUILayout.Foldout(r.foldout, assetPath);
                GUILayout.EndHorizontal();
                if(r.foldout)
                {
                    foreach (var _ref in r.refSources)
                    {
                        GUILayout.BeginHorizontal();
                        GUILayout.Space(65);
                        if(GUILayout.Button(_ref, GUI.skin.label))
                        {
                            EditorGUIUtility.PingObject(AssetDatabase.LoadAssetAtPath<Object>(_ref));
                        }
                        GUILayout.EndHorizontal();
                    }
                }

                GUILayout.EndVertical();
            }

            //备份并删除引用零次的资源
            if(GUILayout.Button("备份并删除多余文件"))
            {
                List<string> toDeleteFiles = new List<string>();
                foreach(var v in checkResult)
                {
                    if(v.refSources.Count == 0)
                    {
                        toDeleteFiles.Add(v.assetPath);
                    }
                }

                if (toDeleteFiles.Count != 0)
                {
                    string path = Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + "Backup";
                    if (!Directory.Exists(path)) Directory.CreateDirectory(path);
                    string suffix = "[" + System.DateTime.Now + "]";
                    suffix = suffix.Replace("/", "-");
                    suffix = suffix.Replace(":", "-");
                    string fileName = "CheckAsses" + suffix + ".unitypackage";
                    string fullPath = Path.Combine(path, fileName);
                    AssetDatabase.ExportPackage(toDeleteFiles.ToArray(), fullPath);

                    foreach (var v in toDeleteFiles)
                    {
                        AssetDatabase.DeleteAsset(v);
                    }

                    AssetDatabase.Refresh();
                    System.Diagnostics.Process.Start(path);
                }
            }
        }
    }

    private List<CheckResult> CheckAssets(string[] allFiles, List<(string, List<string>)> dependences)
    {
        List<CheckResult> result = new List<CheckResult>();//资源Assets路径 + 依赖源
        float progress = 0;
        int index = 0;
        foreach (var v in allFiles)
        {
            List<string> refSources = new List<string>();
            string assetsPath = FullPathToAssetsPath(v);
            string guid = AssetDatabase.AssetPathToGUID(assetsPath);
            foreach (var dep in dependences)
            {
                if (dep.Item2.Contains(guid))
                {
                    refSources.Add(dep.Item1);
                }
            }

            result.Add(new CheckResult()
            {
                assetPath = assetsPath,
                refSources = refSources,
            });

            progress = (float)index / allFiles.Length;
            string title = $"CheckAssets({index}/{allFiles.Length}) ";
            string text = $"{v}";
            EditorUtility.DisplayProgressBar(title, text, progress);
            index++;
        }
        EditorUtility.ClearProgressBar();
        result = result.OrderBy(p => p.refSources.Count).ToList();
        return result;
    }

    private List<(string, List<string>)> GetDependences(List<string> paths)
    {
        List<(string, List<string>)> result = new List<(string, List<string>)>();
        foreach (var path in paths)
        {
            string assetPath = FullPathToAssetsPath(path);
            string[] files = GetAllFiles(assetPath);

            foreach (var file in files)
            {
                string fileAssetPath = FullPathToAssetsPath(file);
                string[] deps = AssetDatabase.GetDependencies(fileAssetPath);
                for (int i = 0; i < deps.Length; i++)
                {
                    deps[i] = AssetDatabase.AssetPathToGUID(deps[i]);
                }

                result.Add((file, deps.ToList()));
            }
        }
        return result;
    }

    private string[] GetAllFiles(List<string> assetPaths)
    {
        List<string> result = new List<string>();
        foreach (var v in assetPaths)
        {
            result.AddRange(GetAllFiles(v));
        }
        return result.ToArray();
    }

    //获取指定路径下的全部文件,返回Asset路径
    private string[] GetAllFiles(string assetPath)
    {
        List<string> result = new List<string>();
        string path = AssetsPathToFullPath(assetPath);
        GetFilesRecursive(path, result);
        return result.ToArray();
    }

    private void GetFilesRecursive(string path, List<string> result)
    {
        DirectoryInfo info = new DirectoryInfo(path);
        FileInfo[] files = info.GetFiles();
        DirectoryInfo[] dirs = info.GetDirectories();
        foreach (var v in files)
        {
            if (IsIgnoreFile(v.Name)) continue;

            string assetPath = v.FullName.Replace(Application.dataPath.Replace("/", "\\"), "Assets");
            result.Add(assetPath);
        }
        foreach (var dir in dirs)
        {
            GetFilesRecursive(dir.FullName, result);
        }
    }

    private string FullPathToAssetsPath(string path)
    {
        return path.Replace(Application.dataPath, "Assets");
    }

    private string AssetsPathToFullPath(string assetsPath)
    {
        return Application.dataPath.Substring(0, Application.dataPath.Length - "Assets".Length) + assetsPath;
    }

    private bool IsIgnoreFile(string fileName)
    {
        return fileName.EndsWith(".meta") || fileName.StartsWith(".") || fileName.EndsWith(".spriteatlas");
    }

    public class CheckResult
    {
        public bool foldout;
        public string assetPath;
        public List<string> refSources;
    }
}
#endif

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 检查资源被场景引用情况
/// </summary>
[CreateAssetMenu(fileName = "CheckAssets")]
public class CheckAssets : ScriptableObject
{
    [HideInInspector]
    public List<string> usefulPath;
    [HideInInspector]
    public List<string> checkPath;
    [HideInInspector]
    public bool showNormal;
}

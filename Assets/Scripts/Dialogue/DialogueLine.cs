using UnityEngine;

/// <summary>
/// 单条对话台词的数据结构。
/// </summary>
[System.Serializable]
public struct DialogueLine
{
    /// <summary>说话人名称</summary>
    public string speaker;

    /// <summary>台词文本</summary>
    [TextArea(2, 5)]
    public string content;
}
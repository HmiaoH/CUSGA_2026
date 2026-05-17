using UnityEngine;

/// <summary>
/// 一段完整的线性对话序列。通过 Unity 菜单创建资产。
/// 菜单路径：Create → Dialogue → DialogueSequence
/// </summary>
[CreateAssetMenu(fileName = "NewDialogue", menuName = "Dialogue/DialogueSequence")]
public class DialogueSequenceSO : ScriptableObject
{
    /// <summary>本段对话包含的所有台词</summary>
    public DialogueLine[] lines;
}
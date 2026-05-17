using System;
using Frameworks;
using UnityEngine;

namespace Managers
{
    /// <summary>
    /// 对话管理器。控制对话的播放、推进和结束。
    /// 使用方式与 CutsceneManager 一致：传入数据 + 完成回调。
    /// </summary>
    public class DialogueManager : ManagerBase<DialogueManager>
    {
        [SerializeField] private DialoguePanel dialoguePanel;

        private DialogueSequenceSO currentSequence;
        private int currentLineIndex;
        private Action onComplete;
        private bool isPlaying;

        protected override void OnInit()
        {
            if (dialoguePanel == null)
            {
                Debug.LogWarning("DialogueManager 缺少 DialoguePanel 引用");
            }
            // ⚠️ 临时测试代码，验证完成后删除
            // var testSequence = Resources.Load<DialogueSequenceSO>("Dialogue/TestDialogue");
            // if (testSequence != null)
            // {
            //     StartDialogue(testSequence, () => Debug.Log("对话播放完毕！"));
            // }
        }

        /// <summary>
        /// 开始播放一段对话。播放完毕后调用 completeCallback。
        /// </summary>
        /// <param name="sequence">对话序列资产</param>
        /// <param name="completeCallback">结束后调用的回调函数</param>
        public void StartDialogue(DialogueSequenceSO sequence, Action completeCallback)
        {
            // 防护：空数据时直接完成，不卡流程
            if (sequence == null || sequence.lines == null || sequence.lines.Length == 0)
            {
                Debug.LogWarning("DialogueManager: 对话序列为空，直接跳过");
                completeCallback?.Invoke();
                return;
            }

            if (dialoguePanel == null)
            {
                Debug.LogError("DialogueManager: DialoguePanel 未配置，无法播放对话");
                completeCallback?.Invoke();
                return;
            }

            currentSequence = sequence;
            currentLineIndex = 0;
            onComplete = completeCallback;
            isPlaying = true;

            // 激活面板并显示第一句
            dialoguePanel.gameObject.SetActive(true);
            ShowCurrentLine();
        }

        /// <summary>
        /// 推进到下一句。由 DialoguePanel 的按钮点击调用。
        /// </summary>
        public void NextLine()
        {
            if (!isPlaying)
            {
                return;
            }

            currentLineIndex++;

            if (currentLineIndex < currentSequence.lines.Length)
            {
                ShowCurrentLine();
            }
            else
            {
                EndDialogue();
            }
        }

        /// <summary>
        /// 显示当前索引对应的台词。
        /// </summary>
        private void ShowCurrentLine()
        {
            dialoguePanel.ShowLine(currentSequence.lines[currentLineIndex]);
        }

        /// <summary>
        /// 结束对话：隐藏面板、清理状态、触发回调。
        /// </summary>
        private void EndDialogue()
        {
            isPlaying = false;
            dialoguePanel.gameObject.SetActive(false);

            currentSequence = null;
            currentLineIndex = 0;

            // 保存并清空回调引用后再调用，防止回调中再次触发 StartDialogue 导致问题
            var callback = onComplete;
            onComplete = null;
            callback?.Invoke();
        }
    }
}
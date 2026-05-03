using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Frameworks;
using UnityEngine;
using UnityEngine.Playables;

namespace Managers
{
    /// <summary>
    /// 分镜管理器
    /// </summary>
    public class CutsceneManager : ManagerBase<CutsceneManager>
    {
        [SerializeField] private List<CutsceneItem> cutsceneList = new List<CutsceneItem>();

        private Dictionary<string, PlayableDirector> cutscenes = new Dictionary<string, PlayableDirector>();
        private Action onComplete;

        // 初始化全部设置为字典
        protected override void OnInit()
        {
            if (cutscenes.Count == 0)
            {
                Debug.LogWarning("No cutscenes found");
            }
            ListToDict();
        }

        /// <summary>
        /// 播放分镜并回调函数
        /// </summary>
        /// <param name="cutsceneName">分镜名称，其实就是CutsceneItem中对应的名称，这里使用Utils中的字段</param>
        /// <param name="completeCallback">结束后调用的函数名称（当前脚本中）</param>
        public void PlayCutscene(string cutsceneName, Action completeCallback)
        {
            if (!cutscenes.TryGetValue(cutsceneName, out var director))
            {
                Debug.LogWarning("找不到过场动画：" + cutsceneName);
                completeCallback?.Invoke();
                return;
            }

            onComplete = completeCallback;

            // 防止重复添加
            director.stopped -= OnDirectorStopped;
            director.stopped += OnDirectorStopped;

            director.Play();
        }

        /// <summary>
        /// 动画播放完成后，触发completeCallback
        /// </summary>
        /// <param name="director">播放的director</param>
        private void OnDirectorStopped(PlayableDirector director)
        {
            director.stopped -= OnDirectorStopped;

            onComplete?.Invoke();
            onComplete = null;
        }


        private void ListToDict()
        {
            foreach (var c in cutsceneList)
            {
                cutscenes[c.name] = c.director;
            }
        }
    }
}

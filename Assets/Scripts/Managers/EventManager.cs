using Frameworks;
using UnityEngine;
using System;
using System.Collections.Generic;

namespace Managers
{
    public class EventManager : ManagerBase<EventManager>
    {
        // Action的本质是 指针。所以可以直接指向 对应物体的 对应函数。
        private readonly Dictionary<string, Action<object>> events =
            new Dictionary<string, Action<object>>();

        // 关闭时清空 events
        protected override void OnShutdown()
        {
            events.Clear();
        }

        /// <summary>
        /// 添加监听
        /// </summary>
        /// <param name="eventName">event名称，全局通用</param>
        /// <param name="callback">回调函数，唤醒后调用的函数名称</param>
        public void AddListener(string eventName, Action<object> callback)
        {
            if (!events.ContainsKey(eventName))
            {
                events[eventName] = null;
            }

            events[eventName] += callback;
        }

        /// <summary>
        /// 移除监听
        /// </summary>
        /// <param name="eventName">event名称，全局通用</param>
        /// <param name="callback">回调函数，唤醒后调用的函数名称</param>
        public void RemoveListener(string eventName, Action<object> callback)
        {
            if (events.ContainsKey(eventName))
            {
                events[eventName] -= callback;
            }
        }

        /// <summary>
        /// 发布信息 查找当前events中 是否有eventName字段。如果有，就回调此event对应的Action 至callback中。如果callback存在 就唤醒。
        /// </summary>
        /// <param name="eventName">event名称，全局通用</param>
        /// <param name="data">发布的数据，如果需要的话</param>
        public void Dispatch(string eventName, object data = null)
        {
            if (events.TryGetValue(eventName, out Action<object> callback))
            {
                callback?.Invoke(data);
            }
        }
    }
}

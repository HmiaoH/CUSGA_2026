using UnityEngine;

namespace Frameworks
{
    public abstract class ManagerBase<T> : SingletonMono<T>, IManager where T : MonoBehaviour
    {
        public bool initialized { get; private set; }



        void IManager.Init()
        {
            if (initialized) {return;}
            OnInit();
            initialized = true;
        }

        void IManager.Tick(float deltaTime)
        {
            if (!initialized) {return;}
            OnTick(deltaTime);
        }

        void IManager.Shutdown()
        {
            if (!initialized) {return;}
            OnShutdown();
            initialized = false;
        }


        protected virtual void OnInit() { }
        protected virtual void OnTick(float deltaTime) { }
        protected virtual void OnShutdown() { }

    }
}

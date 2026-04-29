namespace Frameworks
{
    public interface IManager
    {
        void Init();
        void Tick(float deltaTime);
        void Shutdown();
    }
}

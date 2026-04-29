using System.Collections.Generic;
using Frameworks;

public class ManagerCenter
{
    private List<IManager> managers = new List<IManager>();

    public void Register(IManager manager)
    {
        if (manager == null || managers.Contains(manager))
        {
            return;
        }

        managers.Add(manager);
    }

    public void InitAll()
    {
        foreach (IManager manager in managers)
        {
            manager.Init();
        }
    }

    public void TickAll(float deltaTime)
    {
        foreach (IManager manager in managers)
        {
            manager.Tick(deltaTime);
        }
    }

    public void ShutdownAll()
    {
        for (int i = managers.Count - 1; i >= 0; i--)
        {
            managers[i].Shutdown();
        }
    }
}

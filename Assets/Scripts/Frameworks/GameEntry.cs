using System.Collections.Generic;
using UnityEngine;

using Managers;


public class GameEntry : SingletonMono<GameEntry>
{
    [Header("Managers")]
    [SerializeField] private EventManager eventManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private InputManager inputManager;
    [SerializeField] private CameraManager cameraManager;
    [SerializeField] private GameFlowManager gameFlowManager;
    [SerializeField] private CutsceneManager cutsceneManager;
    [SerializeField] private DialogueManager dialogueManager;

    private ManagerCenter managerCenter;

    protected override void Awake()
    {
        base.Awake();

        // 防止编辑器 自动 损毁Managers
        DontDestroyOnLoad(transform.root.gameObject);

        managerCenter = new ManagerCenter();

        // 订阅 Manager 并初始化
        RegisterManagers();
        managerCenter.InitAll();
    }

    private void Update()
    {
        managerCenter.TickAll(Time.deltaTime);
    }

    private void OnApplicationQuit()
    {
        managerCenter.ShutdownAll();
    }

    private void RegisterManagers()
    {
        managerCenter.Register(eventManager);
        managerCenter.Register(resourceManager);
        managerCenter.Register(audioManager);
        managerCenter.Register(uiManager);
        managerCenter.Register(saveManager);
        managerCenter.Register(inputManager);
        managerCenter.Register(cameraManager);

        managerCenter.Register(cutsceneManager);
        managerCenter.Register(dialogueManager);
        managerCenter.Register(gameFlowManager);
    }
}

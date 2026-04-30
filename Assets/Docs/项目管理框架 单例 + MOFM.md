[TOC]

在本文中，我们将实现一个整体的游戏框架。我们使用的架构是单例模式 + MOFM(Manager Of Managers)模式。

> 注：为了方便维护，本文总的所有字符串，我们都应当使用对应的静态Utils类来替换。
>
> 做法为：
>
> 1. 新建一个脚本，生成一个名叫KeyNames的静态类。
> 2. 下面添加多个字段，如 `public string DOWN = "down"`
> 3. 这样我们在用“down”时候就可以写下 `isPressed(KeyNames.DOWN)` 而非直接填入字段。

# 整体框架预览

## 层级架构

```
GameRoot
├── GameEntry
├── EventManager
├── AudioManager
├── UIManager
├── ResourceManager
├── SaveManager
└── InputManager
```

我们会使用在空物体上 挂载 `MonoBehaviour` 单例脚本的模式 来构建

其中 GameEntry 负责：

- 找到这些已经挂好的 Manager
- 注册到 Manager Center
- 按顺序初始化
- 每帧统一 Tick
- 退出游戏时统一 Shutdown

## 目录架构

```
Assets
└── Scripts
    ├── Framework
    │   ├── SingletonMono.cs
    │   ├── IManager.cs
    │   ├── ManagerBase.cs
    │   ├── ManagerCenter.cs
    │   └── GameEntry.cs
    │
    ├── Managers
    │   ├── EventManager.cs
    │   ├── AudioManager.cs
    │   ├── UIManager.cs
    │   ├── ResourceManager.cs
    │   ├── SaveManager.cs
    │   └── InputManager.cs
    │
    ├── Systems
    │   ├── BattleSystem.cs
    │   ├── InventorySystem.cs
    │   └── DialogueSystem.cs
    │
    └── UI
        └── Panels

```

Framework：框架基础代码
Managers：全局管理器
Systems：玩法系统
UI：界面代码

# Manager基类脚本

整体大致类似于

SingletonMono和 IManager 脚本是Manager的基类。所有Manager都是从这两个继承出的。



SingletonMono 让所有的 Manager 初始生成一个 Instacne实例。以供其他脚本调用。

（还有管理Instance的生命周期）

使用方法为 `xxxManager.Instance.xxxFunction();`



IManager 提供了三种状态的接口 `Init() Tick(deltaTime) Shutdown()`

但是在具体的ManagerBase脚本中，他们并不蕴含各种具体代码，只是检测当前是否被初始化 并执行对应实现具体功能的`Onxxx()`函数。

如 运行`Init()`就会检测是否被初始化（ManagerBase脚本中的 `IsInitialized`字段），如果是，那么不做操作。如果没有被初始化过，就运行`OnInit()`并设置为初始化。



设置好Manager的基类后，我们使用总Manager(GameEntry) 管理其他的Manager。所以需要一个脚本去订阅 并 统一操作 其他子Managers 的基础函数（Init等）。

因此我们需要一个ManagerCenter纯脚本文件。GameEntry可以统一调用里面的managers。

这个脚本中，我们有

- 一个managers 的 list。

- 订阅某一个manager的函数`Register(IManager manager)`

> GameEntry来调用这个函数让其订阅所有managers。

- 统一管理所有的managers。`Init/Tick/ShutdownAll()`

> 也是GameEntry调用来管理。这么做的目的是为了解耦，让GameEntry看起来比较简洁。



最后，在每个具体的manager中，前面做的一切成果，我们都不必在意。只需要在必要的时候覆写`OnInit/Tick/Shutdown()`函数。其他的具体功能实现就自己看吧。

## SingletonMono.cs

单例的基础类

```cs
using UnityEngine;

public abstract class SingletonMono<T> : MonoBehaviour where T : MonoBehaviour
{
    public static T Instance { get; private set; }

  	// 初始化 单例
    protected virtual void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this as T;
    }

  	// 销毁单例
    protected virtual void OnDestroy()
    {
        if (Instance == this)
        {
            Instance = null;
        }
    }
}
```

对于每个 Manager 我们会让GameEntry统一管理他们的销毁。

## IManager.cs

所有 Manager 都实现这个接口，方便统一管理。

```cs
public interface IManager
{
    void Init();
    void Tick(float deltaTime);
    void Shutdown();
}
```

Init：初始化
Tick：每帧更新
Shutdown：退出清理

## ManagerBase.cs

在这 我们会继承单例基类 和 IManager接口。用于统一处理当前Manager状态 具体的函数实现将在Onxxx()函数中实现

这里分开的目的是解耦合。

```cs
using UnityEngine;

public abstract class ManagerBase<T> : SingletonMono<T>, IManager where T : MonoBehaviour
{
    public bool IsInitialized { get; private set; }

    public void Init()
    {
        if (IsInitialized) { return; }

        OnInit();
        IsInitialized = true;
    }

    public void Tick(float deltaTime)
    {
        if (!IsInitialized) { return; }

        OnTick(deltaTime);
    }

    public void Shutdown()
    {
        if (!IsInitialized) { return; }

        OnShutdown();
        IsInitialized = false;
    }

  	//具体实现函数
    protected virtual void OnInit() { }
    protected virtual void OnTick(float deltaTime) { }
    protected virtual void OnShutdown() { }
}
```

## ManagerCenter.cs

ManagerCenter 是 Manager of Managers。

它自己不需要继承 MonoBehaviour，只是一个普通 C# 类。用于统一管理各个 Manager 的启动 更新 和关闭。

```cs
using System.Collections.Generic;

public class ManagerCenter
{
    private readonly List<IManager> managers = new List<IManager>();

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
```

# 具体Manager脚本

## GameEntry.cs

GameEntry 是整个框架的入口。

这里通过 Inspector 拖进去引用。

```cs
using System.Collections.Generic;
using UnityEngine;

public class GameEntry : SingletonMono<GameEntry>
{
    [Header("Managers")]
    [SerializeField] private EventManager eventManager;
    [SerializeField] private ResourceManager resourceManager;
    [SerializeField] private AudioManager audioManager;
    [SerializeField] private UIManager uiManager;
    [SerializeField] private SaveManager saveManager;
    [SerializeField] private InputManager inputManager;
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
      
        managerCenter.Register(cutsceneManager);
    		managerCenter.Register(dialogueManager);
    		managerCenter.Register(gameFlowManager);
    }
}
```

## EventManager.cs

事件管理器，用于解耦。

```cs
using System;
using System.Collections.Generic;

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

  	// 添加监听
    public void AddListener(string eventName, Action<object> callback)
    {
        if (!events.ContainsKey(eventName))
        {
            events[eventName] = null;
        }

        events[eventName] += callback;
    }

  	// 移除监听
    public void RemoveListener(string eventName, Action<object> callback)
    {
        if (events.ContainsKey(eventName))
        {
            events[eventName] -= callback;
        }
    }

  	// 发布信息 查找当前events中 是否有eventName字段。如果有，就回调此event对应的Action 至callback中。如果callback存在 就唤醒。
    public void Dispatch(string eventName, object data = null)
    {
        if (events.TryGetValue(eventName, out Action<object> callback))
        {
            callback?.Invoke(data);
        }
    }
}
```

使用：

```cs
EventManager.Instance.Dispatch("PlayerDead"); 
```

监听：

```cs
EventManager.Instance.AddListener("PlayerDead", OnPlayerDead); 

private void OnPlayerDead(object data) 
{    
  	UIManager.Instance.OpenPanel("GameOverPanel"); 
}
```

## AudioManager.cs

音频管理器。

```cs
using UnityEngine;

public class AudioManager : ManagerBase<AudioManager>
{
    [SerializeField] private AudioSource bgmSource;
    [SerializeField] private AudioSource sfxSource;

    protected override void OnInit()
    {
        if (bgmSource == null)
        {
            Debug.LogWarning("AudioManager 缺少 bgmSource");
        }

        if (sfxSource == null)
        {
            Debug.LogWarning("AudioManager 缺少 sfxSource");
        }
    }

    protected override void OnShutdown()
    {
        StopBGM();
    }

  	// BGM 播放
    public void PlayBGM(AudioClip clip)
    {
        if (clip == null || bgmSource == null)
        {
            return;
        }

        bgmSource.clip = clip;
        bgmSource.loop = true;
        bgmSource.Play();
    }

    public void StopBGM()
    {
        if (bgmSource != null)
        {
            bgmSource.Stop();
        }
    }

  	// 音效 播放
    public void PlaySFX(AudioClip clip)
    {
        if (clip == null || sfxSource == null)
        {
            return;
        }

        sfxSource.PlayOneShot(clip);
    }
}
```

播放音乐，我们要在释放音乐的物品脚本中声明`[SerializeField]`变量。用来拖拽clips。随后使用。如下：

```cs
[SerializeField] private buttonPressedSFX;

AudioManager.Instance.PlaySFX(buttonPressedSFX);
```

## UIManager.cs

UI 管理器。

```cs
using System.Collections.Generic;
using UnityEngine;

public class UIManager : ManagerBase<UIManager>
{
    [SerializeField] private Transform uiRoot;

    private readonly Dictionary<string, GameObject> panels =
        new Dictionary<string, GameObject>();

    protected override void OnInit()
    {
        if (uiRoot == null)
        {
            Debug.LogWarning("UIManager 缺少 uiRoot");
        }
    }

    protected override void OnShutdown()
    {
        foreach (GameObject panel in panels.Values)
        {
            Destroy(panel);
        }

        panels.Clear();
    }

  	// 开启 panel
    public void OpenPanel(string panelName)
    {
        if (panels.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(true);
            return;
        }

        GameObject prefab = Resources.Load<GameObject>("UI/" + panelName);

        if (prefab == null)
        {
            Debug.LogError("找不到 UI 面板：" + panelName);
            return;
        }

        GameObject instance = Instantiate(prefab, uiRoot);
        panels[panelName] = instance;
    }

  	// 关闭 panel
    public void ClosePanel(string panelName)
    {
        if (panels.TryGetValue(panelName, out GameObject panel))
        {
            panel.SetActive(false);
        }
    }
}

```

UI Prefab 放在：

```
Assets/Resources/UI/MainMenuPanel.prefab 
Assets/Resources/UI/GameOverPanel.prefab 
```

调用：

```csharp
UIManager.Instance.OpenPanel("MainMenuPanel"); UIManager.Instance.ClosePanel("MainMenuPanel");
```

这里也可以使用外部暴露的私人变量来存储prefab。

## ResourceManager.cs

先给一个简单版资源管理器。

```cs
using UnityEngine;

public class ResourceManager : ManagerBase<ResourceManager>
{
    public T Load<T>(string path) where T : Object
    {
        return Resources.Load<T>(path);
    }

    public GameObject InstantiatePrefab(string path, Transform parent = null)
    {
        GameObject prefab = Load<GameObject>(path);

        if (prefab == null)
        {
            Debug.LogError("找不到资源：" + path);
            return null;
        }

        return Instantiate(prefab, parent);
    }
}
```

使用：

```csharp
GameObject enemy = ResourceManager.Instance.InstantiatePrefab("Enemies/Goblin"); 
```

如果你后面用 Addressables，可以把这里替换成 Addressables 加载逻辑。其他系统不用大改。

## SaveManager.cs

简单存档管理器。

```cs
using UnityEngine;

public class SaveManager : ManagerBase<SaveManager>
{
    public void SaveInt(string key, int value)
    {
        PlayerPrefs.SetInt(key, value);
        PlayerPrefs.Save();
    }

    public int LoadInt(string key, int defaultValue = 0)
    {
        return PlayerPrefs.GetInt(key, defaultValue);
    }

    public void SaveString(string key, string value)
    {
        PlayerPrefs.SetString(key, value);
        PlayerPrefs.Save();
    }

    public string LoadString(string key, string defaultValue = "")
    {
        return PlayerPrefs.GetString(key, defaultValue);
    }

    public void DeleteKey(string key)
    {
        PlayerPrefs.DeleteKey(key);
    }
}
```

使用：

```csharp
SaveManager.Instance.SaveInt("Coin", 100); 
int coin = SaveManager.Instance.LoadInt("Coin");
```

## InputManager.cs

简单输入管理器。这里可以换成新版输入系统。具体在InputSystem文档中有写

```cs
using UnityEngine;

public class InputManager : ManagerBase<InputManager>
{
    public Vector2 MoveInput { get; private set; }
    public bool IsJumpPressed { get; private set; }

    protected override void OnTick(float deltaTime)
    {
        MoveInput = new Vector2(
            Input.GetAxisRaw("Horizontal"),
            Input.GetAxisRaw("Vertical")
        );

        IsJumpPressed = Input.GetKeyDown(KeyCode.Space);
    }
}
```

使用

```cs
Vector2 move = InputManager.Instance.MoveInput;

if (InputManager.Instance.IsJumpPressed)
{
    Debug.Log("跳跃");
}
```

## 游戏状态枚举

```cs
public enum GameState
{
    None,
    OpeningCutscene,
    MainMenu,
    Playing,
    LevelComplete,
    Dialogue,
    Paused,
    GameOver
}
```

GameFlowManager 根据状态切换流程。

## GameFlowManager

控制游戏进程

```
开头动画 -> 主菜单/游戏开始 -> 关卡进行 -> 过关 -> 剧情对话 -> 下一关
```

```cs
using UnityEngine;

public class GameFlowManager : ManagerBase<GameFlowManager>
{
    public GameState CurrentState { get; private set; } = GameState.None;

  	// 进入开场
    protected override void OnInit()
    {
        StartOpening();
    }

  	// 切换状态
    public void ChangeState(GameState newState)
    {
        if (CurrentState == newState)
        {
            return;
        }
				
      	// 退出 切换 进入
        ExitState(CurrentState);
        CurrentState = newState;
        EnterState(CurrentState);
    }

    private void EnterState(GameState state)
    {
        switch (state)
        {
            case GameState.OpeningCutscene:
                EnterOpeningCutscene();
                break;

            case GameState.MainMenu:
                EnterMainMenu();
                break;

            case GameState.Playing:
                EnterPlaying();
                break;

            case GameState.LevelComplete:
                EnterLevelComplete();
                break;

            case GameState.Dialogue:
                EnterDialogue();
                break;

            case GameState.GameOver:
                EnterGameOver();
                break;
        }
    }

    private void ExitState(GameState state)
    {
        switch (state)
        {
            case GameState.MainMenu:
                UIManager.Instance.ClosePanel("MainMenuPanel");
                break;

            case GameState.Playing:
                UIManager.Instance.ClosePanel("HUDPanel");
                break;

            case GameState.Dialogue:
                UIManager.Instance.ClosePanel("DialoguePanel");
                break;
        }
    }

  	// 开场
    private void StartOpening()
    {
        ChangeState(GameState.OpeningCutscene);
    }

  	// 进入状态函数
    private void EnterOpeningCutscene()
    {
        AudioManager.Instance.PlayBGM("Opening");

        CutsceneManager.Instance.PlayCutscene("OpeningCutscene", () =>
        {
            ChangeState(GameState.MainMenu);
        });
    }

    private void EnterMainMenu()
    {
        AudioManager.Instance.PlayBGM("MainMenu");
        UIManager.Instance.OpenPanel("MainMenuPanel");
    }

    private void EnterPlaying()
    {
        AudioManager.Instance.PlayBGM("Battle");
        UIManager.Instance.OpenPanel("HUDPanel");

        EventManager.Instance.Dispatch("GameStart");
    }

    private void EnterLevelComplete()
    {
        AudioManager.Instance.PlaySFX("LevelComplete");
        UIManager.Instance.OpenPanel("LevelCompletePanel");

        DialogueManager.Instance.StartDialogue("AfterLevel1", () =>
        {
            ChangeState(GameState.Dialogue);
        });
    }

    private void EnterDialogue()
    {
        UIManager.Instance.OpenPanel("DialoguePanel");

        DialogueManager.Instance.StartDialogue("AfterLevel1", () =>
        {
            ChangeState(GameState.Playing);
        });
    }

    private void EnterGameOver()
    {
        AudioManager.Instance.PlayBGM("GameOver");
        UIManager.Instance.OpenPanel("GameOverPanel");
    }

  	// 游戏进程相关函数
    public void StartGame()
    {
        ChangeState(GameState.Playing);
    }

    public void CompleteLevel()
    {
        ChangeState(GameState.LevelComplete);
    }

    public void GameOver()
    {
        ChangeState(GameState.GameOver);
    }
}
```

具体的流程是。我们在其他的节点中会触发GameFlowManager中的相关函数。如StartGame()，也就是我们要在这个Manager中 分别设置好开始游戏后 触发的各种效果。避免在触发进程继续的脚本中实现。

## CutsceneManager 分镜管理

播放动画（使用Timeline）

```cs
using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Playables;

[System.Serializable]
public class CutsceneItem
{
    public string name;
    public PlayableDirector director;
}

public class CutsceneManager : ManagerBase<CutsceneManager>
{
    [SerializeField] private List<CutsceneItem> cutscenes = new List<CutsceneItem>();

    private Dictionary<string, PlayableDirector> cutsceneDict;
    private Action onComplete;

  	// 将初始拖入的名字和director 变成字典，方便后续查找
    protected override void OnInit()
    {
        cutsceneDict = new Dictionary<string, PlayableDirector>();

        foreach (CutsceneItem item in cutscenes)
        {
            if (item == null || string.IsNullOrEmpty(item.name) || item.director == null)
            {
                continue;
            }

            cutsceneDict[item.name] = item.director;
        }
    }

  	// 放动画
    public void PlayCutscene(string cutsceneName, Action completeCallback)
    {
        if (!cutsceneDict.TryGetValue(cutsceneName, out PlayableDirector director))
        {
            Debug.LogWarning("找不到过场动画：" + cutsceneName);
            completeCallback?.Invoke();
            return;
        }

        onComplete = completeCallback;

        director.stopped -= OnDirectorStopped;
        director.stopped += OnDirectorStopped;

        director.Play();
    }

  	// 动画播放完成后的操作
    private void OnDirectorStopped(PlayableDirector director)
    {
        director.stopped -= OnDirectorStopped;

        Action callback = onComplete;
        onComplete = null;

        callback?.Invoke();
    }
}
```

调用

```cs
CutsceneManager.Instance.PlayCutscene("OpeningCutscene", () =>
{
    GameFlowManager.Instance.ChangeState(GameState.MainMenu);
});
```

## DialogueManager

### 定义对话数据

一条对话

```cs
[System.Serializable] 
public class DialogueLine
{   
  	public string speaker;
 		[TextArea]    
 		public string content; 
} 
```

一组对话：

```cs
[System.Serializable]
public class DialogueSequence
{
    public string name;
    public DialogueLine[] lines;
}
```

### DialogueManager

然后写 DialogueManager 写对话逻辑，如切换下一条对话的内容：

```cs
using System;
using System.Collections.Generic;
using UnityEngine;

public class DialogueManager : ManagerBase<DialogueManager>
{
    [SerializeField] private List<DialogueSequence> dialogues =
        new List<DialogueSequence>();

    private Dictionary<string, DialogueSequence> dialogueDict;
    private DialogueSequence currentDialogue;
    private int currentIndex;
    private Action onComplete;

  	// 把自定义的对话数据转化为 字典
    protected override void OnInit()
    {
        dialogueDict = new Dictionary<string, DialogueSequence>();

        foreach (DialogueSequence dialogue in dialogues)
        {
            if (dialogue == null || string.IsNullOrEmpty(dialogue.name))
            {
                continue;
            }

            dialogueDict[dialogue.name] = dialogue;
        }
    }

  	// 开启一段对话
    public void StartDialogue(string dialogueName, Action completeCallback)
    {
        if (!dialogueDict.TryGetValue(dialogueName, out DialogueSequence dialogue))
        {
            Debug.LogWarning("找不到对话：" + dialogueName);
            completeCallback?.Invoke();
            return;
        }

        currentDialogue = dialogue;
        currentIndex = 0;
        onComplete = completeCallback;

        UIManager.Instance.OpenPanel("DialoguePanel");
        ShowCurrentLine();
    }

    public void NextLine()
    {
        if (currentDialogue == null)
        {
            return;
        }

        currentIndex++;

        if (currentIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }

        ShowCurrentLine();
    }

    private void ShowCurrentLine()
    {
        DialogueLine line = currentDialogue.lines[currentIndex];

        EventManager.Instance.Dispatch("DialogueLineChanged", line);
    }

    private void EndDialogue()
    {
        currentDialogue = null;
        UIManager.Instance.ClosePanel("DialoguePanel");

        Action callback = onComplete;
        onComplete = null;

        callback?.Invoke();
    }
}
```

### DialoguePanel

UI 展示

```cs
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialoguePanel : MonoBehaviour
{
    [SerializeField] private TMP_Text speakerText;
    [SerializeField] private TMP_Text contentText;
    [SerializeField] private Button nextButton;

    private void OnEnable()
    {
        EventManager.Instance.AddListener("DialogueLineChanged", OnDialogueLineChanged);
        nextButton.onClick.AddListener(OnClickNext);
    }

    private void OnDisable()
    {
        if (EventManager.Instance != null)
        {
            EventManager.Instance.RemoveListener("DialogueLineChanged", OnDialogueLineChanged);
        }

        nextButton.onClick.RemoveListener(OnClickNext);
    }

    private void OnDialogueLineChanged(object data)
    {
        DialogueLine line = data as DialogueLine;

        if (line == null)
        {
            return;
        }

        speakerText.text = line.speaker;
        contentText.text = line.content;
    }

    private void OnClickNext()
    {
        DialogueManager.Instance.NextLine();
    }
}
```

# 后续拓展

写好Manager脚本后（如AnimationManager、CinemaManager），在GameEntry脚本中 订阅他们，随后在GameFlowManager中在对应的游戏进程中写好这些Manager发挥作用的逻辑。
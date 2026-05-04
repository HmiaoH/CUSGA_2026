# 基础卡牌与卡面动效实现说明

## 1. 本次实现包含什么

这次实现的目标不是直接把完整战斗系统一次性做完，而是先把后续系统最依赖的“卡牌基础层”搭起来。当前已经补上的内容如下：

- `CardDefinition`：扩展后的卡牌静态数据结构，能表达技能牌、伤害牌、移动牌的基础属性
- `CardView`：卡牌 UI 展示组件，负责把卡牌数据渲染到 UGUI 卡面上
- `CardDragPlayHandler`：卡牌拖拽出牌控制器，处理拖拽、上半屏判定、出牌与落牌流程
- `CardHoverMotion`：基础卡面动效执行器，负责悬停放大、抬升、倾斜与点击脉冲
- `CardHandLayout`：手牌扇形布局，让多张卡牌排布更像实体手牌
- `CardHandDemo`：运行时演示脚本，用于快速把几张卡生成到手牌区测试表现
- `WorldCardView`：场景桌面牌展示组件，负责显示已打出卡牌数据
- `BasicCardLibraryGenerator`：Unity 编辑器菜单工具，用来一键生成几张基础卡牌资产

## 2. 脚本职责细则

### 2.1 `Assets/Scripts/CarfDefinition.cs`

这个文件仍保留原路径，是为了不打断你当前工程里已有的 ScriptableObject 引用关系。虽然文件名还是旧名字，但内容已经升级成完整的 `CardDefinition`。

它目前包含：

- `CardCategory`
  - `Skill`
  - `Damage`
  - `Move`
- `CardRarity`
  - `Starter`
  - `Common`
  - `Rare`
- `CardPrimaryEffect`
  - 只是视觉和逻辑上的“主用途”分类，方便后续筛选与卡面标识
- `CardKeyword`
  - 用位掩码方式保存关键词
  - 当前支持：冲锋、活力、回溯、击退、换位、直线移动、任意移动、束缚、致盲
- `CardTargetingMode`
  - 预留给后续出牌选点、选敌人、选自身使用
- `MovementPattern`
  - 明确这张牌的移动方式是直线、任意、回溯还是换位

`CardDefinition` 本体主要字段：

- 基础信息
  - `cardId`
  - `displayName`
  - `rulesText`
  - `flavorText`
  - `category`
  - `rarity`
  - `primaryEffect`
  - `targetingMode`
  - `keywords`

- 数值
  - `cost`
  - `damage`
  - `hitCount`
  - `cardDraw`
  - `vitalityGain`
  - `energyGain`
  - `knockbackDistance`
  - `movementAmount`
  - `movementPattern`
- 表现
  - `artwork`
  - `accentColor`

这样做的原因是：

- 先保证技能牌、伤害牌、移动牌可以共用一套统一容器
- 后续你做卡牌逻辑时，可以逐步从“展示字段”迁移到“战斗结算字段”
- 现阶段不急着拆成复杂的效果节点系统，先让原型能跑、能看、能扩展

### 2.2 `Assets/Scripts/Cards/CardView.cs`

这是卡牌 UI 组件，应该挂在“卡牌预制体根节点”上。

它负责：

- 读取 `CardDefinition`
- 更新卡面文字
- 更新费用、名称、说明、关键词、编号
- 根据卡牌类别切换边框主色
- 控制卡牌是否可交互

建议它只做显示，不做战斗逻辑。这样后面如果你要换成更复杂的卡牌系统，也不需要重写视觉层。

### 2.3 `Assets/Scripts/Cards/CardDragPlayHandler.cs`

这是当前版本的卡牌交互主脚本，负责：

- `OnMouseDown / OnMouseDrag / OnMouseUp` 输入链路
- 与 `CardHoverMotion` 协同（拖拽时加锁动效，防止位置抖动）
- 与 `CardHandLayout` 协同（暴露 `IsDragging / IsConsumed`）
- 出牌判定（上半屏阈值）
- 打出飞行动效
- 生成桌面牌并绑定 `CardDefinition`

### 2.4 `Assets/Scripts/Cards/CardHoverMotion.cs`

这是这次“仿照邪恶冥刻的基础动效”的核心脚本。

它做了几类反馈：

- 鼠标悬停抬升
  - 卡牌向上浮起
- 鼠标悬停放大
  - 卡面略微变大，强调当前聚焦
- 鼠标移动倾斜
  - 鼠标在卡面左右/上下移动时，卡牌会做 3D 倾斜
- 点击脉冲
  - 按下时有一次缩放脉冲，适合以后接“出牌确认”
- 发光图层透明度变化
  - 让悬停时卡牌更有被“点亮”的感觉

当前实现没有依赖 DOTween 或第三方插件，完全靠 Unity 自带插值完成，所以比较稳，适合原型期。

当前版本中，`CardHoverMotion` 直接监听鼠标事件：

- `OnMouseOver`：进入悬停并更新倾斜
- `OnMouseExit`：退出悬停
- `OnMouseDrag`：拖拽中持续更新倾斜
- `OnMouseDown`：触发点击脉冲

同时也保留 `SetDragLock` 给 `CardDragPlayHandler` 调用，确保拖拽时不会和动效插值互相抢写。

### 2.5 `Assets/Scripts/Cards/CardHandLayout.cs`

这个脚本用来把多张卡排成扇形，而不是死板地横着摆。

主要控制：

- 卡牌理想横向间距
- 卡牌最小横向间距
- 根据容器宽度自动压缩手牌排布
- 扇形下坠高度
- 每张卡最大旋转角
- 位置与旋转的平滑速度
- 悬停时将相邻卡牌向两侧挤开

这样和 `CardDragPlayHandler + CardHoverMotion` 叠加后，整体手感会比普通卡牌列表更接近实体卡组。当前版本还会根据手牌容器宽度自动压缩间距，避免卡多时两侧卡牌直接跑出屏幕，并在悬停时主动把周围卡牌挤开，给当前卡更明显的展示空间。

### 2.6 `Assets/Scripts/Cards/CardHandDemo.cs`

这个脚本是测试脚本，不是最终战斗脚本。

它的作用：

- 读取你指定的 `CardDefinition` 列表
- 运行时实例化卡牌 prefab
- 自动绑定到手牌容器里

用它可以先只验证：

- 卡面能否正常显示
- 扇形布局是否合适
- hover / tilt / pulse 动效是否舒服
- 拖拽后是否触发“出牌或回位”

当前版本会在实例化卡牌时自动补上 `CardDragPlayHandler`（如果 prefab 上还没挂）。

### 2.7 `Assets/Scripts/Cards/WorldCardView.cs`

这是场景桌面牌的展示脚本，支持从 `CardDefinition` 读取并同步：

- 卡牌名称
- 费用
- 描述
- 编号
- 插画（可选）
- 强调色（可选，写入材质 `_BaseColor`）

### 2.8 `Assets/Scripts/Editor/BasicCardLibraryGenerator.cs`

这是给 Unity 编辑器使用的菜单脚本，不会进运行时包。

菜单路径：

- `Tools/Cards/Generate Basic Prototype Cards`

点击后会自动在：

- `Assets/_Assets/CardDefinitions/Generated`

生成或更新几张基础卡牌：

- `0.1.0 快速检索`
- `1.1.0 钝击`
- `1.2.1 双斩`
- `2.1.0 踏步前移`
- `2.1.1 穿刺冲锋`
- `2.1.2 游移蓄势`

这几张牌覆盖了三类基础牌：

- 技能牌
- 伤害牌
- 移动牌

也覆盖了几个重要设计点：

- 抽牌
- 单段伤害
- 多段伤害
- 直线移动
- 冲锋
- 任意移动 + 活力

## 3. 需要在 Unity 里手动做的事

### 3.1 手动生成基础卡牌资产

进入 Unity 后，等待脚本编译完成，然后点击：

- `Tools`
- `Cards`
- `Generate Basic Prototype Cards`

执行后会自动生成示例卡牌资产。

> 后续需要拓展的话就手动新建并管理`CardDefinition`

### 3.2 创建卡牌预制体

你需要新建一个 UGUI 预制体，建议结构如下：

1. 创建一个 `Canvas`
2. 在 Canvas 下创建一个空物体，命名为 `HandRoot`
3. 给 `HandRoot` 挂：
   - `RectTransform`
   - `CardHandLayout`
4. 在 Canvas 下新建一个卡牌对象，命名为 `CardPrefab`
5. `CardPrefab` 根节点建议挂：
   - `RectTransform`
   - `CanvasGroup`
   - `Image`
   - `CardDragPlayHandler`
   - `CardView`
   - `CardHoverMotion`
6. 在 `CardPrefab` 根节点下创建一个子空物体，命名为 `MotionRoot`
7. 把所有真正可见的卡面元素都放到 `MotionRoot` 下。这一步不需要填充具体的内容，仅仅是一个占位符。
   在这里你需要设置所有元素的大小、位置、颜色等。
8. 在 `CardHoverMotion` 组件里把 `Target Rect` 指向 `MotionRoot`

这样分层的原因是：

- `CardHandLayout` 负责根节点的扇形排布
- `CardDragPlayHandler` 负责拖拽、出牌与落牌
- `CardHoverMotion` 负责视觉层的抬升、倾斜、缩放
- 两层拆开之后，布局和 hover 动效不会互相覆盖

如果你忘了手动拖 `Target Rect`，当前脚本也会优先自动寻找名为 `MotionRoot` 的子节点。

### 3.3 建议的卡牌 prefab 子物体层级

建议你按下面这个结构建：(作为占位符设定好位置)

`CardPrefab`

- `MotionRoot`
- `Background`：Image
- `Border`：Image
- `Glow`：Image
- `Artwork`：Image
- `CostText`：TextMeshProUGUI
- `TitleText`：TextMeshProUGUI
- `TypeText`：TextMeshProUGUI
- `DescriptionText`：TextMeshProUGUI
- `KeywordText`：TextMeshProUGUI
- `IdText`：TextMeshProUGUI

然后把这些引用拖到 `CardView` 对应字段里。

> 注意：tmp默认字体不支持中文，右键一个中文字体并新建字体资源。然后替换字体

### 3.4 手动创建演示器

在 `HandRoot` 或任意空物体上挂 `CardHandDemo`，并配置：

- `Card Prefab`：拖入你做好的 `CardPrefab`
- `Hand Root`：拖入 `HandRoot`
- `Demo Cards`：拖入刚才生成的几张卡牌资产

运行后就能看到基础手牌效果。

### 3.5 拖拽出牌交互说明

- 按住并拖拽卡牌
- 松手点在屏幕上半部分 -> 卡牌判定为“使用”，会输出调试日志并消失
- 松手点在屏幕下半部分 -> 卡牌取消使用，自动回到手牌布局中的位置

调试日志会打印：

- `CardId`
- `DisplayName`
- `Category`
- `Cost`
- `Damage`
- `HitCount`
- `MovementAmount`
- `CardDraw`
- `VitalityGain`
- `EnergyGain`
- `Keywords`

### 3.6 如果悬停不抬升，优先检查这几项

- 卡牌对象上是否有 `Collider`（当前脚本会自动补 `BoxCollider`）
- 场景摄像机是否能“看见”卡牌对象所在层（`OnMouse` 依赖摄像机射线）
- 如果是 `Screen Space - Overlay` 的纯 UGUI，`OnMouse` 可能不稳定，建议改 `Screen Space - Camera` 或 `World Space`
- `CardHoverMotion` 的 `Target Rect` 是否正确指向 `MotionRoot`

当前脚本已经会自动给卡牌根节点补一个 `Canvas`，用于悬停时临时提高排序，避免放大的卡牌被相邻卡面遮住。

## 4. 当前实现风格说明

“仿照邪恶冥刻”这次实现的是基础手感层，而不是完全复刻美术风格。

目前靠的是：

- 悬停时抬升
- 卡牌围绕鼠标倾斜
- 发光图层淡入
- 手牌扇形排布
- 轻量平滑插值（无正弦漂浮待机）

这会让卡牌已经有明显的“实体感”和“桌面感”。后续如果你要更接近《邪恶冥刻》，下一步建议继续做：

- 卡牌阴影偏移
- 撕裂纸张边缘或旧纸材质
- 卡牌入手时从屏幕下方弹入
- 打出时向目标方向飞出并压扁
- 受限费用时卡牌边缘闪红
- 被选中时手牌其余卡轻微压暗

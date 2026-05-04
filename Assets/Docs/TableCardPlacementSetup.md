# 桌面落牌配置说明

## 1. 功能说明

当手牌拖拽到屏幕上半区域并松手时，系统会：

1. 直接判定出牌成功
2. 在固定桌面位置（或锚点）实例化 `worldCardPrefab`
3. 将当前 `CardDefinition` 绑定到桌面牌
4. 打印调试字段并从手牌隐藏该卡

核心脚本：

- `Assets/Scripts/Cards/CardDragPlayHandler.cs`
- `Assets/Scripts/Cards/WorldCardView.cs`

## 2. 你需要手动做的设置

### 2.1 配置桌面牌 prefab

1. 新建一个世界空间牌对象作为 `worldCardPrefab`
2. 在该 prefab 上挂 `WorldCardView`
3. 把文本/图片/材质引用拖到 `WorldCardView` 对应字段

建议字段：

- `Card Name Text`
- `Card Cost Text`
- `Card Desc Text`
- `Card Id Text`
- `Artwork Image`（可选）
- `Tint Renderer`（可选）

### 2.2 配置拖拽出牌组件

在手牌对象的 `CardDragPlayHandler` 上设置：

- `World Card Prefab`：拖入 2.2 的 prefab
- `Fixed Table Anchor`：推荐绑定桌面上的一个空物体（落牌点）
- `Fixed World Position`：如果不绑定锚点，就用这个世界坐标
- `Use Anchor Rotation`：是否跟随锚点旋转
- `World Card Euler`：按你的牌面朝向设置（例如平放可用 `X=90`）
- `World Card Scale`：桌面牌实例化后的统一缩放倍数。当前建议值：`0.003`
- `World Card Parent`：可选，建议挂到 `PlayedCardsRoot`
- `Play Use Travel Animation`：勾选后松手会先播放飞行动效，再落牌
- `Use Travel Duration`：飞行时长（建议 `0.16~0.3`）
- `Use Travel Arc Height`：飞行弧线高度（建议 `0.1~0.45`）
- `Use Travel Ease`：飞行缓动曲线

## 3. 关键字段调试

出牌成功时会输出：

- `Id`
- `Name`
- `Category`
- `Cost`
- `Damage`
- `HitCount`
- `Move`
- `Draw`
- `VitalityGain`
- `EnergyGain`
- `Keywords`
- `WorldPos`

## 4. 常见问题排查

### 4.0 落到桌面后牌太大/太小

优先调整 `CardDragPlayHandler` 上的 `World Card Scale`。

- 你当前需求建议固定为 `0.003`
- 如果你更换了 `worldCardPrefab` 的原始模型尺寸，通常只需要改这个字段，不用改其他落牌逻辑

### 4.1 拖到上半屏却没有落牌

检查：

- `worldCardPrefab` 是否为空
- `Fixed Table Anchor` 是否为空（若为空会回退到 `Fixed World Position`）
- `Fixed World Position` 是否在当前场景可见位置

### 4.2 落牌角度不对

调 `World Card Euler`，直到牌面方向符合你的场景坐标系。

### 4.2 打出后没有飞行动效

检查：

- `Play Use Travel Animation` 是否勾选
- `Use Travel Duration` 是否大于 `0.01`
- 手里这张牌是否在松手后立即被其他逻辑隐藏

### 4.3 落牌位置和鼠标偏差较大

当前版本是“固定落牌点”，不跟随鼠标落点。如果希望改成鼠标命中桌面落点，需要切回射线落牌逻辑。

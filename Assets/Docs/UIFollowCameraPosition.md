# UI 跟随相机位置（不旋转）实现说明

## 1. 功能目标

这个功能用于让某个 UI 物体跟随指定相机的位置移动，并且：

- 不跟随相机旋转
- 带延迟/拖尾感（平滑跟随）

已实现脚本：

- `Assets/Scripts/UI/FollowCameraPositionUI.cs`

## 2. 核心机制

脚本每帧计算目标点：

- `目标点 = 跟随相机位置 + 偏移量 worldOffset`

然后用 `Vector3.SmoothDamp` 让 UI 物体慢慢跟上，形成延迟运动动效。

为了满足“无旋转”，脚本会在每帧把物体旋转强制保持为初始旋转（可开关）。

## 3. 参数说明

### 3.1 跟随目标

- `followCamera`
  - 要跟随的相机
  - 为空时自动尝试 `Camera.main`
- `worldOffset`
  - 跟随偏移（世界空间）
  - 例如 `(0, 2, 0)` 表示始终在相机上方 2 米的位置

### 3.2 跟随参数

- `smoothTime`
  - 越大越“慢”，拖尾更明显
  - 推荐范围：`0.08 ~ 0.35`
- `maxSpeed`
  - 限制最大追赶速度，防止极端情况下跳变
- `updateTiming`
  - `LateUpdate` 更适合跟随相机，通常更稳定

### 3.3 空间设置

- `followInLocalSpace`
  - 勾选后会写 `localPosition`
  - 常用于 UI 是某个父节点的子物体时
- `lockInitialRotation`
  - 勾选后保持初始旋转不变
  - 用于保证 UI 不随相机转向旋转

## 4. 手动挂载步骤

1. 选中你要跟随相机移动的 UI 物体
2. 添加组件 `FollowCameraPositionUI`
3. 将需要跟随的相机拖到 `Follow Camera`
4. 设置 `World Offset`
5. 先用 `Smooth Time = 0.18` 试手感，再微调
6. `Update Timing` 建议先选 `LateUpdate`

## 5. Canvas 模式注意事项

### 5.1 World Space Canvas

这是最直接的模式，脚本效果最符合预期（世界位置跟随）。

### 5.2 Screen Space - Camera

可用。建议：

- 该 Canvas 绑定同一台渲染相机
- 如果出现偏移不合预期，可尝试 `followInLocalSpace`

### 5.3 Screen Space - Overlay

这个模式没有稳定世界空间语义，不建议做“世界位置跟随”。
如果你必须用 Overlay，通常应改为“跟随屏幕锚点/视口坐标”的另一套逻辑，而不是直接跟随相机世界坐标。

## 6. 调参建议

如果想要“有延迟但不拖泥带水”，可以从这组参数起步：

- `smoothTime = 0.16`
- `maxSpeed = 40`
- `worldOffset = (0, 1.5, 0)`
- `updateTiming = LateUpdate`

如果想要更明显拖尾感：

- 增大 `smoothTime` 到 `0.25 ~ 0.35`
- 适当减小 `maxSpeed`

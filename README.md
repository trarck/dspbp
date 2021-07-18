# 戴森球计划蓝图Mod
蓝图mod。目前只完成了核心的复制和建造功能。
生成蓝图的时候最好建造在赤道附近。
如果蓝图跨越纬线，最好建在同样维度。

## 使用方法
### 基本用法
- 在建造模式时，使用`Ctrl+Z`进入蓝图模式。
- 按快捷键`J`进入编辑蓝图模式。
  - `F1` 单选模式。默认模式。
  - `F2` 多选模式。`+`和`-`分别扩大、减小选择范围。
  - 选择好后，点击鼠标左键创，建蓝图，默认不会保存。
  - 点击鼠标右键，退出创建模式。
  - `Ctrl+S`保存当前创建的蓝图。建议先退出创建模式，防止误操作覆盖生成的蓝图数据。
    也可以在右侧的UI上点击`Save`按钮。
    蓝图保存在`BepInEx\config\DspTrarck`目录。
- 按快捷键`I`进入蓝图建造模式。
  - 快捷键`R`可以对蓝图进行旋转。
  
### UI
- 可以查看之前保存的蓝图。
- 点击蓝图文件名后的`L`可以加载蓝图进行建造。
- 过虑功能：
  - 不复制传送带。
  - 不复制输电塔(可能会有碰撞问题)

## 版本列表

### v1.0.2
- 修改蓝图的名称保存时和当前分开。
- 修正第二次蓝图创建只有传送带的问题。
- UI加入可切换建造距离(LD),默认开始。

### v1.0.1
- 优化蓝图包含的建筑多时卡顿问题。
- 加入显示建造蓝图需要的物品信息。

### v1.0.0
- 建筑的复制和建造功能。
- 蓝图的查看和加载。
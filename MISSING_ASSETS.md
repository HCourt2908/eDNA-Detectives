# Missing Assets — Rosette Deployment / Seamount Map

本清单只对应当前这一页：进入 Rosette Deployment 后显示的海底山脉地图。已找到并接入项目的素材不重复列为缺失项；下列素材需要后续提供正式版本。正式素材替换时请保持当前占位区域的尺寸、比例和透明背景要求不变。

| 编号 | 所属页面/场景 | 素材名称 | 素材类型 | 用途 | 建议尺寸或比例 | 建议格式 | 视觉要求 | 当前占位方式 | 建议保存路径 |
|---:|---|---|---|---|---|---|---|---|---|
| 1 | 海底山脉地图 | 地图信息面板手绘白色边框 | UI 图标 | 包裹右上角 Location ID 信息区，固定在设计图位置 | 635×632 px，约 1:1 | PNG / SVG | 白色不规则手绘边框，内侧灰色面板；透明背景；四角和阴影形状需保持稳定 | 程序生成白色外框、灰色内框和阴影，并标注 `MISSING: DIALOG BORDER` | `game/Assets/Art/Rosette-Deployment/UI/` |
| 2 | 海底山脉地图 | 地图提示面板手绘白色边框 | UI 图标 | 包裹左上角 “more information” 提示区，固定在设计图位置 | 765×350 px，约 2.2:1 | PNG / SVG | 与右侧信息面板同一套边框语言；透明背景；不覆盖文字安全区 | 程序生成白色外框、灰色内框和阴影 | `game/Assets/Art/Rosette-Deployment/UI/` |
| 3 | 海底山脉地图 | Location ID 数据字体与手写标签字体 | 字体 | 用于提示语、Location ID、C/T/D、habities 和 Go! | 拉丁字符完整；建议至少覆盖 12–52 px 字号 | OTF / TTF | 保留设计图中的手写记录感，同时保证缩放后可读 | Unity Text Mesh Pro 默认字体 | `game/Assets/Fonts/` |
| 4 | 海底山脉地图 | 地点信息数据字段纹理 | UI 图标 | 点击问号后，填充 C/T/D/habities 信息栏的正式字段样式（如果最终由图片提供） | 205×52、215×48、435×105 px 三种比例 | PNG / SVG | 与灰色信息面板和黑色手绘线条一致；透明背景 | 程序生成空字段、斜线字段和待接入数据区域 | `game/Assets/Art/Rosette-Deployment/UI/` |

## 已接入的本地素材

| 页面元素 | 项目内路径 | 原始尺寸 | 当前用途 |
|---|---|---:|---|
| 深海背景层 | `game/Assets/Resources/RosetteDeployment/Backgrounds/undersea_background.jpeg` | 2478×1696 | 当前终端 UI 的深海底层背景 |
| 精细海山地图 | `game/Assets/Resources/RosetteDeployment/Map/seamount_terrain.png` | 1536×1024 | 当前地图页使用的海山素材 |
| 旧海山图（不使用） | `game/Assets/Art/Rosette-Deployment/Map/seamounts-hires.jpg` | 1280×599 | 仅保留作旧素材，不再被页面引用 |
| 问号按钮 | `game/Assets/Resources/RosetteDeployment/UI/question.png` | 100×100 | 六个可点击地点标记 |
| GO 按钮底图 | `game/Assets/Resources/RosetteDeployment/UI/buttonStart.png` | 100×100 | 右下固定 GO! 按钮底图 |
